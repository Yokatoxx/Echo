using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AudioLoudnessDetection : MonoBehaviour
{

    public int sampleWindow = 64;

    private AudioClip microphoneClip;
    private string microphoneName;

    private void Start()
    {
        MicrophoneToAudioClip(0);
    }

    private void OnEnable()
    {
        MicrophoneSelector.OnMicrophoneChoiceChanged += ChangeMicrophoneSource;
    }

    private void OnDisable()
    {
        MicrophoneSelector.OnMicrophoneChoiceChanged -= ChangeMicrophoneSource;
    }
    private void ChangeMicrophoneSource(int deviceIndex)
    {
        MicrophoneToAudioClip(deviceIndex);
    }

    private void MicrophoneToAudioClip(int microphoneIndex)
    {
        try
        {
            // Arrêter l'enregistrement précédent si nécessaire
            if (microphoneName != null && Microphone.IsRecording(microphoneName))
            {
                Microphone.End(microphoneName);
            }

            // Vérifier si des microphones sont disponibles
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("Aucun microphone détecté!");
                return;
            }

            // S'assurer que l'index est valide
            microphoneIndex = Mathf.Clamp(microphoneIndex, 0, Microphone.devices.Length - 1);
            microphoneName = Microphone.devices[microphoneIndex];

            Debug.Log("Démarrage du microphone: " + microphoneName);
            microphoneClip = Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'initialisation du microphone: {e.Message}");
        }
    }

    public float GetLoudnessFromMicrophone()
    {
        // Vérifier si le microphone est initialisé
        if (microphoneClip == null || !Microphone.IsRecording(microphoneName))
        {
            Debug.LogWarning("Microphone not initialized or not recording. Restarting...");
            MicrophoneToAudioClip(0);
            return 0;
        }

        int position = Microphone.GetPosition(microphoneName);
        if (position < 0)
        {
            Debug.LogWarning("Invalid microphone position. Restarting microphone...");
            MicrophoneToAudioClip(0);
            return 0;
        }

        return GetLoudnessFromAudioClip(position, microphoneClip);
    }

    public float GetLoudnessFromAudioClip(int clipPosition, AudioClip clip)
    {
        int startPosition = clipPosition - sampleWindow;

        if (startPosition < 0 )
        {
            return 0;
        }

        float[] waveData = new float[sampleWindow];
        clip.GetData(waveData, startPosition);

        float totalLoudness = 0;

        foreach (var sample in waveData )
        {
            totalLoudness += Mathf.Abs(sample);
        }


        return totalLoudness / sampleWindow;
    }

}
