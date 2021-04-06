using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Setup")]
    public float playerHeight;
    public float playerWidth;
    public float climbAnimationLength;
    public float pickupDistance;

    [Header("Movement")]
    public float walkSpeed;
    public float runSpeed;
    public float turnSpeed;
    public float jumpVelocity;

    [Header("Climbing")]
    public float maxClimbDistance;
    public float maxClimbHeight;
    public float climbStartHeightDifference;
    public float climbDuration;

    [HideInInspector] public int money;

    [HideInInspector] public Interactable targetedInteraction;

    [HideInInspector] public Vector3 forwardOverride;
    [HideInInspector] public Vector3 rightOverride;

    float horizontal;
    float vertical;
    float velocityY;
    float climbTimer;
    public float currentSpeed { get; private set; }

    public enum ClimbState { None, SettingPosition, Climbing }
    public ClimbState climbState { get; private set; }

    bool isGrounded;

    Vector3 inputDir;
    Vector3 startClimbingPosition;
    Vector3 targetClimbingPosition;
    Quaternion targetClimbingRotation;

    public Rigidbody rb { get; private set; }
    public Animator anim { get; private set; }

    public Vector3 rbVelocity
    {
        get
        {
            return rb.velocity;
        }
    }

    private void Start()
    {
        forwardOverride = Vector3.forward;
        rightOverride = Vector3.right;

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

        vertical = InputManager.instance.Vertical;
        horizontal = InputManager.instance.Horizontal;
        inputDir = ((rightOverride * horizontal) + (forwardOverride * vertical)).normalized;

        bool running = Input.GetKey(InputManager.instance.sprintKey);
        currentSpeed = (running ? runSpeed : walkSpeed) * inputDir.magnitude;

        anim.SetFloat("Input", (running ? 1 : 0.5f) * inputDir.magnitude, 0.15f, Time.deltaTime);

        if (inputDir == Vector3.zero)
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
                float targetClimbY = hit.point.y;
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

                Vector3 targetClimbXZ = hit.point - (climbNormal * playerWidth);

                startClimbingPosition = hit.point + (climbNormal * (playerWidth / 2));
                startClimbingPosition.y = hit.point.y - climbStartHeightDifference;

                targetClimbingPosition = new Vector3(targetClimbXZ.x, targetClimbY, targetClimbXZ.z);
                targetClimbingRotation = Quaternion.LookRotation(-climbNormal);

                climbState = ClimbState.SettingPosition;
                return;
            }
        }

        if (isGrounded)
        {
            velocityY = jumpVelocity;
            isGrounded = false;
            anim.CrossFade("Jump", 0.1f);
        }
    }

    private void ClimbMovement()
    {
        velocityY = 0;
        rb.isKinematic = true;
        anim.SetFloat("Input", 0);

        switch (climbState)
        {
            case ClimbState.None:
                break;
            case ClimbState.SettingPosition:
                rb.velocity = Vector3.zero;

                if ((startClimbingPosition - transform.position).sqrMagnitude <= Mathf.Pow(15 * Time.deltaTime, 2) &&
                    Mathf.Abs(transform.eulerAngles.y - targetClimbingRotation.eulerAngles.y) <= 3)
                {
                    transform.position = startClimbingPosition;
                    transform.rotation = targetClimbingRotation;

                    anim.speed = climbAnimationLength / climbDuration;
                    anim.CrossFade("Climb", 0.1f);

                    climbState = ClimbState.Climbing;
                }

                transform.position = Vector3.Lerp(transform.position, startClimbingPosition, 15 * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetClimbingRotation, 40 * Time.deltaTime);
                break;
            case ClimbState.Climbing:
                if (climbTimer >= climbDuration - 0.05f)
                {
                    transform.position = targetClimbingPosition;

                    anim.speed = 1;
                    climbTimer = 0;

                    rb.isKinematic = false;

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
        if (climbState != ClimbState.None)
            return;

        rb.velocity = inputDir * currentSpeed;
        rb.velocity += Vector3.up * velocityY;
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
