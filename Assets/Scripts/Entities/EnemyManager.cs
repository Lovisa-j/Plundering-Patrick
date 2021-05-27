using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public int maxAttacks;

    List<EnemyAi> enemiesInScene = new List<EnemyAi>();
    List<EnemyAi> attackingEnemies = new List<EnemyAi>();

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
            if (attackingEnemies[i] == null || !attackingEnemies[i].enabled)
            {
                attackingEnemies.RemoveAt(i);
                break;
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

            if (attackingEnemies.Contains(currentEnemy) && currentEnemy.attackState == AttackState.None)
            {
                attackingEnemies.Remove(currentEnemy);
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

                    attackingEnemies.Add(currentEnemy);
                    continue;
                }


            }
        }
    }

    public void AddEnemy(EnemyAi toAdd)
    {
        if (!enemiesInScene.Contains(toAdd))
            enemiesInScene.Add(toAdd);
    }
}
