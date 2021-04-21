using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public Light flashlight;

    public float viewDistance = 30;
    public float flashlightRange = 10;
    public float viewAngle = 45;
    public float detectionTime = 0.5f;
    public float searchTime = 2;

    public LayerMask playerMask;

    public bool playerSpotted { get; private set; }

    float detectionTimer;

    PlayerSneak pSneak;

    public System.Action<Vector3> onDetection;

    private void Start()
    {
        if (flashlight != null)
        {
            flashlight.range = flashlightRange + 0.2f;
            flashlight.spotAngle = viewAngle * 2;
        }

        pSneak = FindObjectOfType<PlayerSneak>();
    }

    private void Update()
    {
        if (CanSeePlayer())
            detectionTimer += Time.deltaTime;
        else
            detectionTimer -= Time.deltaTime;

        detectionTimer = Mathf.Clamp(detectionTimer, 0, detectionTime);

        if (detectionTimer >= detectionTime)
        {
            if (onDetection != null)
                onDetection(pSneak.transform.position);

            playerSpotted = true;
        }
        else
            playerSpotted = false;
    }

    public void AlertToPosition(Vector3 position)
    {
        if (onDetection != null)
            onDetection(position);
    }

    private bool CanSeePlayer()
    {
        if (pSneak == null || Vector3.Angle((pSneak.transform.position - transform.position).normalized, transform.forward) > viewAngle)
            return false;

        float testRange = viewDistance;
        if (!pSneak.IsInLight())
            testRange = flashlightRange;

        RaycastHit hit;
        for (float f = -0.4f; f < 0.4f; f += 0.2f)
        {
            Vector3 targetPosition = pSneak.transform.position - pSneak.transform.right * f;
            Vector3 targetDirection = (targetPosition - transform.position).normalized;
            if (Physics.Raycast(transform.position, targetDirection, out hit, testRange) && hit.transform == pSneak.transform)
                return true;
        }

        return false;
    }
}
