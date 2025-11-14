using System.Linq;
using UnityEngine;

public enum GrabbableState
{
    Idle,           // Trạng thái nghỉ, tuân theo vật lý thông thường
    Grabbed,        // Đang được cầm tự do
    Snapped,        // Đã được gắn vào một SnapZone
    ConstrainedGrab, // Đang được cầm trong khi vẫn bị ràng buộc (gắn vào SnapZone)
    Anchored //Object dành cho mạch điện.
}

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [Tooltip("Transform của khối lượng vật lý chính. Đây là điểm sẽ di chuyển đến vị trí cầm nắm. Nếu để trống, sẽ dùng transform của chính GameObject này.")]
    [SerializeField] private Transform _centerOfMassTransform;
    [SerializeField] public bool allowConstrainedGrab = true;

    public GrabbableState CurrentState { get; private set; } = GrabbableState.Idle;
    public SnapZone CurrentSnapZone { get; private set; }
    public bool WasJustReleased { get; private set; } = false;

    // --- References ---
    private Renderer _renderer;
    private Color _originalColor;
    private Transform _grabberTransform;
    private Rigidbody _rigidbody;
    private int _originalLayer;
    private IMultiPointSnappable _multiPointHandler;
    private Connector[] _connectors;
    private Joint _joint;

    // --- Strategy Pattern ---
    public IHoldStrategy HoldStrategy { get; private set; }

    #region Unity Methods
    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
        else Debug.LogWarning($"Grabbable '{gameObject.name}' không tìm thấy Renderer. Highlight sẽ không hoạt động.", this);

        _rigidbody = GetComponent<Rigidbody>();
        _originalLayer = gameObject.layer;
        _connectors = GetComponentsInChildren<Connector>();
        _multiPointHandler = GetComponent<IMultiPointSnappable>();

        if (_centerOfMassTransform == null)
        {
            _centerOfMassTransform = _renderer != null ? _renderer.transform : transform;
        }

        SetHoldStrategy(new FreeHoldStrategy());
    }

    private void FixedUpdate()
    {
        if ((CurrentState == GrabbableState.Grabbed || CurrentState == GrabbableState.ConstrainedGrab) && _grabberTransform != null)
        {
            HoldStrategy.Hold(_rigidbody, _grabberTransform, _centerOfMassTransform);
        }
    }
    #endregion

    #region IInteractable Implementation
    public void OnHoverEnter()
    {
        if (CurrentState == GrabbableState.Idle || CurrentState == GrabbableState.Snapped)
        {
            Highlight();
        }
    }

    public void OnHoverExit()
    {
        UnHighlight();
    }

    public void OnSelectStart()
    {

        if (CurrentState == GrabbableState.Snapped)
        {
            SetState(GrabbableState.ConstrainedGrab);
        }
        else if (CurrentState == GrabbableState.Idle)
        {
            SetState(GrabbableState.Grabbed);
        }
    }

    public void OnSelectEnd()
    {
        if (CurrentState == GrabbableState.Grabbed)
        {
            SetState(GrabbableState.Idle);
            // Reset velocity để tránh vật bay đi khi thả
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
        else if (CurrentState == GrabbableState.ConstrainedGrab)
        {
            SetState(GrabbableState.Snapped);
            WasJustReleased = true;

            // ✅ Reset velocity để tránh dao động khi thả ra
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // ✅ CHỈ WakeUp nếu không ở trạng thái kinematic (tránh kích hoạt physics khi đang setup)
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.WakeUp();
            }
        }
        _grabberTransform = null;
    }
    #endregion

    #region Public API for Interaction

    /// <summary>
    /// Được gọi bởi InteractionController khi người dùng muốn snap vật đang cầm.
    /// </summary>
    public void AttemptSnap(SnapZone potentialZone)
    {
        if (_connectors == null || _connectors.Length == 0) return;

        Connector closestConnector = _connectors
            .Where(c => c.ConnectedZone == null)
            .OrderBy(c => Vector3.Distance(c.transform.position, potentialZone.transform.position))
            .FirstOrDefault();

        if (closestConnector != null)
        {
            if (_multiPointHandler != null)
            {
                // Đối tượng mạch điện -> sử dụng logic multi-point
                _multiPointHandler.SnapPoint(closestConnector, potentialZone);
                potentialZone.Connect(closestConnector);

                // THAY ĐỔI CỐT LÕI: Đặt trạng thái mới
                SetState(GrabbableState.Anchored);
            }
            else
            {
                // Đối tượng con lắc -> sử dụng logic cũ
                SnapObjectTo(closestConnector, potentialZone);
            }
        }
    }

    /// <summary>
    /// Được gọi bởi InteractionController khi người dùng muốn unsnap vật đang trỏ vào.
    /// </summary>
    public void AttemptUnsnap()
    {
        if (CurrentState != GrabbableState.Snapped) return;

        var connectedConnectors = _connectors.Where(c => c.ConnectedZone != null).ToList();
        foreach (var connector in connectedConnectors)
        {
            connector.ConnectedZone.Disconnect(connector);
        }

        UnsnapInternalCleanup();
    }

    #endregion

    #region Internal Snap/Unsnap Logic

    private void SnapObjectTo(Connector connector, SnapZone snapZone)
    {
        _grabberTransform = null;

        transform.rotation = snapZone.transform.rotation * Quaternion.Inverse(connector.transform.localRotation);
        transform.position = snapZone.transform.position - (transform.rotation * connector.transform.localPosition);

        CreateJoint(snapZone);

        CurrentSnapZone = snapZone;

        SetState(GrabbableState.Snapped);

        snapZone.Connect(connector);
    }

    private void UnsnapInternalCleanup()
    {
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        // ✅ FIX: Clear CurrentSnapZone reference để cho phép snap lại
        CurrentSnapZone = null;

        SetHoldStrategy(new FreeHoldStrategy());
        SetState(GrabbableState.Idle);

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.WakeUp();
    }

    #endregion
    /// <summary>
    /// Cho phép một hệ thống bên ngoài (như ExperimentManager) cấu hình
    /// trạng thái vật lý của đối tượng khi nó đang ở trạng thái Snapped.
    /// </summary>
    /// <param name="isKinematic">Rigidbody có nên là kinematic hay không.</param>
    public void ConfigureSnappedPhysics(bool isKinematic)
    {
        if (CurrentState != GrabbableState.Snapped)
        {
            Debug.LogWarning("Chỉ có thể cấu hình vật lý khi đối tượng đang ở trạng thái Snapped.", this);
            return;
        }
        _rigidbody.isKinematic = isKinematic;
    }
    public void SetGrabber(Transform grabber) => _grabberTransform = grabber;
    public Rigidbody GetRigidbody() => _rigidbody;
    public void ConsumeReleaseFlag() => WasJustReleased = false;

    /// <summary>
    /// Cho phép các hệ thống bên ngoài thay đổi chiến lược cầm/giữ.
    /// </summary>
    public void SetHoldStrategy(IHoldStrategy strategy)
    {
        HoldStrategy = strategy;
    }

    #region Private Methods
    private void CreateJoint(SnapZone snapZone)
    {
        if (_joint != null)
        {
            Destroy(_joint);
        }

        Rigidbody connectedBody = snapZone.GetComponent<Rigidbody>();

        if (connectedBody == null)
        {
            Debug.LogError($"SnapZone '{snapZone.name}' không có Rigidbody component. Joint không thể được tạo. Vui lòng thêm Rigidbody vào SnapZone.", snapZone);
            return;
        }

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

    private void SetState(GrabbableState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        ApplyPhysicsProfile(CurrentState);
    }

    private void ApplyPhysicsProfile(GrabbableState state)
    {
        switch (state)
        {
            case GrabbableState.Idle:
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                gameObject.layer = _originalLayer;
                break;

            case GrabbableState.Grabbed:
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                gameObject.layer = LayerMask.NameToLayer("GrabbedObject");
                break;

            case GrabbableState.ConstrainedGrab:
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                break;

            case GrabbableState.Snapped:
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                gameObject.layer = _originalLayer;
                break;

            case GrabbableState.Anchored:
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                gameObject.layer = _originalLayer;
                break;
        }
    }

    private void Highlight() { if (_renderer != null) _renderer.material.color = Color.yellow; }
    private void UnHighlight() { if (_renderer != null) _renderer.material.color = _originalColor; }
    #endregion
}