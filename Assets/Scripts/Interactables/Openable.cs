using UnityEngine;

public class Openable : Interactable
{
    public bool usingBool = true;

    public string openInteractionDescription;
    public string closedInteractionDescription;

    bool open;

    public override void Update()
    {
        base.Update();

        if (usingBool)
            anim.SetBool("Open", open);
        else
            anim.SetFloat("Open", open ? 1 : 0, 0.1f, Time.deltaTime);
    }

    public override void Interact(Identification interactingTransform)
    {
        if (animationTimer > 0)
            return;

        open = !open;

        interactionDescription = open ? openInteractionDescription : closedInteractionDescription;

        if (anim != null)
            animationTimer = animationDuration;
        else
            interactionEvents.Invoke();

        GameEvents.onInteraction?.Invoke(GetComponent<Identification>().id, interactingTransform.id);
    }
}
