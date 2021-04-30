using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public BaseController target;
    public LayerMask notPlayer;

    public Vector2 minMaxPitch = new Vector2(-45, 85);

    public Vector3 normalOffset;
    public Vector3 crouchOffset;

    public float moveSpeed;
    public float fieldOfView;
    public float sensitivity;

    public bool hideCursor;

    [HideInInspector] public bool useFixedUpdate = true;

    Vector3 offsetToUse;

    bool overrideFieldOfView;

    float overrideFOVValue;

    public float pitch { get; private set; }
    public float yaw { get; private set; }

    Transform pivot;

    void Start()
    {
        pivot = new GameObject("Pivot").transform;
        transform.parent = pivot;

        target.mCamera = this;
    }

    void LateUpdate()
    {
        Camera thisCamera = GetComponent<Camera>();
        if (thisCamera != null)
        {
            if (overrideFieldOfView)
                thisCamera.fieldOfView = Mathf.Lerp(thisCamera.fieldOfView, overrideFOVValue, moveSpeed * Time.deltaTime);
            else
                thisCamera.fieldOfView = Mathf.Lerp(thisCamera.fieldOfView, fieldOfView, moveSpeed * Time.deltaTime);

        }

        if (hideCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minMaxPitch.x, minMaxPitch.y);

        if (target.crouching)
            offsetToUse = crouchOffset;
        else
            offsetToUse = normalOffset;

        if (!useFixedUpdate)
            UpdatePosition(false);

        float zOffset = offsetToUse.z;
        Vector3 rayPosition = target.transform.position + (pivot.right * offsetToUse.x) + (target.transform.up * offsetToUse.y);
        RaycastHit hit;
        if (Physics.SphereCast(rayPosition, 0.4f, -transform.forward, out hit, offsetToUse.z, notPlayer, QueryTriggerInteraction.Ignore))
            zOffset = hit.distance;

        float localPosX = Mathf.Lerp(transform.localPosition.x, offsetToUse.x, moveSpeed * Time.deltaTime);
        float localPosZ = Mathf.Lerp(transform.localPosition.z, -zOffset, moveSpeed * 2 * Time.deltaTime);
        transform.localPosition = new Vector3(localPosX, 0, localPosZ);

        Vector3 targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        target.SetDirectionalOverride(targetForward, transform.right);

        SetTargetItem();
    }

    void FixedUpdate()
    {
        if (useFixedUpdate)
            UpdatePosition(true);
    }

    void UpdatePosition(bool fixedDelta)
    {
        float delta = fixedDelta ? Time.fixedDeltaTime : Time.deltaTime;

        Vector3 targetPos = target.transform.position + (target.transform.up * offsetToUse.y);

        if (moveSpeed > 0)
            pivot.position = Vector3.Lerp(pivot.position, targetPos, moveSpeed * delta);
        else
            pivot.position = targetPos;

        pivot.eulerAngles = new Vector3(pitch, yaw, 0);
    }

    void SetTargetItem()
    {
        target.targetedInteraction = null;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.05f, transform.forward, out hit, target.stats.pickupDistance + normalOffset.z, notPlayer))
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

    public void OverrideFieldOfView(float value)
    {
        overrideFOVValue = value;
        overrideFieldOfView = true;
    }

    public void StopOverrideFieldOfView()
    {
        overrideFieldOfView = false;
    }
}
