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
        if (_currentHoveredInteractable != null && _currentSelectedInteractable != null)
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
            newHoveredInteractable = hit.collider.GetComponent<IInteractable>();
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
        if (context.performed)
        {
            //If currently holding an object, release it.
            if (_currentSelectedInteractable != null)
            {
                _currentSelectedInteractable.OnSelectEnd();
                _currentSelectedInteractable = null;
            }
            //else, if hovering over an interactable, select it and picking it.
            else if (_currentHoveredInteractable != null)
            {
                _currentSelectedInteractable = _currentHoveredInteractable;
                _currentSelectedInteractable.OnSelectStart();

                if (_currentSelectedInteractable is Grabbable grabbable)
                {
                    grabbable.SetGrabber(_grabAttachPoint);
                }
            }
        }
    }
}
