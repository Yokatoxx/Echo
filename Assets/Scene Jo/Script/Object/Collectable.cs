using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Propriétés de l'objet ramassable")]
    public bool canBePickedUp = true;
    public Vector3 positionInHand = new Vector3(0, 0, 0.5f);
    public Vector3 rotationInHand = Vector3.zero;

    private Rigidbody rb;
    private Collider objectCollider;
    private Transform originalParent;
    private Vector3 originalScale;
    private bool isPickedUp = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
        originalParent = transform.parent;
        originalScale = transform.localScale;
    }

    public void Pickup(Transform handTransform)
    {
        if (!canBePickedUp || isPickedUp) return;

        isPickedUp = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (objectCollider != null)
            objectCollider.enabled = false;

        Vector3 worldScale = transform.lossyScale;

        transform.SetParent(handTransform);
        transform.localPosition = positionInHand;
        transform.localEulerAngles = rotationInHand;

        if (handTransform.lossyScale.x != 0 && handTransform.lossyScale.y != 0 && handTransform.lossyScale.z != 0)
        {
            transform.localScale = new Vector3(
                worldScale.x / handTransform.lossyScale.x,
                worldScale.y / handTransform.lossyScale.y,
                worldScale.z / handTransform.lossyScale.z
            );
        }
    }

    public void Drop()
    {
        if (!isPickedUp) return;

        isPickedUp = false;
        transform.SetParent(originalParent);
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (objectCollider != null)
            objectCollider.enabled = true;
    }

    public bool IsPickedUp()
    {
        return isPickedUp;
    }
}
