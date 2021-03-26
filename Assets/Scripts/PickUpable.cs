using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpable : Interactable
{
    public Item item;

    public void Create()
    {
        GameObject temp = new GameObject();
        temp.transform.parent = transform;
        temp.AddComponent<MeshFilter>().mesh = item.itemMesh;
        temp.AddComponent<MeshRenderer>().materials = item.itemMaterials;
        temp.transform.localPosition = item.itemPositionOffset;
        temp.transform.localEulerAngles = item.itemRotationOffset;
        temp.transform.localScale = item.itemScaleMultiplier;
    }

    public override void Interact(Transform interactingTransform)
    {
        PlayerController controller = interactingTransform.GetComponent<PlayerController>();
        if (controller == null)
            controller = interactingTransform.GetComponentInChildren<PlayerController>();

        if (controller != null)
        {
            base.Interact(interactingTransform);

            controller.money += item.worth;
            Destroy(gameObject);
        }
    }
}
