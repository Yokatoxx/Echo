using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleClickTester : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log($"OnMouseDown détecté sur {this.gameObject.name} à {Time.time}");
    }

    void OnMouseEnter()
    {
        Debug.Log($"OnMouseEnter sur {this.gameObject.name}");
    }

    void OnMouseExit()
    {
        Debug.Log($"OnMouseExit sur {this.gameObject.name}");
    }
}
