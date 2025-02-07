using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float runSpeed = 8f;
    public float mouseSensitivity = 2f;
    public Transform playerCamera;
    public float gravity = 9.81f;
    public Stamina stamina;

    private float verticalRotation = 0f;
    private CharacterController characterController;
    private float verticalVelocity = 0f;
    public bool isHiding = false;

    public LayerMask hidingLayer;
    public float hidingCheckDistance = 2f;

    private Vector3 hidePosition;
    private Camera cam;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cam = playerCamera.GetComponent<Camera>();

        if (characterController == null)
        {
            Debug.LogError("CharacterController non trouvé.");
        }

        if (cam == null)
        {
            Debug.LogError("Camera non trouvée sur playerCamera.");
        }
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
            HandleRotation();
        }
    }

    void HandleMovement()
    {
        float currentSpeed = speed;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && stamina.CanSprint();

        if (isSprinting)
        {
            currentSpeed = runSpeed;
            stamina.UseStamina(Time.deltaTime);
        }

        float moveForward = Input.GetAxis("Vertical");
        float moveSide = Input.GetAxis("Horizontal");
        Vector3 movement = transform.forward * moveForward + transform.right * moveSide;

        if (characterController.isGrounded)
        {
            verticalVelocity = -gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        movement.y = verticalVelocity;
        characterController.Move(movement * currentSpeed * Time.deltaTime);

        HandleRotation();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleHidingInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!isHiding)
            {
                if (Physics.Raycast(ray, out hit, hidingCheckDistance, hidingLayer))
                {
                    if (hit.collider.CompareTag("Cachette"))
                    {
                        HidePlayer(hit);
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

    void HidePlayer(RaycastHit hit)
    {
        hidePosition = transform.position;

        Vector3 hideTargetPosition = hit.collider.transform.position + Vector3.down * 1f;

        characterController.enabled = false;
        transform.position = hideTargetPosition;
        characterController.enabled = true;

        isHiding = true;
    }

    void UnhidePlayer()
    {
        characterController.enabled = false;
        transform.position = hidePosition;
        characterController.enabled = true;

        isHiding = false;
    }
}
