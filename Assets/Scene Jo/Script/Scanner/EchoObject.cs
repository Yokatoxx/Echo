using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoObject : MonoBehaviour
{
    [SerializeField]
    private GameObject collisionPrefab;

    [Header("Growth Settings")]
    public float growthDuration = 10f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float growthSpeed = 1f;
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            GameObject obj = Instantiate(collisionPrefab, contact.point, Quaternion.identity);
            StartCoroutine(GrowAndFade(obj, growthDuration));
        }
    }

    IEnumerator GrowAndFade(GameObject obj, float duration)
    {
        Vector3 initialScale = obj.transform.localScale;
        float elapsedTime = 0f;
        Renderer renderer = obj.GetComponent<Renderer>();

        while (elapsedTime < duration)
        {
            float curveValue = growthCurve.Evaluate(elapsedTime / duration);
            float scaleMultiplier = Mathf.Lerp(minimumSize, maximumSize, curveValue);
            obj.transform.localScale = initialScale * scaleMultiplier;

            if (renderer != null)
            {
                
                if (renderer.material.HasProperty("_IntersectionColorStart"))
                {
                    Color currentColor = renderer.material.GetColor("_IntersectionColorStart");
                    currentColor.a = alphaMax;
                    renderer.material.SetColor("_IntersectionColorStart", currentColor);
                }
            }

            elapsedTime += Time.deltaTime * growthSpeed;
            yield return null;
        }
        obj.transform.localScale = initialScale * maximumSize;

        // fondu
        if (renderer != null)
        {
            if (renderer.material.HasProperty("_IntersectionColorStart"))
            {
                Color currentColor = renderer.material.GetColor("_IntersectionColorStart");
                float currentAlpha = alphaMax;
                while (currentAlpha > alphaMin)
                {
                    currentAlpha = Mathf.MoveTowards(currentAlpha, alphaMin, fadeTransitionSpeed * Time.deltaTime);
                    currentColor.a = currentAlpha;
                    renderer.material.SetColor("_IntersectionColorStart", currentColor);
                    yield return null;
                }
                currentColor.a = alphaMin;
                renderer.material.SetColor("_IntersectionColorStart", currentColor);
            }
        }

        Destroy(obj);
    }

}
