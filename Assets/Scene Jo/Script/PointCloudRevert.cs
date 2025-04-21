using System.Collections;
using UnityEngine;

public class PointCloudRevert : MonoBehaviour
{
    [SerializeField] private int blendShapeIndex = 0;
    [SerializeField] private float blendShapeValueTarget = 100f;
    [SerializeField] private bool isProgressive = false;
    [SerializeField] private float progressiveIncrement = 10f;
    [SerializeField] private float restingValue = 0f;
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float returnDelay = 3f;

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
        // Si l'objet est ramassé, maintenir la valeur du blendshape à la valeur cible
        if (collectableComponent != null && collectableComponent.isPickedUp)
        {
            if (isIndexValid && currentBlendValue != blendShapeValueTarget)
            {
                currentBlendValue = blendShapeValueTarget;
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendValue);
            }

            // Si l'objet est ramassé, annuler le retour à la valeur initiale
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }

            // Désactiver la transition pendant que l'objet est tenu
            isTransitioning = false;
            return;
        }

        // Comportement normal de transition quand l'objet n'est pas ramassé
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
        // Ne pas déclencher le trigger si l'objet est déjà ramassé
        if (collectableComponent != null && collectableComponent.isPickedUp)
            return;

        if (other.CompareTag("Scanner") && isIndexValid)
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }

            if (isProgressive)
            {
                targetValue = Mathf.Clamp(currentBlendValue + progressiveIncrement, 0f, 100f);
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

        // Vérifier si l'objet a été ramassé entre temps
        if (collectableComponent != null && collectableComponent.isPickedUp)
            yield break;

        targetValue = restingValue;
        isTransitioning = true;
    }
}
