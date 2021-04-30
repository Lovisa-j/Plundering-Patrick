using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorHook : MonoBehaviour
{
    BaseController controller;

    public System.Action onThrow;

    public void Initialize(BaseController controller)
    {
        this.controller = controller;
    }

    public void ThrowItem()
    {
        onThrow?.Invoke();
    }
}
