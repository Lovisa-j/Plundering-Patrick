using UnityEngine;

public class AnimatorHook : MonoBehaviour
{
    public System.Action onThrow;
    public System.Action<bool> onUpdateDamageCollider;

    public void ThrowItem()
    {
        onThrow?.Invoke();
    }

    public void UpdateDamageCollider(int value)
    {
        onUpdateDamageCollider?.Invoke((value > 0) ? true : false);
    }
}
