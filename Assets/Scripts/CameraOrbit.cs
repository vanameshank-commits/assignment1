using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public float distance = 5f;
    public float heightOffset = 1.5f; // Looks at the upper body, not the feet

    [Header("Mouse Controls")]
    public float sensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    private float currentX = 0f;
    private float currentY = 15f; // Start pitched slightly down

    void Start()
    {
        // Lock and hide the mouse cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read Mouse Input
        currentX += Input.GetAxis("Mouse X") * sensitivity;
        currentY -= Input.GetAxis("Mouse Y") * sensitivity;

        // 2. Clamp the vertical angle so the camera doesn't flip upside down
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // 3. Calculate the rotation and position
        Vector3 targetCenter = target.position + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // 4. Apply position and rotation
        transform.position = targetCenter - (rotation * Vector3.forward * distance);
        transform.LookAt(targetCenter);
    }
}