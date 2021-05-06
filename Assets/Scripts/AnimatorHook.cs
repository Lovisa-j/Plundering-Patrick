using UnityEngine;

public class AnimatorHook : MonoBehaviour
{
    public System.Action onThrow;

    public void ThrowItem()
    {
        onThrow?.Invoke();
    }
}
