using UnityEngine;

public class IdealPendulumSimulator : MonoBehaviour
{
    private float _angle;
    private float _angularVelocity;
    private float _length;
    private Transform _pivot;
    private Transform _bobRoot;

    private readonly float _gravity = 9.81f;

    /// <summary>
    /// Khởi tạo và bắt đầu mô phỏng script-driven.
    /// </summary>
    /// <param name="pivotPoint">Transform của điểm treo.</param>
    /// <param name="bobRootTransform">Transform của đối tượng gốc của con lắc.</param>
    /// <param name="bobModelTransform">Transform của model con để xác định chiều dài.</param>
    public void StartSimulation(Transform pivotPoint, Transform bobRootTransform, Transform bobModelTransform)
    {
        _pivot = pivotPoint;
        _bobRoot = bobRootTransform;

        _length = Vector3.Distance(_pivot.position, bobModelTransform.position);
        if (_length < 0.1f)
        {
            Debug.LogError("Chiều dài con lắc quá nhỏ, có thể gây lỗi tính toán.");
            _length = 1f;
        }

        Vector3 initialVector = bobModelTransform.position - _pivot.position;
        _angle = Vector3.SignedAngle(Vector3.down, initialVector, Vector3.forward) * Mathf.Deg2Rad;

        _angularVelocity = 0f;
        this.enabled = true;
        Debug.Log($"Ideal Simulation Started. Length: {_length}, Initial Angle: {_angle * Mathf.Rad2Deg} deg");
    }

    public void StopSimulation()
    {
        this.enabled = false;
        _pivot = null;
        _bobRoot = null;
    }

    private void FixedUpdate()
    {
        if (_pivot == null || _bobRoot == null) return;

        float angularAcceleration = -(_gravity / _length) * Mathf.Sin(_angle);
        _angularVelocity += angularAcceleration * Time.fixedDeltaTime;
        _angle += _angularVelocity * Time.fixedDeltaTime;

        UpdateBobPosition();
    }

    private void UpdateBobPosition()
    {
        float x = _length * Mathf.Sin(_angle);
        float y = -_length * Mathf.Cos(_angle);
        Vector3 bobModelTargetPosition = _pivot.position + new Vector3(x, y, 0);


        Vector3 offset = _bobRoot.position - _bobRoot.GetComponentInChildren<Renderer>().transform.position;

        _bobRoot.position = bobModelTargetPosition + offset;
    }
}