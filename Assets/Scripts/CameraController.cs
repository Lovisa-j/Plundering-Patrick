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

    void Start()
    {
        
    }

    void LateUpdate()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -30, 85);

        transform.eulerAngles = new Vector3(pitch, yaw, 0);
        transform.position = target.transform.position + (transform.right * offset.x) + (transform.up * offset.y) - (transform.forward * offset.z);

        Vector3 targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        target.forwardOverride = targetForward;
        target.rightOverride = transform.right;

        SetTargetItem();

       
    }

    void SetTargetItem()
    {
        target.targetedInteraction = null;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 5 + offset.z, notPlayer))
        {
            Interactable interaction = hit.transform.GetComponent<Interactable>();
            if (interaction == null)
            {
                interaction = hit.transform.parent.GetComponent<Interactable>();
            }
            if (interaction != null)
                target.targetedInteraction = interaction;
        }
    }
}
