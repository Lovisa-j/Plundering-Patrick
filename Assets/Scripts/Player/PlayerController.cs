using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : LivingEntity
{
    [Header("Setup")]
    public float characterHeight;
    public float characterWidth;
    public float characterCrouchHeight;
    public Transform[] characterLimbs;
    [Space(10)]
    public float longClimbAnimationLength;
    public float longClimbStartHeightDifference;
    [Space(5)]
    public float shortClimbAnimationLength;
    public float shortClimbStartHeightDifference;

    [Header("Stats")]
    public ControllerStats stats;

    [HideInInspector] public bool showCompass;
    [HideInInspector] public bool lockedMovement;
    [HideInInspector] public bool inMenu;

    [HideInInspector] public Interactable targetedInteraction;

    [HideInInspector] public Vector3 forwardOverride;
    [HideInInspector] public Vector3 rightOverride;

    int money;
    public int Money 
    { 
        get 
        { 
            return money; 
        } 
        set 
        { 
            money = value; 
        } 
    }

    float horizontal;
    float vertical;
    float currentSpeed;
    float speedSmoothVelocity;
    float velocityY;
    float climbTimer;
    float climbDuration;

    public bool crouching { get; private set; }
    public bool isGrounded { get; private set; }

    Vector3 inputDir;
    Vector3 startClimbingPosition;
    Vector3 targetClimbingPosition;
    Quaternion targetClimbingRotation;

    public Vector3 velocity { get; private set; }

    public enum ClimbState { None, SettingPosition, Climbing }
    public ClimbState climbState { get; private set; }

    public CameraController mCamera { get; set; }
    public Rigidbody rb { get; private set; }
    public Animator anim { get; private set; }
    public AnimatorHook aHook { get; private set; }

    void Awake()
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
        if (anim != null)
        {
            aHook = anim.gameObject.AddComponent<AnimatorHook>();
            aHook.Initialize(this);
        }
    }

    void Update()
    {
        if (climbState != ClimbState.None)
        {
            ClimbMovement();
            return;
        }

        lockedMovement = anim.GetBool("LockMovement");

        if (Input.GetKeyDown(InputManager.instance.compassKey) && !inMenu)
            showCompass = !showCompass;

        if (Input.GetKeyDown(InputManager.instance.crouchKey) && !inMenu)
            Crouch();

        vertical = InputManager.instance.Vertical;
        horizontal = InputManager.instance.Horizontal;
        inputDir = ((rightOverride * horizontal) + (forwardOverride * vertical)).normalized;

        if (!inMenu)
        {
            if (lockedMovement)
                MovementLocked();
            else
                MovementNormal();
        }

        if (Input.GetKey(InputManager.instance.jumpClimbKey) && !inMenu)
            ClimbingAndJumping();

        rb.drag = 0;
        if (isGrounded)
        {
            velocityY = -1;
            if (inputDir == Vector3.zero || inMenu)
                rb.drag = 10;
        }
        else
        {
            velocityY -= 9.82f * Time.deltaTime;
            showCompass = false;
        }

        if (Input.GetKeyDown(InputManager.instance.interactKey) && !inMenu && targetedInteraction != null && !showCompass)
            targetedInteraction.Interact(transform);

        anim.SetBool("Compass", showCompass);
    }

    void Crouch()
    {
        crouching = !crouching;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, characterWidth / 2, Vector3.up, out hit, characterHeight + 0.01f, ~(1 << 8), QueryTriggerInteraction.Ignore) && !crouching)
            crouching = true;
        
        anim.SetBool("Crouched", crouching);

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (crouching)
        {
            col.height = characterCrouchHeight;
            col.center = new Vector3(col.center.x, characterCrouchHeight / 2, col.center.z);
        }
        else
        {
            col.height = characterHeight;
            col.center = new Vector3(col.center.x, characterHeight / 2, col.center.z);
        }
    }

    void MovementLocked()
    {
        showCompass = false;

        float targetSpeed = stats.walkSpeed * inputDir.magnitude;
        float smoothTime = (targetSpeed < 0.05f) ? stats.decelerationTime : stats.accelerationTime;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        velocity = isGrounded ? inputDir * currentSpeed : velocity;

        anim.SetFloat("Input", inputDir.magnitude, smoothTime, Time.deltaTime);
    }

    void MovementNormal()
    {
        bool running = Input.GetKey(InputManager.instance.sprintKey);
        if (running)
        {
            if (crouching)
            {
                Crouch();
                if (crouching)
                    running = false;
                else
                    showCompass = false;
            }
            else
                showCompass = false;
        }

        float targetSpeed = (running ? stats.runSpeed : stats.walkSpeed) * inputDir.magnitude;
        float moveAmount = (-0.0071f * Vector3.Angle(transform.forward, velocity)) + 1.1429f;
        moveAmount = Mathf.Clamp01(moveAmount);
        float smoothTime = (targetSpeed < 0.05f) ? stats.decelerationTime : stats.accelerationTime;

        if (crouching)
            targetSpeed = stats.crouchSpeed;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed * moveAmount, ref speedSmoothVelocity, smoothTime);

        velocity = isGrounded ? inputDir * currentSpeed : velocity;

        anim.SetFloat("Input", (running || crouching ? 1 : 0.5f) * inputDir.magnitude, smoothTime, Time.deltaTime);
    }

    void ClimbingAndJumping()
    {
        if (inputDir.magnitude > 0 && !lockedMovement)
        {
            bool ledgeAvailable = true;

            LayerMask raycastLayer = ~(1 << 8 | 1 << 9);
            RaycastHit hit;
            float distance = (characterWidth / 2) + 0.05f;
            while (!Physics.Raycast(transform.position + transform.forward * distance + Vector3.up * stats.maxClimbHeight, -Vector3.up,
                out hit, stats.maxClimbHeight - (characterHeight / 4), raycastLayer, QueryTriggerInteraction.Ignore))
            {
                distance += 0.05f;
                if (distance > stats.maxClimbDistance)
                {
                    ledgeAvailable = false;
                    break;
                }
            }

            RaycastHit testHit;
            if (Physics.SphereCast(transform.position, (characterWidth / 2) - 0.05f, Vector3.up, out testHit, stats.maxClimbHeight, raycastLayer, QueryTriggerInteraction.Ignore))
                ledgeAvailable = false;

            Vector3 rayPos = transform.position;
            rayPos.y = hit.point.y + 0.05f;
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;
            if (Physics.Raycast(rayPos, transform.forward, out testHit, direction.magnitude + 0.4f, raycastLayer, QueryTriggerInteraction.Ignore))
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

                if (Physics.Raycast(rayPosition, rayDirection, out hit, stats.maxClimbDistance + 0.01f, raycastLayer, QueryTriggerInteraction.Ignore))
                {
                    climbNormal = hit.normal;
                    climbNormal.y = 0;
                    climbNormal.Normalize();
                }

                Vector3 targetClimbXZ = hit.point - (climbNormal * characterWidth / 2);
                targetClimbingPosition = new Vector3(targetClimbXZ.x, targetClimbY, targetClimbXZ.z);
                targetClimbingRotation = Quaternion.LookRotation(-climbNormal);

                startClimbingPosition = hit.point + (climbNormal * (characterWidth / 2));
                if (targetClimbY - transform.position.y <= stats.shortClimbHeight)
                    startClimbingPosition.y = targetClimbY - shortClimbStartHeightDifference;
                else
                    startClimbingPosition.y = targetClimbY - longClimbStartHeightDifference;

                climbState = ClimbState.SettingPosition;
                return;
            }
        }

        if (isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(2 * 9.82f * stats.jumpHeight);
            velocityY = jumpVelocity;
            isGrounded = false;
            anim.CrossFade("Jump", 0.1f);
        }
    }

    void ClimbMovement()
    {
        velocityY = 0;
        velocity = Vector3.zero;
        rb.isKinematic = true;
        anim.SetFloat("Input", 0);
        anim.SetBool("Grounded", true);
        if (mCamera != null)
            mCamera.useFixedUpdate = false;

        showCompass = false;
        anim.SetBool("Compass", false);

        switch (climbState)
        {
            case ClimbState.None:
                break;
            case ClimbState.SettingPosition:
                if ((startClimbingPosition - transform.position).sqrMagnitude <= Mathf.Pow(stats.climbAdjustSpeed * Time.deltaTime, 2))
                {
                    if (targetClimbingPosition.y - startClimbingPosition.y <= stats.shortClimbHeight)
                    {
                        anim.speed = shortClimbAnimationLength / stats.shortClimbDuration;
                        anim.CrossFade("Climb_Short", 0.15f);
                        climbDuration = stats.shortClimbDuration;
                    }
                    else
                    {
                        anim.speed = longClimbAnimationLength / stats.longClimbDuration;
                        anim.CrossFade("Climb", 0.15f);
                        climbDuration = stats.longClimbDuration;
                    }

                    climbState = ClimbState.Climbing;
                }

                transform.position = Vector3.Lerp(transform.position, startClimbingPosition, stats.climbAdjustSpeed * Time.deltaTime);
                Vector3 lookDirection = targetClimbingPosition - transform.position;
                lookDirection.y = 0;
                lookDirection.Normalize();
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
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

                    climbState = ClimbState.None;

                    return;
                }

                climbTimer += Time.deltaTime;

                float lerpValue = Mathf.InverseLerp(0, climbDuration, climbTimer);
                transform.position = Vector3.Lerp(startClimbingPosition, targetClimbingPosition, lerpValue);
                transform.rotation = targetClimbingRotation;
                break;
        }
    }

    void FixedUpdate()
    {
        if (climbState != ClimbState.None)
            return;

        if (mCamera != null)
            mCamera.useFixedUpdate = true;

        rb.velocity = velocity + (Vector3.up * velocityY);
        if (lockedMovement && mCamera != null)
        {
            Vector3 lookAtPosition = mCamera.transform.position + (mCamera.transform.forward * 100);
            RaycastHit hit;
            if (Physics.Raycast(mCamera.transform.position, mCamera.transform.forward, out hit, 100))
                lookAtPosition = hit.point;

            Vector3 lookDirection = lookAtPosition - transform.position;
            lookDirection.y = 0;
            lookDirection.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.turnSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Quaternion targetRot = (inputDir != Vector3.zero) ? Quaternion.LookRotation(inputDir) : transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.turnSpeed * Time.fixedDeltaTime);
        }
    }

    #region CollisionDetection
    void OnCollisionEnter(Collision collision)
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

    void OnCollisionStay(Collision collision)
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

    void OnCollisionExit(Collision collision)
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
