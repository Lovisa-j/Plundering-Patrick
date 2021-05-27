using UnityEngine;

[RequireComponent(typeof(Identification))]
public class LivingEntity : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth;
    public GameObject hitEffect;
    public float hitEffectDuration;

    public int health { get; protected set; }

    public UnityEngine.Events.UnityEvent onDeath;

    public System.Action<int, Transform> onTakeHit;

    protected virtual void Start()
    {
        health = maxHealth;
    }

    public void Heal(int amount)
    {
        Heal(amount, false);
    }

    public void Heal(int amount, bool overheal)
    {
        health += amount;
        if (health > maxHealth && !overheal)
            health = maxHealth;
    }

    public virtual void TakeDamage(int damage, Transform damagingTransform, Vector3 hitPosition)
    {
        onTakeHit?.Invoke(damage, damagingTransform);

        string damagingTransId = (damagingTransform.GetComponent<Identification>()) ? damagingTransform.GetComponent<Identification>().id : "";
        GameEvents.onEntityHit?.Invoke(GetComponent<Identification>().id, damagingTransId);

        if (hitEffect != null)
            Destroy(Instantiate(hitEffect, hitPosition, transform.rotation), hitEffectDuration);

        health -= damage;

        if (health <= 0)
            Die(damagingTransform);
    }

    void Die(Transform killedBy)
    {
        onDeath?.Invoke();

        string killingTransId = (killedBy.GetComponent<Identification>()) ? killedBy.GetComponent<Identification>().id : "";
        GameEvents.onEntityDeath?.Invoke(GetComponent<Identification>().id, killingTransId);
    }
}
