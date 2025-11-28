using System;
using System.Collections.Generic;
using UnityEngine;

#region enum
public enum JointType
{
    Fixed,
    Hinge,
    Configurable
}

public enum SnapType
{
    /// <summary>
    /// Căn chỉnh tâm của Connector với tâm của SnapZone.
    /// </summary>
    AlignConnector,

    /// <summary>
    /// Căn chỉnh gốc (pivot) của Grabbable với tâm của SnapZone.
    /// </summary>
    AlignOrigin
}

public enum SnapRole
{
    /// <summary>
    /// Tạo ra một kết nối logic ngay lập tức. Hệ thống sẽ được thông báo.
    /// (Dành cho các kết nối đơn giản như con lắc).
    /// </summary>
    DirectConnection,

    /// <summary>
    /// Chỉ hoạt động như một điểm giữ vật lý. Không có kết nối logic nào được tạo ra.
    /// (Dành cho bảng mạch, nơi kết nối được tạo bởi dây dẫn).
    /// </summary>
    AnchorOnly
}
#endregion 

/// <summary>
/// Định nghĩa một vùng có thể tiếp nhận một Connector để tạo kết nối.
/// Sử dụng một Collider ở chế độ Trigger để phát hiện.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    #region Events
    public static event Action<SnapZone, Grabbable> OnSnapZoneEnter;
    public static event Action<SnapZone> OnSnapZoneExit;
    public static event Action<CircuitComponent> OnComponentSnapped;
    public static event Action<CircuitComponent> OnComponentUnsnapped;
    #endregion

    #region GRAPH
    private static int _nextId = 0;

    /// <summary>
    /// ID định danh duy nhất cho Node này trong đồ thị mạch điện.
    /// </summary>
    public int NodeId { get; private set; }

    /// <summary>
    /// Danh sách tất cả các Connector hiện đang kết nối vật lý vào Node này.
    /// </summary>
    private readonly List<Connector> _connectedConnectors = new List<Connector>();
    public IReadOnlyList<Connector> ConnectedConnectors => _connectedConnectors;
    #endregion

    #region SerializeField

    [Header("Pendulum Configuration")]
    [Tooltip("Trục xoay của khớp nối (Local Space). (0,0,1) = Lắc Trái/Phải. (1,0,0) = Lắc Trước/Sau.")]
    [SerializeField] private Vector3 oscillationAxis = new Vector3(0, 0, 1);

    [Header("Cấu hình Hành vi")]
    [Tooltip("Vai trò của SnapZone này trong hệ thống logic.")]
    [SerializeField] private SnapRole role = SnapRole.DirectConnection;

    [Header("Snapping Behavior")]
    [Tooltip("Hành vi gắn kết khi một vật được snap vào vùng này.")]
    [SerializeField] private SnapType snapBehavior = SnapType.AlignConnector;

    [Tooltip("Loại Joint sẽ được tạo khi một đối tượng được gắn vào.")]
    [SerializeField] private JointType jointType = JointType.Fixed;

    [Tooltip("ID của Connector mà vùng này chấp nhận.")]
    [SerializeField] private string acceptedID = "Default";

    [SerializeField] private Material highlightMaterial;

    #endregion

    #region public Variables
    public JointType DesiredJointType => jointType;
    public SnapType SnapBehavior => snapBehavior;
    public Vector3 OscillationAxis => oscillationAxis;
    #endregion
    #region  private Variables
    private Material _originalMaterial;
    private Renderer _renderer;
    private bool _isHighlighted = false;
    private Grabbable _snappedObject = null;
    #endregion
    private void Awake()
    {
        NodeId = _nextId++;
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

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

        var objectToUnsnap = _snappedObject;
        Debug.Log($"Đối tượng {objectToUnsnap.name} đã được xóa khỏi {name}.");

        _snappedObject = null;

        Unhighlight();

        if (objectToUnsnap.TryGetComponent<CircuitComponent>(out var component))
        {
            OnComponentUnsnapped?.Invoke(component);
            Debug.Log($"Sự kiện OnComponentUnsnapped được phát cho linh kiện: {component.name}");
        }
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

    /// <summary>
    /// Đăng ký một Connector vào Node này. Được gọi bởi Grabbable.
    /// </summary>
    public void Connect(Connector connector)
    {
        if (connector == null || _connectedConnectors.Contains(connector)) return;

        _connectedConnectors.Add(connector);
        connector.SetConnectedZone(this);

        Debug.Log($"[SnapZone.Connect] Role hiện tại: {role}, Connector: {connector.name}, ParentComponent: {connector.ParentComponent?.name ?? "NULL"}");
        if (AudioManager.Instance) AudioManager.Instance.PlaySnap();

        if (role == SnapRole.DirectConnection && connector.ParentComponent != null)
        {
            Debug.Log($"[SnapZone - Direct] Connector '{connector.name}' đã kết nối. Phát sự kiện OnComponentSnapped.");
            OnComponentSnapped?.Invoke(connector.ParentComponent);
        }
        else
        {
            Debug.Log($"[SnapZone - Anchor] Connector '{connector.name}' đã kết nối vật lý vào {name}. Không có sự kiện logic nào được phát.");
        }
    }

    /// <summary>
    /// Hủy đăng ký một Connector khỏi Node này. Được gọi bởi Grabbable.
    /// </summary>
    public void Disconnect(Connector connector)
    {
        if (connector == null || !_connectedConnectors.Contains(connector)) return;

        var componentToUnsnap = connector.ParentComponent;

        _connectedConnectors.Remove(connector);
        connector.ClearConnectedZone();
        if (AudioManager.Instance) AudioManager.Instance.PlayUnsnap();

        if (role == SnapRole.DirectConnection && componentToUnsnap != null)
        {
            Debug.Log($"[SnapZone - Direct] Connector '{connector.name}' đã ngắt kết nối. Phát sự kiện OnComponentUnsnapped.");
            OnComponentUnsnapped?.Invoke(componentToUnsnap);
        }
        else
        {
            Debug.Log($"[SnapZone - Anchor] Connector '{connector.name}' đã ngắt kết nối vật lý khỏi {name}.");
        }
    }
}