using UnityEngine;
using UnityEngine.UI;

public class ThrowObjectHand : MonoBehaviour
{
    [Header("Références")]
    public PlayerHandController handController;
    public Camera playerCamera;
    public Stamina staminaSystem;

    [Header("UI de charge")]
    public Image chargeBar;

    [Header("Paramètres de lancer")]
    public KeyCode throwKey = KeyCode.Mouse0;
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float chargeTime = 1.5f;
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Stamina")]
    public float minStaminaCost = 10f;
    public float maxStaminaCost = 40f;
    public bool requireStaminaToCharge = true;

    [Header("Feedback visuel")]
    public Color minForceColor = Color.green;
    public Color maxForceColor = Color.red;

    private bool isCharging = false;
    private float chargeStartTime;
    private float currentChargeAmount = 0f;
    private float staminaDrainAccumulator = 0f;

    private void Start()
    {
        if (handController == null)
            handController = GetComponent<PlayerHandController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (staminaSystem == null)
            staminaSystem = GetComponent<Stamina>();

        if (chargeBar != null)
            chargeBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(throwKey) && CanThrow() && CanUseStamina())
        {
            isCharging = true;
            chargeStartTime = Time.time;
            currentChargeAmount = 0f;
            staminaDrainAccumulator = 0f;

            if (chargeBar != null)
                chargeBar.gameObject.SetActive(true);
        }

        if (isCharging && Input.GetKey(throwKey))
        {
            if (requireStaminaToCharge && !CanUseStamina())
            {
                ThrowRightHandObject();
                isCharging = false;
                return;
            }

            float holdTime = Time.time - chargeStartTime;
            float previousCharge = currentChargeAmount;
            currentChargeAmount = Mathf.Clamp01(holdTime / chargeTime);

            // Drainer la stamina proportionnellement à l'augmentation de la charge
            if (staminaSystem != null)
            {
                float chargeIncrease = currentChargeAmount - previousCharge;
                float staminaCost = Mathf.Lerp(minStaminaCost, maxStaminaCost, currentChargeAmount) * chargeIncrease;
                staminaDrainAccumulator += staminaCost;

                if (staminaDrainAccumulator >= 0.1f)
                {
                    staminaSystem.UseStamina(staminaDrainAccumulator / staminaSystem.staminaDrainRate);
                    staminaDrainAccumulator = 0f;
                }
            }

            UpdateChargeUI();
        }

        if (isCharging && Input.GetKeyUp(throwKey))
        {
            ThrowRightHandObject();
            isCharging = false;

            if (chargeBar != null)
                chargeBar.gameObject.SetActive(false);
        }
    }

    private bool CanThrow()
    {
        return handController != null && handController.rightHeldObject != null;
    }

    private bool CanUseStamina()
    {
        if (staminaSystem == null) return true;
        return staminaSystem.CanSprint();
    }

    private void ThrowRightHandObject()
    {
        if (!CanThrow()) return;

        float evaluatedCharge = forceCurve.Evaluate(currentChargeAmount);
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, evaluatedCharge);

        if (staminaSystem != null)
        {
            float finalStaminaCost = Mathf.Lerp(minStaminaCost, maxStaminaCost, evaluatedCharge);
            staminaSystem.currentStamina -= finalStaminaCost;
            if (staminaSystem.currentStamina < 0)
                staminaSystem.currentStamina = 0;

            staminaSystem.UpdateStaminaUI();
        }

        GameObject objectToThrow = handController.rightHeldObject.gameObject;
        Rigidbody rb = objectToThrow.GetComponent<Rigidbody>();

        handController.rightHeldObject.Drop();
        handController.rightHeldObject = null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;

            rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * throwForce * 0.5f, ForceMode.Impulse);
        }

        currentChargeAmount = 0f;
    }

    private void UpdateChargeUI()
    {
        if (chargeBar != null)
        {
            chargeBar.fillAmount = currentChargeAmount;
            chargeBar.color = Color.Lerp(minForceColor, maxForceColor, currentChargeAmount);
        }
    }
}
