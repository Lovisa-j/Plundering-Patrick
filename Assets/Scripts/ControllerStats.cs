using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Stats/Character Stats")]
public class ControllerStats : ScriptableObject
{
    [Header("Setup")]
    public float pickupDistance;

    [Header("Movement")]
    public float walkSpeed;
    public float runSpeed;
    public float crouchSpeed;
    public float turnSpeed;
    public float jumpHeight;
    public float accelerationTime;
    public float decelerationTime;

    [Header("Climbing")]
    public float maxClimbDistance;
    public float climbAdjustSpeed;
    [Space(10)]
    public float maxClimbHeight;
    public float longClimbDuration;
    [Space(10)]
    public float shortClimbHeight;
    public float shortClimbDuration;

}
