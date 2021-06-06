using UnityEngine;

public class Cart : MonoBehaviour
{
    [SerializeField] Animator animController;

    private void OnTriggerEnter(Collider other)
    {
        animController.SetBool("fallen", true);
    }

    private void OnTriggerExit(Collider other)
    {
        animController.SetBool("fallen", false);
    }
}
