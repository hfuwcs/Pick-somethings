using UnityEngine;
using UnityEngine.InputSystem;
public class InteractionController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Khoảng cách tối đa mà tia raycast có thể vươn tới.")]
    [SerializeField] private float _interactionDistance = 100f;

    [Tooltip("Layer mask để chỉ định các layer mà tia raycast sẽ tương tác.")]
    [SerializeField] private LayerMask _interactionLayerMask;

    [Tooltip("Điểm mà đối tượng được cầm nắm sẽ di chuyển theo. Nếu để trống, sẽ dùng chính transform của controller này.")]
    [SerializeField] private Transform _grabAttachPoint;

    [Header("Grabbing Logic")]
    [Tooltip("Khoảng cách tối đa mà đối tượng có thể được giữ trước khi tự động thả.")]
    [SerializeField] private float _maxGrabDistance = 3f;

    private Camera _mainCamera;
    private IInteractable _currentHoveredInteractable;
    private IInteractable _currentSelectedInteractable;
    private SnapZone _potentialSnapZone;
    private bool _isUIMode = false;

    #region SnapZone Events
    private void OnEnable()
    {
        SnapZone.OnSnapZoneEnter += HandleSnapZoneEnter;
        SnapZone.OnSnapZoneExit += HandleSnapZoneExit;
    }

    private void OnDisable()
    {
        SnapZone.OnSnapZoneEnter -= HandleSnapZoneEnter;
        SnapZone.OnSnapZoneExit -= HandleSnapZoneExit;
    }
    private void HandleSnapZoneEnter(SnapZone zone, Grabbable grabbable)
    {
        if ((Object)_currentSelectedInteractable == (Object)grabbable)
        {
            Debug.Log($"InteractionController detected potential snap for {grabbable.name} at {zone.name}");
            _potentialSnapZone = zone;
        }
    }

    private void HandleSnapZoneExit(SnapZone zone)
    {
        if (_potentialSnapZone == zone)
        {
            Debug.Log("InteractionController lost potential snap zone.");
            _potentialSnapZone = null;
        }
    }
    #endregion

    private void Start()
    {
        ToggleCursorLock(false);
    }
    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_grabAttachPoint == null)
        {
            _grabAttachPoint = transform;
        }
    }

    private void Update()
    {
        if (_isUIMode)
        {
            if (_currentHoveredInteractable != null)
            {
                _currentHoveredInteractable.OnHoverExit();
                _currentHoveredInteractable = null;
            }
            return;
        }
        HandleHoverDetection();
        CheckForGrabbedObjectBreak();
    }
    public void OnToggleCursor(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isUIMode = !_isUIMode;
            ToggleCursorLock(_isUIMode);
        }
    }
    private void ToggleCursorLock(bool isUIMode)
    {
        if (isUIMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    private void CheckForGrabbedObjectBreak()
    {
        if (_currentSelectedInteractable == null) return;

        Rigidbody grabbedRigidbody = null;
        if (_currentSelectedInteractable is Grabbable grabbable)
        {
            grabbedRigidbody = grabbable.GetRigidbody();
        }

        if (grabbedRigidbody == null) return;

        float distance = Vector3.Distance(_grabAttachPoint.position, grabbedRigidbody.position);

        if (distance > _maxGrabDistance)
        {
            _currentSelectedInteractable.OnSelectEnd();
            _currentSelectedInteractable = null;
        }
    }
    private void HandleHoverDetection()
    {
        // If currently holding an object, skip hover detection.
        if (_currentSelectedInteractable != null)
        {
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenter);
        Debug.DrawRay(ray.origin, ray.direction * _interactionDistance, Color.cyan);
        RaycastHit hit;
        IInteractable newHoveredInteractable = null;

        if (Physics.Raycast(ray, out hit, _interactionDistance, _interactionLayerMask))
        {
            IClickable clickable = hit.collider.GetComponentInParent<IClickable>();
            if (clickable != null)
            {
                // Nếu tìm thấy, ép kiểu nó thành IInteractable và sử dụng nó.
                // Điều này hoạt động vì Switch (IClickable) cũng là một IInteractable.
                newHoveredInteractable = clickable as IInteractable;
            }
            else
            {
                // Nếu không có IClickable, quay lại tìm IInteractable chung (cho Grabbable, Connector).
                newHoveredInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }
        }


        if (newHoveredInteractable != _currentHoveredInteractable)
        {
            // Hover exit on the previous interactable
            _currentHoveredInteractable?.OnHoverExit();

            // Hover enter on the new interactable
            _currentHoveredInteractable = newHoveredInteractable;
            _currentHoveredInteractable?.OnHoverEnter();
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || _isUIMode) return;
        if (_currentHoveredInteractable is IClickable clickable &&
            clickable.AssociatedGrabbable != null &&
            clickable.AssociatedGrabbable.CurrentState == GrabbableState.Anchored)
        {
            clickable.OnClick();
            return; // Hành động đã được xử lý, kết thúc.
        }

        // ƯU TIÊN 2: Xử lý kéo dây
        if (_currentHoveredInteractable is Connector clickedConnector && clickedConnector.IsInteractableForWiring)
        {
            WiringManager.Instance.HandleConnectorClick(clickedConnector);
            return; // Hành động đã được xử lý, kết thúc.
        }

        // ƯU TIÊN 3: Logic Cầm/Thả/Snap mặc định
        if (_currentSelectedInteractable is Grabbable grabbable)
        {
            if (_potentialSnapZone != null && (grabbable.CurrentState == GrabbableState.Grabbed || grabbable.CurrentState == GrabbableState.ConstrainedGrab))
            {
                grabbable.AttemptSnap(_potentialSnapZone);
            }
            else
            {
                _currentSelectedInteractable.OnSelectEnd();
                _currentSelectedInteractable = null;
            }
        }
        else if (_currentHoveredInteractable != null)
        {
            _currentSelectedInteractable = _currentHoveredInteractable;
            _currentSelectedInteractable.OnSelectStart();

            if (_currentSelectedInteractable is Grabbable newGrabbable)
            {
                newGrabbable.SetGrabber(_grabAttachPoint);
            }
        }
    }
    public void OnSecondaryInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        WiringManager.Instance.CancelWiring();
        // TODO: Thêm logic xóa dây đã hoàn thành ở đây
    }
    public void OnUnsnap(InputAction.CallbackContext context)
    {
        if (!context.performed || _isUIMode) return;
        if (_currentSelectedInteractable != null) return;
        var experimentManager = FindFirstObjectByType<PendulumExperimentManager>();

        // Nếu có một Experiment Manager và nó đang chạy, thì không cho phép Unsnap.
        if (experimentManager != null && experimentManager.CurrentState == ExperimentManagerBase.ExperimentState.Running)
        {
            Debug.LogWarning("Không thể tháo vật thể khi thí nghiệm đang chạy. Vui lòng Reset thí nghiệm trước.");
            return;
        }

        if (_currentHoveredInteractable is Grabbable hoveredGrabbable)
        {
            if (hoveredGrabbable.CurrentState == GrabbableState.Snapped || hoveredGrabbable.CurrentState == GrabbableState.Anchored)
            {
                SnapZone previousSnapZone = hoveredGrabbable.CurrentSnapZone;
                if (hoveredGrabbable.CurrentSnapZone != null)
                    Debug.Log("[SnapZone]", hoveredGrabbable.CurrentSnapZone);

                if (previousSnapZone != null)
                {
                    previousSnapZone.ClearSnappedObject();
                }

                hoveredGrabbable.AttemptUnsnap();
                _currentSelectedInteractable = hoveredGrabbable;
                hoveredGrabbable.OnSelectStart();
                hoveredGrabbable.SetGrabber(_grabAttachPoint);
            }
        }
    }
}
