using UnityEngine;

/// <summary>
/// Un script simple pour tester si les événements de souris
/// (OnMouseDown, OnMouseEnter, OnMouseExit) sont correctement
/// détectés sur le Collider de ce GameObject.
/// Affiche des messages dans la console lors de ces événements.
/// </summary>
[RequireComponent(typeof(Collider))] // Assure qu'un Collider est présent
public class SimpleClickTester : MonoBehaviour
{
    /// <summary>
    /// Appelé par Unity lorsque l'utilisateur clique sur le Collider
    /// de ce GameObject pendant que le bouton de la souris est enfoncé.
    /// Nécessite un Physics Raycaster sur la caméra.
    /// </summary>
    void OnMouseDown()
    {
        // Affiche un message dans la console avec le nom de l'objet et l'heure du jeu
        Debug.Log($"OnMouseDown détecté sur {this.gameObject.name} à {Time.time}");
    }

    /// <summary>
    /// Appelé par Unity lorsque le pointeur de la souris entre
    /// dans la zone du Collider de ce GameObject.
    /// Nécessite un Physics Raycaster sur la caméra.
    /// </summary>
    void OnMouseEnter()
    {
         // Affiche un message lorsque la souris entre dans le collider
         Debug.Log($"OnMouseEnter sur {this.gameObject.name}");
    }

    /// <summary>
    /// Appelé par Unity lorsque le pointeur de la souris quitte
    /// la zone du Collider de ce GameObject.
    /// Nécessite un Physics Raycaster sur la caméra.
    /// </summary>
    void OnMouseExit()
    {
         // Affiche un message lorsque la souris quitte le collider
         Debug.Log($"OnMouseExit sur {this.gameObject.name}");
    }
}