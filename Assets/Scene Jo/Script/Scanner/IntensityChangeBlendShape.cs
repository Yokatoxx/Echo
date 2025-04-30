using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntensityChangeBlendShape : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private int blendShapeIndex = 0;

    [Header("Couleurs")]
    [SerializeField] private Color minColor = Color.blue; // Couleur quand la valeur est 0
    [SerializeField] private Color maxColor = Color.red;  // Couleur quand la valeur atteint le seuil

    [Header("Paramètres")]
    [SerializeField] private float thresholdValue = 20f; // Seuil pour considérer "proche de zéro" (0-100)
    [SerializeField] private Color defaultColor = Color.white; // Couleur par défaut au-delà du seuil

    private Material material;
    private Color originalColor;

    void Start()
    {
        // Si aucun SkinnedMeshRenderer n'est assigné, essayer de le trouver sur cet objet
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        // Vérifier si nous avons un SkinnedMeshRenderer
        if (skinnedMeshRenderer != null)
        {
            // Utiliser sharedMaterial pour référencer le matériau existant sans créer de copie
            // ou créer une instance si on veut modifier uniquement ce matériau
            material = new Material(skinnedMeshRenderer.sharedMaterial);
            skinnedMeshRenderer.material = material;
            originalColor = material.color;
            defaultColor = originalColor;
        }
        else
        {
            Debug.LogError("Aucun SkinnedMeshRenderer trouvé. Veuillez en assigner un dans l'inspecteur.");
        }
    }

    void Update()
    {
        if (skinnedMeshRenderer != null && material != null)
        {
            // Obtenir la valeur actuelle du blendshape (0-100)
            float blendShapeValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);

            if (blendShapeValue <= thresholdValue)
            {
                // Normaliser la valeur entre 0 et 1 mais uniquement dans la plage du seuil
                float normalizedValue = blendShapeValue / thresholdValue;

                // Interpoler entre les deux couleurs en fonction de la proximité à zéro
                // (minColor quand c'est 0, maxColor quand c'est égal au seuil)
                Color newColor = Color.Lerp(minColor, maxColor, normalizedValue);

                // Appliquer la nouvelle couleur au matériau
                material.color = newColor;
            }
            else
            {
                // Au-delà du seuil "proche de zéro", utiliser la couleur par défaut
                material.color = defaultColor;
            }
        }
    }
}
