using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chest : Interactable
{
    public override void Interact(Transform interactingTransform)
    {
        anim.SetBool("open", !anim.GetBool("open"));
        base.Interact(interactingTransform);        
    }
}
