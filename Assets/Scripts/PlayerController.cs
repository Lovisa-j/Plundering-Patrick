using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public int money;

    public float walkSpeed;
    public float runSpeed;
    public float turnSpeed;
    public float jumpVelocity;

    [HideInInspector] public Interactable targetedInteraction;

    [HideInInspector] public Vector3 forwardOverride;
    [HideInInspector] public Vector3 rightOverride;

    float horizontal;
    float vertical;
    float velocityY;
    public float currentSpeed { get; private set; }

    bool isGrounded;

    Vector3 inputDir;

    Rigidbody rb;

    public Vector3 rbVelocity
    {
        get
        {
            return rb.velocity;
        }
    }

    void Start()
    {
        forwardOverride = Vector3.forward;
        rightOverride = Vector3.right;

        rb = GetComponent<Rigidbody>();
        rb.angularDrag = 999;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        inputDir = ((rightOverride * horizontal) + (forwardOverride * vertical)).normalized;

        bool running = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = (running ? runSpeed : walkSpeed) * inputDir.magnitude;

        if (inputDir == Vector3.zero)
            rb.drag = 10;
        else
            rb.drag = 0;

        if (Input.GetKeyDown(KeyCode.E) && targetedInteraction != null)
            targetedInteraction.Interact(transform);

        if (isGrounded)
        {
            velocityY = -1;
            if (Input.GetKey(KeyCode.Space))
            {
                velocityY = jumpVelocity;
                isGrounded = false;
            }
        }
        else
            velocityY -= 9.82f * Time.deltaTime;
    }

    void FixedUpdate()
    {
        rb.velocity = inputDir * currentSpeed;
        rb.velocity += Vector3.up * velocityY;
        Quaternion targetRot = (inputDir != Vector3.zero) ? Quaternion.LookRotation(inputDir) : transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(Vector3.up, collision.GetContact(i).normal) < 45)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Angle(Vector3.up, collision.GetContact(i).normal) < 45)
            {
                isGrounded = true;
                break;
            }
        }
    }
}
