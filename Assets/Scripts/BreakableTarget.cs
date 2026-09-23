using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableTarget : MonoBehaviour, IBreakable
{
    [Header("Optional Polish")]
    [SerializeField] private GameObject brokenVFXPrefab;
    [SerializeField] private AudioClip breakSFX;

    public void Break(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (brokenVFXPrefab != null)
        {
            Instantiate(brokenVFXPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
        }

        if (breakSFX != null)
        {
            AudioSource.PlayClipAtPoint(breakSFX, hitPoint);
        }

        // Disable object to complete destruction logic
        gameObject.SetActive(false);
    }
}
