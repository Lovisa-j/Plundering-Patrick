using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpable : Interactable
{
    public Item item;

    public void Create()
    {
        GameObject temp = Instantiate(item.itemPrefab, transform);
        temp.transform.name = item.itemName;
        temp.transform.localPosition = item.itemPositionOffset;
        temp.transform.localEulerAngles = item.itemRotationOffset;
        temp.transform.localScale = item.itemScaleMultiplier;
    }

    public override void Interact(Transform interactingTransform)
    {
        PlayerController player = interactingTransform.GetComponent<PlayerController>();
        if (player == null)
            player = interactingTransform.GetComponentInChildren<PlayerController>();

        if (player != null)
        {
            base.Interact(interactingTransform);

            player.Money += item.worth;
            Destroy(gameObject);
        }
    }
}
