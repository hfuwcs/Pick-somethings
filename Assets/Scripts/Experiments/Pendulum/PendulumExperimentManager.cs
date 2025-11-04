using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PendulumExperimentManager : ExperimentManagerBase
{
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
    //private bool _isWaitingForRightwardPass = true;

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
        _idealSimulator.StopSimulation();
    }

    protected override void ResetExperimentLogic()
    {
        if (pendulumBob.CurrentState != GrabbableState.Idle)
        {
            if (pendulumBob.CurrentSnapZone != null) pendulumBob.CurrentSnapZone.ClearSnappedObject();
            pendulumBob.Unsnap();
        }
        pendulumBob.transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);

        _idealSimulator.StopSimulation();
        _bobRootRigidbody.isKinematic = false;
        _isAssembled = false;
        _visualizer.StopVisualizing();
        ResetMeasurement();
    }
    #endregion

    private void FixedUpdate()
    {
        bool wasJustAssembled = !_isAssembled && pendulumBob.CurrentState == GrabbableState.Snapped;
        if (wasJustAssembled)
        {
            _isAssembled = true;
            Debug.Log("Con lắc đã được lắp ráp. Sẵn sàng để bắt đầu thí nghiệm.");
            _visualizer.StartVisualizing(_bobModelTransform);

            float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
            var pendulumStrategy = new PendulumHoldStrategy(pivotPoint.transform, length);
            pendulumBob.SetHoldStrategy(pendulumStrategy);
            Vector3 initialVector = _bobModelTransform.position - pivotPoint.transform.position;
            var _angle = Vector3.SignedAngle(Vector3.down, initialVector, Vector3.forward) * Mathf.Deg2Rad;
            Debug.Log("[DEBUG] Length: " + length);
            Debug.Log("[DEBUG] Initial Angle (deg): " + (_angle * Mathf.Rad2Deg));
        }

        if (CurrentState != ExperimentState.Running) return;

        if (pendulumBob.CurrentState == GrabbableState.ConstrainedGrab)
        {
            if (_idealSimulator.enabled)
            {
                _idealSimulator.StopSimulation();
            }
        }
        else if (pendulumBob.CurrentState == GrabbableState.Snapped && pendulumBob.WasJustReleased)
        {
            pendulumBob.ConsumeReleaseFlag();
            ApplySimulationMode();
            ResetMeasurement(); 
        }

        if (mode == SimulationMode.Realistic && pendulumBob.CurrentState == GrabbableState.Snapped)
        {
            MeasurePeriodRealistic();
        }

        _lastBobVelocityY = _bobRootRigidbody.linearVelocity.y;
    }

    private void ApplySimulationMode()
    {
        bool isIdealMode = (mode == SimulationMode.Ideal);
        _idealSimulator.enabled = isIdealMode;

        if (isIdealMode)
        {
            _idealSimulator.StartSimulation(pivotPoint.transform, pendulumBob.transform, _bobModelTransform);
        }

        pendulumBob.ConfigureSnappedPhysics(isIdealMode);
    }

    #region Period Calculation & Measurement Logic

    /// <summary>
    /// Tính chu kỳ ngay lập tức bằng công thức lý thuyết cho chế độ Ideal.
    /// </summary>
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

    /// <summary>
    /// Đo chu kỳ bằng cách quan sát chuyển động trong chế độ Realistic.
    /// </summary>
    private void MeasurePeriodRealistic()
    {
        float currentVelocityY = _bobRootRigidbody.linearVelocity.y;
        float currentVelocityX = _bobRootRigidbody.linearVelocity.x;

        if (_lastBobVelocityY < 0 && currentVelocityY >= 0 && currentVelocityX > 0)
        {
            if (!_isTiming)
            {
                // Bắt đầu đo ở lần đi qua sang phải đầu tiên
                _swingStartTime = Time.fixedTime;
                _isTiming = true;
                Debug.Log("Bắt đầu đo chu kỳ (chờ lần đi qua sang phải tiếp theo).");
            }
            else
            {
                // Hoàn thành một chu kỳ ở lần đi qua sang phải tiếp theo
                float period = Time.fixedTime - _swingStartTime;

                if (period < minimumValidPeriod)
                {
                    Debug.LogWarning($"Phép đo chu kỳ ({period:F3}s) bị loại bỏ vì quá ngắn.");
                    _swingStartTime = Time.fixedTime;
                    return;
                }

                _measuredPeriods.Add(period);
                Debug.Log($"Đo được chu kỳ HOÀN CHỈNH: {period:F3}s");

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


    /// <summary>
    /// Reset lại trạng thái của bộ đếm chu kỳ.
    /// </summary>
    private void ResetMeasurement()
    {
        _measuredPeriods.Clear();
        calculatedPeriod = 0f;
        _isTiming = false;
        _lastBobVelocityY = 0f;
        // Không cần biến theo dõi hướng nữa
    }

    #endregion
}