using UnityEngine;

public class POVHandsManager : MonoBehaviour
{
    [Header("Hand Transforms")]
    public Transform leftHand;
    public Transform rightHand;
    
    [Header("Hand Offsets pour POV")]
    [SerializeField] private Vector3 normalLeftOffset = new Vector3(-0.3f, -0.1f, 0.4f);
    [SerializeField] private Vector3 normalRightOffset = new Vector3(0.3f, -0.1f, 0.4f);
    [SerializeField] private Vector3 crouchLeftOffset = new Vector3(-0.25f, 0.05f, 0.3f);
    [SerializeField] private Vector3 crouchRightOffset = new Vector3(0.25f, 0.05f, 0.3f);
    
    private POVCameraTransitionManager cameraManager;
    private PlayerHandController handController;
    
    void Start()
    {
        cameraManager = GetComponent<POVCameraTransitionManager>();
        handController = GetComponent<PlayerHandController>();
    }
    
    void Update()
    {
        UpdateHandPositions();
    }
    
    void UpdateHandPositions()
    {
        if (cameraManager == null || cameraManager.IsTransitioning) return;
        
        bool isCrouching = cameraManager.IsCrouching;
        
        // Ajuster les positions des mains selon l'état
        if (leftHand != null)
        {
            Vector3 targetOffset = isCrouching ? crouchLeftOffset : normalLeftOffset;
            leftHand.localPosition = Vector3.Lerp(leftHand.localPosition, targetOffset, Time.deltaTime * 5f);
        }
        
        if (rightHand != null)
        {
            Vector3 targetOffset = isCrouching ? crouchRightOffset : normalRightOffset;
            rightHand.localPosition = Vector3.Lerp(rightHand.localPosition, targetOffset, Time.deltaTime * 5f);
        }
    }
    
    // Méthode pour ajuster les objets tenus pendant la transition
    public void AdjustHeldObjectsForCrouch(bool isCrouching)
    {
        if (handController == null) return;
        
        // Ajuster les objets dans la main droite
        if (handController.rightHeldObject != null)
        {
            AdjustObjectPosition(handController.rightHeldObject.transform, isCrouching, true);
        }
        
        // Ajuster les objets dans la main gauche
        if (handController.leftHeldObject != null)
        {
            AdjustObjectPosition(handController.leftHeldObject.transform, isCrouching, false);
        }
    }
    
    void AdjustObjectPosition(Transform objectTransform, bool isCrouching, bool isRightHand)
    {
        if (objectTransform == null) return;
        
        // Ajustements subtils pour que les objets restent bien en main
        Vector3 adjustment = isCrouching ? new Vector3(0, 0.1f, -0.1f) : Vector3.zero;
        
        if (!isRightHand)
            adjustment.x *= -1; // Inverser pour la main gauche
        
        objectTransform.localPosition += adjustment;
    }
}