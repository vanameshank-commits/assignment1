using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class Mccontrol : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Physics")]
    public float gravity = -9.81f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    // Used to smooth the transition in the Blend Tree
    private float currentSpeedPercent;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovement();
      //  ApplyGravity();
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate movement direction
        Vector3 moveDirection = transform.right * x + transform.forward * z;

        // Determine if the player is pressing the run button (Left Shift)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Target speed based on input
        float targetSpeed = 0f;
        if (moveDirection.magnitude > 0.1f)
        {
            targetSpeed = isRunning ? runSpeed : walkSpeed;
        }

        // Move the CharacterController
        controller.Move(moveDirection.normalized * targetSpeed * Time.deltaTime);

        // Calculate a percentage (0 to 1) for the Animator Blend Tree
        // 0 = Idle, 0.5 (approx) = Walk, 1 = Run
        float targetSpeedPercent = targetSpeed / runSpeed;

        // Smoothly transition the speed percentage
        currentSpeedPercent = Mathf.Lerp(currentSpeedPercent, targetSpeedPercent, Time.deltaTime * 8f);

        // Send to Animator
        animator.SetFloat("Speed", currentSpeedPercent);
    }

    //void ApplyGravity()
   //{
   //     // Keep the player grounded
   //     if (controller.isGrounded && velocity.y < 0)
   //     {
   //         velocity.y = -2f;
   //     }

   //     // Apply gravity over time
   //     velocity.y += gravity * Time.deltaTime;
   //     controller.Move(velocity * Time.deltaTime);
   // }
}