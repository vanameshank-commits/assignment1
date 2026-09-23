using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class CompletePlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 7.0f;

    [Header("Dash Attack Physics")]
    [SerializeField] private float dashDistance = 8.0f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private float playerRadius = 0.5f;
    [SerializeField] private float skinWidth = 0.05f;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask solidObstacleMask;
    [SerializeField] private LayerMask breakableMask;

    private Rigidbody rb;
    private Animator animator;

    private bool canDash = true;
    private bool isDashing = false;

    // Animator Parameter Hashes (Optimized lookup)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashTriggerHash = Animator.StringToHash("DashTrigger");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Physics Safeguards
        rb.freezeRotation = true;
        rb.useGravity = false; // Set to true if gravity is required for your setup
    }

    private void Update()
    {
        // Lock controls during active dash state
        if (isDashing) return;

        HandleLocomotionInput();
        HandleCombatInput();
    }

    private void HandleLocomotionInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(moveX, 0f, moveZ).normalized;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Movement Speed Selection
        float currentSpeedMultiplier = isSprinting ? runSpeed : walkSpeed;
        Vector3 movementVelocity = inputDir * currentSpeedMultiplier;
        
        rb.linearVelocity = new Vector3(movementVelocity.x, rb.linearVelocity.y, movementVelocity.z);

        // Rotation Smoothing
        if (inputDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(inputDir), Time.deltaTime * 14f);
        }

        // Drive Blend Tree Parameter: 0.0 = Idle, 0.5 = Walk, 1.0 = Run
        float targetAnimSpeed = 0f;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            targetAnimSpeed = isSprinting ? 1.0f : 0.5f;
        }

        animator.SetFloat(SpeedHash, targetAnimSpeed, 0.05f, Time.deltaTime);

        // Space Bar -> Initiate Dash Attack
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            Vector3 dashDirection = inputDir.sqrMagnitude > 0.01f ? inputDir : transform.forward;
            StartCoroutine(ExecuteDashAttack(dashDirection));
        }
    }

    private void HandleCombatInput()
    {
        // Right Click -> Standalone Melee Attack
        if (Input.GetMouseButtonDown(1))
        {
            animator.ResetTrigger(AttackTriggerHash);
            animator.SetTrigger(AttackTriggerHash);
        }
    }

    private IEnumerator ExecuteDashAttack(Vector3 direction)
    {
        canDash = false;
        isDashing = true;

        // Instantly align player rotation to direction vector
        transform.rotation = Quaternion.LookRotation(direction);

        // Cleanly reset and fire dash trigger
        animator.ResetTrigger(DashTriggerHash);
        animator.SetTrigger(DashTriggerHash);

        try
        {
            Vector3 startPos = transform.position;
            float effectiveDistance = dashDistance;

            // Elevate cast origin to $Y = 1.0$ to prevent sphere radius from sweeping into the floor plane
            Vector3 castOrigin = startPos + Vector3.up * 1.0f;

            // 1. Point-blank overlap check for immediate collision bounds
            Collider[] immediateOverlaps = Physics.OverlapSphere(startPos, playerRadius, breakableMask);
            foreach (var col in immediateOverlaps)
            {
                if (col != null && col.TryGetComponent<IBreakable>(out var breakable))
                {
                    breakable.Break(startPos, direction);
                }
            }

            // 2. Predictive Swept Raycasting using ground-safe origin
            LayerMask pathMask = solidObstacleMask | breakableMask;
            RaycastHit[] hits = Physics.SphereCastAll(
                castOrigin,
                playerRadius,
                direction,
                dashDistance,
                pathMask,
                QueryTriggerInteraction.Collide
            );

            // Sort hits chronologically by distance
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Filter out self and child colliders
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    continue;

                int hitLayer = 1 << hit.collider.gameObject.layer;

                // Hit Solid Obstacle -> Clamp maximum dash movement distance
                if ((hitLayer & solidObstacleMask) != 0)
                {
                    effectiveDistance = Mathf.Max(0f, hit.distance - (playerRadius + skinWidth));
                    Debug.Log($"[Dash] Wall Hit: '{hit.collider.name}'. Clamped Distance: {effectiveDistance}");
                    break; // Stop parsing objects beyond solid wall
                }

                // Hit Breakable Object within valid reach -> Shatter target
                if ((hitLayer & breakableMask) != 0 && hit.distance <= effectiveDistance)
                {
                    if (hit.collider.TryGetComponent<IBreakable>(out var breakable))
                    {
                        breakable.Break(hit.point, direction);
                    }
                }
            }

            // 3. Synchronized Physical Interpolation
            Vector3 endPos = startPos + (direction * effectiveDistance);
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;
                rb.MovePosition(Vector3.Lerp(startPos, endPos, t));
                yield return null;
            }

            rb.MovePosition(endPos);
        }
        finally
        {
            // Guarantees state unlock even if errors occur during physics operations
            isDashing = false;
        }

        // Cooldown timer recovery
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizes ground-safe cast origin and player radius in Scene View
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(origin, playerRadius);
        Gizmos.DrawRay(origin, transform.forward * dashDistance);
    }
}
