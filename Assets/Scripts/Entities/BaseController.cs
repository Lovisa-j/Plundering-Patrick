using UnityEngine;

[System.Serializable]
public class Bone
{
    public Transform boneTrans;
    public float weight;
}

[RequireComponent(typeof(Rigidbody))]
public class BaseController : LivingEntity
{
    [Header("Setup")]
    public float characterHeight;
    public float characterWidth;
    public float characterCrouchHeight;
    public Transform[] characterLimbs;
    public Rigidbody spineBody;
    
    [Header("Climbing")]
    public float longClimbAnimationLength;
    public float longClimbStartHeightDifference;
    [Space(5)]
    public float shortClimbAnimationLength;
    public float shortClimbStartHeightDifference;

    [Header("Stats")]
    public ControllerStats stats;

    [HideInInspector] public bool lockedMovement;
    [HideInInspector] public bool overrideLockedMovement;

    [HideInInspector] public Interactable targetedInteraction;

    float currentSpeed;
    float speedSmoothVelocity;
    float velocityY;
    float climbTimer;
    float climbDuration;
    float coyoteTimer;

    bool ragdoll;

    public bool crouching { get; private set; }
    public bool isGrounded { get; private set; }

    Vector3 forwardOverride;
    Vector3 rightOverride;
    Vector3 inputDir;
    Vector3 startClimbingPosition;
    Vector3 targetClimbingPosition;
    Quaternion targetClimbingRotation;

    PhysicMaterial[] colliderMaterials;

    Rigidbody[] limbBodies;

    public Vector3 velocity { get; private set; }

    public enum ClimbState { None, SettingPosition, Climbing }
    public ClimbState climbState { get; private set; }

    public CameraController mCamera { get; set; }
    public Rigidbody rb { get; private set; }
    public Animator anim { get; private set; }
    public AnimatorHook aHook { get; private set; }

    public void SetDirectionalOverride(Vector3 forward, Vector3 right) { forwardOverride = forward; rightOverride = right; }

    void Awake()
    {
        forwardOverride = Vector3.forward;
        rightOverride = Vector3.right;

        InitiatePhysicsMaterials();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.angularDrag = 999;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        if (anim != null)
            aHook = anim.gameObject.AddComponent<AnimatorHook>();

        onDeath.AddListener(OnDeath);

        limbBodies = GetComponentsInChildren<Rigidbody>();
        Ragdoll(false);
    }

    // Initiates the physics materials used on this gameobjects colliders.
    void InitiatePhysicsMaterials()
    {
        Collider[] colliders = GetComponents<Collider>();
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>();
        if (colliders != null && colliders.Length > 0)
        {
            colliderMaterials = new PhysicMaterial[colliders.Length];
            PhysicMaterial material = new PhysicMaterial();
            material.dynamicFriction = 0;
            material.staticFriction = 0;
            material.frictionCombine = PhysicMaterialCombine.Minimum;
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].material = material;
                colliderMaterials[i] = colliders[i].material;
            }
        }
    }

    // An update method used by other classes.
    public void Tick(float horizontal, float vertical, bool running)
    {
        if (climbState != ClimbState.None)
        {
            ClimbMovement();
            return;
        }

        if (ragdoll)
        {
            if (spineBody != null)
                rb.position = spineBody.position;

            rb.velocity = Vector3.zero;
            velocity = Vector3.zero;
            velocityY = 0;

            return;
        }

        lockedMovement = anim.GetBool("LockMovement") || overrideLockedMovement;
        
        SetInputDirection(horizontal, vertical);

        rb.isKinematic = false;
        rb.drag = 0;
        for (int i = 0; i < colliderMaterials.Length; i++)
        {
            colliderMaterials[i].frictionCombine = PhysicMaterialCombine.Minimum;
        }

        if (isGrounded)
        {
            velocityY = -1;
            if (inputDir == Vector3.zero)
            {
                if (currentSpeed < 0.05f)
                    rb.drag = 999;

                for (int i = 0; i < colliderMaterials.Length; i++)
                {
                    colliderMaterials[i].frictionCombine = PhysicMaterialCombine.Maximum;
                }
            }

            coyoteTimer = 0;

            StepUp();
        }
        else
        {
            velocityY -= 9.82f * Time.deltaTime;

            coyoteTimer += Time.unscaledDeltaTime;
        }

        if (lockedMovement)
            MovementLocked();
        else
            MovementNormal(running);
    }

    // Sets the inputDir variable based on the horizontal and vertical values as well as the angle of the slope under the player.
    void SetInputDirection(float horizontal, float vertical)
    {
        Vector3 actualForward = forwardOverride;
        Vector3 actualRight = rightOverride;
        RaycastHit hit;
        if (isGrounded && Physics.Raycast(transform.position + (Vector3.up * 0.01f), -Vector3.up, out hit, 0.15f, ~(1 << 8 | 1 << 10), QueryTriggerInteraction.Ignore))
        {
            actualForward.y = Vector3.Cross(hit.normal, -rightOverride).y;
            actualRight.y = Vector3.Cross(hit.normal, forwardOverride).y;
        }

        inputDir = ((actualRight * horizontal) + (actualForward * vertical)).normalized;
    }

    // A fixedUpdate method used by other classes.
    public void FixedTick(Vector3 lookAtPosition)
    {
        if (climbState != ClimbState.None || ragdoll)
            return;

        if (mCamera != null)
            mCamera.useFixedUpdate = true;

        rb.velocity = velocity + (Vector3.up * velocityY);

        if (GameManager.instance != null && GameManager.instance.gamePaused)
            rb.velocity = Vector3.zero;

        Quaternion targetRot = (inputDir != Vector3.zero) ? Quaternion.LookRotation(new Vector3(inputDir.x, 0, inputDir.z)) : transform.rotation;
        if (lockedMovement)
        {
            Vector3 lookDirection = lookAtPosition - transform.position;
            lookDirection.y = 0;
            lookDirection.Normalize();

            targetRot = (lookDirection != Vector3.zero) ? Quaternion.LookRotation(lookDirection, Vector3.up) : Quaternion.identity;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.turnSpeed * Time.fixedDeltaTime);
    }

    public void Crouch()
    {
        crouching = !crouching;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, characterWidth / 2, Vector3.up, out hit, characterHeight + 0.01f, ~(1 << 8), QueryTriggerInteraction.Ignore) && !crouching)
            crouching = true;
        
        anim.SetBool("Crouched", crouching);

        // Change the collider height if the player is crouching or standing.
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

    public void Interact()
    {
        if (targetedInteraction != null)
            targetedInteraction.Interact(GetComponent<Identification>());
    }

    public void ClimbingAndJumping()
    {
        LayerMask raycastLayer = ~(1 << 8 | 1 << 10);

        if (inputDir.magnitude > 0 && !lockedMovement)
        {
            bool ledgeAvailable = true;

            // Raycast from the current position + height in a forward direction.
            RaycastHit hit;
            float height = stats.maxClimbHeight;
            while (!Physics.Raycast(transform.position + (Vector3.up * height), transform.forward,
                out hit, stats.maxClimbDistance, raycastLayer, QueryTriggerInteraction.Ignore))
            {
                height -= 0.05f;
                if (height <= stats.stepUpHeight + 0.2f)
                {
                    ledgeAvailable = false;
                    break;
                }
            }

            // Check if something is above the player.
            if (ledgeAvailable && Physics.SphereCast(transform.position, (characterWidth / 2) - 0.05f, Vector3.up, out _, characterHeight * 1.25f, raycastLayer, QueryTriggerInteraction.Ignore))
                ledgeAvailable = false;

            // Get the normal for the ledge and check if the angle is within limits.
            Vector3 climbNormal = hit.normal;
            float angle = Vector3.Angle(climbNormal, Vector3.up);
            if (ledgeAvailable && (angle < 60 || angle > 95))
                ledgeAvailable = false;

            climbNormal.y = 0;
            climbNormal.Normalize();

            // Check if there is space on the ledge.
            Vector3 rayPos = hit.point + (Vector3.up * 0.05f) + (climbNormal * 0.01f);
            if (ledgeAvailable && Physics.Raycast(rayPos, -hit.normal, out _, 0.05f + characterWidth, raycastLayer, QueryTriggerInteraction.Ignore))
                ledgeAvailable = false;

            // Set the target position Y-value by raycasting down from the hit point.
            Vector3 targetClimbXZ = hit.point - (climbNormal * characterWidth / 2);
            float targetClimbY = 0;
            
            RaycastHit downHit;
            rayPos = hit.point - (hit.normal * 0.01f) + (Vector3.up * 0.05f);
            if (ledgeAvailable && Physics.Raycast(rayPos, -Vector3.up, out downHit, 0.051f, raycastLayer, QueryTriggerInteraction.Ignore))
                targetClimbY = downHit.point.y + 0.01f;
            else
                ledgeAvailable = false;

            // Check if the character can stand on the target position, if not: check if they can crouch.
            rayPos = new Vector3(targetClimbXZ.x, targetClimbY, targetClimbXZ.z);
            if (ledgeAvailable && Physics.SphereCast(rayPos, (characterWidth / 2) - 0.05f, Vector3.up, out downHit, characterHeight, raycastLayer, QueryTriggerInteraction.Ignore))
            {
                if (downHit.distance < characterHeight && downHit.distance > characterCrouchHeight && !crouching)
                    Crouch();
                else if (downHit.distance < characterCrouchHeight)
                    ledgeAvailable = false;
            }

            if (ledgeAvailable)
            {
                targetClimbingPosition = rayPos;
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

        // Jumping.
        if (isGrounded || coyoteTimer <= stats.coyoteTime)
        {
            Vector3 temp = velocity;
            temp.y = 0;
            temp = inputDir * currentSpeed;
            velocity = temp;

            float jumpVelocity = Mathf.Sqrt(2 * 9.82f * stats.jumpHeight);

            RaycastHit hit;
            if (Physics.SphereCast(transform.position, characterWidth / 2.25f, Vector3.up, out hit, characterHeight + (jumpVelocity * Time.fixedDeltaTime), raycastLayer, QueryTriggerInteraction.Ignore))
                return;
            
            velocityY = jumpVelocity;
            
            isGrounded = false;
            coyoteTimer = stats.coyoteTime;

            anim.CrossFade("Jump", 0.1f);
        }
    }

    // Movement behaviour when using locked rotation.
    void MovementLocked()
    {
        float targetSpeed = stats.walkSpeed * inputDir.magnitude;
        if (crouching)
            targetSpeed = stats.crouchSpeed * inputDir.magnitude;

        float smoothTime = (targetSpeed < 0.05f) ? stats.decelerationTime : stats.accelerationTime;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        velocity = isGrounded ? inputDir * currentSpeed : velocity;

        Vector3 localVelocity = transform.InverseTransformDirection(velocity.normalized).normalized;
        anim.SetFloat("Horizontal", localVelocity.x, smoothTime, Time.deltaTime);
        anim.SetFloat("Vertical", localVelocity.z, smoothTime, Time.deltaTime);
    }

    // Movement behaviour when using standard rotation.
    void MovementNormal(bool running)
    {
        if (running && crouching)
        {
            Crouch();
            if (crouching)
                running = false;
        }

        float targetSpeed = (running ? stats.runSpeed : stats.walkSpeed) * inputDir.magnitude;
        float moveAmount = (-0.0071f * Vector3.Angle(transform.forward, inputDir)) + 1.1429f;
        moveAmount = Mathf.Clamp01(moveAmount);

        if (crouching)
            targetSpeed = stats.crouchSpeed * inputDir.magnitude;

        float smoothTime = (targetSpeed < 0.05f) ? stats.decelerationTime : stats.accelerationTime;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed * moveAmount, ref speedSmoothVelocity, smoothTime);

        velocity = isGrounded ? inputDir * currentSpeed : velocity;

        anim.SetFloat("Horizontal", 0, smoothTime, Time.deltaTime);
        anim.SetFloat("Vertical", (running ? 2 : 1f) * inputDir.magnitude, smoothTime, Time.deltaTime);
    }

    // Movement behaviour when climbing.
    void ClimbMovement()
    {
        inputDir = Vector3.zero;
        velocityY = 0;
        velocity = Vector3.zero;
        rb.isKinematic = true;

        anim.SetBool("Grounded", true);

        if (mCamera != null)
            mCamera.useFixedUpdate = false;

        switch (climbState)
        {
            case ClimbState.None:
                break;
            // Adjusting to the start position of the climb.
            case ClimbState.SettingPosition:
                anim.SetFloat("Horizontal", 0);
                anim.SetFloat("Vertical", 0);

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
            // Performing the climbing movement.
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

    // Move up small ledges.
    void StepUp()
    {
        if (inputDir.magnitude < 0.05f)
            return;

        bool step = true;

        LayerMask raycastLayer = ~(1 << 8);
        RaycastHit hit;
        float height = stats.stepUpHeight;
        while (!Physics.Raycast(transform.position + (Vector3.up * height), inputDir, out hit, (characterWidth / 2) + stats.stepUpDistance, raycastLayer, QueryTriggerInteraction.Ignore))
        {
            height -= 0.04f;

            if (height <= 0.04f)
            {
                step = false;
                break;
            }
        }

        if (step)
        {
            if (Vector3.Angle(Vector3.up, hit.normal) < 75)
                return;

            Vector3 startPosition = hit.point + (Vector3.up * 0.04f) - (hit.normal * 0.05f);

            if (Physics.Raycast(startPosition, -Vector3.up, out hit, 0.041f, raycastLayer, QueryTriggerInteraction.Ignore))
            {
                climbDuration = stats.stepUpDuration;
                startClimbingPosition = transform.position;
                targetClimbingPosition = hit.point;
                targetClimbingRotation = transform.rotation;

                climbState = ClimbState.Climbing;
            }
        }
    }

    public Vector3 GetTargetPosition(Transform aimTransform, Vector3 originalTargetPosition)
    {
        Vector3 targetDirection = originalTargetPosition - aimTransform.position;
        Vector3 aimDirection = aimTransform.forward;
        float blendOut = 0.0f;

        float targetAngle = Vector3.Angle(targetDirection, aimDirection);
        if (targetAngle > 90f)
            blendOut += (targetAngle - 90f) / 50f;

        float targetDistance = targetDirection.magnitude;
        if (targetDistance < 0.75f)
            blendOut += 0.75f - targetDistance;

        Vector3 direction = Vector3.Slerp(targetDirection, aimDirection, blendOut);
        return aimTransform.position + direction;
    }

    // Rotate a bone to look in the direction of the targetPosition.
    public void AimAtTarget(Transform bone, Transform aimTransform, Vector3 targetPosition, float weight)
    {
        Vector3 aimDirection = aimTransform.forward;
        Vector3 targetDirection = targetPosition - aimTransform.position;
        Quaternion targetRotation = Quaternion.FromToRotation(aimDirection, targetDirection.normalized);
        Quaternion weightedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, weight);
        bone.rotation = weightedRotation * bone.rotation;
    }

    public void Ragdoll(bool enable)
    {
        for (int i = 0; i < limbBodies.Length; i++)
        {
            limbBodies[i].isKinematic = !enable;
            limbBodies[i].GetComponent<Collider>().enabled = enable;
            if (enable)
                limbBodies[i].velocity = rb.velocity;
        }

        rb.isKinematic = enable;
        GetComponent<Collider>().enabled = !enable;
        enabled = !enable;
        anim.enabled = !enable;

        ragdoll = enable;
    }

    void OnDeath()
    {
        Ragdoll(true);
    }

    #region CollisionDetection
    void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(-Vector3.up, collision.GetContact(i).normal) < 35 && velocityY > 0)
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
