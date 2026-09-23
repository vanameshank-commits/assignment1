using UnityEngine;
using UnityEngine.UI;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Stats")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float runDrainRate = 15f; // How fast running drains the bar
    public float dashCost = 30f;     // Chunk of stamina taken per dash

    [Header("Regeneration")]
    public float regenRate = 20f;
    public float regenDelay = 1.2f;  // Delay before regenerating after use

    [Header("UI Reference")]
    public Slider staminaSlider;

    private float lastUseTime;

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        // Regenerate stamina if enough time has passed since last use
        if (Time.time - lastUseTime > regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            UpdateUI();
        }
    }

    public bool CanRun()
    {
        return currentStamina > 0;
    }

    public void DrainStaminaForRunning()
    {
        currentStamina -= runDrainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        lastUseTime = Time.time;
        UpdateUI();
    }

    public bool CanDash()
    {
        return currentStamina >= dashCost;
    }

    public void ConsumeDashStamina()
    {
        currentStamina -= dashCost;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        lastUseTime = Time.time;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }
}