using UnityEngine;
using System.Collections.Generic;

public class EnemyBlendshapeController : MonoBehaviour
{
    [Header("Enemy Reference")]
    public EnemyType1 enemyType1; // Ajoutez cette ligne

    [Header("Mesh Settings")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Blendshape Configuration")]
    [SerializeField] private List<BlendshapeData> blendshapes = new List<BlendshapeData>();

    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Material Settings")]
    public Material enemyMaterial;
    public float colorTransitionSpeed = 2f;

    [Header("Colors per State")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color patrolColor = Color.white;
    [SerializeField] private Color pickUpColor = Color.white;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color attackColor = Color.red;

    // Private variables
    private float time;
    private Color currentTargetColor;
    private Color currentColor;
    private Dictionary<string, int> blendshapeIndices = new Dictionary<string, int>();

    // Transition variables
    private bool isTransitioning = false;
    private float transitionProgress = 0f;
    private EnemyState previousState;
    private EnemyState targetState;
    private Dictionary<BlendshapeData, BlendshapeTransitionData> transitionData = new Dictionary<BlendshapeData, BlendshapeTransitionData>();

    public enum EnemyState
    {
        EnemyIdleState,
        EnemyPatrolState,
        EnemyPickUpState,
        EnemyChaseState,
        EnemyAttackState
    }

    [SerializeField] private EnemyState currentState = EnemyState.EnemyIdleState;

    // Ajoutez cette variable pour suivre le dernier état connu
    private EnemyState lastKnownEnemyState = EnemyState.EnemyIdleState;

    [System.Serializable]
    public class BlendshapeData
    {
        [Header("Blendshape Info")]
        public string blendshapeName = "";
        public bool isActive = true;

        [Header("Values per State")]
        public BlendshapeStateValues idleValues = new BlendshapeStateValues(0f, 5f, 1f);
        public BlendshapeStateValues patrolValues = new BlendshapeStateValues(0f, 5f, 1.2f);
        public BlendshapeStateValues pickUpValues = new BlendshapeStateValues(0f, 5f, 0.8f);
        public BlendshapeStateValues chaseValues = new BlendshapeStateValues(50f, 20f, 2f);
        public BlendshapeStateValues attackValues = new BlendshapeStateValues(80f, 30f, 3f);

        [Header("Movement Settings")]
        public float phaseOffset = 0f;

        [HideInInspector]
        public int blendshapeIndex = -1;

        public BlendshapeStateValues GetValuesForState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.EnemyIdleState: return idleValues;
                case EnemyState.EnemyPatrolState: return patrolValues;
                case EnemyState.EnemyPickUpState: return pickUpValues;
                case EnemyState.EnemyChaseState: return chaseValues;
                case EnemyState.EnemyAttackState: return attackValues;
                default: return idleValues;
            }
        }
    }

    [System.Serializable]
    public class BlendshapeStateValues
    {
        [Range(0f, 100f)]
        public float baseValue = 0f;
        [Range(0f, 50f)]
        public float variation = 10f;
        [Range(0.1f, 10f)]
        public float speedMultiplier = 1f;

        public BlendshapeStateValues(float baseVal, float var, float speed)
        {
            baseValue = baseVal;
            variation = var;
            speedMultiplier = speed;
        }
    }

    private class BlendshapeTransitionData
    {
        public float startBaseValue;
        public float startVariation;
        public float startSpeedMultiplier;
        public float targetBaseValue;
        public float targetVariation;
        public float targetSpeedMultiplier;
        public float currentBaseValue;
        public float currentVariation;
        public float currentSpeedMultiplier;
    }

    void Start()
    {
        InitializeBlendshapes();
        InitializeMaterial();
        SetStateImmediate(currentState);

        // Vérifier si la référence à EnemyType1 est assignée
        if (enemyType1 == null)
        {
            // Essayer de la trouver automatiquement sur le même GameObject
            enemyType1 = GetComponent<EnemyType1>();

            if (enemyType1 == null)
            {
                Debug.LogWarning($"EnemyType1 reference not assigned on {gameObject.name}. Please assign it in the inspector or place this script on the same GameObject as EnemyType1.");
            }
        }
    }

    void InitializeBlendshapes()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogError("SkinnedMeshRenderer or Mesh not found!");
            return;
        }

        blendshapeIndices.Clear();

        for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            string shapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
            blendshapeIndices[shapeName] = i;
        }

        foreach (var blendshapeData in blendshapes)
        {
            if (blendshapeIndices.TryGetValue(blendshapeData.blendshapeName, out int index))
            {
                blendshapeData.blendshapeIndex = index;
            }
            else
            {
                blendshapeData.blendshapeIndex = -1;
                if (!string.IsNullOrEmpty(blendshapeData.blendshapeName))
                {
                    Debug.LogWarning($"Blendshape '{blendshapeData.blendshapeName}' not found!");
                }
            }
        }
    }

    void InitializeMaterial()
    {
        if (enemyMaterial == null && skinnedMeshRenderer != null)
        {
            enemyMaterial = skinnedMeshRenderer.material;
        }

        if (enemyMaterial != null)
        {
            if (enemyMaterial.HasProperty("_MainColor"))
            {
                currentColor = enemyMaterial.GetColor("_MainColor");
            }
            else if (enemyMaterial.HasProperty("_Color"))
            {
                currentColor = enemyMaterial.GetColor("_Color");
            }
            else
            {
                currentColor = Color.white;
                Debug.LogWarning("Material doesn't have '_MainColor' or '_Color' property! Using default white color.");
            }
        }
    }

    void Update()
    {
        // AJOUT : Vérifier les changements d'état depuis EnemyType1
        CheckForEnemyStateChanges();

        time += Time.deltaTime;
        UpdateTransition();
        UpdateBlendshapes();
        UpdateMaterialColor();
    }

    // NOUVELLE MÉTHODE : Vérifier les changements d'état depuis EnemyType1
    private void CheckForEnemyStateChanges()
    {
        if (enemyType1 == null) return;

        EnemyState currentEnemyState = GetCurrentEnemyState();

        if (currentEnemyState != lastKnownEnemyState)
        {
            Debug.Log($"Enemy state changed from {lastKnownEnemyState} to {currentEnemyState}");
            SetState(currentEnemyState);
            lastKnownEnemyState = currentEnemyState;
        }
    }

    // NOUVELLE MÉTHODE : Récupérer l'état actuel depuis EnemyType1
    private EnemyState GetCurrentEnemyState()
    {
        if (enemyType1 == null || enemyType1.stateMachine?.CurrentEnemyState == null)
            return EnemyState.EnemyIdleState;

        // Mapper les états de la state machine vers votre enum
        var currentStateObj = enemyType1.stateMachine.CurrentEnemyState;

        if (currentStateObj is EnemyIddleState)
            return EnemyState.EnemyIdleState;
        else if (currentStateObj is EnemyPatrolState)
            return EnemyState.EnemyPatrolState;
        else if (currentStateObj is EnemyPickUpState)
            return EnemyState.EnemyPickUpState;
        else if (currentStateObj is EnemyChaseState)
            return EnemyState.EnemyChaseState;
        else if (currentStateObj is EnemyAttackState)
            return EnemyState.EnemyAttackState;

        return EnemyState.EnemyIdleState; // État par défaut
    }

    void UpdateTransition()
    {
        if (!isTransitioning) return;

        transitionProgress += Time.deltaTime / transitionDuration;
        float curveValue = transitionCurve.Evaluate(transitionProgress);

        // Interpoler les valeurs de transition pour chaque blendshape
        foreach (var kvp in transitionData)
        {
            var data = kvp.Value;
            data.currentBaseValue = Mathf.Lerp(data.startBaseValue, data.targetBaseValue, curveValue);
            data.currentVariation = Mathf.Lerp(data.startVariation, data.targetVariation, curveValue);
            data.currentSpeedMultiplier = Mathf.Lerp(data.startSpeedMultiplier, data.targetSpeedMultiplier, curveValue);
        }

        if (transitionProgress >= 1f)
        {
            isTransitioning = false;
            transitionProgress = 0f;
            currentState = targetState;
            transitionData.Clear();
        }
    }

    void UpdateBlendshapes()
    {
        if (skinnedMeshRenderer == null) return;

        foreach (var blendshapeData in blendshapes)
        {
            if (!blendshapeData.isActive || blendshapeData.blendshapeIndex < 0) continue;

            float baseValue, variation, speedMultiplier;

            if (isTransitioning && transitionData.ContainsKey(blendshapeData))
            {
                // Utilise les valeurs interpolées pendant la transition
                var data = transitionData[blendshapeData];
                baseValue = data.currentBaseValue;
                variation = data.currentVariation;
                speedMultiplier = data.currentSpeedMultiplier;
            }
            else
            {
                // Utilise les valeurs de l'état actuel
                BlendshapeStateValues currentValues = blendshapeData.GetValuesForState(currentState);
                baseValue = currentValues.baseValue;
                variation = currentValues.variation;
                speedMultiplier = currentValues.speedMultiplier;
            }

            // Calcule la variation sinusoïdale autour de la base value avec la vitesse de l'état
            float phaseTime = (time * speedMultiplier) + blendshapeData.phaseOffset;
            float sineWave = Mathf.Sin(phaseTime); // Valeur entre -1 et 1

            // Applique la variation : baseValue ± variation
            float finalValue = baseValue + (sineWave * variation);

            // Clamp entre 0 et 100 pour les blendshapes
            finalValue = Mathf.Clamp(finalValue, 0f, 100f);

            // Applique au blendshape
            skinnedMeshRenderer.SetBlendShapeWeight(blendshapeData.blendshapeIndex, finalValue);
        }
    }

    void UpdateMaterialColor()
    {
        if (enemyMaterial == null) return;

        // Interpolation douce vers la couleur cible
        currentColor = Color.Lerp(currentColor, currentTargetColor, Time.deltaTime * colorTransitionSpeed);

        // Essayer différentes propriétés de couleur
        if (enemyMaterial.HasProperty("_MainColor"))
        {
            enemyMaterial.SetColor("_MainColor", currentColor);
        }
        else if (enemyMaterial.HasProperty("_Color"))
        {
            enemyMaterial.SetColor("_Color", currentColor);
        }
        else if (enemyMaterial.HasProperty("_BaseColor"))
        {
            enemyMaterial.SetColor("_BaseColor", currentColor);
        }
    }

    // Méthode principale pour changer d'état avec transition
    public void SetState(EnemyState newState)
    {
        if (newState == currentState && !isTransitioning) return;

        // Si déjà en transition, termine la transition actuelle immédiatement
        if (isTransitioning)
        {
            CompleteCurrentTransition();
        }

        StartTransition(currentState, newState);
    }

    // Méthode pour changer d'état sans transition (utile à l'initialisation)
    public void SetStateImmediate(EnemyState newState)
    {
        if (isTransitioning)
        {
            StopAllCoroutines();
            isTransitioning = false;
            transitionData.Clear();
        }

        currentState = newState;
        UpdateTargetColor(newState);
    }

    // Lance une transition entre deux états
    private void StartTransition(EnemyState fromState, EnemyState toState)
    {
        previousState = fromState;
        targetState = toState;
        isTransitioning = true;
        transitionProgress = 0f;

        // Prépare les données de transition pour chaque blendshape
        transitionData.Clear();
        foreach (var blendshapeData in blendshapes)
        {
            if (!blendshapeData.isActive) continue;

            BlendshapeStateValues fromValues = blendshapeData.GetValuesForState(fromState);
            BlendshapeStateValues toValues = blendshapeData.GetValuesForState(toState);

            var data = new BlendshapeTransitionData
            {
                startBaseValue = fromValues.baseValue,
                startVariation = fromValues.variation,
                startSpeedMultiplier = fromValues.speedMultiplier,
                targetBaseValue = toValues.baseValue,
                targetVariation = toValues.variation,
                targetSpeedMultiplier = toValues.speedMultiplier,
                currentBaseValue = fromValues.baseValue,
                currentVariation = fromValues.variation,
                currentSpeedMultiplier = fromValues.speedMultiplier
            };

            transitionData[blendshapeData] = data;
        }

        // Met à jour la couleur cible
        UpdateTargetColor(toState);
    }

    // Termine immédiatement la transition actuelle
    private void CompleteCurrentTransition()
    {
        if (!isTransitioning) return;

        isTransitioning = false;
        currentState = targetState;
        transitionData.Clear();
    }

    // Met à jour la couleur cible selon l'état
    private void UpdateTargetColor(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.EnemyIdleState:
                currentTargetColor = idleColor;
                break;
            case EnemyState.EnemyPatrolState:
                currentTargetColor = patrolColor;
                break;
            case EnemyState.EnemyPickUpState:
                currentTargetColor = pickUpColor;
                break;
            case EnemyState.EnemyChaseState:
                currentTargetColor = chaseColor;
                break;
            case EnemyState.EnemyAttackState:
                currentTargetColor = attackColor;
                break;
        }
    }

    // ... Le reste des méthodes restent identiques ...

    // Propriétés publiques pour inspecter l'état
    public bool IsTransitioning => isTransitioning;
    public float TransitionProgress => transitionProgress;
    public EnemyState CurrentState => currentState;
    public EnemyState TargetState => isTransitioning ? targetState : currentState;

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            InitializeBlendshapes();
            if (!isTransitioning)
            {
                SetStateImmediate(currentState);
            }
        }
    }

    [ContextMenu("Refresh Blendshapes")]
    public void RefreshBlendshapes()
    {
        InitializeBlendshapes();
    }

    [ContextMenu("Test Transition to Chase State")]
    public void TestTransitionToChase()
    {
        SetState(EnemyState.EnemyChaseState);
    }

    [ContextMenu("Test Transition to Idle State")]
    public void TestTransitionToIdle()
    {
        SetState(EnemyState.EnemyIdleState);
    }
}