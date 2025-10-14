using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Grabbable : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private Material _highlightMaterial;
    private Material _originalMaterialRef;

    private Color _originalColor;
    private Renderer _renderer;
    private bool _isGrabbed = false;
    private Transform _grabberTransform;
    private Rigidbody _rigidbody;
    private int _originalLayer;
    private SnapZone _currentSnapZone;

    private void Awake()
    {
        _renderer  = GetComponent<Renderer>();
        if(_renderer != null)
        {
            _originalMaterialRef = _renderer.material;
            _originalColor = _renderer.material.color;
        }
        _rigidbody = GetComponent<Rigidbody>();
        _originalLayer = gameObject.layer;
    }
    //private void Update()
    //{
    //    if(_isGrabbed && _grabberTransform != null)
    //    {
    //        transform.position = _grabberTransform.position;
    //        transform.rotation = _grabberTransform.rotation;
    //    }
    //}
    private void FixedUpdate()
    {
        if (!_isGrabbed || _grabberTransform == null)
        {
            return;
        } 
            Debug.Log("GRABBABLE FixedUpdate: Executing...");

            //1 tính toán vị trí mục tiêu
            Vector3 currentPosition = _rigidbody.position;
            Vector3 targetPosition = _grabberTransform.position;
            Vector3 movementVector = targetPosition - currentPosition;
            float distance = movementVector.magnitude;
            Debug.Log($"currentPosition: {currentPosition}, targetPosition: {targetPosition}, Distance: {distance}");
            //Không sweeptest nếu không đi chuyển
            //Not sweeptest if not moving
        if (Vector3.Distance(currentPosition, targetPosition) < 0.001f)
            {
                return;
            }
    
            //2 Sweeeptest
            RaycastHit hitInfo;
            if (_rigidbody.SweepTest(movementVector.normalized, out hitInfo, distance, QueryTriggerInteraction.Ignore))
            {
                Debug.LogError($"SweepTest HIT '{hitInfo.collider.name}' at distance {hitInfo.distance}");
            //Move object tới vị trí va chạm
            //Move object to hit position
            _rigidbody.MovePosition(currentPosition + movementVector.normalized * hitInfo.distance);
            }
            else
            {
                Debug.Log("SweepTest NO HIT, moving to target.");
                //Không có va chạm, move thẳng tới vị trí mục tiêu
                _rigidbody.MovePosition(targetPosition);
            }
        
    }
    public void OnHoverEnter()
    {
        HighlightObject(true);
    }
    public void OnHoverExit()
    {
        HighlightObject(false);
    }
    public void HighlightObject(bool state)
    {
        if (_renderer != null && _highlightMaterial != null)
        {
            _renderer.material = state ? _highlightMaterial : _originalMaterialRef;
        }
    }
    public void OnSelectStart()
    {
        Debug.Log($"Object {gameObject.name} selected.");
        _isGrabbed = true;

        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        gameObject.layer = LayerMask.NameToLayer("GrabbedObject");
    }
    public void OnSelectEnd()
    {
        Debug.Log($"Object {gameObject.name} released.");
        _isGrabbed = false;
        _grabberTransform = null;

        /// <sumary>
        /// Check xem có đang ở trong SnapZone hợp lệ không.
        /// Check if currently in a valid SnapZone.
        ///</sumary>
        if (_currentSnapZone != null && _currentSnapZone.IsObjectAccepted(this))
        {
            _currentSnapZone.SnapObject(this);
        }
        else
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            gameObject.layer = _originalLayer;
        }
        _currentSnapZone?.Highlight(false);
        HighlightObject(false);
    }
    public void SetGrabber(Transform grabber)
    {
        _grabberTransform = grabber;
    }
    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    #region SnapZone Interaction
    //Snapzone trigger events
    private void OnTriggerEnter(Collider other)
    {
        SnapZone snapZone = other.GetComponent<SnapZone>();
        if(snapZone != null && snapZone.IsObjectAccepted(this))
        {
            _currentSnapZone = snapZone;
            snapZone.Highlight(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        SnapZone snapZone = other.GetComponent<SnapZone>();
        if(snapZone !=null && other.gameObject == _currentSnapZone?.gameObject)
        {
            _currentSnapZone.Highlight(false);
            _currentSnapZone = null;
        }
    }
    #endregion
}
