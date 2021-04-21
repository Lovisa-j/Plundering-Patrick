using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Idle,
    Patrol,
    Wait,
    Alert,
    Chase,
    Shoot
}
public class EnemyAi : MonoBehaviour
{
    public AIState actualState = AIState.Idle;

    [Header("Movement")]
    public GameObject[] checkPoints;
    public float walkSpeed = 2;
    public float runSpeed = 4;

    [Header("Senses")]
    public float hearRange = 6;

    //Local
    private float stateTimer = 0;
    private int actualCheckpoint = 0;
    private NavMeshAgent agent;
    PlayerController player;
    Vector3 soundSource;
     void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindObjectOfType<PlayerController>();
    }

     void Update()
    {
        stateTimer += Time.deltaTime;
        switch (actualState)
        {
            case AIState.Idle:
                //ACTIONS
                ChangeState(AIState.Patrol);
                //DECISIONS
                MoveToCheckPoint();
                if (PlayerIsHeard())
                {
                    ChangeState(AIState.Alert);
                }
                break;
            case AIState.Patrol:
                //ACTIONS
                agent.speed = walkSpeed;
                MoveToCheckPoint();
                //DECISIONS
                if (DestinationReached())
                {
                    NextCheckPoint();
                    ChangeState(AIState.Wait);
                }
                if (PlayerIsHeard())
                {
                    ChangeState(AIState.Alert);
                }
                break;
            case AIState.Wait:
                agent.speed = 0;
                agent.isStopped = true;
                if (TimeOut(5))
                {
                    ChangeState(AIState.Patrol);
                }
                if (PlayerIsHeard())
                {
                    ChangeState(AIState.Alert);
                }
                break;
            case AIState.Alert:
                //ACTION
                agent.speed = walkSpeed;
                MoveToSound();
                //DECISIONS
                if (DestinationReached())
                {
                    ChangeState(AIState.Wait);
                }
                if (PlayerIsHeard())
                {
                    ChangeState(AIState.Alert);
                }
                break;
            case AIState.Chase:
                break;
            case AIState.Shoot:
                break;
        }
    }

    void ChangeState(AIState newState)
    {
        actualState = newState;
        stateTimer = 0;
    }

    void MoveToSound()
    {
        agent.destination = soundSource;
        agent.isStopped = false;
    }
    void MoveToCheckPoint()
    {
        agent.destination = checkPoints[actualCheckpoint].transform.position;
        agent.isStopped = false;
    }

    void NextCheckPoint()
    {
        actualCheckpoint++;
        if (actualCheckpoint >= checkPoints.Length)
            actualCheckpoint = 0;
    }

    bool DestinationReached()
    {
        return agent.remainingDistance < agent.stoppingDistance && !agent.pathPending;
        
        
    }

   bool TimeOut(float timeToWait)
    {
        return stateTimer > timeToWait;
    }

    bool PlayerIsHeard()
    {
        Vector3 vel = player.velocity;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool result = distance < hearRange && player.crouching == false && vel.x != 0 && vel.z != 0;

        if (result)
        {           
            soundSource = player.transform.position;            
        }
            
        
            return result;

        
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearRange);
    }
}
