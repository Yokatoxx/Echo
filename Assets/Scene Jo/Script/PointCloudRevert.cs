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

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private bool isTransitioning = false;
    private float currentBlendValue;
    private float targetValue;
    private Coroutine returnCoroutine;
    private bool isIndexValid = false;
    private Collectable collectableComponent;

    private void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        collectableComponent = GetComponent<Collectable>();

        isIndexValid = skinnedMeshRenderer.sharedMesh != null &&
                       blendShapeIndex >= 0 &&
                       blendShapeIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount;
    }

    private void Start()
    {
        currentBlendValue = restingValue;

        if (isIndexValid)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);
        }
    }

    private void Update()
    {
        if (collectableComponent != null && collectableComponent.isPickedUp)
        {
            if (isIndexValid && currentBlendValue != blendShapeValueTarget)
            {
                currentBlendValue = blendShapeValueTarget;
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);
            }

            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            isTransitioning = false;
            return;
        }

        if (isTransitioning && isIndexValid)
        {
            currentBlendValue = Mathf.Lerp(currentBlendValue, targetValue, Time.deltaTime * lerpSpeed);
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);

            if (Mathf.Abs(currentBlendValue - targetValue) < 0.01f)
            {
                currentBlendValue = targetValue;
                isTransitioning = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collectableComponent != null && collectableComponent.isPickedUp)
            return;

        if (other.CompareTag("Scanner") || other.CompareTag("EchoPassif") || other.CompareTag("EchoJoueur"))
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }

            float effectiveIncrement = scannerProgressiveIncrement; // Valeur par défaut pour Scanner

            // Sélectionner l'incrément progressif selon le tag
            if (other.CompareTag("EchoPassif"))
            {
                effectiveIncrement = echoPassifProgressiveIncrement;
            }
            else if (other.CompareTag("EchoJoueur"))
            {
                effectiveIncrement = echoJoueurProgressiveIncrement;
            }

            if (isProgressive)
            {
                targetValue = Mathf.Clamp(currentBlendValue + effectiveIncrement, 0f, 100f);
            }
            else
            {
                targetValue = Mathf.Min(blendShapeValueTarget, 100f);
            }

            isTransitioning = true;
            returnCoroutine = StartCoroutine(ReturnToInitialValueAfterDelay());
        }
    }

    private IEnumerator ReturnToInitialValueAfterDelay()
    {
        yield return new WaitForSeconds(returnDelay);

        if (collectableComponent != null && collectableComponent.isPickedUp)
            yield break;

        targetValue = restingValue;
        isTransitioning = true;
    }
}
