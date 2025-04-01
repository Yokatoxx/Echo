using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudRevert : MonoBehaviour
{
    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh mesh;

    private string blendShapeName = "murs.001";

    public float blendShapeValueTarget = 0f;
    public float initialBlendShapeValue = 0f;

    [SerializeField]
    private float lerpSpeed = 5f;

    [SerializeField]
    private float returnDelay = 3f;

    private bool isTransitioning = false;
    private float startValue;
    private float targetValue;
    private Coroutine returnCoroutine;

    void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        mesh = skinnedMeshRenderer.sharedMesh;
    }

    private void Start()
    {
        ActivateBlendShape();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            initialBlendShapeValue = Mathf.Lerp(initialBlendShapeValue, targetValue, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(initialBlendShapeValue - targetValue) < 0.01f)
            {
                initialBlendShapeValue = targetValue;
                isTransitioning = false;
            }
        }

        ActivateBlendShape();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scanner"))
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }

            startValue = initialBlendShapeValue;
            targetValue = blendShapeValueTarget;
            isTransitioning = true;
            returnCoroutine = StartCoroutine(ReturnToInitialValueAfterDelay());
        }
    }

    private IEnumerator ReturnToInitialValueAfterDelay()
    {
        yield return new WaitForSeconds(returnDelay);

        startValue = initialBlendShapeValue;
        targetValue = initialBlendShapeValue;
        isTransitioning = true;

    }

    private void ActivateBlendShape()
    {
        var index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

        if (index != -1)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(index, initialBlendShapeValue);
        }
    }
}
