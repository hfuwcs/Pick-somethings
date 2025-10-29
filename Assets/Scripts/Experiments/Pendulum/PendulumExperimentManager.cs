using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class PendulumExperimentManager : ExperimentManagerBase
{
    public enum SimulationMode
    {
        Realistic, // Engine-driven
        Ideal      // Script-driven
    }

    [Header("Experiment Mode")]
    [Tooltip("Chọn chế độ mô phỏng: Thực tế (dựa trên engine) hoặc Lý tưởng (dựa trên script).")]
    [SerializeField] private SimulationMode mode = SimulationMode.Realistic;

    [Header("Experiment Components")]
    [SerializeField] private Grabbable pendulumBob;
    [SerializeField] private SnapZone pivotPoint;
    [SerializeField] private Transform resetPoint;

    [Header("Period Measurement")]
    [Tooltip("Số chu kỳ cần đo để lấy trung bình.")]
    [SerializeField] private int cyclesToAverage = 3;
    [SerializeField, ReadOnly] private float averagePeriod;

    // --- References ---
    private Rigidbody _bobRootRigidbody;
    private Transform _bobModelTransform;
    private IdealPendulumSimulator _idealSimulator;
    private bool _isAssembled = false;

    // --- Measurement State ---
    private List<float> _measuredPeriods = new List<float>();
    private float _swingStartTime;
    private bool _isTiming = false;
    private bool _wasBelowEquilibrium = false;

    private PendulumVisualizer _visualizer;


    #region Public Control
    public void SetSimulationMode(SimulationMode newMode)
    {
        if (mode == newMode) return;
        mode = newMode;

        Debug.Log($"Chuyển sang chế độ: {newMode}");
        if (_isAssembled)
        {
            ApplySimulationMode();
        }
    }
    #endregion

    #region Experiment Lifecycle
    protected override void InitializeExperiment()
    {
        _bobRootRigidbody = pendulumBob.GetComponent<Rigidbody>();
        var renderer = pendulumBob.GetComponentInChildren<Renderer>();
        _bobModelTransform = renderer.transform;

        _idealSimulator = pendulumBob.GetComponent<IdealPendulumSimulator>();
        if (_idealSimulator == null)
        {
            _idealSimulator = pendulumBob.gameObject.AddComponent<IdealPendulumSimulator>();
        }
        _idealSimulator.enabled = false;

        _visualizer = pivotPoint.GetComponent<PendulumVisualizer>();
        if (_visualizer == null)
        {
            Debug.LogError("PivotPoint thiếu component PendulumVisualizer.", this);
            enabled = false;
            return;
        }

        //ResetExperimentLogic();
        BeginExperiment();
    }

    protected override void ResetExperimentLogic()
    {
        _idealSimulator.StopSimulation();
        _bobRootRigidbody.isKinematic = true;

        if (pendulumBob.CurrentState != GrabbableState.Idle)
        {
            if (pendulumBob.CurrentSnapZone != null) pendulumBob.CurrentSnapZone.ClearSnappedObject();
            pendulumBob.Unsnap();
        }

        _bobRootRigidbody.linearVelocity = Vector3.zero;
        _bobRootRigidbody.angularVelocity = Vector3.zero;

        _bobRootRigidbody.isKinematic = true;
        pendulumBob.transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);

        _isAssembled = false;
        _visualizer.StopVisualizing();
        ResetMeasurement();
    }

    protected override void StartExperimentLogic() { }
    protected override void EndExperimentLogic() { }
    #endregion

    private void FixedUpdate()
    {
        if (CurrentState != ExperimentState.Running) return;

        bool wasJustAssembled = !_isAssembled && pendulumBob.CurrentState == GrabbableState.Snapped;
        if (wasJustAssembled)
        {
            _isAssembled = true;
            Debug.Log("Con lắc đã được lắp ráp.");
            _visualizer.StartVisualizing(_bobModelTransform);

            //Injection of pendulum hold strategy
            float length = Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
            var pendulumStrategy = new PendulumHoldStrategy(pivotPoint.transform, length);
            Debug.Log($"[TEST] Đã inject PendulumHoldStrategy với chiều dài: {length}");
            pendulumBob.SetHoldStrategy(pendulumStrategy);
            //
            ApplySimulationMode();
        }

        if (pendulumBob.CurrentState == GrabbableState.Snapped && pendulumBob.WasJustReleased)
        {
            pendulumBob.ConsumeReleaseFlag();
            Debug.Log("Người dùng vừa thả con lắc. Kích hoạt lại mô phỏng.");
            ApplySimulationMode();
            ResetMeasurement();
        }

        if (pendulumBob.CurrentState == GrabbableState.ConstrainedGrab)
        {
            if (_idealSimulator.enabled)
            {
                _idealSimulator.StopSimulation();
                Debug.Log("Tạm dừng IdealSimulator vì người chơi đang cầm.");
            }
        }
        if (mode == SimulationMode.Realistic && _isAssembled)
        {
            MeasurePeriod();
        }
    }

    private void ApplySimulationMode()
    {

        bool isIdealMode = (mode == SimulationMode.Ideal);
        _idealSimulator.enabled = isIdealMode;

        if (isIdealMode)
        {
            _idealSimulator.StartSimulation(
                pivotPoint.transform,
                pendulumBob.transform,
                _bobModelTransform
            );
        }
        else
        {
            _idealSimulator.StopSimulation();
        }

        pendulumBob.ConfigureSnappedPhysics(isIdealMode);

        Debug.Log($"Áp dụng chế độ {mode}. Rigidbody.isKinematic = {isIdealMode}");
    }

    #region Period Measurement Logic
    private void MeasurePeriod()
    {
        float equilibriumY = pivotPoint.transform.position.y - Vector3.Distance(pivotPoint.transform.position, _bobModelTransform.position);
        Debug.Log($"Điểm cân bằng Y: {equilibriumY:F3}, Vị trí Bob Y: {_bobModelTransform.position.y:F3}");
        bool isBelowEquilibrium = _bobModelTransform.position.y > equilibriumY;
        Debug.Log($"isBelowEquilibrium: {isBelowEquilibrium}, _wasBelowEquilibrium: {_wasBelowEquilibrium}");

        // Phát hiện thời điểm đi qua điểm cân bằng từ trên xuống
        if (!_wasBelowEquilibrium && isBelowEquilibrium)
        {
            if (!_isTiming)
            {
                // Bắt đầu một chu trình đo mới
                _swingStartTime = Time.fixedTime;
                _isTiming = true;
            }
            else
            {
                // Hoàn thành một chu kỳ
                float period = Time.fixedTime - _swingStartTime;
                _measuredPeriods.Add(period);
                Debug.Log($"Đo được chu kỳ: {period:F3}s");

                if (_measuredPeriods.Count >= cyclesToAverage)
                {
                    averagePeriod = _measuredPeriods.Average();
                    Debug.LogWarning($"CHU KỲ TRUNG BÌNH SAU {_measuredPeriods.Count} LẦN ĐO: {averagePeriod:F3}s");
                    _measuredPeriods.Clear(); // Reset để đo lại
                }

                // Bắt đầu lại timer cho chu kỳ tiếp theo
                _swingStartTime = Time.fixedTime;
            }
        }
        _wasBelowEquilibrium = isBelowEquilibrium;
    }

    private void ResetMeasurement()
    {
        _measuredPeriods.Clear();
        averagePeriod = 0f;
        _isTiming = false;
        _wasBelowEquilibrium = false;
    }
    #endregion
}