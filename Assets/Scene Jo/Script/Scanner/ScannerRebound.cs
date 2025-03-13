using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScannerRebound : MonoBehaviour
{
    [Header("Rebond Configuration")]
    public GameObject reboundPrefab;             // Prefab pour l'effet de rebond
    public float reboundDuration = 1.5f;         // Durée de l'effet de rebond
    public float reboundSize = 1.5f;             // Taille maximale de l'effet de rebond
    public Color reboundColor = new Color(1f, 0.5f, 0f, 0.7f); // Couleur orange semi-transparente pour le rebond

    [Header("Detection")]
    public string detectionTag = "Scannable";    // Tag des objets qui déclenchent des rebonds
    public float detectionThickness = 0.5f;      // Épaisseur de la détection

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

        // Récupère le rayon actuel de la sphère
        currentRadius = transform.localScale.x / 2;

        // Vérifie les collisions uniquement si la sphère grandit
        if (currentRadius > previousRadius)
        {
            DetectObjects();
        }

        previousRadius = currentRadius;
    }

    void DetectObjects()
    {
        // Calcule l'épaisseur de la coquille de détection
        float innerRadius = currentRadius - detectionThickness;
        innerRadius = Mathf.Max(0, innerRadius); // Évite les valeurs négatives

        // Récupère tous les colliders dans la zone sphérique
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentRadius);

        foreach (var hitCollider in hitColliders)
        {
            // Vérifie si l'objet a le tag approprié
            if (hitCollider.CompareTag(detectionTag))
            {
                // Calcule la distance entre le centre de la sphère et l'objet
                Vector3 objectPos = hitCollider.ClosestPoint(transform.position);
                float distance = Vector3.Distance(transform.position, objectPos);

                // Vérifie si l'objet est dans la coquille de détection
                if (distance <= currentRadius && distance >= innerRadius)
                {
                    // Crée un effet de rebond à l'emplacement de l'impact
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

        // Instancie le prefab de rebond
        GameObject reboundEffect = Instantiate(reboundPrefab, position, Quaternion.identity);
        reboundEffect.tag = "ScannerRebound";

        // Configure le rebond
        if (reboundEffect.GetComponent<Renderer>() != null)
        {
            reboundEffect.GetComponent<Renderer>().material.color = reboundColor;
        }

        // Ajoute le rebond comme enfant de l'objet touché s'il est statique
        if (hitObject.gameObject.isStatic)
        {
            reboundEffect.transform.parent = hitObject;
        }

        // Lance l'animation du rebond
        StartCoroutine(AnimateReboundEffect(reboundEffect));
    }

    IEnumerator AnimateReboundEffect(GameObject reboundObject)
    {
        float elapsedTime = 0f;
        float initialAlpha = reboundColor.a;

        Material reboundMaterial = reboundObject.GetComponent<Renderer>().material;

        while (elapsedTime < reboundDuration)
        {
            // Expansion rapide puis maintien de la taille
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

            // Fade out progressif
            float newAlpha = Mathf.Lerp(initialAlpha, 0, elapsedTime / reboundDuration);
            Color currentColor = reboundMaterial.color;
            reboundMaterial.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(reboundObject);
    }

    // Informe le script que la sphère a fini de s'étendre
    public void StopExpanding()
    {
        isExpanding = false;
    }
}
