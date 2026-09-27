using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(StaminaSystem))]
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

    [Header("Visual Effects")]
    [SerializeField] private GameObject dashDustPrefab;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask solidObstacleMask;
    [SerializeField] private LayerMask breakableMask;

    [Header("UI References")]
    [SerializeField] private Image dashCooldownRing;

    private Rigidbody rb;
    private Animator animator;
    private StaminaSystem stamina;

    private bool canDash = true;
    private bool isDashing = false;

    // Animator Parameter Hashes (Optimized lookup)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashTriggerHash = Animator.StringToHash("DashTrigger");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

    // Internal struct to queue breakable objects for mid-dash dynamic shattering
    private struct PendingBreakable
    {
        public IBreakable breakable;
        public float distance;
        public Vector3 hitPoint;

        public PendingBreakable(IBreakable breakable, float distance, Vector3 hitPoint)
        {
            this.breakable = breakable;
            this.distance = distance;
            this.hitPoint = hitPoint;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        stamina = GetComponent<StaminaSystem>();

        // Physics Safeguards
        rb.freezeRotation = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        // Hide the cooldown ring when the game starts because the dash is fully ready
        if (dashCooldownRing != null)
        {
            dashCooldownRing.gameObject.SetActive(false);
        }
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

        // Stamina-gated sprinting check
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && stamina.CanRun();

        // Movement Speed Selection
        float currentSpeedMultiplier = isSprinting ? runSpeed : walkSpeed;
        Vector3 movementVelocity = inputDir * currentSpeedMultiplier;

        // Drain stamina if actively moving and sprinting
        if (isSprinting && inputDir.sqrMagnitude > 0.01f)
        {
            stamina.DrainStaminaForRunning();
        }

        // Linear velocity support for Unity 6+ (Use rb.velocity on older Unity versions)
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

        // Space Bar -> Initiate Dash Attack (Stamina checked and consumed)
        if (Input.GetKeyDown(KeyCode.Space) && canDash && stamina.CanDash())
        {
            stamina.ConsumeDashStamina();
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

        // Instantly align player rotation to dash vector
        transform.rotation = Quaternion.LookRotation(direction);

        // Reset and fire dash animation trigger
        animator.ResetTrigger(DashTriggerHash);
        animator.SetTrigger(DashTriggerHash);

        // Trigger footstep/dash dust burst facing opposite to movement vector
        if (dashDustPrefab != null)
        {
            GameObject dashVFX = Instantiate(dashDustPrefab, transform.position, Quaternion.LookRotation(-direction), transform);
            Destroy(dashVFX, 1f);
        }

        // Trigger visual ghost trail if script component exists on Player
        if (TryGetComponent<DashAfterimage>(out var afterimage))
        {
            afterimage.TriggerGhostTrail();
        }

        try
        {
            Vector3 startPos = transform.position;
            float effectiveDistance = dashDistance;

            // Elevate cast origin to Y = 1.0 to prevent sphere radius from sweeping into the floor plane
            Vector3 castOrigin = startPos + Vector3.up * 1.0f;

            List<PendingBreakable> pendingBreakables = new List<PendingBreakable>();
            HashSet<IBreakable> trackedBreakables = new HashSet<IBreakable>();

            // 1. Point-blank overlap check (Queued for frame 1 of movement to prevent premature destruction)
            Collider[] immediateOverlaps = Physics.OverlapSphere(startPos, playerRadius, breakableMask);
            foreach (var col in immediateOverlaps)
            {
                if (col != null)
                {
                    IBreakable breakable = col.GetComponentInParent<IBreakable>();
                    if (breakable != null && trackedBreakables.Add(breakable))
                    {
                        pendingBreakables.Add(new PendingBreakable(breakable, 0.1f, startPos));
                    }
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
                    break; // Stop parsing targets beyond the solid wall
                }

                // Store breakables to shatter dynamically when player physically reaches them
                if ((hitLayer & breakableMask) != 0 && hit.distance <= effectiveDistance)
                {
                    IBreakable breakable = hit.collider.GetComponentInParent<IBreakable>();
                    if (breakable != null && trackedBreakables.Add(breakable))
                    {
                        pendingBreakables.Add(new PendingBreakable(breakable, hit.distance, hit.point));
                    }
                }
            }

            // 3. Physical Movement & Distance-Synced Shattering
            Vector3 endPos = startPos + (direction * effectiveDistance);
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                rb.MovePosition(currentPos);

                // Calculate distance traveled from start position
                float currentDistanceTraveled = Vector3.Distance(startPos, currentPos);

                // Check if player has physically arrived at queued breakable targets
                for (int i = pendingBreakables.Count - 1; i >= 0; i--)
                {
                    if (currentDistanceTraveled >= pendingBreakables[i].distance)
                    {
                        pendingBreakables[i].breakable.Break(pendingBreakables[i].hitPoint, direction);
                        pendingBreakables.RemoveAt(i); // Remove so object breaks only once
                    }
                }

                yield return null;
            }

            rb.MovePosition(endPos);

            // Shatter any remaining queued items at final destination
            foreach (var item in pendingBreakables)
            {
                item.breakable.Break(item.hitPoint, direction);
            }
        }
        finally
        {
            // Guarantees state unlock even if errors occur during physics operations
            isDashing = false;
        }

        // --- NEW UI COOLDOWN LOGIC ---
        // Make the UI visible and empty the ring the moment the dash finishes
        if (dashCooldownRing != null)
        {
            dashCooldownRing.gameObject.SetActive(true);
            dashCooldownRing.fillAmount = 0f;
        }

        // Cooldown timer recovery with UI fill
        float cooldownTimer = 0f;
        while (cooldownTimer < dashCooldown)
        {
            cooldownTimer += Time.deltaTime;

            if (dashCooldownRing != null)
            {
                // Calculates the percentage from 0.0 to 1.0 to fill the circle smoothly
                dashCooldownRing.fillAmount = cooldownTimer / dashCooldown;
            }

            yield return null; // Wait until the next frame
        }

        // Hide the UI entirely when the cooldown is done
        if (dashCooldownRing != null)
        {
            dashCooldownRing.gameObject.SetActive(false);
        }

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