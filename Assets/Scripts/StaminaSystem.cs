using UnityEngine;
using UnityEngine.UI;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Stats")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float runDrainRate = 15f;
    public float dashCost = 30f;

    [Header("Regeneration")]
    public float regenRate = 20f;
    public float regenDelay = 1.2f;

    [Header("UI Reference")]
    public Slider staminaSlider;
    public CanvasGroup staminaCanvasGroup; // Controls the fading
    public float fadeSpeed = 4f;           // How fast it fades in/out

    private float lastUseTime;

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        // Start fully invisible if stamina is already full
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // Regenerate stamina
        if (Time.time - lastUseTime > regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            UpdateUI();
        }

        // Fade the UI in or out based on current stamina level
        if (staminaCanvasGroup != null)
        {
            // If stamina is not full, target alpha is 1 (visible). If full, target is 0 (invisible).
            float targetAlpha = (currentStamina < maxStamina) ? 1f : 0f;
            staminaCanvasGroup.alpha = Mathf.MoveTowards(staminaCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
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