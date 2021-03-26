using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerSneak : MonoBehaviour
{
    public float walkSoundDistance;
    public float runSoundDistance;

    public float soundDistance { get; private set; }
    public bool inLight { get; private set; }

    public LayerMask playerLayer;

    PlayerController controller;

    void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        IsInLight();

        if (controller.currentSpeed > controller.walkSpeed)
            soundDistance = runSoundDistance;
        else if (controller.currentSpeed > 0)
            soundDistance = walkSoundDistance;
        else
            soundDistance = 0;

        Collider[] colliders = Physics.OverlapSphere(transform.position, soundDistance);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetComponent<EnemyDetection>())
                colliders[i].GetComponent<EnemyDetection>().AlertToPosition(transform.position);
        }
    }

    void IsInLight()
    {
        inLight = false;
        Light[] sceneLights = FindObjectsOfType<Light>();
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i].type == LightType.Directional || sceneLights[i].transform.parent.GetComponent<EnemyDetection>() || !sceneLights[i].gameObject.activeInHierarchy)
                continue;

            bool inRange = false;
            Collider[] colliders = Physics.OverlapSphere(sceneLights[i].transform.position, sceneLights[i].range, playerLayer);
            for (int c = 0; c < colliders.Length; c++)
            {
                if (colliders[c].transform == transform)
                {
                    inRange = true;
                    break;
                }
            }

            if (inRange)
            {
                for (float f = -1; f <= 1; f += 0.2f)
                {
                    Vector3 targetPosition = transform.position - transform.right * f;
                    Vector3 lightDirection = (targetPosition - sceneLights[i].transform.position).normalized;
                    RaycastHit hit;
                    if (Physics.Raycast(sceneLights[i].transform.position, lightDirection, out hit, sceneLights[i].range) && hit.transform == transform)
                    {
                        if (sceneLights[i].type == LightType.Spot)
                        {
                            if (Vector3.Angle(lightDirection, sceneLights[i].transform.forward) < sceneLights[i].spotAngle / 2)
                                inLight = true;
                        }
                        else
                            inLight = true;

                        break;
                    }
                }
            }
        }
    }
}
