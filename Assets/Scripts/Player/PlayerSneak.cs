using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerSneak : MonoBehaviour
{
    public float walkSoundDistance;
    public float runSoundDistance;

    public float soundDistance { get; private set; }

    public LayerMask playerLayer;

    PlayerController controller;

    private void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (controller.velocity.magnitude > controller.stats.walkSpeed && !controller.crouching && !controller.isGrounded)
            Tools.SoundFromPosition(transform.position, runSoundDistance);
        else if (controller.velocity.magnitude > 0 && !controller.crouching && !controller.isGrounded)
            Tools.SoundFromPosition(transform.position, walkSoundDistance);
    }

    public bool IsInLight()
    {
        bool toReturn = false;
        
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
                Vector3 direction;
                RaycastHit hit;
                for (int c = 0; c < controller.characterLimbs.Length; c++)
                {
                    direction = (controller.characterLimbs[c].position - sceneLights[i].transform.position).normalized;
                    if (Physics.Raycast(sceneLights[i].transform.position, direction, out hit, sceneLights[i].range, ~(1 << 0) | (1 << 0), QueryTriggerInteraction.Collide))
                    {
                        Transform testTrans = hit.transform;
                        while (!testTrans.GetComponent<PlayerSneak>())
                        {
                            if (testTrans.parent == null)
                                break;

                            testTrans = testTrans.parent;
                            if (testTrans == hit.transform.root)
                                break;
                        }

                        if (testTrans.GetComponent<PlayerSneak>())
                        {
                            if (sceneLights[i].type == LightType.Spot)
                            {
                                if (Vector3.Angle(direction, sceneLights[i].transform.forward) < sceneLights[i].spotAngle / 2)
                                    toReturn = true;
                            }
                            else
                                toReturn = true;
                        }
                    }
                }
            }
        }

        return toReturn;
    }
}
