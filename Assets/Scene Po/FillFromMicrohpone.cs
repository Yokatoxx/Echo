using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillFromMicrohpone : MonoBehaviour
{
    public Image audioBar;
    public Slider sensitivitySlider;
    public AudioLoudnessDetection detection;

    public float minimumSensibility = 100;
    public float maximumSensibility = 1000;
    public float currentLoudnessSensibility = 500;
    public float threshold = 0.1f;
    public float loudness;

    private void Start()
    {
        if (sensitivitySlider == null) return;

        sensitivitySlider.value = 0f;
        SetLoudnessSensibility(sensitivitySlider.value);
    }

    private void Update()
    {
        loudness = detection.GetLoudnessFromMicrophone() * currentLoudnessSensibility;
        
        if (loudness < threshold)
        {
            loudness = 0.01f;
        }

        audioBar.fillAmount = loudness;

    }

    public void SetLoudnessSensibility(float t)
    {
        currentLoudnessSensibility = Mathf.Lerp(minimumSensibility, maximumSensibility, t);
    }
}
