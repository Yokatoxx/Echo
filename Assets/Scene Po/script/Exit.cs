using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    public bool isCollectComplete = false;
    public GameObject sortie; // Référence à l'objet avec la variable sortie

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (isCollectComplete)
            {
                // Désactiver le collider de l'objet sortie
                if (sortie != null)
                {
                    Collider sortieCollider = sortie.GetComponent<Collider>();
                    if (sortieCollider != null)
                    {
                        sortieCollider.enabled = false;
                    }

                    // Changer le blendshape à 100
                    SkinnedMeshRenderer sortieRenderer = sortie.GetComponent<SkinnedMeshRenderer>();
                    if (sortieRenderer != null && sortieRenderer.sharedMesh.blendShapeCount > 0)
                    {
                        // Change le premier blendshape à 100 (vous pouvez spécifier l'index si nécessaire)
                        sortieRenderer.SetBlendShapeWeight(0, 100f);
                    }
                }

                //transition to the scene "SceneMenuPrincipal"
                UnityEngine.SceneManagement.SceneManager.LoadScene("SceneMenuPrincipal");
            }
        }
    }
}