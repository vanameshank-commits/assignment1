using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(StaminaSystem))] // Add this line
public class BasicMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f; // Controls how fast the character turns

    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Camera Reference")]
    public Transform mainCamera; // Drag your Main Camera here

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float currentSpeedPercent;
    private StaminaSystem stamina;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Fallback: automatically find the main camera if you forget to assign it
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
        stamina = GetComponent<StaminaSystem>();
    }


    void Update()
    {
        HandleMovement();
        ApplyGravity();
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 1. Get the camera's forward and right vectors
        Vector3 camForward = mainCamera.forward;
        Vector3 camRight = mainCamera.right;

        // 2. Flatten them on the Y axis so we don't move up/down into the ground
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 3. Calculate the actual movement direction relative to the camera
        Vector3 moveDirection = camRight * x + camForward * z;

        // If the player is providing input
        // Only allow running if stamina is greater than 0
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && stamina.CanRun();
        float targetSpeed = 0f;

        if (moveDirection.magnitude > 0.1f)
        {
            targetSpeed = isRunning ? runSpeed : walkSpeed;

            // Drain stamina while physically running
            if (isRunning)
            {
                stamina.DrainStaminaForRunning();
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move the CharacterController
        controller.Move(moveDirection.normalized * targetSpeed * Time.deltaTime);

        // Update Animator Blend Tree
        float targetSpeedPercent = targetSpeed / runSpeed;
        currentSpeedPercent = Mathf.Lerp(currentSpeedPercent, targetSpeedPercent, Time.deltaTime * 8f);
        animator.SetFloat("Speed", currentSpeedPercent);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}