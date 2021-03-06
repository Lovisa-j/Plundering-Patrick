using UnityEngine;

public class PlayerAttacking : PlayerController
{
    [Header("Setup")]
    public Bone[] spineBones;
    public Transform aimTransform;
    public Transform throwOrigin;

    [Header("Swinging")]
    public Weapon equippedWeapon;
    public int animationLayer;

    [Header("Shooting")]
    public Gun equippedGun;
    public Transform gunHeldOrigin;
    public Transform gunHolsteredOrigin;

    public int maxAmmoCount;

    [Header("Throwing")]
    public float throwForce;

    bool aiming;
    bool attacking;

    int aimTargetIterations = 10;
    public int currentAmmoCount { get; private set; }

    float boneIkWeight;
    float parryTimer;

    BlockState blockState;
    enum BlockState { Blocking, Parrying, None }

    public Throwable throwable { get; set; }

    public override void Start()
    {
        base.Start();

        controller.aHook.onThrow += ThrowItem;
        controller.aHook.onUpdateDamageCollider += UpdateWeaponCollider;
        controller.onTakeHit += OnTakeHit;

        currentAmmoCount = maxAmmoCount;
    }

    public override void Update()
    {
        base.Update();

        if (ragdoll)
            return;

        if (GameManager.instance != null && GameManager.instance.gamePaused)
            return;

        if (controller.climbState != BaseController.ClimbState.None)
        {
            if (equippedWeapon != null)
                equippedWeapon.gameObject.SetActive(false);
            boneIkWeight = 0;
            return;
        }

        MeleeAttacking();
        Blocking();
        if (!attacking)
        {
            Throwing();
            AimingAndShooting();
        }
        HandleEquippedGun();

        controller.anim.SetBool("Aiming", aiming);

        float targetIkWeight = controller.lockedMovement ? 1 : 0;
        boneIkWeight = Mathf.Lerp(boneIkWeight, targetIkWeight, 10 * Time.deltaTime);
    }

    public override void FixedUpdate()
    {
        if (controller.climbState != BaseController.ClimbState.None)
            return;

        controller.FixedTick(GetLookAtPosition());
    }

    void LateUpdate()
    {
        if (aimTransform == null)
            return;

        for (int i = 0; i < spineBones.Length; i++)
        {
            if (spineBones[i].boneTrans == null)
                continue;

            for (int c = 0; c < aimTargetIterations; c++)
            {
                controller.AimAtTarget(spineBones[i].boneTrans, aimTransform, controller.GetTargetPosition(aimTransform, GetLookAtPosition()), boneIkWeight * spineBones[i].weight);
            }
        }
    }

    Vector3 GetLookAtPosition()
    {
        Vector3 lookAtPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);
        if (aiming || controller.anim.GetCurrentAnimatorStateInfo(animationLayer).IsName("Throw"))
        {
            RaycastHit hit;
            if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100, ~(1 << 0) | (1 << 0), QueryTriggerInteraction.Ignore))
                lookAtPosition = hit.point;
        }

        return lookAtPosition;
    }

    void MeleeAttacking()
    {
        if (blockState != BlockState.None)
            return;

        if (!aiming && throwable == null && Input.GetKeyDown(InputManager.instance.attackKey) && !controller.lockedMovement)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 2);
            Vector3 direction;
            RaycastHit hit;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].transform.GetComponent<LivingEntity>() || colliders[i].transform.GetComponent<PlayerAttacking>())
                    continue;

                direction = (colliders[i].transform.position - transform.position).normalized;
                direction.y = 0;
                direction.Normalize();

                if (Physics.Raycast(transform.position + (Vector3.up * controller.characterHeight / 2), direction, out hit, 2) && hit.transform == colliders[i].transform
                    && Vector3.Angle(transform.forward, direction) < 45 && Vector3.Angle(colliders[i].transform.forward, (transform.position - colliders[i].transform.position).normalized) > 90)
                {
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    colliders[i].transform.GetComponent<LivingEntity>().TakeDamage(int.MaxValue, transform, hit.point);
                    return;
                }
            }
        }

        if (equippedWeapon != null)
        {
            if (throwable != null || controller.anim.GetCurrentAnimatorStateInfo(animationLayer).IsName("Throw"))
            {
                equippedWeapon.gameObject.SetActive(false);
                return;
            }

            equippedWeapon.gameObject.SetActive(true);

            if (!aiming && Input.GetKeyDown(InputManager.instance.attackKey))
                equippedWeapon.Attack(transform);

            equippedWeapon.Tick();

            if (equippedWeapon.currentAttack != null && !attacking)
            {
                controller.anim.CrossFade(equippedWeapon.currentAttack.attackString, equippedWeapon.currentAttack.transitionDuration, animationLayer, equippedWeapon.currentAttack.startTime);
                attacking = true;
                controller.overrideLockedMovement = true;
            }
        }
    }

    void Blocking()
    {
        if (equippedWeapon != null && Input.GetKey(InputManager.instance.blockKey) && !attacking && throwable == null && !aiming)
        {
            controller.anim.SetBool("Blocking", true);
            controller.lockedMovement = true;

            if (parryTimer >= equippedWeapon.parryDuration)
                blockState = BlockState.Blocking;
            else
                blockState = BlockState.Parrying;

            parryTimer += Time.deltaTime;
        }
        else
        {
            blockState = BlockState.None;
            parryTimer = 0;

            controller.anim.SetBool("Blocking", false);
        }
    }

    void Throwing()
    {
        if (throwable != null && !controller.lockedMovement && !aiming)
        {
            if (Input.GetKeyDown(InputManager.instance.attackKey))
            {
                controller.anim.CrossFade("Throw", 0.1f);
                controller.lockedMovement = true;
            }
            else if (Input.GetKeyDown(InputManager.instance.dropKey))
                DropThrowable();
        }
    }

    void AimingAndShooting()
    {
        aiming = false;
        if (equippedGun != null && Input.GetKey(InputManager.instance.aimKey) && blockState == BlockState.None)
        {
            aiming = true;

            if (Input.GetKeyDown(InputManager.instance.attackKey) && currentAmmoCount != 0 &&
                controller.anim.GetCurrentAnimatorStateInfo(animationLayer).IsName("Aiming") ||
                controller.anim.GetCurrentAnimatorStateInfo(animationLayer).IsName("Recoil"))
            {
                Vector3 targetPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);

                RaycastHit hit;
                if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
                    targetPosition = hit.point;

                if (equippedGun.Shoot(targetPosition, transform))
                {
                    controller.anim.SetBool("Recoiling", true);
                    currentAmmoCount--;
                }
            }
        }
    }

    void HandleEquippedGun()
    {
        if (equippedGun != null)
        {
            if ((aiming || controller.anim.GetBool("Recoiling")) && !attacking)
            {
                if (gunHeldOrigin != null && equippedGun.transform.parent != gunHeldOrigin)
                    ParentAndOffsetGun(false);

                if (controller.mCamera != null)
                    controller.mCamera.OverrideFieldOfView(equippedGun.aimingFieldOfView);

                controller.lockedMovement = true;
            }
            else
            {
                if (gunHolsteredOrigin != null && equippedGun.transform.parent != gunHolsteredOrigin)
                    ParentAndOffsetGun(true);

                if (controller.mCamera != null)
                    controller.mCamera.StopOverrideFieldOfView();
            }
        }
    }

    void ParentAndOffsetGun(bool holstered)
    {
        equippedGun.transform.parent = null;
        equippedGun.transform.localScale = Vector3.one;
        equippedGun.transform.parent = holstered ? gunHolsteredOrigin : gunHeldOrigin;
        equippedGun.transform.localPosition = holstered ? equippedGun.holsteredOffset : equippedGun.heldOffset;
        equippedGun.transform.localEulerAngles = Vector3.zero;
    }

    void UpdateWeaponCollider(bool value)
    {
        if (equippedWeapon == null)
            return;

        equippedWeapon.colliderStatus = value;

        if (!value)
        {
            equippedWeapon.FinishAttack();
            attacking = false;
            controller.overrideLockedMovement = false;
        }
    }

    void OnTakeHit(int damageTaken, Transform hittingTransform)
    {
        if (equippedWeapon == null)
            return;

        Vector3 direction = hittingTransform.position - transform.position;
        direction.y = 0;
        direction.Normalize();

        if (Vector3.Angle(aimTransform.forward, direction) > equippedWeapon.blockAngle / 2)
            return;

        if (blockState == BlockState.Blocking)
            controller.Heal(Mathf.RoundToInt(damageTaken * equippedWeapon.blockPercent), true);
        else if (blockState == BlockState.Parrying)
            controller.Heal(damageTaken, true);
    }

    #region Throwable
    public void PickUpThrowable(Throwable throwableItem)
    {
        DropThrowable();

        throwableItem.transform.parent = throwOrigin;
        throwableItem.transform.localPosition = throwableItem.heldOffsetPosition;
        throwableItem.transform.localEulerAngles = throwableItem.heldOffsetRotation;

        throwable = throwableItem;

        throwable.GetComponent<Rigidbody>().isKinematic = true;
        Collider col = throwable.GetComponent<Collider>();
        if (col == null)
            col = throwable.GetComponentInChildren<Collider>();

        col.isTrigger = true;
    }

    void DropThrowable()
    {
        if (throwable == null)
            return;

        throwable.transform.parent = null;
        throwable.transform.position = transform.position + (transform.forward * controller.characterWidth) + (Vector3.up * (controller.characterHeight / 2));
        throwable.transform.eulerAngles = Vector3.zero;
        throwable.GetComponent<Rigidbody>().isKinematic = false;

        Collider col = throwable.GetComponent<Collider>();
        if (col == null)
            col = throwable.GetComponentInChildren<Collider>();

        col.isTrigger = false;
        throwable = null;
    }

    void ThrowItem()
    {
        if (throwable == null)
            return;

        Vector3 targetPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);

        RaycastHit hit;
        if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
            targetPosition = hit.point;

        Vector3 targetDirection = (targetPosition - throwOrigin.position).normalized;

        throwable.Throw(transform, targetDirection * throwForce);
        throwable = null;
    }
    #endregion
}
