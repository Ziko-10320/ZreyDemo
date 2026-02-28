using UnityEngine;
using UnityEngine.SceneManagement; // We need this line to be able to switch scenes.

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("The EXACT name of the scene you want to load (e.g., 'Tutorial').")]
    public string sceneToLoad = "Tutorial";

    // This function will be called by our "Start" button.
    public void StartGame()
    {
        Debug.Log($"Attempting to load scene: {sceneToLoad}");
        // SceneManager.LoadScene tells Unity to load the scene with the specified name.
        SceneManager.LoadScene(sceneToLoad);
    }

    // This function will be called by our "Quit" button.
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        // Application.Quit() closes the game.
        // NOTE: This will NOT work in the Unity Editor, but it will work
        // in a real, built version of your game.
        Application.Quit();
    }
}
