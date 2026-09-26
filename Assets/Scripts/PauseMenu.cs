using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden when the game starts
        pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Unfreeze time
        isPaused = false;

        // Re-lock the cursor for your camera controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Freeze time
        isPaused = true;

        // Unlock the cursor so you can click the Exit button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitToMainMenu()
    {
        // CRITICAL: Unfreeze time before loading, otherwise the next scene will also be frozen!
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}