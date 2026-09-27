using System.Collections;
using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    public static CameraJuice Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        if (camTransform == null) yield break;

        Vector3 startLocalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = startLocalPos + new Vector3(x, y, 0f);

            // Use unscaledDeltaTime so the camera shakes even during time-freeze hit-stop
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        camTransform.localPosition = startLocalPos;
    }
}
