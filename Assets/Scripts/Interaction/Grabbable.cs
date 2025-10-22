using UnityEngine;

public enum GrabbableState
{
    Idle,
    Grabbed,
    Snapped,
    HoldingSnapped
}

[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(Collider))]
public class Grabbable : MonoBehaviour, IInteractable
{
    #region Variables
    private Renderer _renderer;
    private Color _originalColor;

    //private bool _isGrabbed = false;
    private Transform _grabberTransform;
    private Rigidbody _rigidbody;

    private int _originalLayer;
    private Connector _connector;
    private Joint _joint;
    #endregion

    [Header("Configuration")]
    [Tooltip("Transform của khối lượng vật lý chính. Đây là điểm sẽ di chuyển đến vị trí cầm nắm. Nếu để trống, sẽ dùng transform của chính GameObject này.")]
    [SerializeField] private Transform _centerOfMassTransform;

    public GrabbableState CurrentState { get; private set; } = GrabbableState.Idle;
    public SnapZone CurrentSnapZone { get; private set; }

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        else
        {
            Debug.LogWarning($"Grabbable '{gameObject.name}' không tìm thấy Renderer trên chính nó hoặc các đối tượng con. Chức năng highlight sẽ không hoạt động.", this);
        }

        _rigidbody = GetComponent<Rigidbody>();
        _originalLayer = gameObject.layer;
        _connector = GetComponentInChildren<Connector>();

        if (_centerOfMassTransform == null)
        {
            if (_renderer != null)
            {
                _centerOfMassTransform = _renderer.transform;
            }
            else
            {
                _centerOfMassTransform = transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (CurrentState == GrabbableState.Grabbed)
        {
            MoveToGrabber();
        }
        else if (CurrentState == GrabbableState.HoldingSnapped)
        {
            HoldSnappedObject();
        }
    }

    public void OnHoverEnter()
    {
        if (CurrentState == GrabbableState.Idle || CurrentState == GrabbableState.Snapped)
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
        if (CurrentState == GrabbableState.Snapped)
        {
            CurrentState = GrabbableState.HoldingSnapped;
        }
        else
        {
            CurrentState = GrabbableState.Grabbed;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            gameObject.layer = LayerMask.NameToLayer("GrabbedObject");
        }
    }

    public void OnSelectEnd()
    {
        if (CurrentState == GrabbableState.Grabbed)
        {
            _grabberTransform = null;
            CurrentState = GrabbableState.Idle;
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            gameObject.layer = _originalLayer;
        }
        else if (CurrentState == GrabbableState.HoldingSnapped)
        {
            _grabberTransform = null;
            CurrentState = GrabbableState.Snapped;
            _rigidbody.WakeUp();
        }
    }

    // --- Logic Gắn/Tháo ---
    public void SnapTo(SnapZone snapZone)
    {
        if (_connector == null) return;

        Debug.Log($"Snapping {gameObject.name} to {snapZone.name} with {snapZone.DesiredJointType} joint.");

        CurrentState = GrabbableState.Snapped;
        //_isGrabbed = false;
        _grabberTransform = null;

        CurrentSnapZone = snapZone;

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;

        transform.rotation = snapZone.transform.rotation * Quaternion.Inverse(_connector.transform.localRotation);
        transform.position = snapZone.transform.position - (transform.rotation * _connector.transform.localPosition);

        CreateJoint(snapZone);

        gameObject.layer = _originalLayer;
    }
    private void HoldSnappedObject()
    {
        if (_grabberTransform == null) return;


        Vector3 targetPosition;
        if (_centerOfMassTransform != transform)
        {
            Vector3 offset = transform.position - _centerOfMassTransform.position;
            targetPosition = _grabberTransform.position + offset;
        }
        else
        {
            targetPosition = _grabberTransform.position;
        }

        _rigidbody.MovePosition(targetPosition);
    }
    private void CreateJoint(SnapZone snapZone)
    {
        // Lấy Rigidbody của điểm gắn, nếu có
        Rigidbody connectedBody = snapZone.GetComponent<Rigidbody>();

        switch (snapZone.DesiredJointType)
        {
            case JointType.Fixed:
                FixedJoint fixedJoint = gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = connectedBody;
                _joint = fixedJoint;
                break;

            case JointType.Hinge:
                HingeJoint hingeJoint = gameObject.AddComponent<HingeJoint>();
                hingeJoint.connectedBody = connectedBody;
                // Anchor là vị trí của khớp nối trong không gian local của đối tượng này
                hingeJoint.anchor = transform.InverseTransformPoint(snapZone.transform.position);
                // Axis là trục xoay, ví dụ trục Z (0, 0, 1) cho phép xoay qua lại
                hingeJoint.axis = new Vector3(0, 0, 1);
                _joint = hingeJoint;
                break;

            case JointType.Configurable:
                ConfigurableJoint configJoint = gameObject.AddComponent<ConfigurableJoint>();
                configJoint.connectedBody = connectedBody;


                configJoint.xMotion = ConfigurableJointMotion.Locked;
                configJoint.yMotion = ConfigurableJointMotion.Locked;
                configJoint.zMotion = ConfigurableJointMotion.Locked;

                configJoint.angularXMotion = ConfigurableJointMotion.Free;
                configJoint.angularYMotion = ConfigurableJointMotion.Free;
                configJoint.angularZMotion = ConfigurableJointMotion.Free;

                _joint = configJoint;
                break;
        }
    }

    public void Unsnap()
    {
        Debug.Log($"Unsnapping {gameObject.name}");
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }
        CurrentSnapZone = null;
        CurrentState = GrabbableState.Idle;
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
        Vector3 targetPosition;

        if (_centerOfMassTransform != transform)
        {

            Vector3 offset = transform.position - _centerOfMassTransform.position;
            targetPosition = _grabberTransform.position + offset;
        }
        else
        {
            targetPosition = _grabberTransform.position;
        }
        Vector3 currentPosition = _rigidbody.position;
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