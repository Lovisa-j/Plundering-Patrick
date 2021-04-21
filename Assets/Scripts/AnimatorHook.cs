using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorHook : MonoBehaviour
{
    PlayerController controller;

    public System.Action onThrow;

    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

    public void ThrowItem()
    {
        onThrow?.Invoke();
    }
}
