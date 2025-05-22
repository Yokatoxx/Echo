using UnityEngine;
using System.Collections.Generic;

public class EnemyBlendshapeController : MonoBehaviour
{
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
            else
            {
                Debug.LogWarning("Material doesn't have '_MainColor' property!");
            }
        }
    }

    void Update()
    {
        time += Time.deltaTime;
        UpdateTransition();
        UpdateBlendshapes();
        UpdateMaterialColor();
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
        if (enemyMaterial == null || !enemyMaterial.HasProperty("_MainColor")) return;

        // Interpolation douce vers la couleur cible
        currentColor = Color.Lerp(currentColor, currentTargetColor, Time.deltaTime * colorTransitionSpeed);
        enemyMaterial.SetColor("_MainColor", currentColor);
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

    // Méthodes publiques pour modifier les valeurs en runtime
    public void SetBlendshapeValues(string blendshapeName, EnemyState state, float baseValue, float variation, float speedMultiplier = -1f)
    {
        BlendshapeData data = GetBlendshapeData(blendshapeName);
        if (data != null)
        {
            BlendshapeStateValues values = data.GetValuesForState(state);
            values.baseValue = baseValue;
            values.variation = variation;

            if (speedMultiplier > 0f)
                values.speedMultiplier = speedMultiplier;
        }
    }

    public void SetBlendshapeSpeed(string blendshapeName, EnemyState state, float speedMultiplier)
    {
        BlendshapeData data = GetBlendshapeData(blendshapeName);
        if (data != null)
        {
            BlendshapeStateValues values = data.GetValuesForState(state);
            values.speedMultiplier = speedMultiplier;
        }
    }

    public void SetBlendshapeActive(string blendshapeName, bool active)
    {
        BlendshapeData data = GetBlendshapeData(blendshapeName);
        if (data != null)
        {
            data.isActive = active;
        }
    }

    public void SetStateColor(EnemyState state, Color color)
    {
        switch (state)
        {
            case EnemyState.EnemyIdleState:
                idleColor = color;
                break;
            case EnemyState.EnemyPatrolState:
                patrolColor = color;
                break;
            case EnemyState.EnemyPickUpState:
                pickUpColor = color;
                break;
            case EnemyState.EnemyChaseState:
                chaseColor = color;
                break;
            case EnemyState.EnemyAttackState:
                attackColor = color;
                break;
        }

        if (currentState == state || (isTransitioning && targetState == state))
            currentTargetColor = color;
    }

    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Max(0.1f, duration);
    }

    public void AddBlendshape(string blendshapeName)
    {
        if (GetBlendshapeData(blendshapeName) != null)
        {
            Debug.LogWarning($"Blendshape '{blendshapeName}' already exists in the list!");
            return;
        }

        BlendshapeData newData = new BlendshapeData();
        newData.blendshapeName = blendshapeName;
        blendshapes.Add(newData);

        if (blendshapeIndices.TryGetValue(blendshapeName, out int index))
        {
            newData.blendshapeIndex = index;
        }
    }

    public void RemoveBlendshape(string blendshapeName)
    {
        blendshapes.RemoveAll(b => b.blendshapeName == blendshapeName);
    }

    public List<string> GetAvailableBlendshapes()
    {
        return new List<string>(blendshapeIndices.Keys);
    }

    private BlendshapeData GetBlendshapeData(string blendshapeName)
    {
        return blendshapes.Find(b => b.blendshapeName == blendshapeName);
    }

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

    [ContextMenu("Add All Available Blendshapes")]
    public void AddAllAvailableBlendshapes()
    {
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null) return;

        for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            string shapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
            if (GetBlendshapeData(shapeName) == null)
            {
                AddBlendshape(shapeName);
            }
        }
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