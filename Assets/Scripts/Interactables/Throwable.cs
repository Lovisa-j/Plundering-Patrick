using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Throwable : Interactable
{
    public Vector3 heldOffsetPosition;
    public Vector3 heldOffsetRotation;

    [Space(10)]
    public bool breakOnCollision;
    public GameObject breakEffectObject;
    public float breakEffectDuration;
    public float breakSoundDistance = 15;

    bool thrown = false;

    Rigidbody rb;
    Transform thrower;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Interact(Transform interactingTransform)
    {
        if (interactingTransform.GetComponent<PlayerAttacking>())
            interactingTransform.GetComponent<PlayerAttacking>().PickUpThrowable(this);

        interactionEvents.Invoke();
    }

    // Method used when the object is thrown, giving it a force and setting collider values.
    public void Throw(Transform thrower, Vector3 force)
    {
        this.thrower = thrower;

        transform.parent = null;

        Collider col = GetComponent<Collider>();
        if (col == null)
            col = GetComponentInChildren<Collider>();

        col.isTrigger = false;
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddRelativeTorque(Vector3.forward * Random.Range(-force.magnitude / 2, force.magnitude / 2), ForceMode.Impulse);
        thrown = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!breakOnCollision || !thrown || collision.transform == thrower)
            return;

        DestroyWithEffect();

        Tools.SoundFromPosition(transform.position, breakSoundDistance);
    }

    // Destroy the object after instantiating a breaking effect.
    public void DestroyWithEffect()
    {
        if (breakEffectObject != null)
            Destroy(Instantiate(breakEffectObject, transform.position, transform.rotation), breakEffectDuration);

        Destroy(gameObject);
    }
}
