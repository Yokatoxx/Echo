using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutsideSphereInvisible : MonoBehaviour
{
    public float sphereRadius = 30f;
    public LayerMask layersToAffect;
    [Range(0.2f, 2.0f)]
    public float checkInterval = 0.5f; // Augmenté pour réduire la fréquence de vérification
    public float minMoveDistance = 1.0f; // Augmenté pour réduire les vérifications inutiles
    public bool showDebugSphere = true;

    // Cache pour les renderers
    private List<RendererInfo> rendererCache = new List<RendererInfo>();
    private Vector3 lastPosition;
    private float timeSinceLastCheck = 0f;
    private Transform cachedTransform;
    private float sqrMinMoveDistance;
    private float sqrSphereRadius;

    // Structure pour stocker les informations des renderers
    private class RendererInfo
    {
        public Renderer renderer;
        public bool originalState;
        public Bounds lastBounds;
        public bool isValid = true;
    }

    private void Awake()
    {
        cachedTransform = transform;
        lastPosition = cachedTransform.position;
        sqrMinMoveDistance = minMoveDistance * minMoveDistance;
        sqrSphereRadius = sphereRadius * sphereRadius;

        // Initialisation unique des renderers
        InitializeRendererCache();
    }

    private void OnEnable()
    {
        UpdateVisibility();
    }

    private void OnDisable()
    {
        // Restaurer l'état original des renderers
        foreach (var rendererInfo in rendererCache)
        {
            if (rendererInfo.isValid && rendererInfo.renderer != null)
                rendererInfo.renderer.enabled = rendererInfo.originalState;
        }
    }

    private void InitializeRendererCache()
    {
        rendererCache.Clear();

        // Utilisation de FindObjectsOfType uniquement au démarrage
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            GameObject obj = renderer.gameObject;

            // Vérifier si l'objet est dans les layers concernés
            if (((1 << obj.layer) & layersToAffect.value) == 0)
                continue;

            // Créer et stocker les informations du renderer
            RendererInfo info = new RendererInfo
            {
                renderer = renderer,
                originalState = renderer.enabled,
                lastBounds = renderer.bounds,
                isValid = true
            };

            rendererCache.Add(info);
        }
    }

    void Update()
    {
        timeSinceLastCheck += Time.deltaTime;

        // Vérifier si suffisamment de temps s'est écoulé OU si le joueur s'est déplacé suffisamment
        Vector3 currentPos = cachedTransform.position;
        float sqrDistance = (currentPos - lastPosition).sqrMagnitude;

        if (timeSinceLastCheck >= checkInterval || sqrDistance >= sqrMinMoveDistance)
        {
            UpdateVisibility();
            timeSinceLastCheck = 0f;
            lastPosition = currentPos;
        }
    }

    public void UpdateVisibility()
    {
        Vector3 center = cachedTransform.position;

        // Utiliser un regroupement par octants pour ne traiter que les objets potentiellement pertinents
        for (int i = 0; i < rendererCache.Count; i++)
        {
            var rendererInfo = rendererCache[i];

            if (!rendererInfo.isValid || rendererInfo.renderer == null)
            {
                rendererInfo.isValid = false;
                continue;
            }

            try
            {
                // Recalculer les bounds uniquement si nécessaire
                Bounds bounds = rendererInfo.renderer.bounds;

                // Optimisation du test de visibilité en utilisant sqrMagnitude
                Vector3 closestPoint = bounds.ClosestPoint(center);
                bool isInside = (closestPoint - center).sqrMagnitude <= sqrSphereRadius;

                rendererInfo.renderer.enabled = isInside;
            }
            catch (System.Exception)
            {
                // Gestion des erreurs pour éviter les crashs
                rendererInfo.isValid = false;
            }
        }
    }

    public void RefreshRenderers()
    {
        // Nettoyer la liste des renderers invalides avant réinitialisation
        CleanInvalidRenderers();
        InitializeRendererCache();
        UpdateVisibility();
    }

    private void CleanInvalidRenderers()
    {
        rendererCache.RemoveAll(info => !info.isValid || info.renderer == null);
    }

    void OnDrawGizmos()
    {
        if (!showDebugSphere)
            return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }

    // Méthode pour ajouter de nouveaux renderers pendant le jeu
    public void RegisterRenderer(Renderer newRenderer)
    {
        if (newRenderer == null || ((1 << newRenderer.gameObject.layer) & layersToAffect.value) == 0)
            return;

        // Vérifier si le renderer est déjà dans la cache
        foreach (var cachedInfo in rendererCache)  // Renommé 'info' en 'cachedInfo'
        {
            if (cachedInfo.renderer == newRenderer)
                return;
        }

        // Ajouter le nouveau renderer
        RendererInfo info = new RendererInfo
        {
            renderer = newRenderer,
            originalState = newRenderer.enabled,
            lastBounds = newRenderer.bounds,
            isValid = true
        };

        rendererCache.Add(info);

        // Mettre à jour sa visibilité
        Vector3 center = cachedTransform.position;
        Vector3 closestPoint = info.lastBounds.ClosestPoint(center);
        bool isInside = (closestPoint - center).sqrMagnitude <= sqrSphereRadius;
        newRenderer.enabled = isInside;
    }
}
