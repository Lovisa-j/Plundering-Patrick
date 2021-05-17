using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] Vector2 resolution = new Vector2(1920, 1080);

    [SerializeField] bool fullscreen = true;

    [SerializeField] int quality = 0;

    [SerializeField] float sensitivity = 40;
    [SerializeField] float fieldOfView = 80;
    [SerializeField] float volume = -20;

    #region properties
    public bool Fullscreen
    {
        get
        {
            return fullscreen;
        }
        set
        {
            fullscreen = value;
            SetValueForSetting("Fullscreen", fullscreen ? 1 : 0);
        }
    }

    public int Quality
    {
        get
        {
            return quality;
        }
        set
        {
            quality = value;
            SetValueForSetting("Quality", quality);
        }
    }

    public float Sensitivity
    {
        get
        {
            return sensitivity;
        }
        set
        {
            sensitivity = value;
            SetValueForSetting("Sensitivity", sensitivity);
        }
    }
    public float FieldOfView
    {
        get
        {
            return fieldOfView;
        }
        set
        {
            fieldOfView = Mathf.Round(value);
            SetValueForSetting("Field of View", fieldOfView);
        }
    }
    public float ResolutionWidth
    {
        get
        {
            return resolution.x;
        }
        set
        {
            resolution.x = value;
            SetValueForSetting("Resolution Width", resolution.x);
        }
    }
    public float ResolutionHeight
    {
        get
        {
            return resolution.y;
        }
        set
        {
            resolution.y = value;
            SetValueForSetting("Resolution Height", resolution.y);
        }
    }
    public float Volume
    {
        get
        {
            return volume;
        }
        set
        {
            float decimalValue = value;
            decimalValue -= Mathf.Floor(value);
            decimalValue *= 10;
            decimalValue = Mathf.Floor(decimalValue);
            decimalValue /= 10;
            decimalValue += Mathf.Floor(value);
            
            volume = decimalValue;
            SetValueForSetting("Volume", volume);
        }
    }
    #endregion

    string settingsFilePath;

    public static SettingsManager instance;

    void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;

        settingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\Plundering Patrick";

        DirectoryInfo di = new DirectoryInfo(settingsFilePath);
        if (!di.Exists)
            di.Create();

        settingsFilePath += @"\Settings.ini";
        
        GetSettingsFromFile();
    }

    void GetSettingsFromFile()
    {
        float value = ReturnValueForSetting("Sensitivity");
        if (value > 0)
            sensitivity = value;
        else
            Sensitivity = sensitivity;

        value = ReturnValueForSetting("Field of View");
        if (value > 0)
            fieldOfView = value;
        else
            FieldOfView = fieldOfView;

        value = ReturnValueForSetting("Resolution Width");
        if (value > 0)
            resolution.x = value;
        else
            ResolutionWidth = resolution.x;

        value = ReturnValueForSetting("Resolution Height");
        if (value > 0)
            resolution.y = value;
        else
            ResolutionHeight = resolution.y;

        value = ReturnValueForSetting("Volume");
        if (value > -999)
            volume = value;
        else
            Volume = volume;

        value = ReturnValueForSetting("Fullscreen");
        if (value > -999)
            fullscreen = (value > 0) ? true : false;
        else
            Fullscreen = fullscreen;

        value = ReturnValueForSetting("Quality");
        if (value > -1)
            quality = (int)value;
        else
            Quality = quality;
    }

    float ReturnValueForSetting(string settingName)
    {
        float toReturn = -999;
        
        string[] fileLines = File.ReadAllLines(settingsFilePath);
        string[] lineSplits;

        for (int i = 0; i < fileLines.Length; i++)
        {
            lineSplits = fileLines[i].Split('=');
            if (lineSplits.Length <= 1)
                continue;

            if (lineSplits[0] == settingName)
                float.TryParse(lineSplits[1], out toReturn);
        }

        return toReturn;
    }

    void SetValueForSetting(string settingName, float value)
    {
        bool exists = false;

        List<string> fileLines = new List<string>(File.ReadAllLines(settingsFilePath));
        string[] lineSplits;

        for (int i = 0; i < fileLines.Count; i++)
        {
            lineSplits = fileLines[i].Split('=');
            if (lineSplits.Length <= 1)
                continue;

            if (lineSplits[0] == settingName)
            {
                fileLines[i] = settingName + "=" + value;
                exists = true;
                break;
            }
        }

        if (!exists)
            fileLines.Add(settingName + "=" + value);

        File.WriteAllLines(settingsFilePath, fileLines.ToArray());
    }
}
