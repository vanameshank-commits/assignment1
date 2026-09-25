using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableTarget : MonoBehaviour, IBreakable
{
    [Header("FX Feedback")]
    [SerializeField] private GameObject brokenVFXPrefab;
    [SerializeField] private AudioClip breakSFX;

    [Header("3D Fracture Physics (Optional)")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 2f;

    public void Break(Vector3 hitPoint, Vector3 hitDirection)
    {
        // 1. Spawn Particle VFX
        if (brokenVFXPrefab != null)
        {
            Instantiate(brokenVFXPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
        }

        // 2. Play Audio SFX at impact location
        if (breakSFX != null)
        {
            AudioSource.PlayClipAtPoint(breakSFX, hitPoint, 1.0f);
        }

        // 3. Spawn 3D Exploding Physics Debris
        if (debrisPrefab != null)
        {
            GameObject debrisInstance = Instantiate(debrisPrefab, transform.position, transform.rotation);
            Rigidbody[] debrisRbs = debrisInstance.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rb in debrisRbs)
            {
                // Push fragments away from the impact point along dash direction
                rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius);
                rb.AddForce(hitDirection * (explosionForce * 0.5f), ForceMode.Impulse);
            }

            Destroy(debrisInstance, 4f); // Auto-cleanup debris after 4 seconds
        }

        // 4. Disable base object to complete shatter
        gameObject.SetActive(false);
    }
}