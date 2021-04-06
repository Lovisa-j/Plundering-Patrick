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

    private void LateUpdate()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -30, 85);

        float zOffset = offset.z;
        Vector3 rayPosition = target.transform.position + (transform.right * offset.x) + (target.transform.up * offset.y);
        RaycastHit hit;
        if (Physics.SphereCast(rayPosition, 0.4f, -transform.forward, out hit, offset.z, notPlayer, QueryTriggerInteraction.Ignore))
            zOffset = hit.distance;

        transform.position = target.transform.position + (transform.right * offset.x) + (target.transform.up * offset.y) - (transform.forward * zOffset);

        Vector3 targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        target.forwardOverride = targetForward;
        target.rightOverride = transform.right;

        SetTargetItem();
    }

    private void FixedUpdate()
    {
        transform.eulerAngles = new Vector3(pitch, yaw, 0);
    }

    private void SetTargetItem()
    {
        target.targetedInteraction = null;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, target.pickupDistance + offset.z, notPlayer))
        {
            Interactable interaction = hit.transform.GetComponent<Interactable>();
            if (interaction == null)
                interaction = hit.transform.parent.GetComponent<Interactable>();
            if (interaction != null)
                target.targetedInteraction = interaction;
        }
    }
}
