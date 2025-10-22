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

    public static event Action<SnapZone, Grabbable> OnSnapZoneEnter;
    public static event Action<SnapZone> OnSnapZoneExit;

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

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        Connector connector = other.GetComponent<Connector>();
        if (connector == null || _snappedObject != null) return;

        if (connector.ConnectionID == this.acceptedID)
        {
            Debug.Log($"Connector hợp lệ '{other.name}' đã đi vào SnapZone '{this.name}'.");
            Highlight();
            OnSnapZoneEnter?.Invoke(this, connector.ParentGrabbable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Connector connector = other.GetComponent<Connector>();
        if (connector == null) return;
        if (connector.ParentGrabbable == null) return;
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
        _snappedObject = grabbable;
        Highlight();
    }

    public void ClearSnappedObject()
    {
        _snappedObject = null;
        Unhighlight();
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