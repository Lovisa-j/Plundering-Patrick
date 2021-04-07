using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController target;
    public LayerMask notPlayer;

    public Vector3 offset;

    public float moveSpeed;
    public float sensitivity;

    float pitch;
    float yaw;

    Transform pivot;

    private void Start()
    {
        pivot = new GameObject("Pivot").transform;
        transform.parent = pivot;
    }

    private void LateUpdate()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -30, 85);

        float zOffset = offset.z;
        Vector3 rayPosition = target.transform.position + (pivot.right * offset.x) + (target.transform.up * offset.y);
        RaycastHit hit;
        if (Physics.SphereCast(rayPosition, 0.4f, -transform.forward, out hit, offset.z, notPlayer, QueryTriggerInteraction.Ignore))
            zOffset = hit.distance;

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        transform.localPosition = new Vector3(offset.x, 0, -zOffset);
        
=======
=======
>>>>>>> parent of 2070f24 (Custom chest model and short climb)
=======
>>>>>>> parent of 2070f24 (Custom chest model and short climb)
        transform.localPosition = new Vector3(offset.x, 0, zOffset);

>>>>>>> parent of 2070f24 (Custom chest model and short climb)
=======
        transform.localPosition = new Vector3(offset.x, 0, zOffset);

>>>>>>> parent of 2070f24 (Custom chest model and short climb)
        Vector3 targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        target.forwardOverride = targetForward;
        target.rightOverride = transform.right;

        SetTargetItem();
    }

    private void FixedUpdate()
    {
        Vector3 targetPos = target.transform.position + (target.transform.up * offset.y);

        if (moveSpeed > 0)
            pivot.position = Vector3.Lerp(pivot.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        else
            pivot.position = targetPos;

        pivot.eulerAngles = new Vector3(pitch, yaw, 0);
    }

    private void SetTargetItem()
    {
        target.targetedInteraction = null;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.2f, transform.forward, out hit, target.pickupDistance + offset.z, notPlayer))
        {
            Interactable interaction = hit.transform.GetComponent<Interactable>();
            if (interaction == null && hit.transform.parent != null)
            {
                Transform testTrans = hit.transform.parent;
                while (!testTrans.GetComponent<Interactable>())
                {
                    if (testTrans.parent == null)
                        break;

                    testTrans = testTrans.parent;
                    if (testTrans == hit.transform.root)
                        break;
                }
                interaction = testTrans.GetComponent<Interactable>();
            }

            if (interaction != null)
                target.targetedInteraction = interaction;
        }
    }
}
