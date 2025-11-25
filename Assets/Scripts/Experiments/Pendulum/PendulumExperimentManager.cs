using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PendulumExperimentManager : ExperimentManagerBase
{
    #region Enums and Configuration
    public enum SimulationMode
    {
        Realistic, // Engine-driven
        Ideal      // Script-driven
    }

    [Header("Physics (Realistic Mode)")]
    [Tooltip("Hệ số tắt dần. Giá trị càng cao, con lắc dừng lại càng nhanh.")]
    [Range(0f, 5f)]
    [SerializeField] private float dampingFactor = 0.1f;

    [Header("Experiment Mode")]
    [SerializeField] private SimulationMode mode = SimulationMode.Realistic;

    [Header("Experiment Components")]
    [SerializeField] private Grabbable pendulumBob;
    [SerializeField] private SnapZone pivotPoint;
    [SerializeField] private Transform resetPoint;

    [Header("Length Control")]
    [Tooltip("Slider điều chỉnh chiều dài dây.")]
    [SerializeField] private Slider lengthSlider;
    [Tooltip("Text hiển thị giá trị chiều dài.")]
    [SerializeField] private TextMeshProUGUI lengthValueText;
    [SerializeField] private float minLength = 0.3f;
    [SerializeField] private float maxLength = 1.5f;

    [Header("Period Measurement")]
    [Tooltip("Số chu kỳ cần đo để lấy trung bình trong chế độ Realistic.")]
    [SerializeField] private int cyclesToAverage = 3;

    [Tooltip("Chu kỳ dao động tính được. (Lý thuyết cho Ideal, Đo đạc cho Realistic)")]
    [SerializeField, ReadOnly] private float calculatedPeriod;

    [Tooltip("Bỏ qua các phép đo chu kỳ ngắn hơn giá trị này (tính bằng giây) để loại bỏ lỗi do jitter.")]
    [SerializeField] private float minimumValidPeriod = 0.25f;

    [Header("Setup Configuration")]
    [Tooltip("Góc tối đa (độ) mà người dùng có thể kéo con lắc trong giai đoạn setup. Phù hợp với dao động điều hòa nhỏ (< 15°).")]
    [Range(5f, 30f)]
    [SerializeField] private float maxSetupAngleDegrees = 15f;

    #endregion

    #region Private State
    // --- References ---
    private Rigidbody _bobRootRigidbody;
    private Transform _bobModelTransform;
    private IdealPendulumSimulator _idealSimulator;
    private PendulumVisualizer _visualizer;
    private bool _isAssembled = false;

    // --- Measurement State (For Realistic Mode) ---
    private List<float> _measuredPeriods = new List<float>();
    private float _swingStartTime;
    private bool _isTiming = false;
    private float _lastBobVelocityY = 0f;

    // --- Setup State ---
    private bool _isInSetupPhase = false;
    
    // --- UI Throttle ---
    private float _uiUpdateTimer;
    #endregion

    #region Public Control
    public void SetSimulationMode(SimulationMode newMode)
    {
        if (mode == newMode || CurrentState == ExperimentState.Running)
        {
            if (CurrentState == ExperimentState.Running)
                Debug.LogWarning("Không thể thay đổi chế độ khi thí nghiệm đang chạy.");
            return;
        }
        mode = newMode;
        Debug.Log($"Chuyển sang chế độ: {newMode}");
    }

    public void StartPendulumExperiment()
    {
        if (CurrentState == ExperimentState.PreExperiment && _isAssembled)
        {
            BeginExperiment();
        }
        else
        {
            Debug.LogWarning("Không thể bắt đầu thí nghiệm. Hãy chắc chắn rằng con lắc đã được lắp ráp.");
        }
    }

    public void OnLengthSliderChanged(float newLength)
    {
        if (!_isAssembled) return;

        if (lengthValueText != null) lengthValueText.text = $"L = {newLength:F2} m";

        Vector3 direction = (_bobModelTransform.position - pivotPoint.transform.position).normalized;

        if (direction.sqrMagnitude < 0.001f) direction = Vector3.down;
        Vector3 newBobPos = pivotPoint.transform.position + (direction * newLength);
        pendulumBob.transform.position = newBobPos;
        _bobRootRigidbody.position = newBobPos;

        if (pendulumBob.CurrentState == GrabbableState.Snapped || pendulumBob.CurrentState == GrabbableState.Anchored)
        {
            pendulumBob.UpdateJointAnchor(pivotPoint.transform.position);
        }

        if (mode == SimulationMode.Ideal && _idealSimulator != null)
        {
            _idealSimulator.SetLength(newLength);
        }

        bool isIdealSetupPhase = (mode == SimulationMode.Ideal && CurrentState == ExperimentState.PreExperiment);
        Vector3 worldAxis = pivotPoint.transform.TransformDirection(pivotPoint.OscillationAxis);
        var newStrategy = new PendulumHoldStrategy(pivotPoint.transform, newLength, maxSetupAngleDegrees, isIdealSetupPhase, worldAxis);
        pendulumBob.SetHoldStrategy(newStrategy);

        if (mode == SimulationMode.Ideal && CurrentState == ExperimentState.Running)
        {
            CalculatePeriodIdeal();
        }
        
        if (DashboardManager.Instance != null)
            DashboardManager.Instance.UpdateStat("Length", "Chiều dài (l)", $"{newLength:F2} m");
    }
    #endregion

    #region Experiment Lifecycle
    protected override void InitializeExperiment()
    {
        _bobRootRigidbody = pendulumBob.GetComponent<Rigidbody>();
        _bobModelTransform = pendulumBob.GetComponentInChildren<Renderer>().transform;

        _idealSimulator = pendulumBob.GetComponent<IdealPendulumSimulator>();
        if (_idealSimulator == null) _idealSimulator = pendulumBob.gameObject.AddComponent<IdealPendulumSimulator>();
        _idealSimulator.enabled = false;

        _visualizer = pivotPoint.GetComponent<PendulumVisualizer>();

        if (lengthSlider != null)
        {
            lengthSlider.minValue = minLength;
            lengthSlider.maxValue = maxLength;
            lengthSlider.onValueChanged.AddListener(OnLengthSliderChanged);
        }
        
        InitializeDashboard();
    }

    protected override void StartExperimentLogic()
    {
        Debug.Log($"Bắt đầu logic thí nghiệm con lắc ở chế độ: {mode}.");

        if (_isInSetupPhase && mode == SimulationMode.Ideal)
        {
            _isInSetupPhase = false;

            Vector3 currentVector = _bobModelTransform.position - pivotPoint.transform.position;
            float setupAngle = Vector3.SignedAngle(Vector3.down, currentVector, Vector3.forward);

            Debug.Log($"[Ideal Mode] Kết thúc Setup Phase. Bắt đầu mô phỏng từ góc: {setupAngle:F2}°");

            float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
            var runningStrategy = new PendulumHoldStrategy(pivotPoint.transform, length, maxSetupAngleDegrees, false);
            pendulumBob.SetHoldStrategy(runningStrategy);
        }

        ResetMeasurement();

        if (mode == SimulationMode.Ideal)
        {
            CalculatePeriodIdeal();
        }
        else
        {
            _bobRootRigidbody.linearDamping = dampingFactor;
            _bobRootRigidbody.angularDamping = dampingFactor;
            Debug.Log($"[Realistic Mode] Đã áp dụng Damping Factor: {dampingFactor}");
        }

        ApplySimulationMode();
    }

    protected override void EndExperimentLogic()
    {
        if (mode == SimulationMode.Ideal)
        {
            _idealSimulator.StopSimulation();
            Debug.Log("[Ideal Mode] Dừng IdealPendulumSimulator.");
        }

        if (pendulumBob.CurrentState == GrabbableState.Snapped)
        {
            pendulumBob.ConfigureSnappedPhysics(false);
        }
    }

    protected override void ResetExperimentLogic()
    {
        var bobConnector = pendulumBob.GetComponentInChildren<Connector>();
        if (bobConnector != null && pivotPoint.ConnectedConnectors.Contains(bobConnector))
        {
            Debug.Log($"[Reset] Ra lệnh cho PivotPoint ngắt kết nối khỏi {bobConnector.name}.");
            pivotPoint.Disconnect(bobConnector);
        }

        pivotPoint.ClearSnappedObject();
        if (pendulumBob.CurrentState != GrabbableState.Idle)
        {
            pendulumBob.AttemptUnsnap();
        }

        pendulumBob.transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);

        _idealSimulator.StopSimulation();
        _bobRootRigidbody.isKinematic = false;
        _isAssembled = false;
        _isInSetupPhase = false;

        if (_visualizer != null)
        {
            _visualizer.StopVisualizing();
            Debug.Log("[Reset] LineRenderer/Cylinder đã được tắt.");
        }

        ResetMeasurement();
        
        // [DASHBOARD] Reset lại số liệu
        InitializeDashboard();
        if (lengthSlider != null)
             DashboardManager.Instance.UpdateStat("Length", "Chiều dài (l)", $"{lengthSlider.value:F2} m");
    }
    #endregion

    #region Dashboard Logic
    private void InitializeDashboard()
    {
        if (DashboardManager.Instance == null) return;

        DashboardManager.Instance.ClearDashboard();

        // Cài đặt Switch: True = Ideal, False = Realistic
        bool isIdeal = (mode == SimulationMode.Ideal);
        DashboardManager.Instance.SetupModeSwitch(isIdeal, (val) =>
        {
            // Callback khi người dùng gạt cần
            SetSimulationMode(val ? SimulationMode.Ideal : SimulationMode.Realistic);
            
            // Cập nhật text chế độ ngay lập tức
            DashboardManager.Instance.UpdateStat("Mode", "Chế độ", val ? "Lý tưởng" : "Thực tế");
            
            // Reset thí nghiệm để áp dụng mode mới
            ResetExperimentButton(); 
        });

        // Tạo các dòng mặc định
        DashboardManager.Instance.UpdateStat("Mode", "Chế độ", isIdeal ? "Lý tưởng" : "Thực tế");
        DashboardManager.Instance.UpdateStat("Length", "Chiều dài (l)", $"{lengthSlider.value:F2} m");
        DashboardManager.Instance.UpdateStat("Angle", "Góc lệch (α)", "0°");
        DashboardManager.Instance.UpdateStat("Period", "Chu kỳ (T)", "--");
        DashboardManager.Instance.UpdateStat("Count", "Mẫu đo", "0");
    }

    private void UpdateDashboardRealtime()
    {
        if (DashboardManager.Instance == null) return;

        // 1. Góc lệch (Alpha)
        Vector3 currentVector = _bobModelTransform.position - pivotPoint.transform.position;
        
        Vector3 axis = pivotPoint != null ? pivotPoint.OscillationAxis : Vector3.forward;
        axis = pivotPoint.transform.TransformDirection(axis);

        float angle = Vector3.SignedAngle(Vector3.down, currentVector, axis);
        DashboardManager.Instance.UpdateStat("Angle", "Góc lệch (α)", $"{angle:F1}°");

        // 2. Chu kỳ (T)
        if (calculatedPeriod > 0)
        {
            DashboardManager.Instance.UpdateStat("Period", "Chu kỳ (T)", $"{calculatedPeriod:F3} s");
        }
        else if (_isTiming) 
        {
            DashboardManager.Instance.UpdateStat("Period", "Chu kỳ (T)", "<color=yellow>Đang đo...</color>");
        }
        else
        {
            DashboardManager.Instance.UpdateStat("Period", "Chu kỳ (T)", "--");
        }

        // 3. Số mẫu đo (Realistic Mode)
        if (mode == SimulationMode.Realistic)
        {
            DashboardManager.Instance.UpdateStat("Count", "Mẫu đo", $"{_measuredPeriods.Count}/{cyclesToAverage}");
        }
        else
        {
            DashboardManager.Instance.UpdateStat("Count", "Mẫu đo", "N/A");
        }
    }
    #endregion

    private void FixedUpdate()
    {
        CheckAssemblyState();

        if (_isInSetupPhase && mode == SimulationMode.Ideal)
        {
            if (pendulumBob.CurrentState == GrabbableState.Snapped)
            {
                if (!_bobRootRigidbody.isKinematic) _bobRootRigidbody.isKinematic = true;
                _bobRootRigidbody.linearVelocity = Vector3.zero;
                _bobRootRigidbody.angularVelocity = Vector3.zero;
            }

            if (pendulumBob.CurrentState == GrabbableState.ConstrainedGrab ||
                pendulumBob.CurrentState == GrabbableState.Snapped)
            {
                Vector3 currentVector = _bobModelTransform.position - pivotPoint.transform.position;
                float currentAngle = Vector3.SignedAngle(Vector3.down, currentVector, Vector3.forward);

                if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
                {
                    Debug.Log($"[Ideal Setup] Góc hiện tại: {currentAngle:F1}° (Max: ±{maxSetupAngleDegrees}°). Con lắc đang đứng yên.");
                }
            }
        }

        if (CurrentState != ExperimentState.Running && !_isInSetupPhase) return;

        _uiUpdateTimer += Time.fixedDeltaTime;
        if (_uiUpdateTimer >= 0.1f)
        {
            UpdateDashboardRealtime();
            _uiUpdateTimer = 0;
        }

        if (CurrentState != ExperimentState.Running) return;

        if (mode == SimulationMode.Realistic && pendulumBob.CurrentState == GrabbableState.Snapped)
        {
            MeasurePeriodRealistic();
        }

        _lastBobVelocityY = _bobRootRigidbody.linearVelocity.y;
    }

    private void CheckAssemblyState()
    {
        bool wasJustAssembled = !_isAssembled && pendulumBob.CurrentState == GrabbableState.Snapped;

        if (wasJustAssembled)
        {
            _isAssembled = true;
            Debug.Log("Con lắc đã được lắp ráp. Sẵn sàng để bắt đầu thí nghiệm.");

            Transform bobConnectorT = pendulumBob.GetComponentInChildren<Connector>().transform;
            if (_visualizer != null) _visualizer.StartVisualizing(pivotPoint.transform, bobConnectorT);

            float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);

            if (lengthSlider != null)
            {
                lengthSlider.SetValueWithoutNotify(Mathf.Clamp(length, minLength, maxLength));
                if (lengthValueText != null) lengthValueText.text = $"L = {length:F2} m";
            }

            bool isIdealSetupPhase = (mode == SimulationMode.Ideal && CurrentState == ExperimentState.PreExperiment);
            Vector3 worldAxis = pivotPoint.transform.TransformDirection(pivotPoint.OscillationAxis);

            var pendulumStrategy = new PendulumHoldStrategy(pivotPoint.transform, length, maxSetupAngleDegrees, isIdealSetupPhase, worldAxis);

            pendulumBob.SetHoldStrategy(pendulumStrategy);

            if (isIdealSetupPhase)
            {
                _isInSetupPhase = true;
                pendulumBob.ConfigureSnappedPhysics(true);
                Debug.Log($"[Ideal Mode - Setup] Bạn có thể kéo con lắc trong phạm vi ±{maxSetupAngleDegrees}° để chọn góc ban đầu.");
            }
            else if (mode == SimulationMode.Realistic)
            {
                pendulumBob.ConfigureSnappedPhysics(false);
                Debug.Log($"[Realistic Mode] Con lắc sẽ dao động theo vật lý thực tế khi bấm Start.");
            }
            
            // [DASHBOARD] Cập nhật chiều dài ngay khi lắp xong
            if (DashboardManager.Instance != null)
                DashboardManager.Instance.UpdateStat("Length", "Chiều dài (l)", $"{length:F2} m");
        }

        bool wasJustDisassembled = _isAssembled &&
                                   (pendulumBob.CurrentState == GrabbableState.Idle ||
                                    pendulumBob.CurrentState == GrabbableState.Grabbed);
        if (wasJustDisassembled)
        {
            _isAssembled = false;
            _isInSetupPhase = false;
            Debug.Log("Con lắc đã bị tháo gỡ.");

            if (_visualizer != null)
            {
                _visualizer.StopVisualizing();
            }

            if (CurrentState == ExperimentState.Running)
            {
                Debug.LogWarning("Thí nghiệm bị dừng do con lắc bị tháo ra.");
                EndExperiment();
            }
        }
    }

    private void ApplySimulationMode()
    {
        bool isIdealMode = (mode == SimulationMode.Ideal);

        if (isIdealMode)
        {
            Vector3 worldAxis = pivotPoint.transform.TransformDirection(pivotPoint.OscillationAxis);

            _idealSimulator.StartSimulation(pivotPoint.transform, pendulumBob.transform, _bobModelTransform, worldAxis);
            _idealSimulator.enabled = true;
            pendulumBob.ConfigureSnappedPhysics(true);
            Debug.Log("[Ideal Mode] IdealPendulumSimulator đã được kích hoạt.");
        }
        else
        {
            _idealSimulator.enabled = false;
            pendulumBob.ConfigureSnappedPhysics(false);
            Debug.Log("[Realistic Mode] Physics engine sẽ điều khiển dao động con lắc.");
        }
    }

    #region Period Calculation & Measurement
    private void CalculatePeriodIdeal()
    {
        float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
        if (length <= 0)
        {
            calculatedPeriod = 0;
            return;
        }

        float period = 2 * Mathf.PI * Mathf.Sqrt(length / 9.81f);
        calculatedPeriod = period;
    }

    private void MeasurePeriodRealistic()
    {
        float currentVelocityY = _bobRootRigidbody.linearVelocity.y;
        float currentVelocityX = _bobRootRigidbody.linearVelocity.x;

        if (_lastBobVelocityY < 0 && currentVelocityY >= 0)
        {
            Vector3 worldAxis = pivotPoint.transform.TransformDirection(pivotPoint.OscillationAxis);
            Vector3 tangentDirection = Vector3.Cross(worldAxis, Vector3.up);
            bool isForwardSwing = Vector3.Dot(_bobRootRigidbody.linearVelocity, tangentDirection) > 0;

            if (isForwardSwing)
            {
                if (!_isTiming)
                {
                    _swingStartTime = Time.fixedTime;
                    _isTiming = true;
                    Debug.Log("Bắt đầu đo chu kỳ (Realistic).");
                }
                else
                {
                    float period = Time.fixedTime - _swingStartTime;

                    if (period < minimumValidPeriod)
                    {
                        Debug.LogWarning($"Phép đo chu kỳ ({period:F3}s) bị loại bỏ vì quá ngắn (Nhiễu).");
                        _swingStartTime = Time.fixedTime;
                        return;
                    }

                    _measuredPeriods.Add(period);
                    Debug.Log($"Đo được chu kỳ: {period:F3}s");

                    if (_measuredPeriods.Count >= cyclesToAverage)
                    {
                        calculatedPeriod = _measuredPeriods.Average();
                        Debug.LogWarning($"[Realistic Mode] CHU KỲ TRUNG BÌNH sau {_measuredPeriods.Count} lần đo: {calculatedPeriod:F3}s");
                        _measuredPeriods.Clear();
                    }

                    _swingStartTime = Time.fixedTime;
                }
            }
        }
    }

    private void ResetMeasurement()
    {
        _measuredPeriods.Clear();
        calculatedPeriod = 0f;
        _isTiming = false;
        _lastBobVelocityY = 0f;
    }
    #endregion

    public void OnResetButtonPressed()
    {
        // Đây là hàm Wrapper được gọi bởi UI Button và logic nội bộ
        Debug.Log("[UI] Người dùng bấm nút Reset.");

        ResetExperimentLogic();

        if (lengthSlider != null)
        {
            lengthSlider.value = 1.0f;
            if (lengthValueText != null) lengthValueText.text = $"L = {1.0f:F2} m";
        }
    }

    public void OnStartButtonPressed()
    {
        Debug.Log("[UI] Người dùng bấm nút Start.");
        StartPendulumExperiment();
    }
    
    // Gọi wrapper để đồng nhất logic
    public void ResetExperimentButton()
    {
        OnResetButtonPressed();
    }
}