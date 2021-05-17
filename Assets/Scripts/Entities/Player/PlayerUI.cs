using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerController))]
public class PlayerUI : MonoBehaviour
{
    [Header("General Gameplay")]
    public GameObject gameplayUI;
    public GameObject pauseUI;
    public GameObject deathUI;
    public Image healthBarFill;

    [Header("Ammo")]
    public GameObject ammoCountHolder;
    public Image bulletIcon;

    [Header("Light Level")]
    public Image lightLevelDisplay;
    public Color litDisplayColor;
    public Color shadowDisplayColor;

    [Header("Objectives")]
    public TextMeshProUGUI objectiveName;
    public TextMeshProUGUI objectiveDescription;

    [Header("Compass")]
    public GameObject compassObject;
    public GameObject compassUI;
    public RectTransform circularCompassIndicator;
    
    [Header("Items")]
    public TextMeshProUGUI interactionText;
    public GameObject throwingCrosshair;

    [Header("Reading")]
    public GameObject readingUI;
    public TextMeshProUGUI readingText;
    public Button stopReadingButton;

    bool reading;

    int currentObjectiveIndex;

    List<Image> bullets = new List<Image>();

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

        controller.onDeath.AddListener(OnDeath);

        if (stopReadingButton != null)
            stopReadingButton.onClick.AddListener(StopReading);
    }

    void LateUpdate()
    {
        if (reading)
        {
            if (Input.GetKeyDown(InputManager.instance.pauseKey))
            {
                StopReading();
                if (GameManager.instance != null)
                    GameManager.instance.UnpauseGame();
            }

            return;
        }

        if (GameManager.instance != null && GameManager.instance.gamePaused)
        {
            if (pauseUI != null && !pauseUI.activeInHierarchy)
                pauseUI.SetActive(true);

            return;
        }
        else
        {
            if (pauseUI != null && pauseUI.activeInHierarchy)
                pauseUI.SetActive(false);
        }

        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)controller.health / controller.maxHealth;

        Interacting();
        HandleCompass();
        AmmoAndCrosshair();
        HandleObjectives();

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

    void AmmoAndCrosshair()
    {
        if (attacker != null)
        {
            if (ammoCountHolder != null)
            {
                if (attacker.currentAmmoCount > bullets.Count)
                {
                    Image temp = Instantiate(bulletIcon, ammoCountHolder.transform);
                    bullets.Add(temp);
                }
                else if (attacker.currentAmmoCount < bullets.Count)
                {
                    Destroy(bullets[bullets.Count - 1].gameObject);
                    bullets.RemoveAt(bullets.Count - 1);
                }
            }

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
    }

    void HandleObjectives()
    {
        if (ObjectiveHandler.instance == null)
            return;

        if (Input.GetKeyDown(InputManager.instance.cycleObjectivesKey) && ObjectiveHandler.instance.ActiveObjectives.Count > 1)
            currentObjectiveIndex = (currentObjectiveIndex + 1) % ObjectiveHandler.instance.ActiveObjectives.Count;

        if (currentObjectiveIndex < ObjectiveHandler.instance.ActiveObjectives.Count)
        {
            Objective actualObjective = ObjectiveHandler.instance.ActiveObjectives[currentObjectiveIndex];
            if (objectiveName != null)
                objectiveName.text = actualObjective.name;
            if (objectiveDescription != null)
                objectiveDescription.text = actualObjective.description + $" - {actualObjective.Completions}/{actualObjective.requiredCompletions}";
        }
        else
        {
            if (ObjectiveHandler.instance.ActiveObjectives.Count > 0)
                currentObjectiveIndex = ObjectiveHandler.instance.ActiveObjectives.Count - 1;

            if (objectiveName != null)
                objectiveName.text = "";
            if (objectiveDescription != null)
                objectiveDescription.text = "";
        }
    }

    public void ReadText(string textToRead)
    {
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

        if (GameManager.instance != null)
            GameManager.instance.PauseGame();

        reading = true;
    }

    void StopReading()
    {
        if (controller.mCamera != null)
            controller.mCamera.hideCursor = true;

        if (readingUI != null)
            readingUI.SetActive(false);
        if (gameplayUI != null)
            gameplayUI.SetActive(true);

        if (GameManager.instance != null)
            GameManager.instance.UnpauseGame();

        reading = false;
    }

    void OnDeath()
    {
        if (gameplayUI != null)
            gameplayUI.SetActive(false);

        if (compassUI != null)
            compassUI.SetActive(false);

        if (readingUI != null)
            readingUI.SetActive(false);

        if (deathUI != null)
            deathUI.SetActive(true);
    }
}
