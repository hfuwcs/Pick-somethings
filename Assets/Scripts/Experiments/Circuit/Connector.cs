using System.Collections.Generic;
using UnityEngine;
public class Connector : MonoBehaviour, IInteractable
{
    [Header("Wiring Configuration")]
    [Tooltip("Đánh dấu nếu connector này cho phép bắt đầu/kết thúc một kết nối dây.")]
    [SerializeField] private bool isInteractableForWiring = true;
    public bool IsInteractableForWiring => isInteractableForWiring;


    [Tooltip("ID định danh loại kết nối. SnapZone sẽ chỉ chấp nhận Connector có cùng ID.")]
    [SerializeField] private string connectionID = "Default";

    private Grabbable _parentGrabbable;
    public CircuitComponent ParentComponent { get; private set; }

    public string ConnectionID => connectionID;
    public Grabbable ParentGrabbable => _parentGrabbable;
    public SnapZone ConnectedZone { get; private set; }
    private Collider _collider;
    private readonly List<Wire> _connectedWires = new List<Wire>();
    public IReadOnlyList<Wire> ConnectedWires => _connectedWires;
    public bool HasWires => _connectedWires.Count > 0;
    private void Awake()
    {

        _parentGrabbable = GetComponentInParent<Grabbable>();
        ParentComponent = GetComponentInParent<CircuitComponent>();
        _collider = GetComponent<Collider>();
        if (_parentGrabbable == null)
        {
            Debug.LogError($"Connector '{name}' không tìm thấy Grabbable cha.", this);
        }
    }
    public void SetInteractableState(bool isActive)
    {
        if (!isInteractableForWiring) return;
        if (_collider != null)
        {
            _collider.enabled = isActive;
        }
    }
    public void SetConnectedZone(SnapZone zone)
    {
        ConnectedZone = zone;
    }

    public void ClearConnectedZone()
    {
        ConnectedZone = null;
    }
    public void AddWire(Wire wire)
    {
        if (!_connectedWires.Contains(wire))
        {
            _connectedWires.Add(wire);
        }
    }

    public void RemoveWire(Wire wire)
    {
        if (_connectedWires.Contains(wire))
        {
            _connectedWires.Remove(wire);
        }
    }
    public bool HasActiveConnection
    {
        get
        {
            // Connector được coi là "có kết nối" nếu:
            // 1. Có ít nhất 1 dây đang nối vào nó.
            // 2. HOẶC nó đang nằm trong một SnapZone (ví dụ: cắm trên Breadboard).
            return _connectedWires.Count > 0 || ConnectedZone != null;
        }
    }
    #region IInteractable 
    public void OnHoverEnter()
    {
        if (isInteractableForWiring)
        {
            // TODO: Thêm logic highlight cho connector (ví dụ: đổi màu, phóng to)
        }
    }

    public void OnHoverExit()
    {
        if (isInteractableForWiring)
        {
            // TODO: Bỏ highlight
        }
    }

    public void OnSelectStart() { }
    public void OnSelectEnd() { }
    #endregion
}