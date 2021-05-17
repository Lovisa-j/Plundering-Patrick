using UnityEngine;

public class CrossSceneMethods : MonoBehaviour
{
    public Vector3 playerStartPosition;

    void Start()
    {
        if (FindObjectOfType<PlayerController>())
            FindObjectOfType<PlayerController>().SetPosition(playerStartPosition);
        if (FindObjectOfType<PlayerAttacking>())
        {
            if (FindObjectOfType<PlayerAttacking>().throwable != null)
            {
                Destroy(FindObjectOfType<PlayerAttacking>().throwable.gameObject);
                FindObjectOfType<PlayerAttacking>().throwable = null;
            }
        }
    }

    public void GameManagerFinishLevel()
    {
        if (GameManager.instance == null)
            return;

        GameManager.instance.FinishLevel();
    }
}
