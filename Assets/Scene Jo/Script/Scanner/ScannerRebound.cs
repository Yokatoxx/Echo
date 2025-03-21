using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScannerRebound : MonoBehaviour
{
    [Header("Rebond Configuration")]
    public GameObject reboundPrefab;
    public float reboundDuration = 1.5f;
    public float reboundSize = 1.5f;
    public Color reboundColor = new Color(1f, 0.5f, 0f, 0.7f);

    [Header("Detection")]
    public string detectionTag = "Scannable";
    public float detectionThickness = 0.5f;

    private bool isExpanding = true;
    private float currentRadius = 0f;
    private float previousRadius = 0f;
    private Material material;
    private Transform parentTransform;

    void Start()
    {
        material = GetComponent<Renderer>().material;
        parentTransform = transform.parent;
    }

    void Update()
    {
        if (!isExpanding) return;
        currentRadius = transform.localScale.x / 2;
        if (currentRadius > previousRadius)
        {
            DetectObjects();
        }

        previousRadius = currentRadius;
    }

    void DetectObjects()
    {
        float innerRadius = currentRadius - detectionThickness;
        innerRadius = Mathf.Max(0, innerRadius);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(detectionTag))
            {
                Vector3 objectPos = hitCollider.ClosestPoint(transform.position);
                float distance = Vector3.Distance(transform.position, objectPos);

                if (distance <= currentRadius && distance >= innerRadius)
                {

                    CreateReboundEffect(objectPos, hitCollider.transform);
                }
            }
        }
    }

    void CreateReboundEffect(Vector3 position, Transform hitObject)
    {
        // Vérifie si un rebond existe déjà à cet emplacement (évite les doublons)
        // Utilise une petite distance de tolérance
        Collider[] existingRebounds = Physics.OverlapSphere(position, 0.1f);
        foreach (var existing in existingRebounds)
        {
            if (existing.gameObject.CompareTag("ScannerRebound"))
            {
                return; // Rebond déjà existant, on annule
            }
        }

        GameObject reboundEffect = Instantiate(reboundPrefab, position, Quaternion.identity);
        reboundEffect.tag = "ScannerRebound";

        // Configure le rebond
        if (reboundEffect.GetComponent<Renderer>() != null)
        {
            reboundEffect.GetComponent<Renderer>().material.color = reboundColor;
        }

        if (hitObject.gameObject.isStatic)
        {
            reboundEffect.transform.parent = hitObject;
        }
        StartCoroutine(AnimateReboundEffect(reboundEffect));
    }

    IEnumerator AnimateReboundEffect(GameObject reboundObject)
    {
        float elapsedTime = 0f;
        float initialAlpha = reboundColor.a;

        Material reboundMaterial = reboundObject.GetComponent<Renderer>().material;

        while (elapsedTime < reboundDuration)
        {
            float sizeMultiplier;
            if (elapsedTime < reboundDuration * 0.3f)
            {
                sizeMultiplier = Mathf.Lerp(0, reboundSize, elapsedTime / (reboundDuration * 0.3f));
            }
            else
            {
                sizeMultiplier = reboundSize;
            }

            reboundObject.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);

            float newAlpha = Mathf.Lerp(initialAlpha, 0, elapsedTime / reboundDuration);
            Color currentColor = reboundMaterial.color;
            reboundMaterial.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(reboundObject);
    }

    public void StopExpanding()
    {
        isExpanding = false;
    }
}
