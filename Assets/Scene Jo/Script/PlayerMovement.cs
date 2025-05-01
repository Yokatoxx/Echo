using UnityEngine;
using Cinemachine; // N'oubliez pas cette ligne !

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CinemachineImpulseSource))] // Assure que le composant est présent
public class PlayerMovement : MonoBehaviour
{
    // ... (autres variables existantes) ...
    public float speed = 5f;
    public float runSpeed = 8f;
    public float gravity = 9.81f;
    public Stamina stamina;

    private CharacterController characterController;
    private float verticalVelocity = 0f;
    public bool isHiding = false;

    public LayerMask hidingLayer;
    public float hidingCheckDistance = 2f;

    private Vector3 hidePosition;
    private Quaternion hideRotation;
    private Camera cam;
    private AudioManager audioManager;

    public bool isWalking = false;
    private Transform cameraTransform;
    public float bodyRotationSpeed = 10f;

    // Référence à l'Impulse Source
    private CinemachineImpulseSource impulseSource;


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>(); // Récupérer le composant
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Camera principale non trouvée ! Taggez votre caméra principale avec 'MainCamera'.");
            cameraTransform = transform;
        }
        else
        {
            cameraTransform = cam.transform;
        }

        audioManager = FindObjectOfType<AudioManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Vérification que l'impulse source est bien là
        if (impulseSource == null)
        {
            Debug.LogWarning("CinemachineImpulseSource non trouvé sur le joueur. L'effet d'impact ne fonctionnera pas.");
        }
    }

    void Update()
    {
        HandleHidingInput();

        if (!isHiding)
        {
            HandleMovement();
        }
    }

    void HandleMovement()
    {
        // --- Rotation gérée par Cinemachine ---

        // --- Calcul Vitesse ---
        // Déterminer si on essaie de sprinter MAINTENANT pour la logique de mouvement
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && stamina != null && stamina.CanSprint();
        float currentSpeed = isTryingToSprint ? runSpeed : speed;

        // Consommer la stamina si on sprinte effectivement
        if (isTryingToSprint && (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0)) // Seulement si on bouge en sprintant
        {
            stamina.UseStamina(Time.deltaTime);
            // Si CanSprint devient faux PENDANT ce UseStamina, on repasse à la vitesse normale pour ce frame
            if (!stamina.CanSprint())
            {
                currentSpeed = speed;
                isTryingToSprint = false; // Mettre à jour l'état pour la logique de collision
            }
        }


        // --- Calcul Direction Mouvement ---
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

        // --- Orientation du Corps ---
        if (desiredMovementHorizontal.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredMovementHorizontal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
        }

        // --- Gravité ---
        if (characterController.isGrounded)
        {
            verticalVelocity = -gravity * Time.deltaTime;
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

    // ... (HandleHidingInput, HidePlayer, UnhidePlayer restent les mêmes) ...
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
                    if (hit.collider.CompareTag("Cachette"))
                    {
                        HidePlayer(hit.collider.transform);
                        return;
                    }
                }
            }
            else
            {
                UnhidePlayer();
                return;
            }
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
        // Collision avec le décor
        if (!isHiding && hit.gameObject.CompareTag("Decor"))
        {
            // Vérifier si on sprintait activement LORS de l'impact
            // On regarde si la touche Shift est enfoncée ET si on avait encore de la stamina juste avant/pendant l'impact
            // Et si la vitesse de collision est suffisante (pour éviter les déclenchements en frôlant)
            bool wasSprintingOnImpact = Input.GetKey(KeyCode.LeftShift)
                                       && stamina != null && stamina.currentStamina > 0.01f // <-- Ligne corrigée
                                       && characterController.velocity.magnitude > (speed + (runSpeed - speed) * 0.5f);

            if (wasSprintingOnImpact && impulseSource != null)
            {
                // Déclencher l'Impulsion Cinemachine !
                impulseSource.GenerateImpulse();

                // Optionnel : Jouer un son d'impact plus fort ici ?
                // if (audioManager != null) audioManager.PlayHardCollisionSound(hit.point);
            }
            else
            {
                // Jouer le son de collision normal si on ne sprintait pas (ou si pas d'impulse source)
                if (audioManager != null && characterController.velocity.magnitude > 0.1f)
                {
                    audioManager.PlayDecorCollisionSound(hit.point);
                }
            }
        }
    }
}