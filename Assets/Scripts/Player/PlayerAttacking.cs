using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttacking : MonoBehaviour
{
    [Header("Setup")]
    public Transform spineBone;
    public Transform aimTransform;
    public Transform throwOrigin;

    [Header("Shooting")]
    public Gun equippedGun;
    public Transform gunHeldOrigin;
    public Transform gunHolsteredOrigin;

    public int maxAmmoCount;

    [Header("Throwing")]
    public float throwForce;

    bool aiming;

    int aimTargetIterations = 10;
    public int currentAmmoCount { get; private set; }

    float boneIkWeight;

    public Throwable throwable { get; private set; }

    BaseController controller;
    PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<BaseController>();
        controller.aHook.onThrow += ThrowItem;

        currentAmmoCount = maxAmmoCount;
    }

    void Update()
    {
        if (player.inMenu)
            return;

        Throwing();
        AimingAndShooting();
        HandleEquippedGun();
        StealthAttacking();

        controller.anim.SetBool("Aiming", aiming);

        float targetIkWeight = controller.lockedMovement ? 1 : 0;
        boneIkWeight = Mathf.Lerp(boneIkWeight, targetIkWeight, 10 * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (spineBone == null || aimTransform == null)
            return;

        Vector3 targetPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);

        RaycastHit hit;
        if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
            targetPosition = hit.point;

        for (int i = 0; i < aimTargetIterations; i++)
        {
            controller.AimAtTarget(spineBone, aimTransform, controller.GetTargetPosition(aimTransform, targetPosition), boneIkWeight);
        }
    }

    void FixedUpdate()
    {
        if (controller.climbState != BaseController.ClimbState.None)
            return;

        Vector3 lookAtPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);
        RaycastHit hit;
        if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
            lookAtPosition = hit.point;

        controller.FixedTick(lookAtPosition);
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
        if (equippedGun != null && Input.GetKey(InputManager.instance.aimKey))
        {
            aiming = true;

            if (Input.GetKeyDown(InputManager.instance.attackKey) && currentAmmoCount != 0)
            {
                Vector3 targetPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);

                RaycastHit hit;
                if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
                    targetPosition = hit.point;

                if (equippedGun.Shoot(targetPosition))
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
            if (aiming || controller.anim.GetBool("Recoiling"))
            {
                if (gunHeldOrigin != null && equippedGun.transform.parent != gunHeldOrigin)
                {
                    equippedGun.transform.parent = null;
                    equippedGun.transform.localScale = Vector3.one;
                    equippedGun.transform.parent = gunHeldOrigin;
                    equippedGun.transform.localPosition = equippedGun.heldOffset;
                    equippedGun.transform.localEulerAngles = Vector3.zero;
                }

                if (controller.mCamera != null)
                    controller.mCamera.OverrideFieldOfView(equippedGun.aimingFieldOfView);

                controller.lockedMovement = true;
            }
            else
            {
                if (gunHolsteredOrigin != null && equippedGun.transform.parent != gunHolsteredOrigin)
                {
                    equippedGun.transform.parent = null;
                    equippedGun.transform.localScale = Vector3.one;
                    equippedGun.transform.parent = gunHolsteredOrigin;
                    equippedGun.transform.localPosition = equippedGun.holsteredOffset;
                    equippedGun.transform.localEulerAngles = Vector3.zero;
                }

                if (controller.mCamera != null)
                    controller.mCamera.StopOverrideFieldOfView();
            }
        }
    }

    void StealthAttacking()
    {
        if (!aiming && Input.GetKeyDown(InputManager.instance.attackKey) && !controller.lockedMovement)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 3);
            Vector3 direction;
            RaycastHit hit;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].transform.GetComponent<LivingEntity>() || colliders[i].transform.GetComponent<PlayerAttacking>())
                    continue;

                direction = (colliders[i].transform.position - transform.position).normalized;
                direction.y = 0;
                direction.Normalize();

                if (Physics.Raycast(transform.position + (Vector3.up * controller.characterHeight / 2), direction, out hit, 3) && hit.transform == colliders[i].transform
                    && Vector3.Angle(transform.forward, direction) < 45)
                {
                    if (Vector3.Angle(colliders[i].transform.forward, (transform.position - colliders[i].transform.position).normalized) > 90)
                    {
                        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                        colliders[i].transform.GetComponent<LivingEntity>().TakeDamage(int.MaxValue);
                        break;
                    }
                }
            }
        }
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
