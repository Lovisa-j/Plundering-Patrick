using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponAttack[] attacks;

    public bool colliderStatus
    {
        get
        {
            return attackCollider.enabled;
        }
        set
        {
            attackCollider.enabled = value;
        }
    }

    public WeaponAttack currentAttack { get; private set; }

    List<Transform> damagedTransforms = new List<Transform>();

    WeaponAttack nextAttack;
    Collider attackCollider;
    Transform owner;

    void Start()
    {
        attackCollider = GetComponent<Collider>();
        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
    }

    public void Tick()
    {
        if (currentAttack == null && nextAttack != null)
        {
            currentAttack = nextAttack;
            nextAttack = null;
            damagedTransforms.Clear();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        LivingEntity otherEntity = other.GetComponent<LivingEntity>();
        if (!attackCollider.enabled || otherEntity == null || currentAttack == null || damagedTransforms.Contains(other.transform))
            return;

        if (owner == null || other.transform == owner)
            return;

        otherEntity.TakeDamage(currentAttack.damage);
        damagedTransforms.Add(other.transform);
    }

    public void Attack(Transform owner)
    {
        this.owner = owner;

        if (nextAttack != null)
            return;

        for (int i = 0; i < attacks.Length; i++)
        {
            if (currentAttack != null)
            {
                if (attacks[i].conditionalAttack == currentAttack.attackString)
                {
                    nextAttack = attacks[i];
                    break;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(attacks[i].conditionalAttack))
                {
                    nextAttack = attacks[i];
                    break;
                }
            }
        }
    }

    public void FinishAttack()
    {
        currentAttack = null;
    }
}

[System.Serializable]
public class WeaponAttack
{
    public string attackString;
    public float transitionDuration;
    public float startTime;
    
    [Space(10)]
    public int damage;

    [Space(10)]
    public string conditionalAttack;
}
