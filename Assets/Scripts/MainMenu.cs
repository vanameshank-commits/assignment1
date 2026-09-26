using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Make sure your main game scene is named exactly this, or change this string
    public string gameSceneName = "TestScene";

    public void StartGame()
    {
        // Loads your game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        // Quits the application (Note: this only works in a built game, not in the editor)
        Debug.Log("Game is exiting...");
        Application.Quit();
    }
}