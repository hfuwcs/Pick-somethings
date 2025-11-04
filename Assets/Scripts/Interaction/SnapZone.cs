using System;
using UnityEngine;

public enum JointType
{
    Fixed,
    Hinge,
    Configurable
}


/// <summary>
/// Định nghĩa một vùng có thể tiếp nhận một Connector để tạo kết nối.
/// Sử dụng một Collider ở chế độ Trigger để phát hiện.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    [Tooltip("ID của Connector mà vùng này chấp nhận.")]
    [SerializeField] private string acceptedID = "Default";

    #region Events
    public static event Action<SnapZone, Grabbable> OnSnapZoneEnter;
    public static event Action<SnapZone> OnSnapZoneExit;
    public static event Action<CircuitComponent> OnComponentSnapped;
    public static event Action<CircuitComponent> OnComponentUnsnapped;
    #endregion

    [Tooltip("Loại Joint sẽ được tạo khi một đối tượng được gắn vào.")]
    [SerializeField] private JointType jointType = JointType.Fixed;
    [SerializeField] private Material highlightMaterial;
    public JointType DesiredJointType => jointType;
    private Material _originalMaterial;
    private Renderer _renderer;
    private bool _isHighlighted = false;
    private Grabbable _snappedObject = null;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        // ✓ Kiểm tra xem SnapZone có Rigidbody không
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"SnapZone '{gameObject.name}' không có Rigidbody component. Các vật được snap vào sẽ không hoạt động đúng. Vui lòng thêm Rigidbody vào GameObject này.", this);
        }

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Nếu đã có object snap rồi, không cho phép snap thêm
        if (_snappedObject != null) return;

        Connector connector = other.GetComponent<Connector>();
        if (connector == null) return;

        // Verify connector ID phù hợp và parent Grabbable tồn tại
        if (connector.ConnectionID != this.acceptedID) return;
        if (connector.ParentGrabbable == null) return;

        Debug.Log($"Connector hợp lệ '{other.name}' đã đi vào SnapZone '{this.name}'.");
        Highlight();
        OnSnapZoneEnter?.Invoke(this, connector.ParentGrabbable);
    }

    private void OnTriggerExit(Collider other)
    {
        Connector connector = other.GetComponent<Connector>();
        if (connector == null) return;
        if (connector.ParentGrabbable == null) return;

        // Chỉ xử lý nếu là connector phù hợp
        if (connector.ConnectionID != this.acceptedID) return;

        // Nếu connector này không phải là object đang được snap, có thể unhighlight
        if (_snappedObject != connector.ParentGrabbable)
        {
            Debug.Log($"Connector '{other.name}' đã rời khỏi SnapZone '{this.name}'.");
            Unhighlight();

            if (OnSnapZoneExit != null)
            {
                OnSnapZoneExit.Invoke(this);
            }
        }
    }
    public void SetSnappedObject(Grabbable grabbable)
    {
        // Verify grabbable có effect
        if (grabbable == null)
        {
            Debug.LogWarning($"SnapZone {name} nhận grabbable null.", this);
            return;
        }

        // Nếu đã có object snap rồi, warning
        if (_snappedObject != null)
        {
            Debug.LogWarning($"SnapZone {name} đã có đối tượng {_snappedObject.name}, không thể snap đối tượng mới: {grabbable.name}.", this);
            return;
        }

        _snappedObject = grabbable;
        Debug.Log($"Đối tượng {grabbable.name} đã được snap vào {name}.");

        if (_snappedObject.TryGetComponent<CircuitComponent>(out var component))
        {
            OnComponentSnapped?.Invoke(component);
            Debug.Log($"Sự kiện OnComponentSnapped được phát cho linh kiện: {component.name}");
        }
    }

    public void ClearSnappedObject()
    {
        if (_snappedObject == null) return;

        if (_snappedObject.TryGetComponent<CircuitComponent>(out var component))
        {
            OnComponentUnsnapped?.Invoke(component);
            Debug.Log($"Sự kiện OnComponentUnsnapped được phát cho linh kiện: {component.name}");
        }

        Debug.Log($"Đối tượng {_snappedObject.name} đã được xóa khỏi {name}.");
        _snappedObject = null;
    }


    private void Highlight()
    {
        if (_renderer != null && highlightMaterial != null && !_isHighlighted)
        {
            _renderer.material = highlightMaterial;
            _isHighlighted = true;
        }
    }

    private void Unhighlight()
    {
        if (_renderer != null && _isHighlighted)
        {
            _renderer.material = _originalMaterial;
            _isHighlighted = false;
        }
    }
}