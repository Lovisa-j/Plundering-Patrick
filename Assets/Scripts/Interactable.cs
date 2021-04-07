using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string interactionDescription;
    
    [Header("Animation")]
    public Animator anim;

    public string interactAnimation;
    public float animationDuration;

    protected float animationTimer;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent interactionEvents;

    void Update()
    {
        if (animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            if (animationTimer <= 0)
                interactionEvents.Invoke();
        }
    }

    public virtual void Interact(Transform interactingTransform)
    {
        if (animationTimer > 0)
            return;

        if (anim != null && !string.IsNullOrEmpty(interactAnimation))
        {
            anim.Play(interactAnimation);
            animationTimer = animationDuration;
        }
        else
            interactionEvents.Invoke();
    }
}
