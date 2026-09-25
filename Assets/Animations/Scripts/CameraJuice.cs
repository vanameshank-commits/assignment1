using System.Collections;
using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    public static CameraJuice Instance { get; private set; }

    private Vector3 originalPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float duration = 0.1f, float magnitude = 0.15f)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
