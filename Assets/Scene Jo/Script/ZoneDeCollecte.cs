using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneDeCollecte : MonoBehaviour
{
    private List<GameObject> stock = new List<GameObject>();

    private void OnCollisionEnter(Collision collision)
    {
        Collectable collectable = collision.gameObject.GetComponent<Collectable>();
        if (collectable != null)
        {
            stock.Add(collision.gameObject);
            Debug.Log($"Objet collecté : {collision.gameObject.name}. Total dans le stock : {stock.Count}");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Collectable collectable = collision.gameObject.GetComponent<Collectable>();
        if (collectable != null)
        {
            if (stock.Remove(collision.gameObject))
            {
                Debug.Log($"Objet sorti et retiré du stock : {collision.gameObject.name}. Total dans le stock : {stock.Count}");
            }
        }
    }
}
