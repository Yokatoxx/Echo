using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShutDownAtStart : MonoBehaviour
{
    private GameObject[] lights;

    private void Awake()
    {

        lights = GameObject.FindGameObjectsWithTag("Light");
        foreach (GameObject light in lights)
        {
            light.SetActive(false);
        }
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (GameObject light in lights)
            {
                light.SetActive(!light.activeSelf);
            }
        }
    }
}
