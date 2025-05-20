using UnityEngine;
using UnityEngine.UI;

public class PlayerHandController : MonoBehaviour
{
    [Header("Paramètres d'interaction")]
    public float interactionDistance = 3f;
    public LayerMask interactableLayers;
    public KeyCode pickupKey = KeyCode.E;
    // Supprimé les paramètres liés au lancer

    [Header("Mains")]
    public Transform rightHandPosition;
    public Transform leftHandPosition;

    [Header("Images des objets")]
    public Image rightHandItemImage;  // Image pour la main droite
    public Image leftHandItemImage;   // Image pour la main gauche

    [Header("Surbrillance des mains")]
    public Image rightHandHighlight;  // Surbrillance de la main droite
    public Image leftHandHighlight;   // Surbrillance de la main gauche

    private Camera mainCamera;
    public Collectable rightHeldObject;
    [HideInInspector] // Rendue accessible depuis ThrowObjectHand
    public Collectable leftHeldObject;
    [HideInInspector] // Rendue accessible depuis ThrowObjectHand
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

        // Désactiver les images d'objets au démarrage
        if (rightHandItemImage != null)
            rightHandItemImage.gameObject.SetActive(false);
        if (leftHandItemImage != null)
            leftHandItemImage.gameObject.SetActive(false);

        // Mettre à jour les surbrillances des mains
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
        // Gérer la surbrillance de la main droite
        if (rightHandHighlight != null)
        {
            rightHandHighlight.gameObject.SetActive(selectedHandIndex == 0 && rightHeldObject != null);
        }

        // Gérer la surbrillance de la main gauche
        if (leftHandHighlight != null)
        {
            leftHandHighlight.gameObject.SetActive(selectedHandIndex == 1 && leftHeldObject != null);
        }
    }

    private void UpdateHandItemImages()
    {
        // Image de la main droite
        if (rightHandItemImage != null)
        {
            rightHandItemImage.gameObject.SetActive(rightHeldObject != null);
        }

        // Image de la main gauche
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
        }

        // Changer d'objet sélectionné avec la molette de la souris
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheel != 0)
        {
            TrySelectAvailableObject();
        }

        // Supprimé le code de lancement d'objets
    }

    // Rendue publique pour être accessible depuis ThrowObjectHand
    public void TrySelectAvailableObject()
    {
        // Si les deux mains ont un objet, basculer entre les deux
        if (rightHeldObject != null && leftHeldObject != null)
        {
            SwitchSelectedHand();
        }
        // Si seulement la main droite a un objet, la sélectionner
        else if (rightHeldObject != null)
        {
            selectedHandIndex = 0;
            UpdateSelectedHandHighlights();
        }
        // Si seulement la main gauche a un objet, la sélectionner
        else if (leftHeldObject != null)
        {
            selectedHandIndex = 1;
            UpdateSelectedHandHighlights();
        }
        // Si aucune main n'a d'objet, ne rien faire
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
                // Détermine quelle main est disponible
                if (rightHeldObject == null)
                {
                    // Main droite libre, l'objet va dans la main droite
                    PickupInHand(collectable, rightHandPosition, ref rightHeldObject);
                    selectedHandIndex = 0; // Sélectionner automatiquement la main droite
                    Debug.Log("Objet ramassé dans la main DROITE: " + collectable.name);
                }
                else if (leftHeldObject == null)
                {
                    // Main droite occupée mais main gauche libre
                    PickupInHand(collectable, leftHandPosition, ref leftHeldObject);
                    selectedHandIndex = 1; // Sélectionner automatiquement la main gauche
                    Debug.Log("Objet ramassé dans la main GAUCHE: " + collectable.name);
                }
                // Si les deux mains sont occupées, ne rien faire

                // Mettre à jour les surbrillances après avoir ramassé un objet
                UpdateSelectedHandHighlights();
            }
        }
    }

    private void PickupInHand(Collectable collectable, Transform handTransform, ref Collectable handReference)
    {
        // Prévenir que l'objet va être ramassé (pour les sons)
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
