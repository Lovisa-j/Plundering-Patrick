using UnityEngine;

[RequireComponent(typeof(BaseController))]
public class PlayerSneak : MonoBehaviour
{
    public float walkSoundDistance;
    public float runSoundDistance;
    public float minimumLightIntensity = 0.5f;

    public LayerMask playerLayer;
    public LayerMask lightCollisionLayer;

    BaseController controller;

    void Start()
    {
        controller = GetComponent<BaseController>();
    }

    void Update()
    {
        if (controller.velocity.magnitude > controller.stats.walkSpeed + 0.25f && !controller.crouching && controller.isGrounded)
            Tools.SoundFromPosition(transform.position, runSoundDistance);
        else if (controller.velocity.magnitude > 0 && !controller.crouching && controller.isGrounded)
            Tools.SoundFromPosition(transform.position, walkSoundDistance);
    }

    public bool IsInLight()
    {
        bool toReturn = false;
        
        Light[] sceneLights = FindObjectsOfType<Light>();
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i].transform.parent.GetComponent<EnemyAi>() || !sceneLights[i].gameObject.activeInHierarchy || sceneLights[i].intensity < minimumLightIntensity)
                continue;

            bool inRange = (sceneLights[i].type == LightType.Directional) ? true : false;
            if (!inRange)
            {
                Collider[] colliders = Physics.OverlapSphere(sceneLights[i].transform.position, sceneLights[i].range, playerLayer);
                for (int c = 0; c < colliders.Length; c++)
                {
                    if (colliders[c].transform == transform)
                    {
                        inRange = true;
                        break;
                    }
                }
            }

            if (inRange)
            {
                Vector3 startPosition;
                Vector3 direction;
                float range;

                RaycastHit hit;
                for (int c = 0; c < controller.characterLimbs.Length; c++)
                {
                    startPosition = sceneLights[i].transform.position;
                    direction = (controller.characterLimbs[c].position - sceneLights[i].transform.position).normalized;
                    range = sceneLights[i].range;
                    if (sceneLights[i].type == LightType.Directional)
                    {
                        startPosition = controller.characterLimbs[c].position - (sceneLights[i].transform.forward * 1000);
                        direction = sceneLights[i].transform.forward;
                        range = 1500;
                    }

                    if (Physics.Raycast(startPosition, direction, out hit, range, lightCollisionLayer, QueryTriggerInteraction.Collide))
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
