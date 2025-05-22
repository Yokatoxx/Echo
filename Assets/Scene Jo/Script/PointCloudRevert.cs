using System.Collections;
using UnityEngine;

public class PointCloudRevert : MonoBehaviour
{
    [SerializeField] private int blendShapeIndex = 0;
    [SerializeField] private float blendShapeValueTarget = 100f;
    [SerializeField] private bool isProgressive = false;
    [SerializeField] private float restingValue = 0f;
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float returnDelay = 3f;

    [Header("Incréments progressifs par type")]
    [SerializeField] private float scannerProgressiveIncrement = 10f;
    [SerializeField] private float echoPassifProgressiveIncrement = 6f;
    [SerializeField] private float echoJoueurProgressiveIncrement = 12f;

    [Header("Material Cutoff Control")]
    [SerializeField] private Material materialToControl; // Assignez le matériau ici
    [SerializeField] private float materialCutoffRestingValue = 0.8f;
    [SerializeField] private float materialCutoffScannerTargetValue = 0.4f;

    [Header("Tutoriel")]
    [SerializeField] private bool isTutorialActive = true; // Par défaut, le mode tutoriel est actif
    [SerializeField] private float tutorialInitialCutoffValue = 0.5f; // Valeur initiale pour le tutoriel
    [SerializeField] private bool saveTutorialState = true; // Sauvegarder l'état du tutoriel entre les scènes

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private bool isTransitioning = false; // For blendshape
    private float currentBlendValue;
    private float targetValue; // For blendshape
    private Coroutine returnCoroutine;
    private bool isIndexValid = false;
    private Collectable collectableComponent;

    // Material Cutoff state
    private float currentMaterialCutoffValue;
    private float targetMaterialCutoffValue;
    private bool isMaterialCutoffTransitioning = false;
    private static readonly int CutoffPropertyID = Shader.PropertyToID("_Cutoff");
    private static readonly string TutorialCompletedKey = "PointCloudTutorialCompleted";

    private void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        collectableComponent = GetComponent<Collectable>();

        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
        {
            isIndexValid = blendShapeIndex >= 0 &&
                           blendShapeIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount;
        }
        else
        {
            Debug.LogError("SkinnedMeshRenderer ou SharedMesh non trouvé.", this);
            isIndexValid = false;
        }

        // If materialToControl is not assigned, try to get it from the SkinnedMeshRenderer
        if (materialToControl == null && skinnedMeshRenderer != null)
        {
            materialToControl = skinnedMeshRenderer.material; // Gets an instance of the material
        }
        if (materialToControl == null)
        {
            Debug.LogWarning("MaterialToControl non assigné et non trouvé sur le SkinnedMeshRenderer.", this);
        }

        // Charger l'état du tutoriel si nécessaire
        if (saveTutorialState && PlayerPrefs.HasKey(TutorialCompletedKey))
        {
            isTutorialActive = PlayerPrefs.GetInt(TutorialCompletedKey) == 0;
        }
    }

    private void Start()
    {
        currentBlendValue = restingValue;
        targetValue = restingValue; // Initialize targetValue

        if (isIndexValid)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);
        }

        if (materialToControl != null)
        {
            // Définir la valeur de cutoff initiale en fonction de l'état du tutoriel
            if (isTutorialActive)
            {
                currentMaterialCutoffValue = tutorialInitialCutoffValue;
                targetMaterialCutoffValue = tutorialInitialCutoffValue;
            }
            else
            {
                currentMaterialCutoffValue = materialCutoffRestingValue;
                targetMaterialCutoffValue = materialCutoffRestingValue;
            }

            materialToControl.SetFloat(CutoffPropertyID, currentMaterialCutoffValue);
        }
    }

    private void Update()
    {
        if (collectableComponent != null && collectableComponent.isPickedUp)
        {
            // Handle Blendshape
            if (isIndexValid && currentBlendValue != blendShapeValueTarget)
            {
                currentBlendValue = blendShapeValueTarget;
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);
            }
            isTransitioning = false;

            // Handle Material Cutoff
            if (materialToControl != null && currentMaterialCutoffValue != materialCutoffRestingValue) // Or a specific "picked up" cutoff value
            {
                currentMaterialCutoffValue = materialCutoffRestingValue;
                materialToControl.SetFloat(CutoffPropertyID, currentMaterialCutoffValue);
            }
            isMaterialCutoffTransitioning = false;

            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            return;
        }

        // Blendshape transition
        if (isTransitioning && isIndexValid)
        {
            currentBlendValue = Mathf.Lerp(currentBlendValue, targetValue, Time.deltaTime * lerpSpeed);
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);

            if (Mathf.Abs(currentBlendValue - targetValue) < 0.01f)
            {
                currentBlendValue = targetValue;
                isTransitioning = false; // Stop blendshape transition if target reached
            }
        }

        // Material Cutoff transition
        if (isMaterialCutoffTransitioning && materialToControl != null)
        {
            currentMaterialCutoffValue = Mathf.Lerp(currentMaterialCutoffValue, targetMaterialCutoffValue, Time.deltaTime * lerpSpeed);
            materialToControl.SetFloat(CutoffPropertyID, currentMaterialCutoffValue);

            if (Mathf.Abs(currentMaterialCutoffValue - targetMaterialCutoffValue) < 0.01f)
            {
                currentMaterialCutoffValue = targetMaterialCutoffValue;
                isMaterialCutoffTransitioning = false; // Stop cutoff transition if target reached
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collectableComponent != null && collectableComponent.isPickedUp)
            return;

        bool isScanner = other.CompareTag("Scanner");
        bool isEchoPassif = other.CompareTag("EchoPassif");
        bool isEchoJoueur = other.CompareTag("EchoJoueur");

        if (isScanner || isEchoPassif || isEchoJoueur)
        {
            // Si c'est en mode tutoriel et première interaction, désactiver le tutoriel
            if (isTutorialActive)
            {
                CompleteTutorial();
            }

            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }

            // Blendshape target
            float effectiveIncrement = scannerProgressiveIncrement;
            if (isEchoPassif) effectiveIncrement = echoPassifProgressiveIncrement;
            else if (isEchoJoueur) effectiveIncrement = echoJoueurProgressiveIncrement;

            if (isProgressive)
            {
                // Mode progressif pour le blendshape
                targetValue = Mathf.Clamp(currentBlendValue + effectiveIncrement, 0f, 100f);

                // Mode progressif pour le cutoff - réduire progressivement
                // Plus le cutoff est bas, plus les particules sont visibles
                float cutoffRange = materialCutoffRestingValue - materialCutoffScannerTargetValue;
                float cutoffIncrement = cutoffRange * (effectiveIncrement / 100f);

                // Assurer que la valeur de cutoff diminue (car plus elle est basse, plus l'effet est visible)
                targetMaterialCutoffValue = Mathf.Max(
                    materialCutoffScannerTargetValue,
                    currentMaterialCutoffValue - cutoffIncrement
                );

                Debug.Log($"Progression: Blend {currentBlendValue} -> {targetValue}, Cutoff {currentMaterialCutoffValue} -> {targetMaterialCutoffValue}");
            }
            else
            {
                // Mode non progressif - aller directement à la valeur cible
                targetValue = Mathf.Min(blendShapeValueTarget, 100f);
                targetMaterialCutoffValue = materialCutoffScannerTargetValue;
            }

            isTransitioning = true;
            isMaterialCutoffTransitioning = true;

            returnCoroutine = StartCoroutine(ReturnToInitialValueAfterDelay());
        }
    }

    private void CompleteTutorial()
    {
        isTutorialActive = false;

        // Sauvegarder l'état du tutoriel si nécessaire
        if (saveTutorialState)
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1); // 1 = tutoriel complété
            PlayerPrefs.Save();
        }

        // Mettre à jour la valeur de repos du cutoff
        targetMaterialCutoffValue = materialCutoffRestingValue;
        isMaterialCutoffTransitioning = true;

        Debug.Log("Tutoriel terminé : Cutoff changé vers la valeur de repos normale.");
    }

    private IEnumerator ReturnToInitialValueAfterDelay()
    {
        yield return new WaitForSeconds(returnDelay);

        if (collectableComponent != null && collectableComponent.isPickedUp)
            yield break;

        // Reset Blendshape
        targetValue = restingValue;
        isTransitioning = true;

        // Reset Material Cutoff
        if (materialToControl != null)
        {
            // Utiliser la valeur appropriée selon l'état du tutoriel
            targetMaterialCutoffValue = isTutorialActive ? tutorialInitialCutoffValue : materialCutoffRestingValue;
            isMaterialCutoffTransitioning = true;
        }
    }
}
