using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Setup")]
    public float playerHeight;
    public float playerWidth;
    public float longClimbAnimationLength;
    public float shortClimbAnimationLength;
    public float pickupDistance;

    [Header("Movement")]
    public float walkSpeed;
    public float runSpeed;
    public float turnSpeed;
    public float jumpHeight;
    public float accelerationTime;
    public float decelerationTime;

    [Header("Climbing")]
    public float maxClimbDistance;
    public float maxClimbHeight;
    public float longClimbStartHeightDifference;
    public float longClimbDuration;
    public float shortClimbHeight;
    public float shortClimbStartHeightDifference;
    public float shortClimbDuration;

    [HideInInspector] public int money;

    [HideInInspector] public Interactable targetedInteraction;

    [HideInInspector] public Vector3 forwardOverride;
    [HideInInspector] public Vector3 rightOverride;

    float horizontal;
    float vertical;
    float currentSpeed;
    float speedSmoothVelocity;
    float velocityY;
    float climbTimer;
    float climbDuration;

    bool isGrounded;

    Vector3 inputDir;
    Vector3 startClimbingPosition;
    Vector3 targetClimbingPosition;
    Quaternion targetClimbingRotation;

    public Vector3 velocity { get; private set; }

    public enum ClimbState { None, SettingPosition, Climbing }
    public ClimbState climbState { get; private set; }

    public Rigidbody rb { get; private set; }
    public Animator anim { get; private set; }

    private void Start()
    {
        forwardOverride = Vector3.forward;
        rightOverride = Vector3.right;

        Collider thisCollider = GetComponent<Collider>();
        if (thisCollider == null)
            thisCollider = GetComponentInChildren<Collider>();
        if (thisCollider != null)
        {
            PhysicMaterial material = new PhysicMaterial();
            material.dynamicFriction = 0;
            material.staticFriction = 0;
            material.frictionCombine = PhysicMaterialCombine.Minimum;
            thisCollider.material = material;
        }

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.angularDrag = 999;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
    }

    private void Update()
    {
        if (climbState != ClimbState.None)
        {
            ClimbMovement();
            return;
        }

        //Getting input and setting target speed
        vertical = InputManager.instance.Vertical;
        horizontal = InputManager.instance.Horizontal;
        inputDir = ((rightOverride * horizontal) + (forwardOverride * vertical)).normalized;

        bool running = Input.GetKey(InputManager.instance.sprintKey);
        float targetSpeed = (running ? runSpeed : walkSpeed) * inputDir.magnitude;
        float moveAmount = (-0.0071f * Vector3.Angle(transform.forward, velocity)) + 1.1429f;
        moveAmount = Mathf.Clamp01(moveAmount);
        float smoothTime = (targetSpeed > 0.1f) ? accelerationTime : decelerationTime;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed * moveAmount, ref speedSmoothVelocity, smoothTime);

        velocity = (isGrounded) ? inputDir * currentSpeed : velocity;

        anim.SetFloat("Input", (running ? 1 : 0.5f) * inputDir.magnitude, 0.15f, Time.deltaTime);

        if (inputDir == Vector3.zero && isGrounded)
            rb.drag = 10;
        else
            rb.drag = 0;

        if (Input.GetKeyDown(InputManager.instance.interactKey) && targetedInteraction != null)
            targetedInteraction.Interact(transform);

        if (Input.GetKey(InputManager.instance.jumpClimbKey))
            ClimbingAndJumping();

        if (isGrounded)
            velocityY = -1;
        else
            velocityY -= 9.82f * Time.deltaTime;
    }

    private void ClimbingAndJumping()
    {
        bool ledgeAvailable = true;
        if (inputDir.magnitude > 0)
        {
            RaycastHit hit;
            float distance = (playerWidth / 2) + 0.05f;
            while (!Physics.Raycast(transform.position + transform.forward * distance + Vector3.up * maxClimbHeight, -Vector3.up, out hit, maxClimbHeight - (playerHeight / 4), ~(1 << 8)))
            {
                distance += 0.05f;
                if (distance > maxClimbDistance)
                {
                    ledgeAvailable = false;
                    break;
                }
            }

            RaycastHit testHit;
            if (Physics.SphereCast(transform.position, (playerWidth / 2) - 0.05f, Vector3.up, out testHit, maxClimbHeight, ~(1 << 8)))
                ledgeAvailable = false;

            Vector3 rayPos = transform.position;
            rayPos.y = hit.point.y + 0.05f;
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;
            if (Physics.Raycast(rayPos, transform.forward, out testHit, direction.magnitude + 0.4f, ~(1 << 8)))
                ledgeAvailable = false;

            if (ledgeAvailable)
            {
                float targetClimbY = hit.point.y - 0.01f;
                Vector3 climbNormal = new Vector3();

                Vector3 rayPosition = transform.position;
                rayPosition.y = hit.point.y - 0.001f;
                Vector3 rayDirection = hit.point - transform.position;
                rayDirection.y = 0;
                rayDirection.Normalize();

                if (Physics.Raycast(rayPosition, rayDirection, out hit, maxClimbDistance + 0.01f))
                {
                    climbNormal = hit.normal;
                    climbNormal.y = 0;
                    climbNormal.Normalize();
                }

                Vector3 targetClimbXZ = hit.point - (climbNormal * playerWidth / 2);
                targetClimbingPosition = new Vector3(targetClimbXZ.x, targetClimbY, targetClimbXZ.z);
                targetClimbingRotation = Quaternion.LookRotation(-climbNormal);

                startClimbingPosition = hit.point + (climbNormal * (playerWidth / 2));
                if (targetClimbY - transform.position.y <= shortClimbHeight)
                    startClimbingPosition.y = targetClimbY - shortClimbStartHeightDifference;
                else
                    startClimbingPosition.y = targetClimbY - longClimbStartHeightDifference;

                climbState = ClimbState.SettingPosition;
                return;
            }
        }

        if (isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(2 * 9.82f * jumpHeight);
            velocityY = jumpVelocity;
            isGrounded = false;
            anim.CrossFade("Jump", 0.1f);
        }
    }

    private void ClimbMovement()
    {
        velocityY = 0;
        velocity = Vector3.zero;
        rb.isKinematic = true;
        anim.SetFloat("Input", 0);

        switch (climbState)
        {
            case ClimbState.None:
                break;
            case ClimbState.SettingPosition:
                if ((startClimbingPosition - transform.position).sqrMagnitude <= 0.0025f)
                {
                    transform.rotation = targetClimbingRotation;

                    if (targetClimbingPosition.y - startClimbingPosition.y <= shortClimbHeight)
                    {
                        anim.speed = shortClimbAnimationLength / shortClimbDuration;
                        anim.CrossFade("Climb_Short", 0.1f);
                        climbDuration = shortClimbDuration;
                    }
                    else
                    {
                        anim.speed = longClimbAnimationLength / longClimbDuration;
                        anim.CrossFade("Climb", 0.1f);
                        climbDuration = longClimbDuration;
                    }

                    climbState = ClimbState.Climbing;
                }
                break;
            case ClimbState.Climbing:
                if (climbTimer >= climbDuration)
                {
                    transform.position = targetClimbingPosition;

                    anim.speed = 1;
                    climbTimer = 0;

                    rb.isKinematic = false;
                    rb.drag = 10;
                    isGrounded = true;
                    anim.SetBool("Grounded", true);

                    climbState = ClimbState.None;

                    return;
                }

                climbTimer += Time.deltaTime;

                float lerpValue = Mathf.InverseLerp(0, climbDuration, climbTimer);
                transform.position = Vector3.Lerp(startClimbingPosition, targetClimbingPosition, lerpValue);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (climbState == ClimbState.SettingPosition)
        {
            if ((startClimbingPosition - transform.position).sqrMagnitude <= Mathf.Pow(10 * Time.fixedDeltaTime, 2))
                transform.position = startClimbingPosition;

            transform.position = Vector3.Lerp(transform.position, startClimbingPosition, 10 * Time.fixedDeltaTime);

            Vector3 lookDirection = (targetClimbingPosition - transform.position);
            lookDirection.y = 0;
            lookDirection.Normalize();
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        if (climbState != ClimbState.None)
            return;

        rb.velocity = velocity + (Vector3.up * velocityY);
        Quaternion targetRot = (inputDir != Vector3.zero) ? Quaternion.LookRotation(inputDir) : transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
    }

    #region CollisionDetection
    private void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(-Vector3.up, collision.GetContact(i).normal) < 20 && velocityY > 0)
            {
                velocityY = 0;
                break;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(Vector3.up, collision.GetContact(i).normal) < 45)
            {
                isGrounded = true;
                anim.SetBool("Grounded", true);
                break;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        anim.SetBool("Grounded", false);
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(Vector3.up, collision.GetContact(i).normal) < 45)
            {
                isGrounded = true;
                anim.SetBool("Grounded", true);
                break;
            }
        }
    }
    #endregion
}
