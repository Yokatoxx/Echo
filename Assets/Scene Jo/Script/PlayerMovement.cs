using UnityEngine;
using Cinemachine;
using System.Collections; // Nécessaire pour Time.time

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float runSpeed = 8f;
    public float gravity = 9.81f;
    public float bodyRotationSpeed = 10f;

    [Header("Stamina")]
    public Stamina stamina;

    [Header("Hiding")]
    public bool isHiding = false;
    public LayerMask hidingLayer;
    public float hidingCheckDistance = 2f;

    [Header("Impulse Setup")]
    [Tooltip("Assignez ici l'objet enfant qui porte l'Impulse Source (ex: CameraTarget).")]
    public Transform impulseSourceTarget;
    [Tooltip("Temps minimum (en secondes) entre deux impulsions de collision.")]
    public float impulseCooldown = 0.5f;

    // --- Private Variables ---
    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource;
    private Camera cam;
    private Transform cameraTransform;
    private AudioManager audioManager;

    private float verticalVelocity = 0f;
    private Vector3 hidePosition;
    private Quaternion hideRotation;
    public bool isWalking = false;
    private float lastImpulseTime = -1f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (impulseSourceTarget != null)
        {
            impulseSource = impulseSourceTarget.GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                Debug.LogError("CinemachineImpulseSource non trouvé sur l'objet cible assigné (" + impulseSourceTarget.name + "). L'effet d'impact ne fonctionnera pas.", this);
            }
        }
        else
        {
            Debug.LogError("La variable 'Impulse Source Target' n'est pas assignée dans l'inspecteur pour PlayerMovement. L'effet d'impact ne fonctionnera pas.", this);
        }

        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Camera principale non trouvée ! Taggez votre caméra principale avec 'MainCamera'. Utilisation du transform du joueur comme fallback.", this);
            cameraTransform = transform;
        }
        else
        {
            cameraTransform = cam.transform;
        }

        audioManager = FindObjectOfType<AudioManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialiser lastImpulseTime pour permettre une impulsion dès le début
        lastImpulseTime = -impulseCooldown;
    }

    void Update()
    {
        HandleHidingInput();

        if (!isHiding)
        {
            HandleMovement();
        }
        else
        {
            isWalking = false;
        }
    }

    void HandleMovement()
    {
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && stamina != null && stamina.CanSprint();
        float currentSpeed = isTryingToSprint ? runSpeed : speed;

        if (isTryingToSprint && (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0))
        {
            stamina.UseStamina(Time.deltaTime);
            if (!stamina.CanSprint())
            {
                currentSpeed = speed;
                isTryingToSprint = false;
            }
        }

        float moveForwardInput = Input.GetAxisRaw("Vertical");
        float moveSideInput = Input.GetAxisRaw("Horizontal");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMovementHorizontal = (forward * moveForwardInput + right * moveSideInput).normalized;
        isWalking = desiredMovementHorizontal.magnitude > 0.1f;

        if (characterController.isGrounded)
        {
            verticalVelocity = -gravity * Time.deltaTime * 2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // --- Application du Mouvement ---
        Vector3 finalMovement = desiredMovementHorizontal * currentSpeed;
        finalMovement.y = verticalVelocity;
        characterController.Move(finalMovement * Time.deltaTime);
    }
    void HandleHidingInput()
    {
        if (cam == null) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!characterController.isGrounded && !isHiding) return;
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (!isHiding)
            {
                if (Physics.Raycast(ray, out hit, hidingCheckDistance, hidingLayer))
                {
                    if (hit.collider.CompareTag("Cachette")) { HidePlayer(hit.collider.transform); return; }
                }
            }
            else { UnhidePlayer(); return; }
        }
    }
    void HidePlayer(Transform hidingSpot)
    {
        hidePosition = transform.position;
        hideRotation = transform.rotation;
        Vector3 targetPosition = hidingSpot.position + hidingSpot.forward * 0.5f;
        characterController.enabled = false;
        transform.position = targetPosition;
        transform.rotation = Quaternion.LookRotation(-hidingSpot.forward);
        characterController.enabled = true;
        isHiding = true;
        isWalking = false;
    }
    void UnhidePlayer()
    {
        characterController.enabled = false;
        transform.position = hidePosition;
        transform.rotation = hideRotation;
        characterController.enabled = true;
        isHiding = false;
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isHiding || impulseSource == null)
        {
            return;
        }

        if (hit.gameObject.CompareTag("Decor"))
        {
            bool wasSprintingOnImpact = Input.GetKey(KeyCode.LeftShift)
                                       && stamina != null && stamina.currentStamina > 0.01f
                                       && characterController.velocity.magnitude > (speed + 0.1f);

            // Vérifie si on sprintait ET si le cooldown est terminé
            if (wasSprintingOnImpact && Time.time >= lastImpulseTime + impulseCooldown)
            {
                Vector3 impactVelocity = characterController.velocity;
                impactVelocity.y = 0;

                if (impactVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 impulseDirection = -impactVelocity.normalized;
                    impulseSource.GenerateImpulse(impulseDirection);

                    // Mettre à jour le temps de la dernière impulsion SEULEMENT si une impulsion a été générée
                    lastImpulseTime = Time.time;

                }
            }
            else if (!wasSprintingOnImpact)
            {
                if (audioManager != null && characterController.velocity.magnitude > 0.1f)
                {
                    audioManager.PlayDecorCollisionSound(hit.point);
                }
            }
        }
    }
}