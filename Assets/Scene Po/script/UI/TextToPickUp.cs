using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextToPickUp : MonoBehaviour
{

    private Transform mainCam;
    private Transform associatedObject;
    private Transform worldSpaceCanvas;
    private GameObject text;
    private bool isPickedUp = false;
    private List<TextToPickUp> allTexts = new List<TextToPickUp>();



    public float distanceToDisable = 5f;
    public float timeToDisable = 3f;
    public Vector3 offset;

    private bool textHasBeenEnabled = false;

    private void Start()
    {
        foreach (TextToPickUp textToPickUp in FindObjectsOfType<TextToPickUp>())
        {
            allTexts.Add(textToPickUp);
        }

        text = this.gameObject;
        text.GetComponent<TextMeshProUGUI>().enabled = false;
        mainCam = Camera.main.transform;
        associatedObject = transform.parent;
        worldSpaceCanvas = GameObject.FindWithTag("WorldSpaceCanvas").transform;

        transform.SetParent(worldSpaceCanvas);
    }

    private void Update()
    {
        isPickedUp = associatedObject.GetComponent<Collectable>().isPickedUp;
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.position);
        transform.position = associatedObject.position + offset;

        if (Vector3.Distance(mainCam.position, associatedObject.position) < distanceToDisable)
        {
            foreach (TextToPickUp textToPickUp in allTexts)
            {
                if (!textToPickUp.textHasBeenEnabled)
                {
                    textToPickUp.text.GetComponent<TextMeshProUGUI>().enabled = true;
                    textToPickUp.textHasBeenEnabled = true;
                    StartCoroutine(textToPickUp.DisableText());
                }
            }
        }

        if (isPickedUp)
        {
            text.GetComponent<TextMeshProUGUI>().enabled = false;
        }
    }

    private IEnumerator DisableText()
    {
        yield return new WaitForSeconds(timeToDisable);
        text.GetComponent<TextMeshProUGUI>().enabled = false;
    }

}
