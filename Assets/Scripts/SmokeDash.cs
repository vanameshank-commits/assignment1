using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SmokeDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1.5f;

    [Header("Visuals & Effects")]
    public GameObject playerMesh; // Drag your character's visual model here
    public ParticleSystem smokeEffect; // Drag the smoke particle system here

    private CharacterController controller;
    public bool isDashing { get; private set; } // Let other scripts know we are dashing
    private float nextDashTime = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Trigger dash on 'E' or Left Shift (change KeyCode as needed)
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= nextDashTime && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        // 1. Disappear and trigger smoke
        if (playerMesh != null) playerMesh.SetActive(false);
        if (smokeEffect != null) smokeEffect.Play();

        // 2. Determine dash direction based on player input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dashDirection = (transform.right * h + transform.forward * v).normalized;

        // If standing still, default the dash to straight ahead
        if (dashDirection.magnitude == 0)
        {
            dashDirection = transform.forward;
        }

        float startTime = Time.time;

        // 3. Move rapidly over time
        while (Time.time < startTime + dashDuration)
        {
            // Move ignores gravity here for a clean, straight smoke dash
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // 4. Reappear and stop emitting smoke
        if (playerMesh != null) playerMesh.SetActive(true);
        if (smokeEffect != null) smokeEffect.Stop();

        isDashing = false;
    }
}