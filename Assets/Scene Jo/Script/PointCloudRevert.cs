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
            currentMaterialCutoffValue = materialCutoffRestingValue;
            targetMaterialCutoffValue = materialCutoffRestingValue; // Initialize target
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
                targetValue = Mathf.Clamp(currentBlendValue + effectiveIncrement, 0f, 100f);
            }
            else
            {
                targetValue = Mathf.Min(blendShapeValueTarget, 100f);
            }
            isTransitioning = true;

            // Material Cutoff target
            if (isScanner)
            {
                targetMaterialCutoffValue = materialCutoffScannerTargetValue;
            }
            // For "EchoPassif" and "EchoJoueur", the cutoff will return to restingValue via the coroutine.
            // If you want specific values for them, you can add conditions here:
            // else if (isEchoPassif) { targetMaterialCutoffValue = someOtherValue; }
            // else if (isEchoJoueur) { targetMaterialCutoffValue = anotherValue; }
            else
            {
                // If not a scanner, ensure it aims for the resting value if it was changed by a scanner previously
                // Or, if it should stay as is unless scanner, this can be adjusted.
                // For now, triggering return sequence will handle resetting to resting value.
            }
            isMaterialCutoffTransitioning = true;


            returnCoroutine = StartCoroutine(ReturnToInitialValueAfterDelay());
        }
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
            targetMaterialCutoffValue = materialCutoffRestingValue;
            isMaterialCutoffTransitioning = true;
        }
    }
}