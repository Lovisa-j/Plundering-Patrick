using UnityEngine;
using TMPro;

public class KeybindUpdater : MonoBehaviour
{
    public KeyName trackedName;
    public TextMeshProUGUI buttonText;

    public void UpdateText()
    {
        if (buttonText == null || InputManager.instance == null)
            return;

        switch (trackedName)
        {
            case KeyName.Forward:
                buttonText.text = InputManager.instance.forwardKey.ToString();
                break;
            case KeyName.Back:
                buttonText.text = InputManager.instance.backKey.ToString();
                break;
            case KeyName.Right:
                buttonText.text = InputManager.instance.rightKey.ToString();
                break;
            case KeyName.Left:
                buttonText.text = InputManager.instance.leftKey.ToString();
                break;
            case KeyName.Sprint:
                buttonText.text = InputManager.instance.sprintKey.ToString();
                break;
            case KeyName.Crouch:
                buttonText.text = InputManager.instance.crouchKey.ToString();
                break;
            case KeyName.Jump:
                buttonText.text = InputManager.instance.jumpClimbKey.ToString();
                break;
            case KeyName.Attack:
                buttonText.text = InputManager.instance.attackKey.ToString();
                break;
            case KeyName.Aim:
                buttonText.text = InputManager.instance.aimKey.ToString();
                break;
            case KeyName.Block:
                buttonText.text = InputManager.instance.blockKey.ToString();
                break;
            case KeyName.Pause:
                buttonText.text = InputManager.instance.pauseKey.ToString();
                break;
            case KeyName.Interact:
                buttonText.text = InputManager.instance.interactKey.ToString();
                break;
            case KeyName.CycleObjectives:
                buttonText.text = InputManager.instance.cycleObjectivesKey.ToString();
                break;
            case KeyName.Compass:
                buttonText.text = InputManager.instance.compassKey.ToString();
                break;
            case KeyName.DropItem:
                buttonText.text = InputManager.instance.dropKey.ToString();
                break;
            default:
                break;
        }

        Destroy(this);
    }
}
