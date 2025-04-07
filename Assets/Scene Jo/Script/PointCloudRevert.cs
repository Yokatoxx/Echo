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

    private void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

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
        targetValue = restingValue;
        isTransitioning = true;
    }
}
