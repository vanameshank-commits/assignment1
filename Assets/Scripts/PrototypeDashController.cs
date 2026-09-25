using System;
using System.Collections;

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PrototypeDashController : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private float playerRadius = 0.5f;
    [SerializeField] private float skinWidth = 0.05f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask solidObstacleMask;
    [SerializeField] private LayerMask breakableMask;

    private Rigidbody rb;
    private bool isDashing;
    private bool canDash = true;
    private Vector3 movementInput;

    // Debugging Gizmos
    private Vector3 debugStartPos;
    private Vector3 debugEndPos;
    private bool debugDrawPath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Read movement input every frame
        movementInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        // Trigger dash when space is pressed
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            Vector3 dashDirection = movementInput != Vector3.zero ? movementInput : transform.forward;
            StartCoroutine(ExecuteDash(dashDirection));
        }
    }

    private void FixedUpdate()
    {
        // Apply regular movement when not dashing
        if (isDashing) return;

        if (movementInput.sqrMagnitude > 0f)
        {
            Vector3 move = movementInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);

            // Smoothly rotate to movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movementInput);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private IEnumerator ExecuteDash(Vector3 direction)
    {
        canDash = false;
        isDashing = true;

        Vector3 startPos = transform.position;
        float effectiveDistance = dashDistance;

        // Edge Case 1: Point-blank overlap check at current position
        Collider[] immediateOverlaps = Physics.OverlapSphere(startPos, playerRadius, breakableMask);
        foreach (var col in immediateOverlaps)
        {
            if (col.TryGetComponent<IBreakable>(out var breakable))
            {
                breakable.Break(startPos, direction);
            }
        }

        // Swept Volume Detection
        LayerMask pathMask = solidObstacleMask | breakableMask;
        RaycastHit[] hits = Physics.SphereCastAll(
            startPos,
            playerRadius,
            direction,
            dashDistance,
            pathMask,
            QueryTriggerInteraction.Collide
        );

        // Sort hits chronologically by distance
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Edge Case 2 & 3: Resolve Wall Bounds and Filter Occluded Breakables
        foreach (RaycastHit hit in hits)
        {
            int hitLayer = 1 << hit.collider.gameObject.layer;

            // Encountered Solid Wall -> Clamp maximum movement distance
            if ((hitLayer & solidObstacleMask) != 0)
            {
                effectiveDistance = Mathf.Max(0f, hit.distance - (playerRadius + skinWidth));
                break; // Ignore everything past this wall
            }

            // Encountered Breakable Object within valid distance
            if ((hitLayer & breakableMask) != 0 && hit.distance <= effectiveDistance)
            {
                if (hit.collider.TryGetComponent<IBreakable>(out var breakable))
                {
                    breakable.Break(hit.point, direction);
                }
            }
        }

        // Execute Smooth Interpolated Physics Position
        Vector3 endPos = startPos + (direction * effectiveDistance);

        debugStartPos = startPos;
        debugEndPos = endPos;
        debugDrawPath = true;

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dashDuration;

            rb.MovePosition(Vector3.Lerp(startPos, endPos, t));
            yield return null;
        }

        rb.MovePosition(endPos);
        isDashing = false;

        // Cooldown timer
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawPath) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(debugStartPos, debugEndPos);
        Gizmos.DrawWireSphere(debugStartPos, playerRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(debugEndPos, playerRadius);
    }
}
