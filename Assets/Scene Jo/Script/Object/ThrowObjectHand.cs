using UnityEngine;
using UnityEngine.UI;

public class ThrowObjectHand : MonoBehaviour
{
    [Header("Références")]
    public PlayerHandController handController;
    public Camera playerCamera;
    public Stamina staminaSystem;

    [Header("Paramètres de lancer")]
    public KeyCode throwKey = KeyCode.Mouse0;
    public KeyCode placeKey = KeyCode.Mouse1;
    public float throwForce = 12f;                  
    public float placeDistance = 1.5f;               

    public float staminaCost = 20f;

    private void Start()
    {
        if (handController == null)
            handController = GetComponent<PlayerHandController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (staminaSystem == null)
            staminaSystem = GetComponent<Stamina>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(throwKey) && CanThrow() && CanUseStamina())
        {
            ThrowSelectedObject();
        }

        // Pose l'objet devant quand le clic droit est pressé
        if (Input.GetKeyDown(placeKey) && CanThrow())
        {
            PlaceObjectInFront();
        }
    }

    private bool CanThrow()
    {
        // Vérifie si un objet est tenu dans la main actuellement sélectionnée
        if (handController == null) return false;

        if (handController.selectedHandIndex == 0)
            return handController.rightHeldObject != null;
        else
            return handController.leftHeldObject != null;
    }

    private bool CanUseStamina()
    {
        if (staminaSystem == null) return true;

        // Vérifie si le joueur a assez de stamina pour lancer
        return staminaSystem.currentStamina >= staminaCost;
    }

    private void ThrowSelectedObject()
    {
        if (!CanThrow()) return;

        if (staminaSystem != null)
        {
            staminaSystem.currentStamina -= staminaCost;
            if (staminaSystem.currentStamina < 0)
                staminaSystem.currentStamina = 0;

            staminaSystem.UpdateStaminaUI();
        }

        // Récupérer l'objet à lancer selon la main sélectionnée
        Collectable objectToThrow = GetSelectedObject();
        if (objectToThrow == null) return;

        Rigidbody rb = objectToThrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;

            rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * throwForce * 0.5f, ForceMode.Impulse);
        }

        ThrowableSoundObject throwableSound = objectToThrow.GetComponent<ThrowableSoundObject>();
        if (throwableSound != null)
        {
            throwableSound.OnRelease();
        }
    }

    private void PlaceObjectInFront()
    {
        if (!CanThrow()) return;

        // Récupérer l'objet à placer selon la main sélectionnée
        Collectable objectToPlace = GetSelectedObject();
        if (objectToPlace == null) return;

        // Calculer la position devant le joueur
        Vector3 placePosition = playerCamera.transform.position + playerCamera.transform.forward * placeDistance;

        // Vérifier s'il y a une surface devant le joueur pour y placer l'objet
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, placeDistance))
        {
            // Si on touche une surface, placer l'objet légèrement au-dessus de celle-ci
            placePosition = hit.point + Vector3.up * 0.1f;
        }

        Rigidbody rb = objectToPlace.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.position = placePosition;
        }
        else
        {
            objectToPlace.transform.position = placePosition;
        }

        ThrowableSoundObject throwableSound = objectToPlace.GetComponent<ThrowableSoundObject>();
        if (throwableSound != null)
        {
            throwableSound.OnRelease();
        }
    }

    // Méthode  pour obtenir l'objet sélectionné et le retirer de la main
    private Collectable GetSelectedObject()
    {
        Collectable selectedObject = null;

        if (handController.selectedHandIndex == 0)
        {
            // Main droite
            selectedObject = handController.rightHeldObject;
            handController.rightHeldObject = null;
        }
        else
        {
            // Main gauche
            selectedObject = handController.leftHeldObject;
            handController.leftHeldObject = null;
        }

        if (selectedObject == null) return null;

        // Mettre à jour les surbrillances après avoir modifié les références
        handController.TrySelectAvailableObject();

        // Détacher l'objet de la main
        selectedObject.Drop();

        return selectedObject;
    }
}
