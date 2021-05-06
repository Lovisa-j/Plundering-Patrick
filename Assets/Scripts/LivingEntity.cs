using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    [Header("Death Effect")]
    public GameObject deathEffect;
    public float deathEffectDuration;

    [Header("Health")]
    public int maxHealth;
    
    public int health { get; protected set; }

    public UnityEngine.Events.UnityEvent onDeath;

    protected virtual void Start()
    {
        health = maxHealth;
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > maxHealth)
            health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
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

        Destroy(gameObject);

        onDeath?.Invoke();
    }
}
