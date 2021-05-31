using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackingEnemy
{
    public EnemyAi enemyReference;
    public float attackTimer;

    public AttackingEnemy(EnemyAi enemyReference)
    {
        this.enemyReference = enemyReference;
    }

    public override int GetHashCode()
    {
        return enemyReference.GetHashCode();
    }
}

public class EnemyManager : MonoBehaviour
{
    public int maxAttacks;

    public float maxTimeToAttack = 10;

    List<EnemyAi> enemiesInScene = new List<EnemyAi>();
    List<AttackingEnemy> attackingEnemies = new List<AttackingEnemy>();

    #region Singleton
    public static EnemyManager instance;

    void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }
    #endregion

    void LateUpdate()
    {
        for (int i = 0; i < attackingEnemies.Count; i++)
        {
            if (attackingEnemies[i].enemyReference == null || !attackingEnemies[i].enemyReference.enabled)
            {
                attackingEnemies.RemoveAt(i);
                continue;
            }

            attackingEnemies[i].attackTimer += Time.deltaTime;
            if (attackingEnemies[i].attackTimer > maxTimeToAttack)
            {
                attackingEnemies[i].enemyReference.ChangeState(AIState.Chase);
                attackingEnemies.RemoveAt(i);
            }
        }

        EnemyAi currentEnemy;
        for (int i = 0; i < enemiesInScene.Count; i++)
        {
            if (enemiesInScene[i] == null)
            {
                enemiesInScene.RemoveAt(i);
                i--;
                continue;
            }
            else if (!enemiesInScene[i].enabled)
                continue;

            currentEnemy = enemiesInScene[i];

            if (currentEnemy.attackState == AttackState.None)
            {
                bool contains = false;
                for (int c = 0; c < attackingEnemies.Count; c++)
                {
                    if (attackingEnemies[c].enemyReference == currentEnemy)
                    {
                        attackingEnemies.RemoveAt(c);
                        contains = true;
                        break;
                    }
                }
                if (contains)
                    continue;
            }

            if (currentEnemy.actualState == AIState.Chase)
            {
                if (attackingEnemies.Count < maxAttacks && currentEnemy.attackState != AttackState.None)
                {
                    if (currentEnemy.attackState == AttackState.Melee)
                        currentEnemy.ChangeState(AIState.Attack);
                    else if (currentEnemy.attackState == AttackState.Ranged)
                        currentEnemy.ChangeState(AIState.Shoot);

                    attackingEnemies.Add(new AttackingEnemy(currentEnemy));
                    continue;
                }

                // Set the enemy destinations here.
            }
        }
    }

    public void AddEnemy(EnemyAi toAdd)
    {
        if (!enemiesInScene.Contains(toAdd))
            enemiesInScene.Add(toAdd);
    }
}
