using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public bool gamePaused;
    public bool hideCursor = true;

    public System.Action onLevelFinish;

    public static GameManager instance;

    void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
                Destroy(gameObject);
        }
        else
            instance = this;

        UnpauseGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(InputManager.instance.pauseKey))
            Pausing();

        if (hideCursor && !gamePaused)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Pausing()
    {
        gamePaused = !gamePaused;
        Time.timeScale = gamePaused ? 0 : 1;
    }

    public void PauseGame()
    {
        gamePaused = true;
        Time.timeScale = 0;
    }

    public void UnpauseGame()
    {
        gamePaused = false;
        Time.timeScale = 1;
    }

    public void FinishLevel()
    {
        onLevelFinish?.Invoke();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int targetScene = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(targetScene);
    }
}
