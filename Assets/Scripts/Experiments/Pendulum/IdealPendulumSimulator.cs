using UnityEngine;

public class IdealPendulumSimulator : MonoBehaviour
{
    // --- State Variables ---
    private float _angle;           // Góc hiện tại (radian) so với phương thẳng đứng
    private float _angularVelocity; // Vận tốc góc hiện tại

    // --- Configuration (Set on Start) ---
    private float _length;          // Chiều dài con lắc
    private Transform _pivot;       // Điểm treo
    private Transform _bobRoot;     // Transform gốc của đối tượng con lắc
    private Transform _bobModel;    // Transform của model con (để xác định khối tâm)
    private Vector3 _modelOffset;   // Vector offset từ gốc đến model (local space)

    private const float GRAVITY = 9.81f;

    /// <summary>
    /// Khởi tạo và bắt đầu mô phỏng script-driven.
    /// </summary>
    public void StartSimulation(Transform pivotPoint, Transform bobRootTransform, Transform bobModelTransform)
    {
        _pivot = pivotPoint;
        _bobRoot = bobRootTransform;
        _bobModel = bobModelTransform;

        _length = Vector3.Distance(_pivot.position, _bobModel.position);
        if (_length < 0.1f)
        {
            Debug.LogError("Chiều dài con lắc quá nhỏ, có thể gây lỗi tính toán.", this);
            _length = 1f; // Giá trị an toàn
        }

        _modelOffset = _bobRoot.InverseTransformPoint(_bobModel.position);

        Vector3 initialVector = _bobModel.position - _pivot.position;
        Vector3 projectedVector = Vector3.ProjectOnPlane(initialVector, _pivot.forward);

        _angle = Vector3.SignedAngle(Vector3.down, projectedVector, _pivot.forward) * Mathf.Deg2Rad;
        _angularVelocity = 0f;

        this.enabled = true;
        Debug.Log($"Ideal Simulation Started. Length: {_length:F2}m, Initial Angle: {_angle * Mathf.Rad2Deg:F2} deg");
    }

    public void StopSimulation()
    {
        this.enabled = false;
    }

    private void FixedUpdate()
    {
        if (_pivot == null || _bobRoot == null) return;

        float angularAcceleration = -(GRAVITY / _length) * Mathf.Sin(_angle);
        _angularVelocity += angularAcceleration * Time.fixedDeltaTime;
        _angle += _angularVelocity * Time.fixedDeltaTime;

        UpdateBobTransform();
    }

    private void UpdateBobTransform()
    {

        float x = _length * Mathf.Sin(_angle);
        float y = -_length * Mathf.Cos(_angle);
        Vector3 localModelPosition = new Vector3(x, y, 0);

        Vector3 worldModelTargetPosition = _pivot.TransformPoint(localModelPosition);


        Vector3 upDirection = (_pivot.position - worldModelTargetPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(_pivot.forward, upDirection);

        _bobRoot.rotation = targetRotation;

        Vector3 worldOffset = _bobRoot.rotation * _modelOffset;
        _bobRoot.position = worldModelTargetPosition - worldOffset;
    }
}