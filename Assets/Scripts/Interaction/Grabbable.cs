using UnityEngine;

public enum GrabbableState
{
    Idle,
    Grabbed,
    Snapped
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Grabbable : MonoBehaviour, IInteractable
{
    #region Variables
    private Renderer _renderer;
    private Color _originalColor;

    private bool _isGrabbed = false;
    private Transform _grabberTransform;
    private Rigidbody _rigidbody;

    private int _originalLayer;
    private Connector _connector;
    private FixedJoint _joint;
    #endregion

    public GrabbableState CurrentState { get; private set; } = GrabbableState.Idle;
    public SnapZone CurrentSnapZone { get; private set; }

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        _rigidbody = GetComponent<Rigidbody>();
        _originalLayer = gameObject.layer;

        _connector = GetComponentInChildren<Connector>();
    }

    private void FixedUpdate()
    {
        if (_isGrabbed && _grabberTransform != null)
        {
            MoveToGrabber();
        }
    }

    public void OnHoverEnter()
    {
        if (CurrentState == GrabbableState.Idle)
        {
            HighLight();
        }
    }

    public void OnHoverExit()
    {
        if (_renderer != null) UnHighlight();
    }

    private void HighLight() 
    { 
        if (_renderer != null) _renderer.material.color = Color.yellow;
    }
    private void UnHighlight() 
    { 
        if (_renderer != null) _renderer.material.color = _originalColor;
    }
    public void OnSelectStart()
    {
        Debug.Log($"Object {gameObject.name} selected.");
        _isGrabbed = true;
        CurrentState = GrabbableState.Grabbed;
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        gameObject.layer = LayerMask.NameToLayer("GrabbedObject");
    }

    public void OnSelectEnd()
    {
        Debug.Log($"Object {gameObject.name} released.");
        _isGrabbed = false;
        _grabberTransform = null;
        CurrentState = GrabbableState.Idle;
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        gameObject.layer = _originalLayer;
    }

    // --- Logic Gắn/Tháo ---
    public void SnapTo(Transform snapPoint)
    {
        if (_connector == null) return;

        Debug.Log($"Snapping {gameObject.name} to {snapPoint.name}");

        CurrentState = GrabbableState.Snapped;
        _isGrabbed = false;
        _grabberTransform = null;

        CurrentSnapZone = snapPoint.GetComponent<SnapZone>();

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        Collider connectorCollider = _connector.GetComponent<Collider>();
        if (connectorCollider != null) connectorCollider.enabled = false;

        transform.rotation = snapPoint.rotation * Quaternion.Inverse(_connector.transform.localRotation);
        transform.position = snapPoint.position - (transform.rotation * _connector.transform.localPosition);

        if (connectorCollider != null) connectorCollider.enabled = true;

        _joint = gameObject.AddComponent<FixedJoint>();
        if (snapPoint.TryGetComponent<Rigidbody>(out var snapZoneRigidbody))
        {
            _joint.connectedBody = snapZoneRigidbody;
        }
        gameObject.layer = _originalLayer;
    }

    public void Unsnap()
    {
        Debug.Log($"Unsnapping {gameObject.name}");
        if (_joint != null)
        {
            Destroy(_joint);
        }
        CurrentSnapZone = null;
    }

    public void SetGrabber(Transform grabber)
    {
        _grabberTransform = grabber;
    }

    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    private void MoveToGrabber()
    {
        Vector3 currentPosition = _rigidbody.position;
        Vector3 targetPosition = _grabberTransform.position;
        Vector3 movementVector = targetPosition - currentPosition;
        float distance = movementVector.magnitude;

        if (distance <= 0) return;

        RaycastHit hitInfo;
        if (_rigidbody.SweepTest(movementVector.normalized, out hitInfo, distance, QueryTriggerInteraction.Ignore))
        {
            _rigidbody.MovePosition(currentPosition + movementVector.normalized * hitInfo.distance);
        }
        else
        {
            _rigidbody.MovePosition(targetPosition);
        }
    }
}