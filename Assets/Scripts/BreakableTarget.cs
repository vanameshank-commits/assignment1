using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableTarget : MonoBehaviour, IBreakable
{
    [SerializeField] private Color prototypeGizmoColor = Color.red;

    private void OnDrawGizmos()
    {
        Gizmos.color = prototypeGizmoColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void Break(Vector3 hitPoint, Vector3 direction)
    {
        Debug.Log($"[Breakable] Destroyed {gameObject.name} at {hitPoint}");
        gameObject.SetActive(false); // Clean disable for prototyping
    }
}
