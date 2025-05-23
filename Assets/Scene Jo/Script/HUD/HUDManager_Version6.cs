using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Stamina staminaSystem;
    [SerializeField] private ChargeableEchoScanner chargeableEcho;
    [SerializeField] private PlayerHandController handController;

    [Header("HUD Elements")]
    [SerializeField] private CanvasGroup staminaHUD;
    [SerializeField] private CanvasGroup echoHUD;
    [SerializeField] private CanvasGroup objectInHandHUD;

    [Header("Stamina UI")]
    [SerializeField] private Image staminaContourImage;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Echo UI")]
    [SerializeField] private Image echoContourImage;
    [SerializeField] private Image echoFillImage;
    [SerializeField] private TextMeshProUGUI echoText;

    [Header("Hand UI")]
    [SerializeField] private Image leftHandImage;
    [SerializeField] private Image rightHandImage;
    [SerializeField] private Image leftHandSelectionImage;
    [SerializeField] private Image rightHandSelectionImage;
    [SerializeField] private TextMeshProUGUI objectNameText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float displayDuration = 3.0f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Detection Settings")]
    [SerializeField] private float valueChangeThreshold = 0.01f;
    [SerializeField] private float initialDelay = 0.5f;

    private float lastStaminaValue = -1f;
    private float lastEchoValue = -1f;
    private Collectable lastLeftHandObject = null;
    private Collectable lastRightHandObject = null;
    private int lastSelectedHandIndex = -1;

    private Coroutine staminaFadeCoroutine;
    private Coroutine echoFadeCoroutine;
    private Coroutine objectFadeCoroutine;

    private bool isStaminaVisible = false;
    private bool isEchoVisible = false;
    private bool isObjectVisible = false;
    private bool isCheckingEnabled = false;

    private void Awake()
    {
        
        ForceHideAllImmediate();
    }

    private void Start()
    {
        
        InitializeHUD();

        
        StartCoroutine(EnableCheckingAfterDelay());
    }

    private IEnumerator EnableCheckingAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);

       
        isCheckingEnabled = true;

        
        if (staminaSystem != null)
            lastStaminaValue = staminaSystem.currentStamina;

        if (chargeableEcho != null)
            lastEchoValue = GetEchoValue();
    }

    private void Update()
    {
        
        if (!isCheckingEnabled) return;

        CheckStaminaChanges();
        CheckEchoChanges();
        CheckHandChanges();
        UpdateHUDValues();
    }

    private void InitializeHUD()
    {
        
        if (staminaSystem != null)
            lastStaminaValue = staminaSystem.currentStamina;

        if (chargeableEcho != null)
            lastEchoValue = GetEchoValue();

        if (handController != null)
        {
            lastLeftHandObject = handController.leftHeldObject;
            lastRightHandObject = handController.rightHeldObject;
            lastSelectedHandIndex = handController.selectedHandIndex;
        }
    }

    #region Stamina Management
    private void CheckStaminaChanges()
    {
        if (staminaSystem == null) return;

        float currentStamina = staminaSystem.currentStamina;

        
        if (Mathf.Abs(currentStamina - lastStaminaValue) > valueChangeThreshold)
        {
            
            ShowStaminaHUD();
            lastStaminaValue = currentStamina;
        }
    }

    private void ShowStaminaHUD()
    {
        if (staminaFadeCoroutine != null)
            StopCoroutine(staminaFadeCoroutine);

        staminaFadeCoroutine = StartCoroutine(FadeHUD(staminaHUD, true, HUDType.Stamina));
    }

    private void UpdateStaminaUI()
    {
        if (staminaSystem == null) return;

        float currentStamina = staminaSystem.currentStamina;
        float maxStamina = staminaSystem.maxStamina;

        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = currentStamina / maxStamina;
        }

        if (staminaText != null)
            staminaText.text = $"{currentStamina:F0}/{maxStamina:F0}";
    }
    #endregion

    #region Echo Management
    private void CheckEchoChanges()
    {
        if (chargeableEcho == null) return;

        float currentEcho = GetEchoValue();

        if (Mathf.Abs(currentEcho - lastEchoValue) > valueChangeThreshold)
        {
            ShowEchoHUD();
            lastEchoValue = currentEcho;
        }
    }

    private float GetEchoValue()
    {
        if (chargeableEcho != null)
        {
            var chargeField = chargeableEcho.GetType().GetField("currentCharge",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (chargeField != null)
                return (float)chargeField.GetValue(chargeableEcho);

            var chargeProperty = chargeableEcho.GetType().GetProperty("currentCharge",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (chargeProperty != null)
                return (float)chargeProperty.GetValue(chargeableEcho);
        }
        return 0f;
    }

    private void ShowEchoHUD()
    {
        if (echoFadeCoroutine != null)
            StopCoroutine(echoFadeCoroutine);

        echoFadeCoroutine = StartCoroutine(FadeHUD(echoHUD, true, HUDType.Echo));
    }

    private void UpdateEchoUI()
    {
        if (chargeableEcho == null) return;

        float currentEcho = GetEchoValue();
        float maxEcho = GetMaxEchoValue();

        if (echoFillImage != null)
        {
            echoFillImage.fillAmount = currentEcho / maxEcho;
        }

        if (echoText != null)
            echoText.text = $"{currentEcho:F1}/{maxEcho:F1}";
    }

    private float GetMaxEchoValue()
    {
        if (chargeableEcho != null)
        {
            var maxChargeField = chargeableEcho.GetType().GetField("maxCharge",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (maxChargeField != null)
                return (float)maxChargeField.GetValue(chargeableEcho);

            var maxChargeProperty = chargeableEcho.GetType().GetProperty("maxCharge",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (maxChargeProperty != null)
                return (float)maxChargeProperty.GetValue(chargeableEcho);
        }
        return 100f;
    }
    #endregion

    #region Hand Management
    private void CheckHandChanges()
    {
        if (handController == null) return;

        bool hasChanged = false;

        if (handController.leftHeldObject != lastLeftHandObject)
        {
            lastLeftHandObject = handController.leftHeldObject;
            hasChanged = true;
        }

        if (handController.rightHeldObject != lastRightHandObject)
        {
            lastRightHandObject = handController.rightHeldObject;
            hasChanged = true;
        }

        if (handController.selectedHandIndex != lastSelectedHandIndex)
        {
            lastSelectedHandIndex = handController.selectedHandIndex;
            hasChanged = true;
        }

        if (hasChanged)
        {
            bool hasAnyObject = handController.leftHeldObject != null || handController.rightHeldObject != null;

            if (hasAnyObject)
                ShowObjectHUD();
            else
                HideObjectHUD();
        }
    }

    private void ShowObjectHUD()
    {
        if (objectFadeCoroutine != null)
            StopCoroutine(objectFadeCoroutine);

        objectFadeCoroutine = StartCoroutine(FadeHUD(objectInHandHUD, true, HUDType.Object));
    }

    private void HideObjectHUD()
    {
        if (objectFadeCoroutine != null)
            StopCoroutine(objectFadeCoroutine);

        objectFadeCoroutine = StartCoroutine(FadeHUD(objectInHandHUD, false, HUDType.Object));
    }

    private void UpdateHandUI()
    {
        if (handController == null) return;

        if (leftHandSelectionImage != null)
            leftHandSelectionImage.enabled = (handController.selectedHandIndex == 1);

        if (rightHandSelectionImage != null)
            rightHandSelectionImage.enabled = (handController.selectedHandIndex == 0);

        if (objectNameText != null)
        {
            Collectable selectedObject = null;

            if (handController.selectedHandIndex == 0)
                selectedObject = handController.rightHeldObject;
            else
                selectedObject = handController.leftHeldObject;

            if (selectedObject != null)
                objectNameText.text = selectedObject.name;
            else
                objectNameText.text = "Aucun objet";
        }

        UpdateHandSprites();
    }

    private void UpdateHandSprites()
    {
        if (leftHandImage != null)
        {
        }

        if (rightHandImage != null)
        {
        }
    }
    #endregion

    #region Fade System
    private enum HUDType
    {
        Stamina,
        Echo,
        Object
    }

    private IEnumerator FadeHUD(CanvasGroup canvasGroup, bool fadeIn, HUDType hudType)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float curveValue = fadeCurve.Evaluate(progress);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        switch (hudType)
        {
            case HUDType.Stamina:
                isStaminaVisible = fadeIn;
                break;
            case HUDType.Echo:
                isEchoVisible = fadeIn;
                break;
            case HUDType.Object:
                isObjectVisible = fadeIn;
                break;
        }

        if (fadeIn && hudType != HUDType.Object)
        {
            yield return new WaitForSeconds(displayDuration);

            bool shouldStayVisible = false;

            switch (hudType)
            {
                case HUDType.Stamina:
                    if (staminaSystem != null)
                        shouldStayVisible = Mathf.Abs(staminaSystem.currentStamina - lastStaminaValue) > valueChangeThreshold;
                    break;
                case HUDType.Echo:
                    shouldStayVisible = Mathf.Abs(GetEchoValue() - lastEchoValue) > valueChangeThreshold;
                    break;
            }

            if (!shouldStayVisible)
            {
                yield return StartCoroutine(FadeHUD(canvasGroup, false, hudType));
            }
        }
    }
    #endregion

    private void UpdateHUDValues()
    {
        if (isStaminaVisible) UpdateStaminaUI();
        if (isEchoVisible) UpdateEchoUI();
        if (isObjectVisible) UpdateHandUI();
    }

    #region Public Methods
    public void ForceShowStamina(float duration = -1f)
    {
        ShowStaminaHUD();
        if (duration > 0f)
        {
            StartCoroutine(ForceHideAfterDelay(staminaHUD, duration, HUDType.Stamina));
        }
    }

    public void ForceShowEcho(float duration = -1f)
    {
        ShowEchoHUD();
        if (duration > 0f)
        {
            StartCoroutine(ForceHideAfterDelay(echoHUD, duration, HUDType.Echo));
        }
    }

    public void ForceHideAll()
    {
        if (staminaFadeCoroutine != null) StopCoroutine(staminaFadeCoroutine);
        if (echoFadeCoroutine != null) StopCoroutine(echoFadeCoroutine);
        if (objectFadeCoroutine != null) StopCoroutine(objectFadeCoroutine);

        StartCoroutine(FadeHUD(staminaHUD, false, HUDType.Stamina));
        StartCoroutine(FadeHUD(echoHUD, false, HUDType.Echo));
        StartCoroutine(FadeHUD(objectInHandHUD, false, HUDType.Object));
    }


    public void ForceHideAllImmediate()
    {
        if (staminaFadeCoroutine != null) StopCoroutine(staminaFadeCoroutine);
        if (echoFadeCoroutine != null) StopCoroutine(echoFadeCoroutine);
        if (objectFadeCoroutine != null) StopCoroutine(objectFadeCoroutine);

        if (staminaHUD != null)
        {
            staminaHUD.alpha = 0f;
            isStaminaVisible = false;
        }

        if (echoHUD != null)
        {
            echoHUD.alpha = 0f;
            isEchoVisible = false;
        }

        if (objectInHandHUD != null)
        {
            objectInHandHUD.alpha = 0f;
            isObjectVisible = false;
        }
    }

    private IEnumerator ForceHideAfterDelay(CanvasGroup canvasGroup, float delay, HUDType hudType)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FadeHUD(canvasGroup, false, hudType));
    }
    #endregion

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0.1f, fadeOutDuration);
        displayDuration = Mathf.Max(0f, displayDuration);
        valueChangeThreshold = Mathf.Max(0.001f, valueChangeThreshold);
        initialDelay = Mathf.Max(0.1f, initialDelay);
    }
}