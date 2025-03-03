using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovingObject))]
public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject objectPrefab;
    public float spawnInterval = 3f;
    public float spawnOffset = 0.5f;

    [Header("Growth Settings")]
    public float growthDuration = 10f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float growthSpeed = 1f;
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

    [Header("Shader Property Names")]
    [Tooltip("Nom de la propriété de couleur dans le shader")]
    public string colorPropertyName = "_IntersectionColor";

    [Header("Object Lifetime")]
    public float objectLifetime = 5f;

    private MovingObject movingObject;
    private bool isSpawning = true;
    private float nextSpawnTime;

    void Start()
    {
        movingObject = GetComponent<MovingObject>();
        if (movingObject == null)
        {
            enabled = false;
            return;
        }

        nextSpawnTime = Time.time + spawnInterval;
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (isSpawning && movingObject != null)
            {
                SpawnObject();
            }
        }
    }

    void SpawnObject()
    {
        if (objectPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * spawnOffset;

            GameObject spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);

            EchoObject echo = spawnedObject.GetComponent<EchoObject>();
            if (echo != null)
            {
                echo.growthDuration = growthDuration;
                echo.minimumSize = minimumSize;
                echo.maximumSize = maximumSize;
                echo.growthCurve = growthCurve;
                echo.growthSpeed = growthSpeed;
                echo.alphaMax = alphaMax;
                echo.alphaMin = alphaMin;
                echo.fadeTransitionSpeed = fadeTransitionSpeed;
            }
            else
            {
                EffectController effectController = spawnedObject.AddComponent<EffectController>();
                effectController.Initialize(
                    growthDuration,
                    minimumSize,
                    maximumSize,
                    growthCurve,
                    alphaMax,
                    alphaMin,
                    colorPropertyName
                );
            }
            Destroy(spawnedObject, objectLifetime);
        }
    }

    public void EnableSpawning()
    {
        isSpawning = true;
    }

    public void DisableSpawning()
    {
        isSpawning = false;
    }

    public void ToggleSpawning()
    {
        isSpawning = !isSpawning;
    }
}

public class EffectController : MonoBehaviour
{
    private float growthDuration;
    private float minimumSize;
    private float maximumSize;
    private AnimationCurve growthCurve;
    private float alphaMax;
    private float alphaMin;
    private string colorPropertyName;

    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
    private Vector3 originalScale;
    private bool initialized = false;

    public void Initialize(float duration, float minSize, float maxSize,
                          AnimationCurve curve, float maxAlpha, float minAlpha,
                          string colorProperty)
    {
        growthDuration = duration;
        minimumSize = minSize;
        maximumSize = maxSize;
        growthCurve = curve;
        alphaMax = maxAlpha;
        alphaMin = minAlpha;
        colorPropertyName = colorProperty;

        originalScale = transform.localScale;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat != null && mat.HasProperty(colorPropertyName))
                {
                    if (!originalColors.ContainsKey(mat))
                    {
                        originalColors.Add(mat, mat.GetColor(colorPropertyName));
                    }
                }
            }
        }

        initialized = true;
        StartCoroutine(GrowAndFade());
    }

    private IEnumerator GrowAndFade()
    {
        if (!initialized)
        {
            yield break;
        }

        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < growthDuration)
        {
            // Vérifier si l'objet existe encore
            if (this == null)
            {
                yield break;
            }

            elapsedTime = Time.time - startTime;
            float t = elapsedTime / growthDuration;
            float curveValue = growthCurve.Evaluate(t);

            // Scale
            float scaleFactor = Mathf.Lerp(minimumSize, maximumSize, curveValue);
            transform.localScale = originalScale * scaleFactor;

            // Alpha
            float alpha = Mathf.Lerp(alphaMax, alphaMin, curveValue);

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                foreach (Material mat in renderer.materials)
                {
                    if (mat != null && originalColors.ContainsKey(mat) && mat.HasProperty(colorPropertyName))
                    {
                        Color newColor = originalColors[mat];
                        newColor.a = alpha;
                        mat.SetColor(colorPropertyName, newColor);
                    }
                }
            }

            yield return null;
        }
    }
}
