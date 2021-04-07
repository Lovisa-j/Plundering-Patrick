using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PlayerUI : MonoBehaviour
{
    public Text itemPickupText;

    PlayerController controller;

    private void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (itemPickupText != null)
        {
            itemPickupText.enabled = controller.targetedInteraction != null;
            if (itemPickupText.enabled)
            {
                if (controller.targetedInteraction is PickUpable)
                    itemPickupText.text = "Take\n" + ((PickUpable)controller.targetedInteraction).item.itemName;
                else
                    itemPickupText.text = controller.targetedInteraction.interactionDescription;
            }
        }
    }
}
