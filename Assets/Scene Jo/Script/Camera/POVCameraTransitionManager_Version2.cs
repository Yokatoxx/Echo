using UnityEngine;
using Cinemachine;

public class POVCameraTransitionManager : MonoBehaviour
{
    [Header("Cinemachine POV Cameras")]
    public CinemachineVirtualCamera normalPOVCamera;
    public CinemachineVirtualCamera crouchPOVCamera;

    [Header("Player References")]
    public Transform playerHead;
    public Transform handsParent;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeightOffset = -0.6f;
    [Range(1f, 20f)]
    public float transitionSpeed = 8f;

    [Header("Hand Positions")]
    public Vector3 normalHandsLocalPos = new Vector3(0, -0.4f, 0.6f);
    public Vector3 crouchHandsLocalPos = new Vector3(0, -0.2f, 0.4f);

    [Header("FOV Settings")]
    public float normalFOV = 75f;
    public float crouchFOV = 70f;

    [Header("Advanced Settings")]
    [Range(0.05f, 1f)]
    public float cinemachineBlendTime = 0.15f;
    public bool useInstantCameraSwitch = false;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivityX = 200f;
    public float mouseSensitivityY = 200f;
    [Range(-90f, 0f)]
    public float minVerticalAngle = -80f;
    [Range(0f, 90f)]
    public float maxVerticalAngle = 80f;

    private bool isCrouching = false;
    private bool wasPressingCrouch = false;
    private bool wantsToCrouch = false;

    // Composants POV
    private CinemachinePOV normalPOV;
    private CinemachinePOV crouchPOV;
    private CinemachineBrain cinemachineBrain;

    // Variables pour l'interpolation continue
    private float currentTransitionProgress = 0f;

    // Variables pour la gestion centralisée de la rotation
    private float currentHorizontalRotation = 0f;
    private float currentVerticalRotation = 0f;
    private bool isTransitioning = false;

    void Start()
    {
        SetupPOVCameras();

        // Récupérer et configurer le Cinemachine Brain
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        if (cinemachineBrain != null)
        {
            UpdateCinemachineBlendTime();
        }

        // Démarrer avec la caméra normale
        normalPOVCamera.Priority = 10;
        crouchPOVCamera.Priority = 5;
        currentTransitionProgress = 0f;

        // Désactiver l'input automatique des POV pour le contrôler manuellement
        DisablePOVInput();

        // Initialiser les rotations
        if (normalPOV != null)
        {
            currentHorizontalRotation = normalPOV.m_HorizontalAxis.Value;
            currentVerticalRotation = normalPOV.m_VerticalAxis.Value;
        }

        // Verrouiller le curseur
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCrouchInput();
        HandleMouseInput();
        UpdateTransition();
        ApplyRotationToCameras();

        // Debug en temps réel
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"Transition Progress: {currentTransitionProgress:F2}, H: {currentHorizontalRotation:F1}, V: {currentVerticalRotation:F1}");
        }

        // Déverrouiller le curseur avec Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // Reverrouiller le curseur en cliquant
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void HandleCrouchInput()
    {
        bool isPressingCrouch = Input.GetKey(crouchKey);
        wantsToCrouch = isPressingCrouch;
        wasPressingCrouch = isPressingCrouch;
    }

    void HandleMouseInput()
    {
        // Gérer l'input de la souris de manière centralisée
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

            // Appliquer la rotation horizontale
            currentHorizontalRotation += mouseX;

            // Appliquer la rotation verticale avec clamp
            currentVerticalRotation -= mouseY; // Inversion pour un contrôle naturel
            currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, minVerticalAngle, maxVerticalAngle);
        }
    }

    void ApplyRotationToCameras()
    {
        // Appliquer les rotations aux deux caméras
        if (normalPOV != null)
        {
            normalPOV.m_HorizontalAxis.Value = currentHorizontalRotation;
            normalPOV.m_VerticalAxis.Value = currentVerticalRotation;
        }

        if (crouchPOV != null)
        {
            crouchPOV.m_HorizontalAxis.Value = currentHorizontalRotation;
            crouchPOV.m_VerticalAxis.Value = currentVerticalRotation;
        }
    }

    void DisablePOVInput()
    {
        // Désactiver l'input automatique des composants POV
        if (normalPOV != null)
        {
            normalPOV.m_HorizontalAxis.m_InputAxisName = "";
            normalPOV.m_VerticalAxis.m_InputAxisName = "";
        }

        if (crouchPOV != null)
        {
            crouchPOV.m_HorizontalAxis.m_InputAxisName = "";
            crouchPOV.m_VerticalAxis.m_InputAxisName = "";
        }
    }

    void UpdateTransition()
    {
        // Déterminer la direction de la transition
        float targetProgress = wantsToCrouch ? 1f : 0f;

        // Vérifier si on est en train de transitioner
        isTransitioning = Mathf.Abs(currentTransitionProgress - targetProgress) > 0.001f;

        // Interpoler vers la cible avec la vitesse définie
        if (isTransitioning)
        {
            float previousProgress = currentTransitionProgress;

            currentTransitionProgress = Mathf.MoveTowards(
                currentTransitionProgress,
                targetProgress,
                transitionSpeed * Time.deltaTime
            );

            // Mettre à jour les états seulement si le progrès a changé
            if (Mathf.Abs(currentTransitionProgress - previousProgress) > 0.001f)
            {
                UpdateCameraState();
                UpdateHandsPosition();
                UpdateFOV();
            }
        }

        // Mettre à jour l'état booléen pour compatibilité
        bool shouldBeCrouching = currentTransitionProgress > 0.5f;
        if (shouldBeCrouching != isCrouching)
        {
            isCrouching = shouldBeCrouching;
        }
    }

    void UpdateCameraState()
    {
        if (useInstantCameraSwitch)
        {
            // Switch instantané sans blend Cinemachine
            if (currentTransitionProgress > 0.5f)
            {
                crouchPOVCamera.Priority = 10;
                normalPOVCamera.Priority = 5;
            }
            else
            {
                normalPOVCamera.Priority = 10;
                crouchPOVCamera.Priority = 5;
            }
        }
        else
        {
            // Utiliser une transition plus agressive
            float threshold = 0.1f;

            if (wantsToCrouch && currentTransitionProgress > threshold)
            {
                crouchPOVCamera.Priority = 10;
                normalPOVCamera.Priority = 5;
            }
            else if (!wantsToCrouch && currentTransitionProgress < (1f - threshold))
            {
                normalPOVCamera.Priority = 10;
                crouchPOVCamera.Priority = 5;
            }
        }
    }

    void UpdateHandsPosition()
    {
        if (handsParent == null) return;

        Vector3 targetPosition = Vector3.Lerp(normalHandsLocalPos, crouchHandsLocalPos, currentTransitionProgress);
        handsParent.localPosition = targetPosition;
    }

    void UpdateFOV()
    {
        float targetFOV = Mathf.Lerp(normalFOV, crouchFOV, currentTransitionProgress);

        if (normalPOVCamera != null)
            normalPOVCamera.m_Lens.FieldOfView = targetFOV;
        if (crouchPOVCamera != null)
            crouchPOVCamera.m_Lens.FieldOfView = targetFOV;
    }

    void UpdateCinemachineBlendTime()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Time = cinemachineBlendTime;
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            if (cinemachineBrain != null)
            {
                UpdateCinemachineBlendTime();
            }

            // Mettre à jour la sensibilité en temps réel
            if (normalPOV != null)
            {
                normalPOV.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
                normalPOV.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
                normalPOV.m_VerticalAxis.m_MinValue = minVerticalAngle;
                normalPOV.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
            }

            if (crouchPOV != null)
            {
                crouchPOV.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
                crouchPOV.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
                crouchPOV.m_VerticalAxis.m_MinValue = minVerticalAngle;
                crouchPOV.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
            }
        }
    }

    void SetupPOVCameras()
    {
        // Configuration caméra normale
        if (normalPOVCamera != null)
        {
            normalPOVCamera.Follow = playerHead;
            normalPOV = normalPOVCamera.GetCinemachineComponent<CinemachinePOV>();

            if (normalPOV == null)
            {
                normalPOV = normalPOVCamera.AddCinemachineComponent<CinemachinePOV>();
            }

            // Configuration de base - l'input sera géré manuellement
            normalPOV.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
            normalPOV.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
            normalPOV.m_VerticalAxis.m_MinValue = minVerticalAngle;
            normalPOV.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
            normalPOV.m_VerticalAxis.m_Wrap = false;
            normalPOV.m_HorizontalAxis.m_Wrap = true;

            normalPOVCamera.m_Lens.FieldOfView = normalFOV;
        }

        // Configuration caméra accroupie
        if (crouchPOVCamera != null)
        {
            GameObject crouchTarget = new GameObject("CrouchCameraTarget");
            crouchTarget.transform.SetParent(playerHead);
            crouchTarget.transform.localPosition = new Vector3(0, crouchHeightOffset, 0);
            crouchTarget.transform.localRotation = Quaternion.identity;

            crouchPOVCamera.Follow = crouchTarget.transform;
            crouchPOV = crouchPOVCamera.GetCinemachineComponent<CinemachinePOV>();

            if (crouchPOV == null)
            {
                crouchPOV = crouchPOVCamera.AddCinemachineComponent<CinemachinePOV>();
            }

            // Configuration identique à la caméra normale
            crouchPOV.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
            crouchPOV.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
            crouchPOV.m_VerticalAxis.m_MinValue = minVerticalAngle;
            crouchPOV.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
            crouchPOV.m_VerticalAxis.m_Wrap = false;
            crouchPOV.m_HorizontalAxis.m_Wrap = true;

            crouchPOVCamera.m_Lens.FieldOfView = crouchFOV;
        }
    }

    // Accesseurs publics
    public bool IsCrouching => isCrouching;
    public bool WantsToCrouch => wantsToCrouch;
    public float TransitionProgress => currentTransitionProgress;
    public bool IsTransitioning => isTransitioning;

    // Méthodes pour contrôler la sensibilité depuis l'extérieur
    public void SetMouseSensitivity(float sensitivityX, float sensitivityY)
    {
        mouseSensitivityX = sensitivityX;
        mouseSensitivityY = sensitivityY;
    }

    // Méthodes pour test et debug
    [ContextMenu("Test Crouch Transition")]
    public void TestCrouchTransition()
    {
        wantsToCrouch = !wantsToCrouch;
        Debug.Log($"Forcing transition to: {(wantsToCrouch ? "Crouch" : "Normal")}");
    }

    [ContextMenu("Reset Camera Rotation")]
    public void ResetCameraRotation()
    {
        currentHorizontalRotation = 0f;
        currentVerticalRotation = 0f;
        ApplyRotationToCameras();
    }
}