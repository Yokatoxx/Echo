using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    public bool isCollectComplete = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (isCollectComplete)
            {
                //transition to the scene "SceneMenuPrincipal"
                UnityEngine.SceneManagement.SceneManager.LoadScene("SceneMenuPrincipal");
            }
        }
    }
}
