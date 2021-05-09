using UnityEngine;

public class Interactable : MonoBehaviour
{
    [TextArea(0, 5)]
    public string interactionDescription;
    
    [Header("Animation")]
    public Animator anim;

    public string interactAnimation;
    public float animationDuration;

    protected float animationTimer;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent interactionEvents;

    public virtual void Update()
    {
        if (animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            if (animationTimer <= 0)
                interactionEvents.Invoke();
        }
    }

    // The base method that will be called when another object interacts with this one.
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
