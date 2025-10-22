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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        HandleHoverDetection();
        CheckForGrabbedObjectBreak();
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

        if(Physics.Raycast(ray, out hit, _interactionDistance, _interactionLayerMask))
        {
            newHoveredInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        
        if(newHoveredInteractable != _currentHoveredInteractable)
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
        if (!context.performed) return;

        if (_currentSelectedInteractable is Grabbable selectedGrabbable)
        {
            if (_potentialSnapZone != null)
            {
                selectedGrabbable.SnapTo(_potentialSnapZone);
                _potentialSnapZone.SetSnappedObject(selectedGrabbable);
                _currentSelectedInteractable = null;
                _potentialSnapZone = null;
            }
            else
            {
                selectedGrabbable.OnSelectEnd();
                _currentSelectedInteractable = null;
            }
        }
        else if (_currentHoveredInteractable is Grabbable hoveredGrabbable)
        {
            if (hoveredGrabbable.CurrentState == GrabbableState.Snapped)
            {
                if (hoveredGrabbable.CurrentSnapZone != null)
                {
                    hoveredGrabbable.CurrentSnapZone.ClearSnappedObject();
                }

                hoveredGrabbable.Unsnap();
                _currentSelectedInteractable = hoveredGrabbable; 
                hoveredGrabbable.OnSelectStart(); 

                if (hoveredGrabbable is Grabbable grabbable)
                {
                    grabbable.SetGrabber(_grabAttachPoint);
                }
            }
            else if (hoveredGrabbable.CurrentState == GrabbableState.Idle)
            {
                _currentSelectedInteractable = hoveredGrabbable;
                hoveredGrabbable.OnSelectStart();

                if (hoveredGrabbable is Grabbable grabbable)
                {
                    grabbable.SetGrabber(_grabAttachPoint);
                }
            }
        }
    }
}
