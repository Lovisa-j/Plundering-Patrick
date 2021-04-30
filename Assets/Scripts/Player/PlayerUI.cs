﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerUI : MonoBehaviour
{
    [Header("General Gameplay")]
    public GameObject gameplayUI;
    public Image healthBarFill;
    public Text ammoCount;
    public GameObject throwingCrosshair;

    [Header("Light Level")]
    public Image lightLevelDisplay;
    public Color litDisplayColor;
    public Color shadowDisplayColor;

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

    PlayerController player;
    BaseController controller;
    PlayerAttacking attacker;
    PlayerSneak sneak;

    void Start()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<BaseController>();
        attacker = GetComponent<PlayerAttacking>();
        sneak = GetComponent<PlayerSneak>();

        if (stopReadingButton != null)
            stopReadingButton.onClick.AddListener(StopReading);
    }

    void Update()
    {
        if (reading)
        {
            if (Input.GetKeyDown(InputManager.instance.pauseKey))
                StopReading();

            return;
        }

        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)controller.health / controller.maxHealth;

        Interacting();
        HandleCompass();
        if (attacker != null)
        {
            if (ammoCount != null)
                ammoCount.text = $"Bullets: {attacker.currentAmmoCount}";

            if (throwingCrosshair != null)
            {
                if (attacker.throwable != null && !throwingCrosshair.activeInHierarchy)
                    throwingCrosshair.SetActive(true);
                else if (attacker.throwable == null && throwingCrosshair.activeInHierarchy)
                    throwingCrosshair.SetActive(false);

                if (player.showCompass || reading)
                    throwingCrosshair.SetActive(false);
            }
        }

        if (sneak != null && lightLevelDisplay != null)
            lightLevelDisplay.color = sneak.IsInLight() ? litDisplayColor : shadowDisplayColor;
    }

    void Interacting()
    {
        if (interactionText != null)
        {
            interactionText.enabled = controller.targetedInteraction != null && !player.showCompass && !reading;
            if (interactionText.enabled)
            {
                interactionText.text = controller.targetedInteraction.interactionDescription;

                if (string.IsNullOrEmpty(controller.targetedInteraction.interactionDescription))
                {
                    if (controller.targetedInteraction is PickUpable)
                        interactionText.text = $"Take\n{((PickUpable)controller.targetedInteraction).item.itemName}";
                    else if (controller.targetedInteraction is Readable)
                        interactionText.text = $"Read\n'{((Readable)controller.targetedInteraction).readableName}'";
                    else if (controller.targetedInteraction is Throwable)
                        interactionText.text = $"Pick up\n{controller.targetedInteraction.transform.name}";
                }
            }
        }
    }

    void HandleCompass()
    {
        if (compassObject != null)
        {
            if (player.showCompass && !compassObject.activeInHierarchy)
                compassObject.SetActive(true);
            else if (!player.showCompass && compassObject.activeInHierarchy)
                compassObject.SetActive(false);
        }
        if (compassUI != null)
        {
            if (player.showCompass && !compassUI.activeInHierarchy)
                compassUI.SetActive(true);
            else if (!player.showCompass && compassUI.activeInHierarchy)
                compassUI.SetActive(false);
        }

        if (circularCompassIndicator != null && controller.mCamera != null)
            circularCompassIndicator.rotation = Quaternion.Euler(0, 0, controller.mCamera.transform.eulerAngles.y);
    }

    public void ReadText(string textToRead)
    {
        Time.timeScale = 0;

        if (controller.mCamera != null)
            controller.mCamera.hideCursor = false;

        if (readingUI != null)
        {
            if (readingText != null)
                readingText.text = textToRead;

            readingUI.SetActive(true);
        }

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
        if (compassUI != null)
            compassUI.SetActive(false);

        player.inMenu = true;

        reading = true;
    }

    void StopReading()
    {
        Time.timeScale = 1;

        if (controller.mCamera != null)
            controller.mCamera.hideCursor = true;

        if (readingUI != null)
            readingUI.SetActive(false);
        if (gameplayUI != null)
            gameplayUI.SetActive(true);

        player.inMenu = false;

        reading = false;
    }
}
