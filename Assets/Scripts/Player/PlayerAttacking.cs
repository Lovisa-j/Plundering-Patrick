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

    PlayerController controller;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        controller.aHook.onThrow += ThrowItem;

        currentAmmoCount = maxAmmoCount;
    }

    void Update()
    {
        if (controller.inMenu)
            return;

        Throwing();
        AimingAndShooting();
        HandleEquippedGun();

        controller.anim.SetBool("Aiming", aiming);

        float targetIkWeight = controller.lockedMovement ? 1 : 0;
        boneIkWeight = Mathf.Lerp(boneIkWeight, targetIkWeight, 10 * Time.deltaTime);
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

    void LateUpdate()
    {
        if (spineBone == null || aimTransform == null)
            return;

        Vector3 targetPosition = GetTargetPosition();
        for (int i = 0; i < aimTargetIterations; i++)
        {
            AimAtTarget(spineBone, targetPosition, boneIkWeight);
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

    Vector3 GetTargetPosition()
    {
        Vector3 targetPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);

        RaycastHit hit;
        if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100))
            targetPosition = hit.point;

        Vector3 targetDirection = targetPosition - aimTransform.position;
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

    void AimAtTarget(Transform bone, Vector3 targetPosition, float weight)
    {
        Vector3 aimDirection = aimTransform.forward;
        Vector3 targetDirection = targetPosition - aimTransform.position;
        Quaternion targetRotation = Quaternion.FromToRotation(aimDirection, targetDirection.normalized);
        Quaternion weightedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, weight);
        bone.rotation = weightedRotation * bone.rotation;
    }
}
