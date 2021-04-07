using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Movement")]
    public string forwardKey;
    public string backKey;
    public string rightKey;
    public string leftKey;
    public string sprintKey;
    public string crouchKey;
    public string jumpClimbKey;

    [Header("Other")]
    public string interactKey;

    public float Vertical { get; private set; }
    public float Horizontal { get; private set; }

    #region Singleton
    public static InputManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }
    #endregion

    void Update()
    {
        Vertical = 0;
        if (Input.GetKey(forwardKey))
            Vertical++;
        if (Input.GetKey(backKey))
            Vertical--;

        Horizontal = 0;
        if (Input.GetKey(rightKey))
            Horizontal++;
        if (Input.GetKey(leftKey))
            Horizontal--;
    }
}
