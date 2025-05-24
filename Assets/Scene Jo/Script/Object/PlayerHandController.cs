using UnityEngine;
using UnityEngine.UI;

public class PlayerHandController : MonoBehaviour
{
    [Header("Paramètres d'interaction")]
    public float interactionDistance = 10f;
    public LayerMask interactableLayers;
    public KeyCode pickupKey = KeyCode.E;

    [Header("Mains")]
    public Transform rightHandPosition;
    public Transform leftHandPosition;

    [Header("Images des objets")]
    public Image rightHandItemImage;
    public Image leftHandItemImage;

    [Header("Surbrillance des mains")]
    public Image rightHandHighlight;
    public Image leftHandHighlight;

    private Camera mainCamera;
    public Collectable rightHeldObject;
    [HideInInspector]
    public Collectable leftHeldObject;
    [HideInInspector]
    public int selectedHandIndex = 0; // 0 = main droite, 1 = main gauche
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

        if (rightHandItemImage != null)
            rightHandItemImage.gameObject.SetActive(false);
        if (leftHandItemImage != null)
            leftHandItemImage.gameObject.SetActive(false);

        UpdateSelectedHandHighlights();
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
        UpdateHandItemImages();
    }

    private void UpdateSelectedHandHighlights()
    {
        if (rightHandHighlight != null)
        {
            rightHandHighlight.gameObject.SetActive(selectedHandIndex == 0 && rightHeldObject != null);
        }

        if (leftHandHighlight != null)
        {
            leftHandHighlight.gameObject.SetActive(selectedHandIndex == 1 && leftHeldObject != null);
        }
    }

    private void UpdateHandItemImages()
    {
        if (rightHandItemImage != null)
        {
            rightHandItemImage.gameObject.SetActive(rightHeldObject != null);
        }

        if (leftHandItemImage != null)
        {
            leftHandItemImage.gameObject.SetActive(leftHeldObject != null);
        }
    }

    private void HandleRaycastInteraction()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        Physics.Raycast(ray, out hitInfo, interactionDistance, interactableLayers);
    }

    private void HandleInputs()
    {
        // Ramasser un objet avec la touche E
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupObject();
            TryToggleScanner();
        }

        // Changer d'objet sélectionné avec la molette de la souris
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheel != 0)
        {
            TrySelectAvailableObject();
        }

    }

    private void TryToggleScanner()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // Utiliser RaycastAll pour obtenir tous les objets touchés par le raycast
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactableLayers);

        // objets touchés pour trouver un SpawnScannerObject
        foreach (RaycastHit hit in hits)
        {
            SpawnScannerObject scanner = hit.collider.GetComponent<SpawnScannerObject>();
            if (scanner != null)
            {
                scanner.isOn = !scanner.isOn;
                return;
            }
        }
    }

    public void TrySelectAvailableObject()
    {
        if (rightHeldObject != null && leftHeldObject != null)
        {
            SwitchSelectedHand();
        }
        else if (rightHeldObject != null)
        {
            selectedHandIndex = 0;
            UpdateSelectedHandHighlights();
        }
        else if (leftHeldObject != null)
        {
            selectedHandIndex = 1;
            UpdateSelectedHandHighlights();
        }
    }

    private void SwitchSelectedHand()
    {
        // Alterner entre main droite (0) et main gauche (1)
        selectedHandIndex = (selectedHandIndex == 0) ? 1 : 0;
        UpdateSelectedHandHighlights();
    }

    private void TryPickupObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayers))
        {
            if (hit.collider.TryGetComponent<Collectable>(out Collectable collectable) && collectable.canBePickedUp)
            {
                if (rightHeldObject == null)
                {
                    PickupInHand(collectable, rightHandPosition, ref rightHeldObject);
                    selectedHandIndex = 0;
                    Debug.Log("Objet ramassé dans la main DROITE: " + collectable.name);
                }
                else if (leftHeldObject == null)
                {
                    PickupInHand(collectable, leftHandPosition, ref leftHeldObject);
                    selectedHandIndex = 1;
                    Debug.Log("Objet ramassé dans la main GAUCHE: " + collectable.name);
                }
                // Si les deux mains sont occupées, ne rien faire

                UpdateSelectedHandHighlights();
            }
        }
    }

    private void PickupInHand(Collectable collectable, Transform handTransform, ref Collectable handReference)
    {
        ThrowableSoundObject throwableSound = collectable.GetComponent<ThrowableSoundObject>();
        if (throwableSound != null)
        {
            throwableSound.OnGrab();
        }

        // Ramasser l'objet
        collectable.Pickup(handTransform);
        handReference = collectable;
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