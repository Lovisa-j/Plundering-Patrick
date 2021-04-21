using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Openable : Interactable
{
    public string openInteractionDescription;
    public string closedInteractionDescription;

    public override void Interact(Transform interactingTransform)
    {
        if (animationTimer > 0)
            return;

        anim.SetBool("Open", !anim.GetBool("Open"));
        interactionDescription = anim.GetBool("Open") ? openInteractionDescription : closedInteractionDescription;

        if (anim != null)
            animationTimer = animationDuration;
        else
            interactionEvents.Invoke();
    }
}
