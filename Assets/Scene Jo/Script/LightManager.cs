using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToggle : MonoBehaviour
{
    public GameObject objectToToggle1;
    public GameObject objectToToggle2;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            objectToToggle1.SetActive(!objectToToggle1.activeSelf);
            objectToToggle2.SetActive(!objectToToggle2.activeSelf);

        }
    }
}
