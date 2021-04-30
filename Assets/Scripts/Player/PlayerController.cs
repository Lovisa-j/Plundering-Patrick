using System.Collections;
using System.Collections.Generic;
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

    BaseController controller;

    void Start()
    {
        controller = GetComponent<BaseController>();
    }

    void Update()
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
}
