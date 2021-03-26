using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerUI : MonoBehaviour
{
    public Text itemPickupText;

    PlayerController controller;

    void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (itemPickupText != null)
        {
            itemPickupText.enabled = controller.targetedInteraction != null;
            if (itemPickupText.enabled)
            {
                if (controller.targetedInteraction is PickUpable)
                    itemPickupText.text = "[E] Take\n" + ((PickUpable)controller.targetedInteraction).item.itemName;
                else
                    itemPickupText.text = controller.targetedInteraction.interactionDescription;
            }
        }
    }
}
