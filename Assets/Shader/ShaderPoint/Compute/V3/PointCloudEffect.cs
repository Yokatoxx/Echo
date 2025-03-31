using UnityEngine;

[RequireComponent(typeof(HDRPPointCloudSystem))]
public class PointCloudEffect : MonoBehaviour
{
    [Header("Effets")]
    [SerializeField] private bool useEffects = true;
    
    [Header("Animation")]
    [SerializeField] private bool animatePoints = false;
    [SerializeField] private float animationSpeed = 1.0f;
    [SerializeField] private float animationAmplitude = 0.1f;
    [SerializeField] private Vector3 animationDirection = Vector3.up;
    
    [Header("Couleur")]
    [SerializeField] private bool animateColor = false;
    [SerializeField] private Color colorStart = Color.blue;
    [SerializeField] private Color colorEnd = Color.red;
    [SerializeField] private float colorSpeed = 1.0f;
    
    [Header("Taille")]
    [SerializeField] private bool animateSize = false;
    [SerializeField] private float sizeMin = 0.005f;
    [SerializeField] private float sizeMax = 0.02f;
    [SerializeField] private float sizeSpeed = 1.0f;
    
    // Référence au système principal
    private HDRPPointCloudSystem pointCloudSystem;
    
    private void Start()
    {
        pointCloudSystem = GetComponent<HDRPPointCloudSystem>();
    }
    
    private void Update()
    {
        if (!useEffects || pointCloudSystem == null || !pointCloudSystem.IsPointCloudActive())
            return;
            
        // Animation de couleur
        if (animateColor)
        {
            float t = (Mathf.Sin(Time.time * colorSpeed) + 1) * 0.5f;
            Color newColor = Color.Lerp(colorStart, colorEnd, t);
            pointCloudSystem.SetPointColor(newColor);
        }
        
        // Animation de taille
        if (animateSize)
        {
            float t = (Mathf.Sin(Time.time * sizeSpeed) + 1) * 0.5f;
            float newSize = Mathf.Lerp(sizeMin, sizeMax, t);
            pointCloudSystem.SetPointSize(newSize);
        }
        
        // Note: L'animation de position nécessiterait une implémentation plus complexe
        // avec un compute shader dédié
    }
}