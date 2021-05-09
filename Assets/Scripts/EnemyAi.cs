using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Patrol,
    Wait,
    Alert,
    Chase,
    Shoot
}

[RequireComponent(typeof(BaseController))]
public class EnemyAi : MonoBehaviour
{
    public Transform aimTransform;
    public Transform spineBone;

    [Header("Movement")]
    public GameObject[] checkPoints;
    public Vector2 minMaxInvestigationDistance = new Vector2(2, 10);

    [Header("Sight")]
    public Transform sightOrigin;
    public float fullViewDistance = 40;
    public float darkViewDistance = 15;
    public float viewAngleHorizontal = 140;
    public float viewAngleVertical = 50;
    public float spotTime = 0.5f;

    [Header("Combat")]
    public Gun equippedGun;
    public float attackCooldown = 2;
    public float attackDelay = 1.5f;
    public float positionChangeTime = 2;
    public Vector2 preferedAttackDistance = new Vector2(4, 8);
    public float friendlyAlertDistance = 20;

    bool running;

    float horizontal;
    float vertical;
    float stateTimer = 0;
    float spotTimer;
    float attackPositioningTimer;
    float boneIkWeight;
    
    int actualCheckpoint = 0;

    AIState actualState;

    Vector3 alertPosition;
    Vector3 playerPosition;
    Vector3 attackPosition;

    BaseController controller;
    NavMeshAgent agent;

    bool DestinationReached
    {
        get
        {
            Vector3 targetPosition = agent.destination;
            targetPosition.y = transform.position.y;
            return ((targetPosition - transform.position).sqrMagnitude < Mathf.Pow(controller.velocity.magnitude * Time.fixedDeltaTime, 2) && agent.path.corners.Length <= 2) ||
                (targetPosition - transform.position).sqrMagnitude < 0.000625f;
        }
    }

    void Start()
    {
        controller = GetComponent<BaseController>();
        GameObject temp = new GameObject("Agent");
        temp.transform.parent = transform;
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;

        agent = temp.AddComponent<NavMeshAgent>();
        agent.height = controller.characterHeight - 0.08333319f;
        agent.isStopped = true;

        if (GetComponentInChildren<Light>())
        {
            GetComponentInChildren<Light>().spotAngle = viewAngleHorizontal;
            GetComponentInChildren<Light>().range = darkViewDistance;
        }
    }

    void Update()
    {
        controller.SetDirectionalOverride(transform.forward, transform.right);

        stateTimer += Time.deltaTime;

        Spotting();

        switch (actualState)
        {
            case AIState.Patrol:
                Patroling();
                break;
            case AIState.Wait:
                Waiting();
                break;
            case AIState.Alert:
                Alerted();
                break;
            case AIState.Chase:
                Chasing();
                break;
            case AIState.Shoot:
                Shooting();
                break;
        }

        controller.Tick(horizontal, vertical, running);
    }

    void FixedUpdate()
    {
        Vector3 targetPosition = (playerPosition == Vector3.zero) ? alertPosition : playerPosition;
        controller.FixedTick(targetPosition);
    }

    void LateUpdate()
    {
        if (spineBone == null || aimTransform == null || controller.climbState != BaseController.ClimbState.None || !controller.lockedMovement)
            return;

        if (actualState == AIState.Patrol)
            boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 10 * Time.deltaTime);

        Vector3 targetPosition = (playerPosition != Vector3.zero) ? playerPosition : alertPosition;
        for (int i = 0; i < 10; i++)
        {
            controller.AimAtTarget(spineBone, aimTransform, controller.GetTargetPosition(aimTransform, targetPosition), boneIkWeight);
        }
    }

    void Spotting()
    {
        if (actualState != AIState.Chase && actualState != AIState.Shoot)
        {
            if (CanSeePlayer())
            {
                spotTimer += Time.deltaTime;
                if (spotTimer >= spotTime)
                    ChangeState(AIState.Chase);
            }
            else
                spotTimer -= Time.deltaTime;
        }

        spotTimer = Mathf.Clamp(spotTimer, 0, spotTime);

        if (GetComponentInChildren<Light>())
            GetComponentInChildren<Light>().color = Color.Lerp(Color.white, Color.red, spotTimer / spotTime);
    }

    #region States
    void Patroling()
    {
        running = false;
        controller.overrideLockedMovement = false;

        MoveToPosition(checkPoints[actualCheckpoint].transform.position);

        if (DestinationReached)
        {
            actualCheckpoint = (actualCheckpoint + 1) % checkPoints.Length;
            ChangeState(AIState.Wait);
        }
    }

    void Waiting()
    {
        if (TimeOut(5))
            ChangeState(AIState.Patrol);
    }

    void Alerted()
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(alertPosition, out hit, 10, ~(1 << 0) | (1 << 0));

        MoveToPosition(hit.position);

        if (((agent.destination - transform.position).sqrMagnitude < Mathf.Pow(minMaxInvestigationDistance.x, 2) || 
            (agent.destination - transform.position).sqrMagnitude > Mathf.Pow(minMaxInvestigationDistance.y, 2) ||
            Mathf.Abs(alertPosition.y - transform.position.y) > controller.characterHeight) && 
            (agent.destination - transform.position).sqrMagnitude < Mathf.Pow(darkViewDistance, 2))
        {
            Vector3 targetPosition = alertPosition - (Vector3.up * controller.characterHeight / 2);
            Vector3 targetDirection;
            bool canSeePosition = false;

            for (int i = 1; i <= 10; i++)
            {
                targetDirection = targetPosition - sightOrigin.position;
                
                RaycastHit rayHit;
                if (!Physics.SphereCast(sightOrigin.position, controller.characterWidth / 2, targetDirection.normalized, out rayHit, targetDirection.magnitude))
                {
                    canSeePosition = true;
                    break;
                }

                targetPosition += Vector3.up * controller.characterHeight / 10;
            }

            if (canSeePosition)
            {
                MoveToPosition(transform.position);
                horizontal = 0;
                vertical = 0;
                ChangeState(AIState.Wait);

                controller.overrideLockedMovement = true;

                boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 5 * Time.deltaTime);
            }
            else
            {
                controller.overrideLockedMovement = false;

                boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 5 * Time.deltaTime);
            }
        }
        else
            boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 10 * Time.deltaTime);

        if (DestinationReached)
            ChangeState(AIState.Wait);
    }

    void Chasing()
    {
        running = true;

        if (CanSeePlayer())
        {
            alertPosition = playerPosition;
            if (attackPosition == Vector3.zero || DestinationReached)
            {
                attackPositioningTimer += Time.deltaTime;
                if (attackPositioningTimer >= positionChangeTime)
                {
                    Vector2 circlePosition;
                    Vector3 targetPosition;
                    Vector3 origin;
                    NavMeshHit hit;

                    do
                    {
                        circlePosition = new Vector2(alertPosition.x, alertPosition.z) + (Random.insideUnitCircle * Random.Range(preferedAttackDistance.x, preferedAttackDistance.y));
                        targetPosition = new Vector3(circlePosition.x, transform.position.y, circlePosition.y);
                        if (NavMesh.SamplePosition(targetPosition, out hit, 10, ~(1 << 0) | (1 << 0)))
                            attackPosition = hit.position;

                        origin = attackPosition;
                        origin.y = sightOrigin.position.y;
                    } while (!CanSeePlayer(origin));

                    attackPositioningTimer = 0;
                }
            }
            else
                attackPositioningTimer = 0;

            MoveToPosition(attackPosition);

            controller.overrideLockedMovement = true;

            boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 10 * Time.deltaTime);

            Tools.SoundFromPosition(transform.position, friendlyAlertDistance);

            if (TimeOut(attackCooldown))
                ChangeState(AIState.Shoot);
        }
        else
        {
            MoveToPosition(alertPosition);
            stateTimer = 0;

            controller.overrideLockedMovement = false;

            boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 10 * Time.deltaTime);

            if (DestinationReached)
                ChangeState(AIState.Wait);

            playerPosition = Vector3.zero;
            attackPosition = Vector3.zero;
        }
    }

    void Shooting()
    {
        horizontal = 0;
        vertical = 0;
        boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 10 * Time.deltaTime);
        controller.anim.SetBool("Aiming", true);

        if (TimeOut(attackDelay))
        {
            if (equippedGun != null && equippedGun.Shoot(alertPosition))
            {
                controller.anim.SetBool("Recoiling", true);
                StopShooting();
            }
        }

        if (!CanSeePlayer())
            StopShooting();
    }
    #endregion

    void ChangeState(AIState newState)
    {
        actualState = newState;
        stateTimer = 0;
    }

    void MoveToPosition(Vector3 position)
    {
        agent.SetDestination(position);

        Vector3 targetPosition = (agent.path.corners.Length > 1) ? agent.path.corners[1] : agent.destination;
        Vector3 worldDirection = targetPosition - transform.position;
        worldDirection.y = 0;
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection.normalized);

        horizontal = localDirection.x;
        vertical = localDirection.z;

        if (DestinationReached)
        {
            targetPosition = agent.destination;
            targetPosition.y = transform.position.y;
            transform.position = targetPosition;

            horizontal = 0;
            vertical = 0;
        }
    }

    void StopShooting()
    {
        controller.anim.SetBool("Aiming", false);
        ChangeState(AIState.Chase);
    }

    public void AlertToPosition(Vector3 position)
    {
        if ((actualState == AIState.Chase && CanSeePlayer()) || actualState == AIState.Shoot)
            return;

        alertPosition = position;
        ChangeState(AIState.Alert);
    }

    bool CanSeePlayer()
    {
        return CanSeePlayer(sightOrigin.position);
    }

    bool CanSeePlayer(Vector3 fromPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(fromPosition, fullViewDistance);
        for (int i = 0; i < colliders.Length; i++)
        {
            BaseController targetController = colliders[i].GetComponent<BaseController>();
            if (targetController != null && colliders[i].GetComponent<PlayerController>())
            {
                float viewDistance = fullViewDistance;
                if (targetController.GetComponent<PlayerSneak>())
                    viewDistance = targetController.GetComponent<PlayerSneak>().IsInLight() ? fullViewDistance : darkViewDistance;

                Vector3 direction;
                RaycastHit hit;
                for (int c = 0; c < targetController.characterLimbs.Length; c++)
                {
                    direction = (targetController.characterLimbs[c].position - fromPosition).normalized;
                    if (Physics.Raycast(fromPosition, direction, out hit, viewDistance, ~(1 << 0) | (1 << 0), QueryTriggerInteraction.Collide))
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

                        if (testTrans.GetComponent<PlayerController>() && IsDirectionWithinView(direction))
                        {
                            alertPosition = targetController.characterLimbs[c].position;
                            playerPosition = targetController.transform.position;
                            playerPosition.y += controller.characterHeight * 0.8f;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    bool TimeOut(float timeToWait)
    {
        return stateTimer > timeToWait;
    }

    bool IsDirectionWithinView(Vector3 direction)
    {
        if (sightOrigin == null)
            return false;

        Vector3 xzDirection = direction;
        Vector3 yDirection = direction;

        yDirection.x = sightOrigin.forward.x;
        yDirection.z = sightOrigin.forward.z;
        yDirection.Normalize();

        xzDirection.y = 0;
        xzDirection.Normalize();

        if (Vector3.Angle(sightOrigin.forward, xzDirection) <= viewAngleHorizontal / 2 && Vector3.Angle(sightOrigin.forward, yDirection) <= viewAngleVertical / 2)
            return true;

        return false;
    }
}
