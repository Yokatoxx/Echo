using UnityEngine;

public class FadeObjectEmission : MonoBehaviour
{
    private Renderer objectRenderer;
    private MaterialPropertyBlock propBlock;
    private Color baseColor;
    private Color baseEmission;
    private bool isRegistered = false;

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        // Sauver les couleurs de base
        if (objectRenderer != null)
        {
            baseColor = objectRenderer.sharedMaterial.GetColor("_Color");
            if (objectRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            {
                baseEmission = objectRenderer.sharedMaterial.GetColor("_EmissionColor");
            }
            else
            {
                baseEmission = Color.black;
            }
        }
    }

    void Start()
    {
        // Déplacer l'enregistrement dans Start pour s'assurer que FadeManagerEmission est initialisé
        if (FadeManagerEmission.Instance != null && !isRegistered)
        {
            FadeManagerEmission.Instance.RegisterObject(this);
            isRegistered = true;
        }
    }

    // Assurer l'enregistrement même si FadeManager est créé après
    void OnEnable()
    {
        if (FadeManagerEmission.Instance != null && !isRegistered)
        {
            FadeManagerEmission.Instance.RegisterObject(this);
            isRegistered = true;
        }
    }

    public void SetAlphaAndEmission(float alpha)
    {
        if (objectRenderer == null) return;

        objectRenderer.GetPropertyBlock(propBlock);

        // Alpha sur la couleur principale
        Color color = baseColor;
        color.a = alpha;
        propBlock.SetColor("_Color", color);

        // Emission fade
        Color emission = baseEmission * alpha; // Fader l'émission en même temps
        propBlock.SetColor("_EmissionColor", emission);

        objectRenderer.SetPropertyBlock(propBlock);
    }
}
