using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;

    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;

    public Toggle fullscreenToggle;

    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Slider fieldOfViewSlider;

    public TextMeshProUGUI volumeValue;
    public TextMeshProUGUI sensitivityValue;
    public TextMeshProUGUI fieldOfViewValue;

    Resolution[] resolutions;

    private void Start()
    {
        resolutions = Screen.resolutions;

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        
        if (SettingsManager.instance != null)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == SettingsManager.instance.ResolutionWidth &&
                    resolutions[i].height == SettingsManager.instance.ResolutionHeight)
                {
                    resolutionDropdown.value = i;
                    resolutionDropdown.RefreshShownValue();
                    break;
                }
            }

            Screen.SetResolution((int)SettingsManager.instance.ResolutionWidth, (int)SettingsManager.instance.ResolutionHeight, Screen.fullScreen);

            if (qualityDropdown != null)
            {
                if (SettingsManager.instance.Quality < 0)
                    qualityDropdown.value = 0;
                else if (SettingsManager.instance.Quality > qualityDropdown.options.Count - 1)
                    qualityDropdown.value = qualityDropdown.options.Count - 1;
                else
                    qualityDropdown.value = SettingsManager.instance.Quality;

                qualityDropdown.RefreshShownValue();
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = SettingsManager.instance.Fullscreen;

            audioMixer.SetFloat("volume", SettingsManager.instance.Volume);
            if (volumeSlider != null)
                volumeSlider.value = SettingsManager.instance.Volume;
            if (volumeValue != null)
                volumeValue.text = SettingsManager.instance.Volume.ToString("F1");

            if (sensitivitySlider != null)
                sensitivitySlider.value = SettingsManager.instance.Sensitivity;
            if (sensitivityValue != null)
                sensitivityValue.text = SettingsManager.instance.Sensitivity.ToString("F0");

            if (fieldOfViewSlider != null)
                fieldOfViewSlider.value = SettingsManager.instance.FieldOfView;
            if (fieldOfViewValue != null)
                fieldOfViewValue.text = SettingsManager.instance.FieldOfView.ToString("F0");
        }
    }

    public void SetResolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.ResolutionWidth = resolution.width;
            SettingsManager.instance.ResolutionHeight = resolution.height;
        }
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);

        if (volumeValue != null)
            volumeValue.text = volume.ToString("F1");

        if (SettingsManager.instance != null)
            SettingsManager.instance.Volume = volume;
    }

    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);

        if (SettingsManager.instance != null)
            SettingsManager.instance.Quality = qualityIndex;
    }

    public void SetFullScreen (bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (SettingsManager.instance != null)
            SettingsManager.instance.Fullscreen = isFullscreen;
    }

    public void SetSensitivity(float value)
    {
        if (SettingsManager.instance == null)
            return;

        SettingsManager.instance.Sensitivity = value;
        if (sensitivityValue != null)
            sensitivityValue.text = value.ToString("F0");
    }

    public void SetFieldOfView(float value)
    {
        if (SettingsManager.instance == null)
            return;

        SettingsManager.instance.FieldOfView = value;
        if (fieldOfViewValue != null)
            fieldOfViewValue.text = value.ToString("F0");
    }
}
