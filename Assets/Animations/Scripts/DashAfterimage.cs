using System.Collections;
using UnityEngine;

public class DashAfterimage : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private SkinnedMeshRenderer playerMeshRenderer;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private float activeTime = 0.12f;
    [SerializeField] private float spawnRate = 0.03f;
    [SerializeField] private float fadeSpeed = 3f;

    public void TriggerGhostTrail()
    {
        StartCoroutine(SpawnTrailRoutine());
    }

    private IEnumerator SpawnTrailRoutine()
    {
        float elapsed = 0f;

        while (elapsed < activeTime)
        {
            elapsed += spawnRate;

            if (playerMeshRenderer != null)
            {
                // Create a temporary mesh snapshot of the current frame pose
                GameObject ghostObj = new GameObject("DashGhost");
                ghostObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
                ghostObj.transform.localScale = transform.localScale;

                MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();

                Mesh snapshotMesh = new Mesh();
                playerMeshRenderer.BakeMesh(snapshotMesh);
                meshFilter.mesh = snapshotMesh;
                meshRenderer.material = ghostMaterial != null ? ghostMaterial : playerMeshRenderer.material;

                StartCoroutine(FadeGhostRoutine(ghostObj, meshRenderer));
            }

            yield return new WaitForSeconds(spawnRate);
        }
    }

    private IEnumerator FadeGhostRoutine(GameObject ghostObj, MeshRenderer renderer)
    {
        Color initialColor = renderer.material.color;
        float alpha = initialColor.a;

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            Color newColor = initialColor;
            newColor.a = Mathf.Clamp01(alpha);
            renderer.material.color = newColor;
            yield return null;
        }

        Destroy(ghostObj);
    }
}
