using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    public Image staminaBar;
    public Image cooldownBlinkImage;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    public float staminaCooldown = 2f;
    public float blinkInterval = 0.5f;

    private bool isSprinting = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private float blinkTimer = 0f;
    private bool isBlinkVisible = true;

    void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
        if (cooldownBlinkImage != null)
            cooldownBlinkImage.enabled = false;
    }

    void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                isBlinkVisible = !isBlinkVisible;
                if (cooldownBlinkImage != null)
                    cooldownBlinkImage.enabled = isBlinkVisible;
                blinkTimer = blinkInterval;
            }

            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                if (cooldownBlinkImage != null)
                    cooldownBlinkImage.enabled = false;
            }
        }
        else
        {
            if (!isSprinting && currentStamina < maxStamina)
            {
                RegenerateStamina();
            }
            if (cooldownBlinkImage != null)
                cooldownBlinkImage.enabled = false;
        }
    }

    public bool CanSprint()
    {
        return currentStamina > 0 && !isOnCooldown;
    }

    public void UseStamina(float deltaTime)
    {
        isSprinting = true;
        currentStamina -= staminaDrainRate * deltaTime;
        if (currentStamina <= 0)
        {
            currentStamina = 0;
            StartCooldown();
        }
        UpdateStaminaUI();
    }

    private void RegenerateStamina()
    {
        currentStamina += staminaRegenRate * Time.deltaTime;
        if (currentStamina > maxStamina)
        {
            currentStamina = maxStamina;
        }
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaBar != null)
        {
            staminaBar.fillAmount = currentStamina / maxStamina;
        }
    }

    private void LateUpdate()
    {
        isSprinting = false;
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = staminaCooldown;
        if (cooldownBlinkImage != null)
        {
            cooldownBlinkImage.enabled = true;
            isBlinkVisible = true;
            blinkTimer = blinkInterval;
        }
    }
}
