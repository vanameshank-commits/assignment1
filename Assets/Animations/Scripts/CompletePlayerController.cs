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

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashTriggerHash = Animator.StringToHash("DashTrigger");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

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
        if (isDashing) return;

        HandleLocomotionInput();
        HandleCombatInput();
    }

    private void HandleLocomotionInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(moveX, 0f, moveZ).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && stamina.CanRun();
        float currentSpeedMultiplier = isSprinting ? runSpeed : walkSpeed;
        Vector3 movementVelocity = inputDir * currentSpeedMultiplier;

        if (isSprinting && inputDir.sqrMagnitude > 0.01f)
        {
            stamina.DrainStaminaForRunning();
        }

        rb.linearVelocity = new Vector3(movementVelocity.x, rb.linearVelocity.y, movementVelocity.z);

        if (inputDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(inputDir), Time.deltaTime * 14f);
        }

        float targetAnimSpeed = 0f;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            targetAnimSpeed = isSprinting ? 1.0f : 0.5f;
        }

        animator.SetFloat(SpeedHash, targetAnimSpeed, 0.05f, Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && canDash && stamina.CanDash())
        {
            stamina.ConsumeDashStamina();
            Vector3 dashDirection = inputDir.sqrMagnitude > 0.01f ? inputDir : transform.forward;
            StartCoroutine(ExecuteDashAttack(dashDirection));
        }
    }

    private void HandleCombatInput()
    {
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

        transform.rotation = Quaternion.LookRotation(direction);

        animator.ResetTrigger(DashTriggerHash);
        animator.SetTrigger(DashTriggerHash);

        if (dashDustPrefab != null)
        {
            GameObject dashVFX = Instantiate(dashDustPrefab, transform.position, Quaternion.LookRotation(-direction), transform);
            Destroy(dashVFX, 1f);
        }

        if (TryGetComponent<DashAfterimage>(out var afterimage))
        {
            afterimage.TriggerGhostTrail();
        }

        try
        {
            Vector3 startPos = transform.position;
            float effectiveDistance = dashDistance;

            // Ground-level origin (Y = 0.5) to reliably hit crates resting on floor
            Vector3 castOrigin = startPos + Vector3.up * 0.5f;

            // 1. Check point-blank wall directly in front at start
            if (Physics.Raycast(castOrigin, direction, out RaycastHit immediateSolidHit, playerRadius + skinWidth, solidObstacleMask, QueryTriggerInteraction.Ignore))
            {
                effectiveDistance = 0f;
            }

            List<PendingBreakable> pendingBreakables = new List<PendingBreakable>();
            HashSet<IBreakable> trackedBreakables = new HashSet<IBreakable>();

            if (effectiveDistance > 0f)
            {
                // 2. Point-blank breakable overlap check at start point
                Collider[] immediateOverlaps = Physics.OverlapSphere(castOrigin, playerRadius, breakableMask);
                foreach (var col in immediateOverlaps)
                {
                    if (col != null)
                    {
                        IBreakable breakable = col.GetComponentInParent<IBreakable>();
                        if (breakable != null && trackedBreakables.Add(breakable))
                        {
                            pendingBreakables.Add(new PendingBreakable(breakable, 0.0f, startPos));
                        }
                    }
                }

                // 3. Predictive Swept SphereCast along path
                LayerMask pathMask = solidObstacleMask | breakableMask;
                RaycastHit[] hits = Physics.SphereCastAll(
                    castOrigin,
                    playerRadius,
                    direction,
                    dashDistance,
                    pathMask,
                    QueryTriggerInteraction.Ignore
                );

                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                        continue;

                    int hitLayer = 1 << hit.collider.gameObject.layer;

                    // Solid Obstacle -> Stop dash movement before reaching wall collider
                    if ((hitLayer & solidObstacleMask) != 0)
                    {
                        float stopDistance = Mathf.Max(0f, hit.distance - skinWidth);
                        effectiveDistance = Mathf.Min(effectiveDistance, stopDistance);
                        break; // Ignore targets beyond solid wall
                    }

                    // Breakable Obstacle -> Queue for destruction if reached before solid obstacle
                    if ((hitLayer & breakableMask) != 0)
                    {
                        if (hit.distance <= effectiveDistance)
                        {
                            IBreakable breakable = hit.collider.GetComponentInParent<IBreakable>();
                            if (breakable != null && trackedBreakables.Add(breakable))
                            {
                                pendingBreakables.Add(new PendingBreakable(breakable, hit.distance, hit.point));
                            }
                        }
                    }
                }
            }

            // 4. Smooth Physical Interpolation & Distance-Synced Shattering
            Vector3 endPos = startPos + (direction * effectiveDistance);
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                rb.MovePosition(currentPos);

                float currentDistanceTraveled = Vector3.Distance(startPos, currentPos);

                for (int i = pendingBreakables.Count - 1; i >= 0; i--)
                {
                    if (currentDistanceTraveled >= pendingBreakables[i].distance)
                    {
                        pendingBreakables[i].breakable.Break(pendingBreakables[i].hitPoint, direction);
                        pendingBreakables.RemoveAt(i);
                    }
                }

                yield return null;
            }

            rb.MovePosition(endPos);

            // Shatter any remaining breakables reached at end position
            foreach (var item in pendingBreakables)
            {
                item.breakable.Break(item.hitPoint, direction);
            }
        }
        finally
        {
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
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawWireSphere(origin, playerRadius);
        Gizmos.DrawRay(origin, transform.forward * dashDistance);
    }
}