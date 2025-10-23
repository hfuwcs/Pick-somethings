using System;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Quản lý logic cụ thể cho thí nghiệm Con lắc đơn.
/// Kế thừa từ ExperimentManagerBase để tuân thủ vòng đời thí nghiệm chuẩn.
/// </summary>
public class PendulumExperimentManager : ExperimentManagerBase
{
    [Header("Experiment Components")]
    [Tooltip("Đối tượng quả lắc có thể cầm nắm.")]
    [SerializeField] private Grabbable pendulumBob;

    [Tooltip("Điểm treo (SnapZone) mà quả lắc sẽ được gắn vào.")]
    [SerializeField] private SnapZone pivotPoint;

    [Tooltip("Vị trí và góc xoay ban đầu để reset quả lắc.")]
    [SerializeField] private Transform resetPoint;

    [Header("Experiment Data")]
    [Tooltip("Chu kỳ dao động (T) tính toán được.")]
    [SerializeField, ReadOnly] private float calculatedPeriod;

    // Sự kiện để thông báo cho UI khi có một chu kỳ mới được tính toán.
    public static event Action<float> OnPeriodCalculated;

    private Rigidbody _bobRigidbody;
    private bool _isAssembled = false; // Cờ để kiểm tra xem con lắc đã được lắp ráp chưa.

    #region Period Calculation Variables
    private float _swingTimer = 0f;
    private int _swingCount = 0;
    private bool _wasSwingingRight;
    #endregion

    #region Experiment Lifecycle Implementation
    protected override void InitializeExperiment()
    {
        if (pendulumBob == null || pivotPoint == null || resetPoint == null)
        {
            Debug.LogError("Vui lòng gán đầy đủ các thành phần (Pendulum Bob, Pivot Point, Reset Point) cho Experiment Manager.", this);
            enabled = false;
            return;
        }

        _bobRigidbody = pendulumBob.GetComponent<Rigidbody>();

        ResetExperimentLogic();
        BeginExperiment();//TODO: Remove this line after testing
    }

    protected override void StartExperimentLogic()
    {
        Debug.Log("Sẵn sàng để lắp ráp con lắc.");
        //TODO: Add new logic if needed.
    }

    protected override void EndExperimentLogic()
    {
        // Dừng mọi tính toán và có thể vô hiệu hóa vật lý.
        _bobRigidbody.isKinematic = true;
    }

    protected override void ResetExperimentLogic()
    {
        // Nếu con lắc đang được gắn, tháo nó ra.
        if (pendulumBob.CurrentState == GrabbableState.Snapped || pendulumBob.CurrentState == GrabbableState.HoldingSnapped)
        {
            pivotPoint.ClearSnappedObject();
            pendulumBob.Unsnap();
        }

        // Tắt các thuộc tính vật lý.
        _bobRigidbody.isKinematic = true;
        _bobRigidbody.useGravity = false;
        _bobRigidbody.linearVelocity = Vector3.zero;
        _bobRigidbody.angularVelocity = Vector3.zero;

        // Đưa quả lắc về vị trí reset.
        pendulumBob.transform.position = resetPoint.position;
        pendulumBob.transform.rotation = resetPoint.rotation;

        // Đặt lại các biến trạng thái và dữ liệu.
        _isAssembled = false;
        calculatedPeriod = 0f;
        ResetPeriodCalculation();
    }
    #endregion

    private void Update()
    {
        // Chỉ chạy logic khi thí nghiệm đang ở trạng thái Running.
        if (CurrentState != ExperimentState.Running) return;

        //Check nếu con lắc đã được lắp ráp.
        if (!_isAssembled && pendulumBob.CurrentState == GrabbableState.Snapped && pendulumBob.CurrentSnapZone == pivotPoint)
        {
            OnPendulumAssembled();
        }

        //Check và tính chu kỳ dao động nếu con lắc đã được lắp ráp.
        if (_isAssembled)
        {
            CalculatePeriod();
        }
    }

    private void OnPendulumAssembled()
    {
        _isAssembled = true;
        _bobRigidbody.isKinematic = false;
        _bobRigidbody.useGravity = true;
        Debug.Log("Con lắc đã được lắp ráp. Bắt đầu mô phỏng vật lý.");

        ResetPeriodCalculation();
    }

    /// <summary>
    /// Logic để tính chu kỳ dao động.
    /// Phương pháp: Ghi nhận thời gian giữa hai lần liên tiếp quả lắc đi qua điểm thấp nhất theo cùng một hướng.
    /// </summary>
    private void CalculatePeriod()
    {
        // Vector vận tốc theo phương ngang (trục x).
        // Giả định con lắc dao động chủ yếu trong mặt phẳng XY.
        float horizontalVelocity = _bobRigidbody.linearVelocity.x;

        // Xác định hướng di chuyển hiện tại.
        bool isCurrentlySwingingRight = horizontalVelocity > 0.01f;
        bool isCurrentlySwingingLeft = horizontalVelocity < -0.01f;

        // Bắt đầu timer khi con lắc bắt đầu một chu trình mới (vừa qua đáy và đi sang phải).
        if (_swingCount == 0 && isCurrentlySwingingRight)
        {
            _swingTimer = Time.time;
            _swingCount = 1;
            _wasSwingingRight = true;
        }

        // Nếu một chu trình đã bắt đầu...
        if (_swingCount > 0)
        {
            // Kiểm tra xem nó có đổi hướng không.
            if (_wasSwingingRight && isCurrentlySwingingLeft)
            {
                _wasSwingingRight = false; // Đã đi hết nửa chu kỳ.
            }
            // Kiểm tra xem nó có hoàn thành một chu kỳ đầy đủ không (đổi hướng từ trái sang phải).
            else if (!_wasSwingingRight && isCurrentlySwingingRight)
            {
                // Hoàn thành một chu kỳ. Tính toán và phát sự kiện.
                calculatedPeriod = Time.time - _swingTimer;
                OnPeriodCalculated?.Invoke(calculatedPeriod);
                Debug.Log($"Chu kỳ mới được tính toán: {calculatedPeriod:F2}s");

                // Reset để bắt đầu đo chu kỳ tiếp theo.
                _swingTimer = Time.time;
                _wasSwingingRight = true;
            }
        }
    }

    private void ResetPeriodCalculation()
    {
        _swingTimer = 0f;
        _swingCount = 0;
        _wasSwingingRight = false;
    }
}