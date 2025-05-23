using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextToThrow : MonoBehaviour
{

    private GameObject associatedObject;
    private Transform canvas;
    protected GameObject text;
    private List<TextToThrow> allTexts = new List<TextToThrow>();

    public float timeToDisable = 3f;
    public Vector3 offset;

    protected bool textHasBeenEnabled = false;


    private void Start()
    {
        foreach (TextToThrow textToThrow in FindObjectsOfType<TextToThrow>())
        {
            allTexts.Add(textToThrow);
        }

        text = this.gameObject;
        text.GetComponent<TextMeshProUGUI>().enabled = false;
        associatedObject = transform.parent.gameObject;
        canvas = GameObject.FindWithTag("Canvas").transform;
        transform.SetParent(canvas);
    }

    private void Update()
    {
        text.GetComponent<RectTransform>().position = transform.parent.position + offset;


        if (associatedObject.GetComponent<Collectable>().isPickedUp)
        {
            foreach (TextToThrow textToThrow in allTexts)
            {
                if (!textToThrow.textHasBeenEnabled)
                {
                    textToThrow.text.GetComponent<TextMeshProUGUI>().enabled = true;
                    textToThrow.textHasBeenEnabled = true;
                    StartCoroutine(textToThrow.DisableText());
                }
            }
        }
    }
    protected IEnumerator DisableText()
    {
        yield return new WaitForSeconds(timeToDisable);
        text.GetComponent<TextMeshProUGUI>().enabled = false;
    }


}
