using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject echoSphere;
    public Transform spawnPoint;
    public Color echoColor = new Color(0.0f, 0.5f, 1.0f, 0.5f);

    [Header("Echo Effect Settings")]
    public float duration = 3f;
    public float maxRadius = 50f;
    public float propagationSpeed = 15f;
    public float fadeOutSpeed = 1.5f;
    public int numberOfPulses = 1;
    public float pulseDelay = 0.5f;

    [Header("Microphone Settings")]
    [Range(0f, 1f)]
    public float activationThreshold = 0.2f;
    public float sensitivityMultiplier = 5f;
    public int microphoneIndex = 0;
    public float analysisUpdateRate = 0.05f;
    public bool muteInput = true;
    public float responseCooldown = 1f;

    private FMOD.System coreSystem;
    private FMOD.Sound microphoneSound;
    private FMOD.Channel microphoneChannel;
    private FMOD.ChannelGroup masterGroup;

    private float currentLoudness = 0f;
    private bool canSpawn = true;
    private float analysisTimer = 0f;

    private Queue<float> volumeHistory = new Queue<float>();
    private int historySize = 5;

    void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        try
        {
            coreSystem = FMODUnity.RuntimeManager.CoreSystem;
            coreSystem.getMasterChannelGroup(out masterGroup);
            LogInputDevices();
            StartMicrophoneCapture();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'initialisation: {e.Message}");
        }
    }

    void LogInputDevices()
    {
        int numDrivers = 0;
        int numConnected = 0;
        coreSystem.getRecordNumDrivers(out numDrivers, out numConnected);
        Debug.Log($"Périphériques d'entrée: {numDrivers} disponibles, {numConnected} connectés");

        for (int i = 0; i < numDrivers; i++)
        {
            string name = "";
            int rate = 0;
            FMOD.SPEAKERMODE mode = FMOD.SPEAKERMODE.DEFAULT;
            int channels = 0;
            FMOD.DRIVER_STATE state = FMOD.DRIVER_STATE.CONNECTED;

            coreSystem.getRecordDriverInfo(i, out name, 256, out System.Guid guid, out rate, out mode, out channels, out state);
            Debug.Log($"Périphérique {i}: {name} (Canaux: {channels}, Fréquence: {rate} Hz, État: {state})");
        }

        if (microphoneIndex >= numDrivers && numDrivers > 0)
        {
            microphoneIndex = 0;
            Debug.LogWarning("Index du microphone trop élevé, utilisation du périphérique 0");
        }
        else if (numDrivers == 0)
        {
            Debug.LogError("Aucun périphérique d'entrée audio détecté!");
        }
    }

    void StartMicrophoneCapture()
    {
        try
        {
            FMOD.CREATESOUNDEXINFO exInfo = new FMOD.CREATESOUNDEXINFO();
            exInfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            exInfo.numchannels = 1;
            exInfo.defaultfrequency = 44100;
            exInfo.format = FMOD.SOUND_FORMAT.PCM16;

            uint bytesPerSample = (uint)sizeof(short);
            uint bytesPerSecond = (uint)exInfo.defaultfrequency * bytesPerSample * (uint)exInfo.numchannels;
            uint bufferLengthInSeconds = 2;
            exInfo.length = bytesPerSecond * bufferLengthInSeconds;

            byte[] buffer = new byte[exInfo.length];

            FMOD.MODE soundMode = FMOD.MODE.CREATESAMPLE | FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_NORMAL;

            FMOD.RESULT result = coreSystem.createSound(buffer, soundMode, ref exInfo, out microphoneSound);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Erreur lors de la création du son: {result}");
                return;
            }

            result = coreSystem.recordStart(microphoneIndex, microphoneSound, true);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Erreur lors du démarrage de l'enregistrement: {result}");
                return;
            }

            result = coreSystem.playSound(microphoneSound, masterGroup, false, out microphoneChannel);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Erreur lors de la lecture du son: {result}");
                return;
            }

            if (muteInput)
            {
                microphoneChannel.setVolume(0);
                Debug.Log("Son du microphone coupé (mode analyse uniquement)");
            }

            Debug.Log("Enregistrement et analyse du microphone démarrés avec succès");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception lors de la configuration du microphone: {e.Message}");
        }
    }

    void Update()
    {
        analysisTimer += Time.deltaTime;
        if (analysisTimer >= analysisUpdateRate)
        {
            analysisTimer = 0f;
            AnalyzeAudio();
        }

        if (currentLoudness > activationThreshold && canSpawn)
        {
            StartCoroutine(SpawnEcholocationEffect());
            StartCoroutine(CooldownTimer());
        }
    }

    void AnalyzeAudio()
    {
        try
        {
            if (!microphoneChannel.hasHandle())
                return;

            bool isRecording = false;
            coreSystem.isRecording(microphoneIndex, out isRecording);

            if (!isRecording)
            {
                Debug.LogWarning("L'enregistrement n'est pas actif!");
                return;
            }

            float volume = 0f;
            microphoneChannel.getAudibility(out volume);

            while (volumeHistory.Count >= historySize)
                volumeHistory.Dequeue();

            volumeHistory.Enqueue(volume);

            float avgVolume = 0;
            foreach (float v in volumeHistory)
                avgVolume += v;
            avgVolume /= volumeHistory.Count;

            currentLoudness = Mathf.Clamp01(avgVolume * sensitivityMultiplier);

            if (currentLoudness > activationThreshold)
            {
                Debug.Log($"Niveau sonore: {currentLoudness:F2} (Volume brut: {volume:F2})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'analyse audio: {e.Message}");
        }
    }

    IEnumerator SpawnEcholocationEffect()
    {
        Vector3 spawnPosition = spawnPoint.position;

        int pulsesToSpawn = Mathf.Max(1, Mathf.RoundToInt(numberOfPulses * currentLoudness));
        float sizeModifier = Mathf.Lerp(0.5f, 1.5f, currentLoudness);

        for (int i = 0; i < pulsesToSpawn; i++)
        {
            GameObject echoSphereInstance = Instantiate(echoSphere, spawnPosition, Quaternion.identity);

            if (echoSphereInstance.GetComponent<Renderer>() != null)
            {
                Material material = echoSphereInstance.GetComponent<Renderer>().material;
                material.color = echoColor;
            }

            StartCoroutine(AnimateEchoSphere(echoSphereInstance, sizeModifier));

            yield return new WaitForSeconds(pulseDelay);
        }
    }

    IEnumerator AnimateEchoSphere(GameObject sphere, float sizeMultiplier = 1.0f)
    {
        float elapsedTime = 0f;
        float initialAlpha = echoColor.a;
        Material material = sphere.GetComponent<Renderer>().material;

        while (elapsedTime < duration)
        {
            float currentRadius = Mathf.Lerp(0, maxRadius * sizeMultiplier, propagationSpeed * elapsedTime / maxRadius);
            sphere.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

            Color currentColor = material.color;
            float newAlpha = Mathf.Lerp(initialAlpha, 0, fadeOutSpeed * elapsedTime / duration);
            material.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(sphere);
    }

    IEnumerator CooldownTimer()
    {
        canSpawn = false;
        yield return new WaitForSeconds(responseCooldown);
        canSpawn = true;
    }

    void OnDestroy()
    {
        try
        {
            if (coreSystem.hasHandle())
                coreSystem.recordStop(microphoneIndex);

            if (microphoneSound.hasHandle())
                microphoneSound.release();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors du nettoyage des ressources: {e.Message}");
        }
    }

    public float GetLoudness()
    {
        return currentLoudness;
    }
}
