using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShutDownAtStart : MonoBehaviour
{
    private GameObject[] lights;

    private void Awake()
    {
        // Récupère tous les objets avec le tag "Light" et les désactive
        lights = GameObject.FindGameObjectsWithTag("Light");
        foreach (GameObject light in lights)
        {
            light.SetActive(false);
        }
    }

    private void Update()
    {
        // Si la touche L est pressée, réactive tous les objets
        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (GameObject light in lights)
            {
                light.SetActive(!light.activeSelf);
            }
        }
    }
}
