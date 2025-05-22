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
    private bool wantsToCrouch = false;
    private CinemachinePOV normalPOV;
    private CinemachinePOV crouchPOV;
    private CinemachineBrain cinemachineBrain;
    private float currentTransitionProgress = 0f;
    private float currentHorizontalRotation = 0f;
    private float currentVerticalRotation = 0f;
    private bool isTransitioning = false;

    void Start()
    {
        SetupPOVCameras();
        SetupCinemachineBrain();
        InitializeCameraPriorities();
        DisablePOVInput();
        InitializeRotations();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleInput();
        UpdateTransition();
        ApplyRotationToCameras();
        HandleCursorToggle();
    }

    void HandleInput()
    {
        wantsToCrouch = Input.GetKey(crouchKey);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

            currentHorizontalRotation += mouseX;
            currentVerticalRotation = Mathf.Clamp(currentVerticalRotation - mouseY, minVerticalAngle, maxVerticalAngle);
        }
    }

    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void UpdateTransition()
    {
        float targetProgress = wantsToCrouch ? 1f : 0f;
        isTransitioning = Mathf.Abs(currentTransitionProgress - targetProgress) > 0.001f;

        if (isTransitioning)
        {
            float previousProgress = currentTransitionProgress;
            currentTransitionProgress = Mathf.MoveTowards(currentTransitionProgress, targetProgress, transitionSpeed * Time.deltaTime);

            if (Mathf.Abs(currentTransitionProgress - previousProgress) > 0.001f)
            {
                UpdateCameraState();
                UpdateHandsPosition();
                UpdateFOV();
            }
        }

        bool shouldBeCrouching = currentTransitionProgress > 0.5f;
        if (shouldBeCrouching != isCrouching)
            isCrouching = shouldBeCrouching;
    }

    void UpdateCameraState()
    {
        float threshold = useInstantCameraSwitch ? 0.5f : 0.1f;

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

    void UpdateHandsPosition()
    {
        if (handsParent != null)
            handsParent.localPosition = Vector3.Lerp(normalHandsLocalPos, crouchHandsLocalPos, currentTransitionProgress);
    }

    void UpdateFOV()
    {
        float targetFOV = Mathf.Lerp(normalFOV, crouchFOV, currentTransitionProgress);
        normalPOVCamera.m_Lens.FieldOfView = targetFOV;
        crouchPOVCamera.m_Lens.FieldOfView = targetFOV;
    }

    void ApplyRotationToCameras()
    {
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

    void SetupPOVCameras()
    {
        SetupPOVCamera(normalPOVCamera, playerHead, ref normalPOV, normalFOV);

        GameObject crouchTarget = new GameObject("CrouchCameraTarget");
        crouchTarget.transform.SetParent(playerHead);
        crouchTarget.transform.localPosition = new Vector3(0, crouchHeightOffset, 0);
        crouchTarget.transform.localRotation = Quaternion.identity;

        SetupPOVCamera(crouchPOVCamera, crouchTarget.transform, ref crouchPOV, crouchFOV);
    }

    void SetupPOVCamera(CinemachineVirtualCamera camera, Transform followTarget, ref CinemachinePOV pov, float fov)
    {
        if (camera == null) return;

        camera.Follow = followTarget;
        pov = camera.GetCinemachineComponent<CinemachinePOV>() ?? camera.AddCinemachineComponent<CinemachinePOV>();

        pov.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
        pov.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
        pov.m_VerticalAxis.m_MinValue = minVerticalAngle;
        pov.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
        pov.m_VerticalAxis.m_Wrap = false;
        pov.m_HorizontalAxis.m_Wrap = true;

        camera.m_Lens.FieldOfView = fov;
    }

    void SetupCinemachineBrain()
    {
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = cinemachineBlendTime;
    }

    void InitializeCameraPriorities()
    {
        normalPOVCamera.Priority = 10;
        crouchPOVCamera.Priority = 5;
        currentTransitionProgress = 0f;
    }

    void DisablePOVInput()
    {
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

    void InitializeRotations()
    {
        if (normalPOV != null)
        {
            currentHorizontalRotation = normalPOV.m_HorizontalAxis.Value;
            currentVerticalRotation = normalPOV.m_VerticalAxis.Value;
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = cinemachineBlendTime;

        UpdatePOVSettings(normalPOV);
        UpdatePOVSettings(crouchPOV);
    }

    void UpdatePOVSettings(CinemachinePOV pov)
    {
        if (pov == null) return;

        pov.m_HorizontalAxis.m_MaxSpeed = mouseSensitivityX;
        pov.m_VerticalAxis.m_MaxSpeed = mouseSensitivityY;
        pov.m_VerticalAxis.m_MinValue = minVerticalAngle;
        pov.m_VerticalAxis.m_MaxValue = maxVerticalAngle;
    }

    public bool IsCrouching => isCrouching;
    public bool WantsToCrouch => wantsToCrouch;
    public float TransitionProgress => currentTransitionProgress;
    public bool IsTransitioning => isTransitioning;

    public void SetMouseSensitivity(float sensitivityX, float sensitivityY)
    {
        mouseSensitivityX = sensitivityX;
        mouseSensitivityY = sensitivityY;
    }

    [ContextMenu("Test Crouch Transition")]
    public void TestCrouchTransition()
    {
        wantsToCrouch = !wantsToCrouch;
    }

    [ContextMenu("Reset Camera Rotation")]
    public void ResetCameraRotation()
    {
        currentHorizontalRotation = 0f;
        currentVerticalRotation = 0f;
        ApplyRotationToCameras();
    }
}