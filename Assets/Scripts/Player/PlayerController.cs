using UnityEngine;

[RequireComponent(typeof(BaseController))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public bool showCompass;
    [HideInInspector] public bool inMenu;

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
    }

    public virtual void Update()
    {
        if (controller.climbState != BaseController.ClimbState.None)
        {
            controller.Tick(0, 0, false);
            return;
        }

        horizontal = InputManager.instance.Horizontal;
        vertical = InputManager.instance.Vertical;

        if (inMenu)
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
        }

        running = Input.GetKey(InputManager.instance.sprintKey);
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
}
