using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float runSpeed = 8f;
    public float mouseSensitivity = 2f;
    public Transform playerCamera;
    public float gravity = 9.81f;
    public Stamina stamina; // Référence au script Stamina

    private float verticalRotation = 0f;
    private CharacterController characterController;
    private float verticalVelocity = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        if (stamina == null)
        {
            Debug.LogError("Stamina script is not assigned.");
        }
    }

    void Update()
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

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
