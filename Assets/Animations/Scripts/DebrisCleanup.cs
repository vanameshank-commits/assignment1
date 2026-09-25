using System.Collections;
using UnityEngine;

public class DebrisCleanup : MonoBehaviour
{
    [SerializeField] private float lifetime = 3.0f;
    [SerializeField] private float fadeDuration = 1.0f;

    private void Start()
    {
        StartCoroutine(CleanupRoutine());
    }

    private IEnumerator CleanupRoutine()
    {
        // Wait before starting the fade out
        yield return new WaitForSeconds(lifetime);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (var ren in renderers)
            {
                foreach (var mat in ren.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
