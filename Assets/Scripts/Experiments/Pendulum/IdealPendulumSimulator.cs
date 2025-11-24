using UnityEngine;

public class IdealPendulumSimulator : MonoBehaviour
{
    private float _angle;
    private float _angularVelocity;
    private float _length;
    private Transform _pivot;
    private Transform _bobRoot;
    
    // [OPTIMIZATION] Lưu trữ khoảng cách lệch giữa Root và Visual Model để không phải tính lại mỗi frame
    private Vector3 _visualOffset; 

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

        // Tính toán vector lệch giữa Root (cái Grabbable) và Model (cái quả cầu visual)
        // Vì ta muốn Model nằm đúng vị trí vật lý, nên Root phải nằm lệch đi một chút nếu Pivot của Root không trùng tâm quả cầu.
        _visualOffset = _bobRoot.position - bobModelTransform.position;

        Vector3 initialVector = bobModelTransform.position - _pivot.position;
        _angle = Vector3.SignedAngle(Vector3.down, initialVector, Vector3.forward) * Mathf.Deg2Rad;

        _angularVelocity = 0f;
        this.enabled = true;
        Debug.Log($"Ideal Simulation Started. Length: {_length}, Initial Angle: {_angle * Mathf.Rad2Deg} deg");
    }

    // [NEW] Hàm cập nhật chiều dài động (Gọi từ Manager/Slider)
    public void SetLength(float newLength)
    {
        if (newLength < 0.1f) newLength = 0.1f; // Bảo vệ chia cho 0
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

        // Phương trình vi phân dao động điều hòa: a = -(g/l) * sin(theta)
        float angularAcceleration = -(_gravity / _length) * Mathf.Sin(_angle);
        
        _angularVelocity += angularAcceleration * Time.fixedDeltaTime;
        _angle += _angularVelocity * Time.fixedDeltaTime;

        UpdateBobPosition();
    }

    private void UpdateBobPosition()
    {
        // Tính toán vị trí vật lý lý tưởng của tâm quả cầu (Model)
        float x = _length * Mathf.Sin(_angle);
        float y = -_length * Mathf.Cos(_angle);
        Vector3 bobModelTargetPosition = _pivot.position + new Vector3(x, y, 0);

        // Cập nhật vị trí của Root dựa trên vị trí Model + Offset đã cache
        _bobRoot.position = bobModelTargetPosition + _visualOffset;
        
        // Reset vận tốc vật lý để tránh xung đột với Transform update
        var rb = _bobRoot.GetComponent<Rigidbody>();
        if(rb != null && !rb.isKinematic)
        {
             rb.linearVelocity = Vector3.zero;
             rb.angularVelocity = Vector3.zero;
        }
    }
}