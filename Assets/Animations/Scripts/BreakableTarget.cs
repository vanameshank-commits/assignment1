using System.Collections;
using UnityEngine;

public class BreakableTarget : MonoBehaviour, IBreakable
{
    [Header("VFX & SFX")]
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private AudioClip breakSFX;

    [Header("Physics Impulses")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 3f;

    public void Break(Vector3 hitPoint, Vector3 hitDirection)
    {
        // 1. Play Break Audio
        if (breakSFX != null)
        {
            AudioSource.PlayClipAtPoint(breakSFX, hitPoint, 1.0f);
        }

        // 2. Trigger Camera Shake
        CameraJuice.Instance?.Shake(0.1f, 0.25f);

        // 3. Spawn Wood Particle Burst
        if (explosionVFXPrefab != null)
        {
            Instantiate(explosionVFXPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
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
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Renderer>(out var ren)) ren.enabled = false;

        // Run hit-stop while object is still active in hierarchy
        StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.03f);
        Time.timeScale = 1.0f;

        // Safely deactivate GameObject after time scale is restored
        gameObject.SetActive(false);
    }
}