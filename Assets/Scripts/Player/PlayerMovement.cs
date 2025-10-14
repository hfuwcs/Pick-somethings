using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 playerVelocity;

    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    private void Update()
    {
        //check if grounded
        bool isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y <0)
        {
            playerVelocity.y = -2f;
        }

        //moving on ground
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveDirection * speed * Time.deltaTime);

        //jumping and gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.performed && controller.isGrounded)
        {
            // v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }    
}
