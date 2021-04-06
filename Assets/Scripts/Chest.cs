﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chest : Interactable
{
    public string openInteractionDescription;
    public string closedInteractionDescription;

    public override void Interact(Transform interactingTransform)
    {
        anim.SetBool("Open", !anim.GetBool("Open"));
        interactionDescription = anim.GetBool("Open") ? openInteractionDescription : closedInteractionDescription;

        base.Interact(interactingTransform);        
    }
}
