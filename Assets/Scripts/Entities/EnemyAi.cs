using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Patrol,
    Wait,
    Alert,
    Chase,
    Shoot,
    Attack
}

[RequireComponent(typeof(BaseController))]
public class EnemyAi : MonoBehaviour
{
    public Transform aimTransform;
    public Bone[] spineBones;
    public LayerMask playerLayer;
    public LayerMask viewLayer;

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
    public int animationLayer;

    [Range(0, 1)] public float shootPercentage = 0.5f;

    public Vector2 preferedDistance = new Vector2(4, 8);
    public float positionChangeTime = 2;
    public float attackCooldown = 2;
    public float friendlyAlertDistance = 20;

    [Header("Melee")]
    public Weapon equippedWeapon;
    public float meleeDistance = 1.5f;

    [Header("Shooting")]
    public Gun equippedGun;
    public float shootDelay = 1.5f;

    bool attacking;
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
    Vector3 attackPosition;
    Vector3 shootPosition;

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
        controller.aHook.onUpdateDamageCollider += UpdateWeaponCollider;
        controller.onTakeHit += OnTakeDamage;

        // Initialize the NavMeshAgent.
        GameObject temp = new GameObject("Agent");
        temp.transform.parent = transform;
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.layer = gameObject.layer;

        agent = temp.AddComponent<NavMeshAgent>();
        agent.height = controller.characterHeight - 0.08333319f;
        agent.isStopped = true;
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
            case AIState.Attack:
                Attacking();
                break;
        }

        if (agent.destination != transform.position)
            Move();
        else
            Stop();

        controller.Tick(horizontal, vertical, running);

        agent.transform.localPosition = Vector3.zero;
        agent.isStopped = true;
    }

    void FixedUpdate()
    {
        controller.FixedTick(alertPosition);
    }

    void LateUpdate()
    {
        if (aimTransform == null || controller.climbState != BaseController.ClimbState.None)
            return;

        if (actualState == AIState.Patrol)
            boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 10 * Time.deltaTime);

        for (int i = 0; i < spineBones.Length; i++)
        {
            if (spineBones[i].boneTrans == null)
                continue;

            for (int c = 0; c < 10; c++)
            {
                controller.AimAtTarget(spineBones[i].boneTrans, aimTransform, controller.GetTargetPosition(aimTransform, alertPosition), boneIkWeight * spineBones[i].weight);
            }
        }
    }

    // Handles spotting of the player when the enemy is not alerted.
    void Spotting()
    {
        if (actualState != AIState.Chase && actualState != AIState.Shoot && actualState != AIState.Attack)
        {
            if (CanSeePlayer())
            {
                spotTimer += Time.deltaTime;
                if (spotTimer >= spotTime)
                {
                    ChangeState(AIState.Chase);
                    attackPosition = transform.position;
                }
            }
            else
                spotTimer -= Time.deltaTime;
        }

        spotTimer = Mathf.Clamp(spotTimer, 0, spotTime);
    }

    #region States
    void Patroling()
    {
        running = false;
        controller.overrideLockedMovement = false;
        attackPosition = Vector3.zero;

        agent.SetDestination(checkPoints[actualCheckpoint].transform.position);

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

        float sqrDistanceToDest = (hit.position - transform.position).sqrMagnitude;

        bool distanceLess = sqrDistanceToDest < Mathf.Pow(minMaxInvestigationDistance.x, 2);
        bool distanceMore = sqrDistanceToDest > Mathf.Pow(minMaxInvestigationDistance.y, 2);
        bool heightDiff = Mathf.Abs(alertPosition.y - transform.position.y) > controller.characterHeight;

        bool canSeeAlertPosition = CanSeePosition(alertPosition);
        if (canSeeAlertPosition)
        {
            controller.overrideLockedMovement = true;
            boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 5 * Time.deltaTime);

            if (!heightDiff && sightOrigin != null)
                alertPosition.y = sightOrigin.position.y;
        }
        else
        {
            controller.overrideLockedMovement = false;
            boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 5 * Time.deltaTime);
        }

        if ((distanceLess || distanceMore || heightDiff) && sqrDistanceToDest < Mathf.Pow(darkViewDistance, 2))
        {
            if (canSeeAlertPosition)
            {
                Stop();
                ChangeState(AIState.Wait);
                return;
            }
        }

        agent.SetDestination(hit.position);

        if (DestinationReached)
            ChangeState(AIState.Wait);
    }

    void Chasing()
    {
        if (CanSeePlayer())
        {
            if ((new Vector3(alertPosition.x, transform.position.y, alertPosition.z) - transform.position).sqrMagnitude > preferedDistance.y)
                running = true;
            else
                running = false;

            bool tooClose = (alertPosition - new Vector3(attackPosition.x, alertPosition.y, attackPosition.z)).sqrMagnitude < Mathf.Pow(preferedDistance.x, 2);
            bool tooFar = (alertPosition - new Vector3(transform.position.x, alertPosition.y, transform.position.z)).sqrMagnitude > Mathf.Pow(preferedDistance.y, 2);

            if (tooClose)
            {
                attackPosition = Vector3.zero;
                attackPositioningTimer = positionChangeTime;
                tooFar = false;
            }

            if (DestinationReached)
                attackPosition = Vector3.zero;

            if (attackPosition == Vector3.zero && !tooFar)
            {
                attackPositioningTimer += Time.deltaTime;
                if (attackPositioningTimer >= positionChangeTime)
                {
                    Vector2 circlePosition;
                    Vector3 targetPosition;
                    Vector3 origin;
                    NavMeshHit hit;

                    circlePosition = new Vector2(alertPosition.x, alertPosition.z) + (Random.insideUnitCircle * Random.Range(preferedDistance.x, preferedDistance.y));
                    targetPosition = new Vector3(circlePosition.x, transform.position.y, circlePosition.y);

                    if (NavMesh.SamplePosition(targetPosition, out hit, 10, ~(1 << 0) | (1 << 0)))
                    {
                        attackPosition = hit.position;
                        
                        origin = attackPosition;
                        origin.y += Mathf.Abs(sightOrigin.position.y - transform.position.y);
                    }
                    else
                        origin = targetPosition;

                    if (CanSeePlayer(origin))
                    {
                        attackPositioningTimer = 0;

                        agent.SetDestination(attackPosition);
                    }
                    else
                    {
                        attackPosition = Vector3.zero;
                        Stop();
                    }
                }
            }
            else
            {
                attackPositioningTimer = 0;

                if (tooFar)
                {
                    agent.SetDestination(alertPosition);
                    attackPositioningTimer = positionChangeTime;
                    attackPosition = Vector3.zero;
                }
            }

            controller.overrideLockedMovement = true;

            boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 10 * Time.deltaTime);

            Collider[] colliders = Physics.OverlapSphere(transform.position, friendlyAlertDistance, ~viewLayer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].GetComponent<EnemyAi>())
                    colliders[i].GetComponent<EnemyAi>().AlertToPosition(alertPosition);
            }

            if (TimeOut(attackCooldown))
            {
                bool shoot = (Random.value <= shootPercentage) ? true : false;

                if (shoot)
                    ChangeState(AIState.Shoot);
                else
                    ChangeState(AIState.Attack);
            }
        }
        // If the enemy can't see the player.
        else
        {
            agent.SetDestination(alertPosition);
            stateTimer = 0;

            if (DestinationReached)
            {
                controller.overrideLockedMovement = false;
                boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 5 * Time.deltaTime);

                Stop();
                ChangeState(AIState.Wait);
            }
            else
            {
                float sqrAlertDistance = (alertPosition - new Vector3(transform.position.x, alertPosition.y, transform.position.z)).sqrMagnitude;
                if (CanSeePosition(alertPosition) && sqrAlertDistance <= Mathf.Pow(darkViewDistance, 2))
                {
                    controller.overrideLockedMovement = true;
                    boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 10 * Time.deltaTime);
                }
                else
                {
                    controller.overrideLockedMovement = false;
                    boneIkWeight = Mathf.Lerp(boneIkWeight, 0, 10 * Time.deltaTime);
                }
            }

            attackPosition = Vector3.zero;
        }
    }

    void Shooting()
    {
        Stop();
        boneIkWeight = Mathf.Lerp(boneIkWeight, 1, 10 * Time.deltaTime);
        controller.anim.SetBool("Aiming", true);

        if (TimeOut(shootDelay))
        {
            if (equippedGun != null && equippedGun.Shoot(shootPosition, transform))
            {
                controller.anim.SetBool("Recoiling", true);
                StopShooting();
            }
        }

        if (!CanSeePlayer())
            StopShooting();
    }

    void Attacking()
    {
        attackPosition = Vector3.zero;

        Vector3 direction = alertPosition - transform.position;
        direction.y = 0;

        agent.SetDestination(alertPosition);

        if (equippedWeapon != null && direction.sqrMagnitude <= Mathf.Pow(meleeDistance, 2))
        {
            equippedWeapon.Attack(transform);

            equippedWeapon.Tick();

            if (equippedWeapon.currentAttack != null && !attacking)
            {
                controller.anim.CrossFade(equippedWeapon.currentAttack.attackString, equippedWeapon.currentAttack.transitionDuration, animationLayer, equippedWeapon.currentAttack.startTime);
                attacking = true;
                controller.overrideLockedMovement = true;
            }
        }

        if (!CanSeePlayer())
            ChangeState(AIState.Chase);
    }
    #endregion

    void ChangeState(AIState newState)
    {
        actualState = newState;
        stateTimer = 0;
    }

    // Sets the horizontal and vertical to values which make the enemy move to the NavMeshAgents destination.
    void Move()
    {
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

    void Stop()
    {
        agent.SetDestination(transform.position);
        horizontal = 0;
        vertical = 0;
    }

    void StopShooting()
    {
        controller.anim.SetBool("Aiming", false);
        ChangeState(AIState.Chase);
    }

    // Sets the collider on the weapon to be enabled or disabled depending on an event.
    void UpdateWeaponCollider(bool value)
    {
        if (equippedWeapon == null)
            return;

        equippedWeapon.colliderStatus = value;

        // If the collider is being set to disabled, it means that the attack is finishing.
        if (!value)
        {
            equippedWeapon.FinishAttack();
            attacking = false;
            controller.overrideLockedMovement = false;
            ChangeState(AIState.Chase);
        }
    }

    // A method subscribed to the onTakeHit event of the BaseController.
    void OnTakeDamage(int damageTaken, Transform damagingTransform)
    {
        if (damagingTransform.GetComponent<EnemyAi>())
            return;

        Vector3 direction = damagingTransform.position - transform.position;
        direction.y = 0;

        AlertToPosition(transform.position + (direction * 2));
    }

    public void AlertToPosition(Vector3 position)
    {
        if (CanSeePlayer())
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
        Collider[] colliders = Physics.OverlapSphere(fromPosition, fullViewDistance, playerLayer);
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
                    if (Physics.Raycast(fromPosition, direction, out hit, viewDistance, viewLayer, QueryTriggerInteraction.Collide))
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
                            if (actualState != AIState.Alert && actualState != AIState.Patrol && actualState != AIState.Wait)
                            {
                                shootPosition = targetController.characterLimbs[c].position;
                                alertPosition = targetController.transform.position;
                                alertPosition.y += targetController.characterHeight - 0.25f;
                            }

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    bool CanSeePosition(Vector3 position)
    {
        bool toReturn = false;

        Vector3 targetPosition = position + (Vector3.up * controller.characterHeight);
        Vector3 targetDirection;

        for (int i = 0; i < 20; i++)
        {
            targetDirection = targetPosition - sightOrigin.position;

            RaycastHit rayHit;
            if (!Physics.SphereCast(sightOrigin.position, controller.characterWidth / 2, targetDirection.normalized, out rayHit, targetDirection.magnitude, ~(1<<8 | 1<<10)))
            {
                toReturn = true;
                break;
            }

            targetPosition -= Vector3.up * controller.characterHeight / 10;
        }

        return toReturn;
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

        xzDirection.y = sightOrigin.forward.y;
        xzDirection.Normalize();

        if (Vector3.Angle(sightOrigin.forward, xzDirection) <= viewAngleHorizontal / 2 && Vector3.Angle(sightOrigin.forward, yDirection) <= viewAngleVertical / 2)
            return true;

        return false;
    }

    void OnDrawGizmos()
    {
        if (agent != null)
            Gizmos.DrawSphere(agent.destination, 1);
    }
}
