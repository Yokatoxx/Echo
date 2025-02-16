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
        //foreach (var name in Microphone.devices)
        //{
        //   Debug.Log(name);
        //}

        microphoneName = Microphone.devices[microphoneIndex];
        microphoneClip = Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate); 
    }

    public float GetLoudnessFromMicrophone()
    {
        return GetLoudnessFromAudioClip(Microphone.GetPosition(microphoneName), microphoneClip);
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
