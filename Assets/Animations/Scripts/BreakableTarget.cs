using System.Collections;
using UnityEngine;

// Supports both Unity 6 (Unity.Cinemachine) and legacy Cinemachine 2.x
#if UNITY_2023_2_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

public class BreakableTarget : MonoBehaviour, IBreakable
{
    [Header("VFX & SFX")]
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private AudioClip breakSFX;

    [Header("Physics Impulses")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 3f;

    private Collider targetCollider;
    private Renderer targetRenderer;

    private void Awake()
    {
        targetCollider = GetComponent<Collider>();
        targetRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        // Reset collider and renderer visibility when re-activated by spawner
        if (targetCollider != null) targetCollider.enabled = true;
        if (targetRenderer != null) targetRenderer.enabled = true;
    }

    public void Break(Vector3 hitPoint, Vector3 hitDirection)
    {
        // Guard against multiple hits on the same frame if already broken
        if (targetCollider != null && !targetCollider.enabled) return;

        // 1. Play Break Audio
        if (breakSFX != null)
        {
            AudioSource.PlayClipAtPoint(breakSFX, hitPoint, 1.0f);
        }

        // 2. Trigger Cinemachine Impulse Shake & Legacy CameraJuice Fallback
        if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
        {
            impulse.GenerateImpulse();
        }
        else
        {
            CameraJuice.Instance?.Shake(0.1f, 0.25f);
        }

        // 3. Spawn Wood Particle Burst
        if (explosionVFXPrefab != null)
        {
            Quaternion spawnRotation = hitDirection != Vector3.zero
                ? Quaternion.LookRotation(hitDirection)
                : Quaternion.identity;

            Instantiate(explosionVFXPrefab, hitPoint, spawnRotation);
        }

        // 4. Spawn Physical Debris Rigidbodies
        if (debrisPrefab != null)
        {
            GameObject debrisInstance = Instantiate(debrisPrefab, transform.position, transform.rotation);
            Rigidbody[] rbs = debrisInstance.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rb in rbs)
            {
                rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius);
                rb.AddForce(hitDirection * (explosionForce * 0.1f), ForceMode.Impulse);
            }

            Destroy(debrisInstance, 3.0f);
        }

        // Hide visuals and colliders instantly so the object appears destroyed on frame 1
        if (targetCollider != null) targetCollider.enabled = false;
        if (targetRenderer != null) targetRenderer.enabled = false;

        // Run hit-stop while object is still active in hierarchy
        StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.03f);
        Time.timeScale = 1.0f;

        // Safely deactivate GameObject after time scale is restored (ready for spawner pooling)
        gameObject.SetActive(false);
    }
}