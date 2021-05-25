using System;
using UnityEngine;
using TMPro;

public enum KeyName
{
    Forward,
    Back,
    Right,
    Left,
    Sprint,
    Crouch,
    Jump,
    Attack,
    Aim,
    Block,
    Pause,
    Interact,
    CycleObjectives,
    Compass,
    DropItem
}

public class InputManager : MonoBehaviour
{
    public GameObject UiBlock;
    public KeybindUpdater[] keybindUpdaters;

    [Header("Movement")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backKey = KeyCode.S;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpClimbKey = KeyCode.Space;

    [Header("Combat")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode aimKey = KeyCode.Mouse1;
    public KeyCode blockKey = KeyCode.LeftControl;

    [Header("Other")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode cycleObjectivesKey = KeyCode.Y;
    public KeyCode compassKey = KeyCode.T;
    public KeyCode dropKey = KeyCode.R;

    public float Vertical { get; private set; }
    public float Horizontal { get; private set; }

    TextMeshProUGUI changeButtonText;

    bool inputChange;

    KeyName changingKey;

    #region Singleton
    public static InputManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }
    #endregion

    void Start()
    {
        if (SettingsManager.instance != null)
        {
            string value;
            foreach (KeyName keyName in Enum.GetValues(typeof(KeyName)))
            {
                value = SettingsManager.instance.ReturnStringForSetting(keyName.ToString());
                if (!string.IsNullOrEmpty(value))
                {
                    foreach (KeyCode kCode in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (kCode.ToString() == value)
                        {
                            UpdateKey(keyName, kCode);
                            break;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < keybindUpdaters.Length; i++)
        {
            if (keybindUpdaters[i] != null)
                keybindUpdaters[i].UpdateText();
        }
    }

    void Update()
    {
        Vertical = 0;
        if (Input.GetKey(forwardKey))
            Vertical++;
        if (Input.GetKey(backKey))
            Vertical--;

        Horizontal = 0;
        if (Input.GetKey(rightKey))
            Horizontal++;
        if (Input.GetKey(leftKey))
            Horizontal--;

        if (inputChange)
        {
            if (UiBlock != null && !UiBlock.activeInHierarchy)
                UiBlock.SetActive(true);

            if (changeButtonText != null)
                changeButtonText.text = "...";

            if (Input.anyKeyDown)
                ChangeKey();
        }
        else if (UiBlock != null && UiBlock.activeInHierarchy)
            UiBlock.SetActive(false);
    }

    void ChangeKey()
    {
        KeyCode keyPressed = KeyCode.None;

        foreach (KeyCode kCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kCode))
            {
                keyPressed = kCode;
                break;
            }
        }

        UpdateKey(changingKey, keyPressed);

        if (SettingsManager.instance != null)
            SettingsManager.instance.SetValueForSetting(changingKey.ToString(), keyPressed.ToString());

        if (changeButtonText != null)
            changeButtonText.text = keyPressed.ToString();

        inputChange = false;
    }

    void UpdateKey(KeyName basedOn, KeyCode toSet)
    {
        switch (basedOn)
        {
            case KeyName.Forward:
                forwardKey = toSet;
                break;
            case KeyName.Back:
                backKey = toSet;
                break;
            case KeyName.Right:
                rightKey = toSet;
                break;
            case KeyName.Left:
                leftKey = toSet;
                break;
            case KeyName.Sprint:
                sprintKey = toSet;
                break;
            case KeyName.Crouch:
                crouchKey = toSet;
                break;
            case KeyName.Jump:
                jumpClimbKey = toSet;
                break;
            case KeyName.Attack:
                attackKey = toSet;
                break;
            case KeyName.Aim:
                aimKey = toSet;
                break;
            case KeyName.Block:
                blockKey = toSet;
                break;
            case KeyName.Pause:
                pauseKey = toSet;
                break;
            case KeyName.Interact:
                interactKey = toSet;
                break;
            case KeyName.CycleObjectives:
                cycleObjectivesKey = toSet;
                break;
            case KeyName.Compass:
                compassKey = toSet;
                break;
            case KeyName.DropItem:
                dropKey = toSet;
                break;
            default:
                break;
        }
    }

    public void ChangeKey(string keyToChange)
    {
        Array values = Enum.GetValues(typeof(KeyName));
        for (int i = 0; i < values.Length; i++)
        {
            if (keyToChange == ((KeyName)values.GetValue(i)).ToString())
            {
                changingKey = (KeyName)values.GetValue(i);

                inputChange = true;
                return;
            }
        }

        Debug.Log("Invalid string input!");
    }

    public void SetButtonText(TextMeshProUGUI buttonText)
    {
        changeButtonText = buttonText;
    }
}
