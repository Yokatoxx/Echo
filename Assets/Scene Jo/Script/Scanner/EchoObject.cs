using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoObject : MonoBehaviour
{
    [SerializeField]
    private GameObject collisionPrefab;

    [Header("Echo Control")]
    [SerializeField] private bool enableEchoAtStart = false; // Activer l'écho au démarrage
    [SerializeField] private float echoActivationDelay = 5f; // Délai avant activation automatique (en secondes)
    private bool isEchoActive = false; // État actuel de l'écho

    [Header("Growth Settings")]
    public float growthDuration = 10f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float growthSpeed = 1f;
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

    [Header("Surface Detection")]
    [SerializeField] private bool enableSurfaceDetection = false; // Variable bool pour activer la détection de surface
    [SerializeField] private float woodSurfaceScaleModifier = 1.05f; // +5% pour WoodSurface
    [SerializeField] private float tileSurfaceScaleModifier = 1.10f; // +10% pour TileSurface
    [SerializeField] private float carpetSurfaceScaleModifier = 0.90f; // -10% pour CarpetSurface

    private void Start()
    {
        // Définir l'état initial de l'écho
        isEchoActive = enableEchoAtStart;

        // Si l'écho n'est pas activé au départ et qu'un délai est défini, démarrer le timer
        if (!enableEchoAtStart && echoActivationDelay > 0)
        {
            StartCoroutine(ActivateEchoAfterDelay());
        }

        Debug.Log($"EchoObject initialisé - Écho {(isEchoActive ? "activé" : "désactivé")} au démarrage");
    }

    private IEnumerator ActivateEchoAfterDelay()
    {
        Debug.Log($"Écho sera activé dans {echoActivationDelay} secondes");
        yield return new WaitForSeconds(echoActivationDelay);

        isEchoActive = true;
        Debug.Log("Écho automatiquement activé après le délai");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Vérifier si l'écho est activé avant de traiter les collisions
        if (!isEchoActive)
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            GameObject obj = Instantiate(collisionPrefab, contact.point, Quaternion.identity);

            // Appliquer la modification d'échelle selon la surface si activée
            if (enableSurfaceDetection)
            {
                ApplySurfaceScaleModification(obj, collision.collider.gameObject);
            }

            StartCoroutine(GrowAndFade(obj, growthDuration));
        }
    }

    /// <summary>
    /// Active ou désactive l'écho manuellement
    /// </summary>
    /// <param name="enable">True pour activer, false pour désactiver</param>
    public void SetEchoEnabled(bool enable)
    {
        isEchoActive = enable;
        Debug.Log($"Écho {(enable ? "activé" : "désactivé")} manuellement");
    }

    /// <summary>
    /// Retourne l'état actuel de l'écho
    /// </summary>
    /// <returns>True si l'écho est actif, false sinon</returns>
    public bool IsEchoActive()
    {
        return isEchoActive;
    }

    /// <summary>
    /// Applique la modification d'échelle selon le type de surface détecté par les tags
    /// </summary>
    /// <param name="spawnedObject">L'objet instancié à modifier</param>
    /// <param name="surfaceObject">L'objet de surface pour détecter le tag</param>
    private void ApplySurfaceScaleModification(GameObject spawnedObject, GameObject surfaceObject)
    {
        float scaleModifier = 1.0f;
        string surfaceType = "None";

        // Détecter le type de surface basé sur les tags
        if (surfaceObject.CompareTag("WoodSurface"))
        {
            scaleModifier = woodSurfaceScaleModifier;
            surfaceType = "Wood";
        }
        else if (surfaceObject.CompareTag("TileSurface"))
        {
            scaleModifier = tileSurfaceScaleModifier;
            surfaceType = "Tile";
        }
        else if (surfaceObject.CompareTag("CarpetSurface"))
        {
            scaleModifier = carpetSurfaceScaleModifier;
            surfaceType = "Carpet";
        }

        // Appliquer la modification d'échelle si un tag de surface a été détecté
        if (scaleModifier != 1.0f)
        {
            Vector3 originalScale = spawnedObject.transform.localScale;
            Vector3 newScale = originalScale * scaleModifier;
            spawnedObject.transform.localScale = newScale;

            Debug.Log($"Surface détectée: {surfaceType}. Échelle du prefab modifiée de {originalScale} à {newScale} (facteur: {scaleModifier})");
        }
    }

    /// <summary>
    /// Retourne le type de surface d'un GameObject basé sur son tag
    /// </summary>
    /// <param name="surfaceObject">L'objet à analyser</param>
    /// <returns>Le nom du type de surface ou "None" si aucun tag de surface n'est détecté</returns>
    public string GetSurfaceType(GameObject surfaceObject)
    {
        if (surfaceObject.CompareTag("WoodSurface"))
            return "Wood";
        else if (surfaceObject.CompareTag("TileSurface"))
            return "Tile";
        else if (surfaceObject.CompareTag("CarpetSurface"))
            return "Carpet";
        else
            return "None";
    }

    /// <summary>
    /// Active ou désactive la détection de surface
    /// </summary>
    /// <param name="enable">True pour activer, false pour désactiver</param>
    public void SetSurfaceDetectionEnabled(bool enable)
    {
        enableSurfaceDetection = enable;
        Debug.Log($"Détection de surface {(enable ? "activée" : "désactivée")}");
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