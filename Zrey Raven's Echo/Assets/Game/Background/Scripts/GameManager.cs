using UnityEngine;
using UnityEngine.SceneManagement; // Needed for Restart and Main Menu
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    [Header("Pause Panel UI")]
    [Tooltip("Drag the parent Panel object for your pause menu here.")]
    public GameObject pausePanel;

    // A simple flag to check if the game is currently paused.
    public static bool isPaused = false;

    void Start()
    {
        // Make sure the pause panel is hidden when the game starts.
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        // Ensure time is running normally at the start.
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // Keyboard.current gives us access to the current state of the keyboard.
        // .escapeKey checks the state of the Escape key.
        // .wasPressedThisFrame is the new equivalent of GetKeyDown().
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // The rest of the logic is exactly the same.
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // This function will be called to pause the game.
    public void PauseGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true); // Show the pause panel.
        }

        // --- THIS IS THE MAGIC LINE ---
        // Time.timeScale = 0f freezes EVERYTHING that relies on Unity's Time system:
        // Physics (Rigidbodies), Animations, and anything in Update() that uses Time.deltaTime.
        Time.timeScale = 0f;
        ZreyMovements.inputActions.Player.Disable();
        // Enable the "UI" map.
        ZreyMovements.inputActions.UI.Enable();        // We also pause all AudioSources in the game.
        AudioListener.pause = true;
       
        isPaused = true;
        Debug.Log("Game Paused.");
    }

    // This function will be called by our "Resume" button.
    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false); // Hide the pause panel.
        }

        // Restore the normal flow of time.
        Time.timeScale = 1f;
        ZreyMovements.inputActions.UI.Disable();
        ZreyMovements.inputActions.Player.Enable();
       
        // Unpause all audio.
        AudioListener.pause = false;
       
        isPaused = false;
        Debug.Log("Game Resumed.");
    }

    // This function will be called by our "Restart" button.
    public void RestartLevel()
    {
        // Before we restart, we MUST unpause the game. Otherwise, the new scene
        // will load but still be frozen because Time.timeScale is 0.
        ResumeGame();

        // Get the name of the currently active scene and reload it.
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        Debug.Log($"Restarting level: {currentSceneName}");
    }

    // This function will be called by our "Main Menu" button.
    public void LoadMainMenu()
    {
        // Same as restart, we must unpause first.
        ResumeGame();

        // Load the Main Menu scene (make sure its name is correct).
        SceneManager.LoadScene("MainMenu"); // Change "MainMenu" if your scene has a different name.
        Debug.Log("Loading Main Menu...");
    }
}
