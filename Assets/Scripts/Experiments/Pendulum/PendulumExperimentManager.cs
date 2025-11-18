using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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
            
            // Cập nhật lại strategy để bỏ giới hạn góc khi đang chạy thí nghiệm
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
            Debug.Log("[Reset] LineRenderer đã được tắt.");
        }
        
        ResetMeasurement();
    }
    #endregion

    private void FixedUpdate()
    {
        CheckAssemblyState();

        if (_isInSetupPhase && mode == SimulationMode.Ideal)
        {
            if (pendulumBob.CurrentState == GrabbableState.Snapped)
            {
                if (!_bobRootRigidbody.isKinematic)
                {
                    _bobRootRigidbody.isKinematic = true;
                }
                
                _bobRootRigidbody.linearVelocity = Vector3.zero;
                _bobRootRigidbody.angularVelocity = Vector3.zero;
            }
            
            if (pendulumBob.CurrentState == GrabbableState.ConstrainedGrab || 
                pendulumBob.CurrentState == GrabbableState.Snapped)
            {
                Vector3 currentVector = _bobModelTransform.position - pivotPoint.transform.position;
                float currentAngle = Vector3.SignedAngle(Vector3.down, currentVector, Vector3.forward);
                // Log mỗi 0.5 giây để tránh spam
                if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
                {
                    Debug.Log($"[Ideal Setup] Góc hiện tại: {currentAngle:F1}° (Max: ±{maxSetupAngleDegrees}°). Con lắc đang đứng yên.");
                }
            }
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
            if (_visualizer != null) _visualizer.StartVisualizing(_bobModelTransform);

            float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
            
            bool isIdealSetupPhase = (mode == SimulationMode.Ideal && CurrentState == ExperimentState.PreExperiment);
            var pendulumStrategy = new PendulumHoldStrategy(pivotPoint.transform, length, maxSetupAngleDegrees, isIdealSetupPhase);
            pendulumBob.SetHoldStrategy(pendulumStrategy);
            
            if (isIdealSetupPhase)
            {
                _isInSetupPhase = true;
                pendulumBob.ConfigureSnappedPhysics(true); // isKinematic = true
                Debug.Log($"[Ideal Mode - Setup] Bạn có thể kéo con lắc trong phạm vi ±{maxSetupAngleDegrees}° để chọn góc ban đầu. Con lắc sẽ đứng yên cho đến khi bấm Start.");
            }
            else if (mode == SimulationMode.Realistic)
            {
                pendulumBob.ConfigureSnappedPhysics(false); // isKinematic = false
                Debug.Log($"[Realistic Mode] Con lắc sẽ dao động theo vật lý thực tế khi bấm Start.");
            }
            
            Vector3 initialVector = _bobModelTransform.position - pivotPoint.transform.position;
            var _angle = Vector3.SignedAngle(Vector3.down, initialVector, Vector3.forward) * Mathf.Deg2Rad;
            Debug.Log("[DEBUG] Length: " + length);
            Debug.Log("[DEBUG] Initial Angle (deg): " + (_angle * Mathf.Rad2Deg));
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
                Debug.Log("LineRenderer đã được tắt.");
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

            _idealSimulator.StartSimulation(pivotPoint.transform, pendulumBob.transform, _bobModelTransform);
            _idealSimulator.enabled = true;
            
            pendulumBob.ConfigureSnappedPhysics(true);
            Debug.Log("[Ideal Mode] IdealPendulumSimulator đã được kích hoạt. Script sẽ điều khiển vị trí con lắc.");
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
        Debug.LogWarning($"[Ideal Mode] Chu kỳ lý thuyết tính được: {calculatedPeriod:F3}s với L={length:F2}m");
    }

    private void MeasurePeriodRealistic()
    {
        float currentVelocityY = _bobRootRigidbody.linearVelocity.y;
        float currentVelocityX = _bobRootRigidbody.linearVelocity.x;

        if (_lastBobVelocityY < 0 && currentVelocityY >= 0 && currentVelocityX > 0)
        {
            if (!_isTiming)
            {
                _swingStartTime = Time.fixedTime;
                _isTiming = true;
                Debug.Log("Bắt đầu đo chu kỳ.");
            }
            else
            {
                float period = Time.fixedTime - _swingStartTime;
                if (period < minimumValidPeriod)
                {
                    Debug.LogWarning($"Phép đo chu kỳ ({period:F3}s) bị loại bỏ vì quá ngắn.");
                    _swingStartTime = Time.fixedTime; // Reset timer
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

                _swingStartTime = Time.fixedTime; // Bắt đầu lại timer cho lần đo tiếp theo
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
}