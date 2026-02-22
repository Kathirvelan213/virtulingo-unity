using UnityEngine;

public class MainPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 2f;
    public float gravity = -9.8f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- Mouse rotation (Y axis only) ---
        float mouseX = Input.GetAxis("Mouse X") * 200f * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // --- Movement input ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(x, 0f, z).normalized;

        // Get camera forward/right (ignore vertical tilt)
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        // Camera-relative movement
        Vector3 moveDirection = cameraForward * z + cameraRight * x;

        // Rotate character toward movement direction
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // Move character
        controller.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);

        // Smooth animation blending
        float targetSpeed = inputDirection.magnitude;
        float currentSpeed = animator.GetFloat("Speed");
        float smoothSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 10f * Time.deltaTime);
        animator.SetFloat("Speed", smoothSpeed);

        animator.SetBool("IsGrounded", isGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
