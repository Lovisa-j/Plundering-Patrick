using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Movement")]
    public string forwardKey = "w";
    public string backKey = "s";
    public string rightKey = "d";
    public string leftKey = "a";
    public string sprintKey = "left shift";
    public string crouchKey = "left ctrl";
    public string jumpClimbKey = "space";

    [Header("Other")]
    public string pauseKey = "escape";
    public string interactKey = "e";
    public string compassKey = "c";
    public string attackKey = "mouse 0";
    public string aimKey = "mouse 1";
    public string dropKey = "t";

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
