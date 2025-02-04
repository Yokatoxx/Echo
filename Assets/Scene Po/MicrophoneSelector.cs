using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class MicrophoneSelector : MonoBehaviour
{
    public TMP_Dropdown sourceDropdown;
    public int chosenDeviceIndex = 0;

    public static UnityAction<int> OnMicrophoneChoiceChanged;

    private void Start()
    {
        PopulateSourceDropdown();
    }

    private void PopulateSourceDropdown()
    {
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var microphone in Microphone.devices)
        {
            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(microphone, null);

            options.Add(optionData);
        }

        sourceDropdown.options = options;
    }

    public void ChooseMicrophone (int optionIndex)
    {
        chosenDeviceIndex = optionIndex;
        OnMicrophoneChoiceChanged?.Invoke(chosenDeviceIndex);
    }

}
