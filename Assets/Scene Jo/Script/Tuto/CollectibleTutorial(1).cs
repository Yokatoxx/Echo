using UnityEngine;
using System.Collections.Generic;

public class CollectibleTutorial : BaseTutorial
{
    private List<GameObject> collectibleObjects = new List<GameObject>();
    private GameObject lastLookedAtCollectible;
    private bool isTextVisible = false;

    protected override void OnInitialize()
    {
        // Trouver tous les collectibles
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        collectibleObjects.AddRange(collectibles);

        Debug.Log($"CollectibleTutorial: Trouvé {collectibleObjects.Count} objets collectibles");
    }

    protected override void OnStartTutorial()
    {
        // Le tutoriel collectible est passif
        Debug.Log("CollectibleTutorial: Tutoriel démarré - Cherchez des objets révélés !");
    }

    protected override void OnUpdateTutorial()
    {
        HandleCollectibleLookAt();
        CheckIfPlayerHoldsObject();
    }

    private void HandleCollectibleLookAt()
    {
        bool shouldShowText = false;
        GameObject currentCollectible = null;

        if (IsPlayerLookingAt("Collectible", tutorialData.raycastDistance, out RaycastHit hit))
        {
            if (IsCollectibleRevealed(hit.collider.gameObject))
            {
                shouldShowText = true;
                currentCollectible = hit.collider.gameObject;

                // Si on regarde un nouveau collectible ou si le texte n'est pas visible
                if (currentCollectible != lastLookedAtCollectible || !isTextVisible)
                {
                    // Cacher l'ancien texte s'il existe
                    if (isTextVisible)
                    {
                        textManager.HideText("collectible");
                    }

                    // Calculer la position du texte
                    Vector3 textPosition = textManager.CalculateTextPosition(
                        currentCollectible.transform.position,
                        tutorialData.collectibleTextHeightOffset
                    );

                    // Créer ou mettre à jour le texte
                    textManager.CreateWorldText(
                        "collectible",
                        tutorialData.collectibleText,
                        tutorialData.collectibleTextColor,
                        tutorialData.collectibleTextSize,
                        textPosition,
                        tutorialData.collectibleTextScale
                    );

                    // Afficher le texte
                    textManager.ShowText("collectible");
                    isTextVisible = true;
                    lastLookedAtCollectible = currentCollectible;

                    Debug.Log($"CollectibleTutorial: Texte affiché pour {currentCollectible.name}");
                }
                else if (currentCollectible == lastLookedAtCollectible && isTextVisible)
                {
                    // Mettre à jour la position si on regarde toujours le même objet
                    Vector3 textPosition = textManager.CalculateTextPosition(
                        currentCollectible.transform.position,
                        tutorialData.collectibleTextHeightOffset
                    );
                    textManager.UpdateTextPosition("collectible", textPosition);
                }
            }
        }

        // Cacher le texte si on ne regarde plus un collectible révélé
        if (!shouldShowText && isTextVisible)
        {
            textManager.HideText("collectible");
            isTextVisible = false;
            lastLookedAtCollectible = null;
            Debug.Log("CollectibleTutorial: Texte caché");
        }
    }

    private void CheckIfPlayerHoldsObject()
    {
        if (tutorialManager.handController != null)
        {
            bool hasObject = tutorialManager.handController.rightHeldObject != null ||
                           tutorialManager.handController.leftHeldObject != null;

            if (hasObject)
            {
                Debug.Log("CollectibleTutorial: Joueur tient un objet - Tutoriel terminé !");
                CompleteTutorial();
            }
        }
    }

    private bool IsCollectibleRevealed(GameObject collectibleObject)
    {
        PointCloudRevert pointCloudRevert = collectibleObject.GetComponent<PointCloudRevert>();

        if (pointCloudRevert == null)
        {
            pointCloudRevert = collectibleObject.GetComponentInChildren<PointCloudRevert>();
        }

        if (pointCloudRevert == null)
        {
            pointCloudRevert = collectibleObject.GetComponentInParent<PointCloudRevert>();
        }

        if (pointCloudRevert == null)
        {
            Debug.LogWarning($"PointCloudRevert script not found on collectible: {collectibleObject.name}. Assuming it's revealed.");
            return true;
        }

        try
        {
            var restingValueField = pointCloudRevert.GetType().GetField("restingValue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var skinnedMeshRendererField = pointCloudRevert.GetType().GetField("skinnedMeshRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var blendShapeIndexField = pointCloudRevert.GetType().GetField("blendShapeIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (restingValueField != null && skinnedMeshRendererField != null && blendShapeIndexField != null)
            {
                float restingValue = (float)restingValueField.GetValue(pointCloudRevert);
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)skinnedMeshRendererField.GetValue(pointCloudRevert);
                int blendShapeIndex = (int)blendShapeIndexField.GetValue(pointCloudRevert);

                bool isRevealed = CheckBlendshapeValue(skinnedMeshRenderer, blendShapeIndex, restingValue);

                if (Debug.isDebugBuild)
                {
                    Debug.Log($"Collectible {collectibleObject.name}: Revealed = {isRevealed}");
                }

                return isRevealed;
            }
            else
            {
                Debug.LogWarning($"Cannot access private fields in PointCloudRevert script for: {collectibleObject.name}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error accessing PointCloudRevert fields for {collectibleObject.name}: {e.Message}");
            return true;
        }
    }

    private bool CheckBlendshapeValue(SkinnedMeshRenderer skinnedMeshRenderer, int blendShapeIndex, float restingValue)
    {
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            return true;
        }

        if (blendShapeIndex < 0 || blendShapeIndex >= skinnedMeshRenderer.sharedMesh.blendShapeCount)
        {
            Debug.LogWarning($"BlendShape index {blendShapeIndex} is out of range. Mesh has {skinnedMeshRenderer.sharedMesh.blendShapeCount} blendshapes.");
            return true;
        }

        float currentValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        bool isRevealed = Mathf.Abs(currentValue - restingValue) > tutorialData.blendshapeRestingTolerance;

        return isRevealed;
    }

    protected override void OnCompleteTutorial()
    {
        if (isTextVisible)
        {
            textManager.HideText("collectible");
            isTextVisible = false;
        }
    }

    protected override void OnStopTutorial()
    {
        if (isTextVisible)
        {
            textManager.HideText("collectible");
            isTextVisible = false;
        }
    }

    // Méthode de debug
    public bool DebugCheckCollectible(GameObject collectible)
    {
        return IsCollectibleRevealed(collectible);
    }

    // Méthode de debug pour forcer l'affichage
    [ContextMenu("Debug - Force Show Collectible Text")]
    public void DebugForceShowText()
    {
        if (collectibleObjects.Count > 0)
        {
            GameObject testCollectible = collectibleObjects[0];
            Vector3 textPosition = textManager.CalculateTextPosition(
                testCollectible.transform.position,
                tutorialData.collectibleTextHeightOffset
            );

            textManager.CreateWorldText(
                "collectible_debug",
                "DEBUG: " + tutorialData.collectibleText,
                Color.red,
                tutorialData.collectibleTextSize,
                textPosition,
                tutorialData.collectibleTextScale
            );

            textManager.ShowText("collectible_debug");
            Debug.Log("Debug: Texte collectible forcé affiché");
        }
    }
}