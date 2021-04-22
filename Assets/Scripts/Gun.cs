using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Setup")]
    public Vector3 heldOffset;
    public Vector3 holsteredOffset;

    [Header("Muzzle")]
    public Transform muzzle;
    public GameObject muzzleFlash;
    public float muzzleFlashDuration;

    [Header("Stats")]
    public int damage;

    public float fireRate;
    public float aimingFieldOfView;
    public float shotSoundDistance;

    float timeToFire;

    public bool Shoot(Vector3 targetPosition)
    {
        if (Time.time < timeToFire)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(muzzle.position, (targetPosition - muzzle.position).normalized, out hit))
        {
            Rigidbody hitRb = hit.transform.GetComponent<Rigidbody>();
            LivingEntity hitEntity = hit.transform.GetComponent<LivingEntity>();
            if (hitEntity != null)
                hitEntity.TakeDamage(damage);
            else if (hitRb != null)
                hitRb.AddForceAtPosition(muzzle.forward * 5, hit.point, ForceMode.Impulse);
        }

        Tools.SoundFromPosition(transform.position, shotSoundDistance);

        if (muzzleFlash != null)
            Destroy(Instantiate(muzzleFlash, muzzle.position, muzzle.rotation), muzzleFlashDuration);

        timeToFire = Time.time + (1f / fireRate);

        return true;
    }
}
