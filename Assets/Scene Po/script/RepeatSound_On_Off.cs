using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatSound_On_Off : MonoBehaviour
{
    public float range = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, range))
            {
                if (hit.transform.gameObject.tag == "RepeatSound")
                {
                    if (hit.transform.gameObject.GetComponent<SpawnScannerObject>().isOn == false)
                    {
                        hit.transform.gameObject.GetComponent<SpawnScannerObject>().isOn = true;
                    }
                    else if (hit.transform.gameObject.GetComponent<SpawnScannerObject>().isOn == true)
                    {
                        hit.transform.gameObject.GetComponent<SpawnScannerObject>().isOn = false;

                    }
                }
            }
        }
    }
}
