using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour, IInteractable
{
    [Header("Snapping Configuration")]
    [Tooltip("Prefab hoặc GameObject cụ thể mà vùng này chấp nhận. Nếu để trống, nó sẽ chấp nhận bất kỳ đối tượng Grabbable nào.")]
    [SerializeField] private Grabbable _acceptedObject;

    [Header("Visual Feedback")]
    [Tooltip("Material sẽ được áp dụng cho SnapZone khi một đối tượng hợp lệ đang hover trên nó.")]
    [SerializeField] private Material _highlightMaterial;

    private Renderer _renderer;
    private Material _originalMaterial;

    //flag occupied
    private bool _isOccupied = false;

    private Grabbable _snappedObject = null;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        _renderer = GetComponent<Renderer>();
        if(_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
    }

    /// <summary>
    /// Kiểm tra xem đối tượng được cung cấp có được SnapZone này chấp nhận hay không.
    /// Check if the provided object is accepted by this SnapZone.
    /// </summary>
    public bool IsObjectAccepted(Grabbable grabbable)
    {
        //
        return !_isOccupied && (_acceptedObject == null || grabbable == _acceptedObject);
    }
    /// <summary>
    /// Thực hiện hành động snap. Khóa đối tượng vào vị trí và tạo Joint.
    /// Snap action: Lock the object in place and create a Joint.
    /// </summary>
    public void SnapObject(Grabbable grabbable)
    {
        if (!IsObjectAccepted(grabbable)) return;
        
        grabbable.enabled = false; // Disable Grabbable script to prevent further interaction

        grabbable.transform.SetLocalPositionAndRotation(transform.position, transform.rotation);

        FixedJoint joint = grabbable.gameObject.AddComponent<FixedJoint>();
        Rigidbody zoneRb = GetComponent<Rigidbody>();
        if(zoneRb == null)
        {
            zoneRb = gameObject.AddComponent<Rigidbody>();
            zoneRb.isKinematic = true; // Make the SnapZone's Rigidbody kinematic
        }
        joint.connectedBody = zoneRb;


        _isOccupied = true;
        _snappedObject = grabbable;
        Highlight(false);
    }

    private void UnSnapObject()
    {
        if (!_isOccupied && _snappedObject == null) return;
        // Remove the FixedJoint
        FixedJoint joint = _snappedObject.GetComponent<FixedJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }
        _snappedObject.enabled = true; // Re-enable Grabbable script
        _snappedObject.OnSelectStart(); //
        _isOccupied = false;
        _snappedObject = null;
    }

    /// <summary>
    /// Kích hoạt hoặc vô hiệu hóa hiệu ứng highlight.
    /// activate or deactivate highlight effect.
    /// </summary>
    public void Highlight(bool state)
    {
        if (_renderer != null && _highlightMaterial != null)
        {
            _renderer.material = state ? _highlightMaterial : _originalMaterial;
        }
    }

    #region IInteractable Implementation
    public void OnHoverEnter()
    {
        if (_isOccupied && _snappedObject != null)
        {
            //Highlight(true);
            _snappedObject.HighlightObject(true);
        }
    }
    public void OnHoverExit()
    {
        if (_isOccupied && _snappedObject != null)
        {
            Highlight(false);
            _snappedObject.HighlightObject(false);
        }
    }
    public void OnSelectStart()
    {
        if (_isOccupied)
        {
            UnSnapObject();
        }
    }
    public void OnSelectEnd()
    {
        
    }
    #endregion
}
