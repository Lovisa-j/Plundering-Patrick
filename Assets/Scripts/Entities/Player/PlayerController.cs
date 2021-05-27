using UnityEngine;

[RequireComponent(typeof(BaseController))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public bool showCompass;

    protected bool ragdoll;
    bool running;

    int money;
    public int Money
    {
        get
        {
            return money;
        }
        set
        {
            money = value;
        }
    }

    float horizontal;
    float vertical;

    protected BaseController controller;

    public virtual void Start()
    {
        controller = GetComponent<BaseController>();
        controller.onDeath.AddListener(OnPlayerDeath);
    }

    public virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ragdoll = !ragdoll;
            controller.Ragdoll(ragdoll);
        }
        if (ragdoll)
        {
            controller.Tick(0, 0, false);
            return;
        }

        horizontal = InputManager.instance.Horizontal;
        vertical = InputManager.instance.Vertical;

        if (GameManager.instance != null && GameManager.instance.gamePaused)
        {
            horizontal = 0;
            vertical = 0;
            showCompass = false;
        }
        else
        {
            if (Input.GetKeyDown(InputManager.instance.compassKey))
                showCompass = !showCompass;

            if (Input.GetKeyDown(InputManager.instance.crouchKey))
                controller.Crouch();

            if (Input.GetKey(InputManager.instance.jumpClimbKey))
                controller.ClimbingAndJumping();

            if (Input.GetKeyDown(InputManager.instance.interactKey) && !showCompass)
                controller.Interact();

            running = Input.GetKey(InputManager.instance.sprintKey);
        }

        controller.Tick(horizontal, vertical, running);

        if (controller.lockedMovement || controller.climbState != BaseController.ClimbState.None || !controller.isGrounded || (running && !controller.crouching))
            showCompass = false;

        controller.anim.SetBool("Compass", showCompass);
    }

    public virtual void FixedUpdate()
    {
        if (controller.climbState != BaseController.ClimbState.None)
            return;

        Vector3 lookAtPosition = controller.mCamera.transform.position + (controller.mCamera.transform.forward * 100);
        RaycastHit hit;
        if (Physics.Raycast(controller.mCamera.transform.position, controller.mCamera.transform.forward, out hit, 100, ~(1 << 0) | (1 << 0), QueryTriggerInteraction.Ignore))
            lookAtPosition = hit.point;

        controller.FixedTick(lookAtPosition);
    }

    void OnPlayerDeath()
    {
        if (GameManager.instance != null)
            GameManager.instance.hideCursor = false;
    }

    public void SetPosition(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        controller.rb.velocity = Vector3.zero;
        controller.rb.isKinematic = true;
    }
}
