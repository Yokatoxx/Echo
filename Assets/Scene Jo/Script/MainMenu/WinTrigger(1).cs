using UnityEngine;
using System.Collections;

public class WinManager : MonoBehaviour
{
    [Header("Win Condition")]
    [SerializeField] public bool isCollectComplete = false;

    [Header("Target Object Settings")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private string blendShapeName = "BlendShape1";
    [SerializeField] private float targetBlendShapeValue = 100f;
    [SerializeField] private float blendShapeTransitionDuration = 2f;
    [SerializeField] private AnimationCurve blendShapeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shader Cutoff Settings")]
    [SerializeField] private float startCutoffValue = 0f;
    [SerializeField] private float targetCutoffValue = 1f;
    [SerializeField] private bool useSameCurveForCutoff = true;
    [SerializeField] private AnimationCurve cutoffCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Renderer targetRenderer;
    private Material targetMaterial;
    private Collider targetCollider;
    private int blendShapeIndex = -1;
    private bool hasTriggered = false;
    private float initialBlendShapeValue = 0f;
    private float initialCutoffValue = 0f;

    // Propriétés pour le shader
    private static readonly int CutoffProperty = Shader.PropertyToID("_Cutoff");

    void Start()
    {
        // Validation et initialisation
        if (targetObject == null)
        {
            Debug.LogError("WinManager: Target Object n'est pas assigné!");
            return;
        }

        // Récupérer le SkinnedMeshRenderer
        skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("WinManager: Le Target Object n'a pas de SkinnedMeshRenderer!");
            return;
        }

        // Récupérer le Renderer et le Material
        targetRenderer = targetObject.GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogError("WinManager: Le Target Object n'a pas de Renderer!");
            return;
        }

        // Récupérer le material (créer une instance pour éviter de modifier l'asset)
        targetMaterial = targetRenderer.material;

        // Vérifier si le shader a la propriété _Cutoff
        if (!targetMaterial.HasProperty(CutoffProperty))
        {
            Debug.LogError("WinManager: Le material n'a pas de propriété '_Cutoff'!");
        }
        else
        {
            // Stocker la valeur initiale du cutoff
            initialCutoffValue = targetMaterial.GetFloat(CutoffProperty);
        }

        // Récupérer le Collider
        targetCollider = targetObject.GetComponent<Collider>();
        if (targetCollider == null)
        {
            Debug.LogError("WinManager: Le Target Object n'a pas de Collider!");
            return;
        }

        // Trouver l'index du blendshape
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        if (mesh != null)
        {
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i) == blendShapeName)
                {
                    blendShapeIndex = i;
                    // Stocker la valeur initiale du blendshape
                    initialBlendShapeValue = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    break;
                }
            }

            if (blendShapeIndex == -1)
            {
                Debug.LogError($"WinManager: BlendShape '{blendShapeName}' non trouvé sur le mesh!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le player et si les conditions sont remplies
        if (!hasTriggered &&
            isCollectComplete &&
            other.CompareTag(playerTag))
        {
            TriggerWinSequence();
        }
    }

    private void TriggerWinSequence()
    {
        hasTriggered = true;

        Debug.Log("WinManager: Séquence de victoire déclenchée!");

        // Commencer la transition du blendshape et du cutoff avec lerp
        if (blendShapeIndex != -1 && targetMaterial != null)
        {
            StartCoroutine(AnimateBlendShapeAndCutoff());
        }

        // Désactiver le collider immédiatement
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
            Debug.Log("WinManager: Collider du target object désactivé");
        }

        // Appeler l'événement de victoire
        OnWinConditionMet();
    }

    private IEnumerator AnimateBlendShapeAndCutoff()
    {
        float elapsedTime = 0f;
        float startBlendShapeValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        float startCutoff = targetMaterial.GetFloat(CutoffProperty);

        Debug.Log($"WinManager: Animation du BlendShape de {startBlendShapeValue} vers {targetBlendShapeValue}");
        Debug.Log($"WinManager: Animation du Cutoff de {startCutoff} vers {targetCutoffValue}");

        while (elapsedTime < blendShapeTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / blendShapeTransitionDuration;

            // Animation du BlendShape
            float blendShapeCurveValue = blendShapeCurve.Evaluate(progress);
            float currentBlendShapeValue = Mathf.Lerp(startBlendShapeValue, targetBlendShapeValue, blendShapeCurveValue);
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentBlendShapeValue);

            // Animation du Cutoff
            AnimationCurve cutoffCurveToUse = useSameCurveForCutoff ? blendShapeCurve : cutoffCurve;
            float cutoffCurveValue = cutoffCurveToUse.Evaluate(progress);
            float currentCutoffValue = Mathf.Lerp(startCutoffValue, targetCutoffValue, cutoffCurveValue);
            targetMaterial.SetFloat(CutoffProperty, currentCutoffValue);

            yield return null;
        }

        // S'assurer que les valeurs finales sont exactes
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, targetBlendShapeValue);
        targetMaterial.SetFloat(CutoffProperty, targetCutoffValue);

        Debug.Log($"WinManager: BlendShape '{blendShapeName}' animation terminée à {targetBlendShapeValue}");
        Debug.Log($"WinManager: Cutoff animation terminée à {targetCutoffValue}");
    }

    private void OnWinConditionMet()
    {
        // Ici vous pouvez ajouter d'autres actions à effectuer lors de la victoire
        Debug.Log("WinManager: Condition de victoire remplie!");

        // Exemple d'actions supplémentaires:
        // AudioSource.PlayClipAtPoint(winSound, transform.position);
        // UIManager.Instance.ShowWinScreen();
        // SceneManager.LoadScene("NextLevel");
    }

    // Méthodes publiques pour contrôler l'état
    public void SetCompleted(bool completed)
    {
        isCollectComplete = completed;
        Debug.Log($"WinManager: IsCompleted défini à {completed}");
    }

    public bool GetCompleted()
    {
        return isCollectComplete;
    }

    public void ResetWinManager()
    {
        // Arrêter toutes les coroutines en cours
        StopAllCoroutines();

        hasTriggered = false;
        isCollectComplete = false;

        // Remettre le blendshape à sa valeur initiale
        if (blendShapeIndex != -1 && skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, initialBlendShapeValue);
        }

        // Remettre le cutoff à sa valeur initiale
        if (targetMaterial != null && targetMaterial.HasProperty(CutoffProperty))
        {
            targetMaterial.SetFloat(CutoffProperty, initialCutoffValue);
        }

        // Réactiver le collider
        if (targetCollider != null)
        {
            targetCollider.enabled = true;
        }

        Debug.Log("WinManager: Reset effectué");
    }

    // Méthodes pour tester en mode éditeur
    [ContextMenu("Test Animation")]
    public void TestAnimation()
    {
        if (Application.isPlaying && blendShapeIndex != -1 && targetMaterial != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateBlendShapeAndCutoff());
        }
    }

    [ContextMenu("Set Cutoff to Start Value")]
    public void SetCutoffToStart()
    {
        if (targetMaterial != null && targetMaterial.HasProperty(CutoffProperty))
        {
            targetMaterial.SetFloat(CutoffProperty, startCutoffValue);
        }
    }

    [ContextMenu("Set Cutoff to Target Value")]
    public void SetCutoffToTarget()
    {
        if (targetMaterial != null && targetMaterial.HasProperty(CutoffProperty))
        {
            targetMaterial.SetFloat(CutoffProperty, targetCutoffValue);
        }
    }

    // Nettoyage du material instancié
    void OnDestroy()
    {
        if (targetMaterial != null && targetRenderer != null)
        {
            // Si c'est une instance créée par le script, la détruire
            if (targetMaterial != targetRenderer.sharedMaterial)
            {
                if (Application.isPlaying)
                    Destroy(targetMaterial);
                else
                    DestroyImmediate(targetMaterial);
            }
        }
    }

    // Pour débugger dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (GetComponent<Collider>() != null)
        {
            Gizmos.color = isCollectComplete ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
        }
    }
}