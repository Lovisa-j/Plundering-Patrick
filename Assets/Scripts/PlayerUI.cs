using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerUI : MonoBehaviour
{
    [Header("Compass")]
    public GameObject compassObject;
    public GameObject compassUI;
    public RectTransform circularCompassIndicator;
    
    [Header("Items")]
    public Text interactionText;
    public GameObject readingUI;
    public Text readingText;
    public Button stopReadingButton;

    bool reading;

    PlayerController controller;

    private void Start()
    {
        controller = GetComponent<PlayerController>();

        if (stopReadingButton != null)
            stopReadingButton.onClick.AddListener(StopReading);
    }

    private void Update()
    {
        if (reading)
        {
            if (Input.GetKeyDown(InputManager.instance.pauseKey))
                StopReading();

            return;
        }

        Interacting();
        HandleCompass();
    }

    private void Interacting()
    {
        if (interactionText != null)
        {
            interactionText.enabled = controller.targetedInteraction != null && !controller.showCompass;
            if (interactionText.enabled)
            {
                interactionText.text = controller.targetedInteraction.interactionDescription;

                if (string.IsNullOrEmpty(controller.targetedInteraction.interactionDescription))
                {
                    if (controller.targetedInteraction is PickUpable)
                        interactionText.text = $"Take\n{((PickUpable)controller.targetedInteraction).item.itemName}";
                    else if (controller.targetedInteraction is Readable)
                        interactionText.text = $"Read\n'{((Readable)controller.targetedInteraction).readableName}'";
                }
            }
        }
    }

    private void HandleCompass()
    {
        if (compassObject != null)
        {
            if (controller.showCompass && !compassObject.activeInHierarchy)
                compassObject.SetActive(true);
            else if (!controller.showCompass && compassObject.activeInHierarchy)
                compassObject.SetActive(false);
        }
        if (compassUI != null)
        {
            if (controller.showCompass && !compassUI.activeInHierarchy)
                compassUI.SetActive(true);
            else if (!controller.showCompass && compassUI.activeInHierarchy)
                compassUI.SetActive(false);
        }

        if (circularCompassIndicator != null && controller.playerCamera != null)
            circularCompassIndicator.rotation = Quaternion.Euler(0, 0, controller.playerCamera.transform.eulerAngles.y);
    }

    public void ReadText(string textToRead)
    {
        Time.timeScale = 0;

        if (controller.playerCamera != null)
            controller.playerCamera.hideCursor = false;

        if (readingUI != null)
        {
            if (readingText != null)
                readingText.text = textToRead;

            readingUI.SetActive(true);
        }

        if (interactionText != null)
            interactionText.enabled = false;
        if (compassUI != null)
            compassUI.SetActive(false);

        reading = true;
    }

    private void StopReading()
    {
        Time.timeScale = 1;

        if (controller.playerCamera != null)
            controller.playerCamera.hideCursor = true;

        if (readingUI != null)
            readingUI.SetActive(false);

        reading = false;
    }
}
