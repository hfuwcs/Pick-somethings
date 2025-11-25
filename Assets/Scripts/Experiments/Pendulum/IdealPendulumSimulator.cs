using UnityEngine;

public class IdealPendulumSimulator : MonoBehaviour
{
    private float _angle;
    private float _angularVelocity;
    private float _length;
    private Transform _pivot;
    private Transform _bobRoot;
    private Vector3 _visualOffset;
    private Vector3 _rotationAxis = Vector3.forward;

    private readonly float _gravity = 9.81f;

    public void StartSimulation(Transform pivotPoint, Transform bobRootTransform, Transform bobModelTransform, Vector3 rotationAxis)
    {
        _pivot = pivotPoint;
        _bobRoot = bobRootTransform;
        _rotationAxis = rotationAxis;

        _length = Vector3.Distance(_pivot.position, bobModelTransform.position);

        if (_length < 0.1f)
        {
            Debug.LogError("Chiều dài con lắc quá nhỏ, có thể gây lỗi tính toán.");
            _length = 1f;
        }

        _visualOffset = _bobRoot.position - bobModelTransform.position;

        Vector3 initialVector = bobModelTransform.position - _pivot.position;
        
        _angle = Vector3.SignedAngle(Vector3.down, initialVector, _rotationAxis) * Mathf.Deg2Rad;

        _angularVelocity = 0f;
        this.enabled = true;

        Debug.Log($"Ideal Simulation Started. Axis: {_rotationAxis}, Length: {_length}, Initial Angle: {_angle * Mathf.Rad2Deg} deg");
    }

    public void SetLength(float newLength)
    {
        if (newLength < 0.1f) newLength = 0.1f;
        _length = newLength;
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

        // Công thức gia tốc góc: a = -(g/L) * sin(theta)
        float angularAcceleration = -(_gravity / _length) * Mathf.Sin(_angle);

        _angularVelocity += angularAcceleration * Time.fixedDeltaTime;
        _angle += _angularVelocity * Time.fixedDeltaTime;

        UpdateBobPosition();
    }

    private void UpdateBobPosition()
    {
        Vector3 restVector = Vector3.down * _length;
        Quaternion rotation = Quaternion.AngleAxis(_angle * Mathf.Rad2Deg, _rotationAxis);
        Vector3 offsetFromPivot = rotation * restVector;
        Vector3 bobModelTargetPosition = _pivot.position + offsetFromPivot;
        _bobRoot.position = bobModelTargetPosition + _visualOffset;

        var rb = _bobRoot.GetComponent<Rigidbody>();
        if(rb != null && !rb.isKinematic)
        {
             rb.linearVelocity = Vector3.zero;
             rb.angularVelocity = Vector3.zero;
        }
    }
}