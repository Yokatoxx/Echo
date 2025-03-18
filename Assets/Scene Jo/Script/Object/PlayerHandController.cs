using UnityEngine;
using UnityEngine.UI;

public class PlayerHandController : MonoBehaviour
{
    [Header("Paramètres d'interaction")]
    public float interactionDistance = 3f;
    public LayerMask interactableLayers;
    public KeyCode pickupRightKey = KeyCode.E;
    public KeyCode pickupLeftKey = KeyCode.Q;
    public KeyCode dropRightKey = KeyCode.R;
    public KeyCode dropLeftKey = KeyCode.F;

    [Header("Mains")]
    public Transform rightHandPosition;
    public Transform leftHandPosition;

    [Header("Affichage d'informations")]
    public bool showInteractionPrompt = true;
    public Text interactionPromptText;

    private Camera mainCamera;
    public Collectable rightHeldObject;
    private Collectable leftHeldObject;
    private RaycastHit hitInfo;

    void Start()
    {
        mainCamera = Camera.main;
        SetupHandTransform(ref rightHandPosition, "RightHandPosition", new Vector3(0.5f, -0.3f, 1f));
        SetupHandTransform(ref leftHandPosition, "LeftHandPosition", new Vector3(-0.5f, -0.3f, 1f));

        if (rightHandPosition != null)
            rightHandPosition.localScale = Vector3.one;
        if (leftHandPosition != null)
            leftHandPosition.localScale = Vector3.one;

        if (interactionPromptText != null)
            interactionPromptText.gameObject.SetActive(false);
    }

    private void SetupHandTransform(ref Transform handTransform, string handName, Vector3 defaultPosition)
    {
        if (handTransform == null)
        {
            GameObject handObj = new GameObject(handName);
            handObj.transform.SetParent(mainCamera.transform);
            handObj.transform.localPosition = defaultPosition;
            handObj.transform.localScale = Vector3.one;
            handTransform = handObj.transform;
        }
    }

    void Update()
    {
        HandleRaycastInteraction();
        HandleInputs();
    }

    private void HandleRaycastInteraction()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        bool hit = Physics.Raycast(ray, out hitInfo, interactionDistance, interactableLayers);

        if (hit && hitInfo.collider.TryGetComponent<Collectable>(out Collectable collectable))
        {
            if (showInteractionPrompt)
            {
                string promptMessage = "";

                if (rightHeldObject == null)
                    promptMessage += "Appuyez sur " + pickupRightKey + " pour ramasser avec la main droite. ";

                if (leftHeldObject == null)
                    promptMessage += "Appuyez sur " + pickupLeftKey + " pour ramasser avec la main gauche.";

                if (!string.IsNullOrEmpty(promptMessage) && interactionPromptText != null)
                {
                    interactionPromptText.text = promptMessage;
                    interactionPromptText.gameObject.SetActive(true);
                }
                else
                {
                    Debug.Log(promptMessage);
                }
            }
        }
        else if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(pickupRightKey) && rightHeldObject == null)
            TryPickupObject(rightHandPosition, ref rightHeldObject);

        if (Input.GetKeyDown(pickupLeftKey) && leftHeldObject == null)
            TryPickupObject(leftHandPosition, ref leftHeldObject);

        if (rightHeldObject != null && Input.GetKeyDown(dropRightKey))
        {
            rightHeldObject.Drop();
            rightHeldObject = null;
        }

        if (leftHeldObject != null && Input.GetKeyDown(dropLeftKey))
        {
            leftHeldObject.Drop();
            leftHeldObject = null;
        }
    }

    private void TryPickupObject(Transform handTransform, ref Collectable heldObjectRef)
    {
        if (Physics.Raycast(mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)),
                out RaycastHit hit, interactionDistance, interactableLayers))
        {
            if (hit.collider.TryGetComponent<Collectable>(out Collectable collectable) && collectable.canBePickedUp)
            {
                collectable.Pickup(handTransform);
                heldObjectRef = collectable;

                if (interactionPromptText != null)
                    interactionPromptText.gameObject.SetActive(false);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (mainCamera != null)
        {
            Gizmos.color = Color.red;
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            Gizmos.DrawRay(ray.origin, ray.direction * interactionDistance);
        }
    }
}
