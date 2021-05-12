using UnityEngine;

[RequireComponent(typeof(Identification))]
public class LivingEntity : MonoBehaviour
{
    [Header("Death Effect")]
    public GameObject deathEffect;
    public float deathEffectDuration;

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

        if (hitEffect != null)
            Destroy(Instantiate(hitEffect, hitPosition, transform.rotation), hitEffectDuration);

        health -= damage;

        if (health <= 0)
            Death();
    }

    void Death()
    {
        if (deathEffect != null)
        {
            if (deathEffectDuration > 0)
                Destroy(Instantiate(deathEffect, transform.position, transform.rotation), deathEffectDuration);
            else
                Instantiate(deathEffect, transform.position, transform.rotation);
        }

        onDeath?.Invoke();

        GameEvents.onEntityDeath?.Invoke(GetComponent<Identification>().id);

        Destroy(gameObject);
    }
}
