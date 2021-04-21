using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyDetection))]
public class EnemyMovement : MonoBehaviour
{
    public float speed;
    public float turnSpeed;
    public float attackSpeed;
    public float lungeSpeed;

    float horizontal;
    float vertical;
    float searchTimer;
    float timeToAttack;

    Vector3 lastPlayerPosition;
    Vector3 resetPosition = new Vector3(10000, 10000, 10000);

    EnemyDetection detection;
    Rigidbody rb;
    NavMeshAgent agent;
    CapsuleCollider myCollider;

    void Start()
    {
        detection = GetComponent<EnemyDetection>();
        rb = GetComponent<Rigidbody>();
        agent = gameObject.AddComponent<NavMeshAgent>();
        myCollider = GetComponent<CapsuleCollider>();

        rb.mass = 999;
        rb.angularDrag = 999;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        lastPlayerPosition = resetPosition;
        detection.onDetection += OnAlertedToPosition;
    }

    void Update()
    {
        if (lastPlayerPosition != resetPosition)
        {
            agent.SetDestination(lastPlayerPosition);
            agent.isStopped = true;

            if (agent.path.corners.Length > 1)
            {
                Vector3 targetDirection = agent.path.corners[1] - transform.position;
                targetDirection.y = 0;
                Vector3 localDirection = transform.InverseTransformDirection(targetDirection.normalized);

                horizontal = localDirection.x;
                vertical = localDirection.z;

                if ((lastPlayerPosition - transform.position).sqrMagnitude < 2.25f)
                {
                    horizontal = 0;
                    vertical = 0;

                    if (detection.playerSpotted)
                    {
                        if (Time.time >= timeToAttack)
                        {
                            timeToAttack = Time.time + (1f / attackSpeed);
                            StartCoroutine(Attack());
                        }
                    }
                    else
                    {
                        searchTimer += Time.deltaTime;
                        if (searchTimer > detection.searchTime)
                        {
                            lastPlayerPosition = resetPosition;
                            searchTimer = 0;
                        }
                    }
                }
            }
        }

        if (Mathf.Abs(horizontal) > 0 || Mathf.Abs(vertical) > 0)
            rb.drag = 0;
        else
            rb.drag = 10;
    }

    void FixedUpdate()
    {
        rb.velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * speed;
        Quaternion targetRot = (rb.velocity != Vector3.zero) ? Quaternion.LookRotation(rb.velocity) : transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
    }

    void OnAlertedToPosition(Vector3 position)
    {
        lastPlayerPosition = position;
    }

    IEnumerator Attack()
    {
        myCollider.isTrigger = true;

        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (lastPlayerPosition - transform.position).normalized;
        Vector3 attackPosition = lastPlayerPosition - (dirToTarget * myCollider.radius);

        float percent = 0;

        while (percent <= 1)
        {
            percent += Time.deltaTime * lungeSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        myCollider.isTrigger = false;
    }
}
