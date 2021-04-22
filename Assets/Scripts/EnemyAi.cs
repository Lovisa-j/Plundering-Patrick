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
public class EnemyAi : LivingEntity
{
    public AIState actualState = AIState.Idle;

    [Header("Movement")]
    public GameObject[] checkPoints;
    public float walkSpeed = 2;
    public float runSpeed = 4;

    [Header("Sight")]
    public Transform sightOrigin;
    public float fullViewDistance = 40;
    public float darkViewDistance = 15;
    public float viewAngleHorizontal = 80;
    public float viewAngleVertical = 35;
    public float spotTime = 0.5f;

    //Local
    private float stateTimer = 0;
    private float spotTimer;
    private int actualCheckpoint = 0;
    private NavMeshAgent agent;
    Vector3 alertPosition;

    bool DestinationReached
    {
        get
        {
            return agent.remainingDistance < agent.stoppingDistance && !agent.pathPending;
        }
    }

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        if (GetComponentInChildren<Light>())
        {
            GetComponentInChildren<Light>().spotAngle = viewAngleHorizontal;
            GetComponentInChildren<Light>().range = darkViewDistance;
        }
    }

     void Update()
    {
        stateTimer += Time.deltaTime;
        if (CanSeePlayer())
        {
            spotTimer += Time.deltaTime;
            if (spotTimer >= spotTime)
                ChangeState(AIState.Chase);
        }
        else
            spotTimer -= Time.deltaTime;

        spotTimer = Mathf.Clamp(spotTimer, 0, spotTime);
        if (GetComponentInChildren<Light>())
            GetComponentInChildren<Light>().color = Color.Lerp(Color.white, Color.red, spotTimer / spotTime);

        switch (actualState)
        {
            case AIState.Idle:
                //ACTIONS
                ChangeState(AIState.Patrol);
                //DECISIONS
                MoveToPosition(checkPoints[actualCheckpoint].transform.position);
                break;
            case AIState.Patrol:
                //ACTIONS
                agent.speed = walkSpeed;
                MoveToPosition(checkPoints[actualCheckpoint].transform.position);
                //DECISIONS
                if (DestinationReached)
                {
                    NextCheckPoint();
                    ChangeState(AIState.Wait);
                }
                break;
            case AIState.Wait:
                agent.speed = 0;
                agent.isStopped = true;
                if (TimeOut(5))
                {
                    ChangeState(AIState.Patrol);
                }
                break;
            case AIState.Alert:
                //ACTION
                agent.speed = walkSpeed;
                MoveToPosition(alertPosition);
                //DECISIONS
                if (DestinationReached)
                {
                    ChangeState(AIState.Wait);
                }
                break;
            case AIState.Chase:
                agent.speed = runSpeed;
                MoveToPosition(alertPosition);

                if (DestinationReached && !CanSeePlayer())
                {
                    ChangeState(AIState.Wait);
                }
                break;
            case AIState.Shoot:
                break;
        }
    }

    bool CanSeePlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(sightOrigin.position, fullViewDistance);
        for (int i = 0; i < colliders.Length; i++)
        {
            PlayerController targetController = colliders[i].GetComponent<PlayerController>();
            if (targetController != null)
            {
                float viewDistance = fullViewDistance;
                if (targetController.GetComponent<PlayerSneak>())
                    viewDistance = targetController.GetComponent<PlayerSneak>().IsInLight() ? fullViewDistance : darkViewDistance;

                Vector3 direction;
                RaycastHit hit;
                for (int c = 0; c < targetController.characterLimbs.Length; c++)
                {
                    direction = (targetController.characterLimbs[c].position - sightOrigin.position).normalized;
                    if (Physics.Raycast(sightOrigin.position, direction, out hit, viewDistance, ~(1 << 0) | (1 << 0), QueryTriggerInteraction.Collide))
                    {
                        Transform testTrans = hit.transform;
                        while (!testTrans.GetComponent<PlayerController>())
                        {
                            if (testTrans.parent == null)
                                break;

                            testTrans = testTrans.parent;
                            if (testTrans == hit.transform.root)
                                break;
                        }

                        if (testTrans.GetComponent<PlayerController>())
                        {
                            Vector3 yDirection = (targetController.characterLimbs[c].position - sightOrigin.position).normalized;
                            Vector3 xzDirection = yDirection;

                            yDirection.x = sightOrigin.forward.x;
                            yDirection.z = sightOrigin.forward.z;
                            yDirection.Normalize();

                            xzDirection.y = 0;
                            xzDirection.Normalize();
                            if (Vector3.Angle(sightOrigin.forward, xzDirection) < viewAngleHorizontal / 2 && Vector3.Angle(sightOrigin.forward, yDirection) < viewAngleVertical / 2)
                            {
                                alertPosition = colliders[i].transform.position;
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    void ChangeState(AIState newState)
    {
        actualState = newState;
        stateTimer = 0;
    }

    void MoveToPosition(Vector3 position)
    {
        agent.destination = position;
        agent.isStopped = false;
    }

    void NextCheckPoint()
    {
        actualCheckpoint = (actualCheckpoint + 1) % checkPoints.Length;
    }

    bool TimeOut(float timeToWait)
    {
        return stateTimer > timeToWait;
    }

    public void AlertToPosition(Vector3 position)
    {
        alertPosition = position;
        ChangeState(AIState.Alert);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
    }
}
