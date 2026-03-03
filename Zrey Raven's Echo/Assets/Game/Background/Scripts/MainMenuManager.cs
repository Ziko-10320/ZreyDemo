using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // We need this to control UI elements like Image
using System.Collections; // We need this for Coroutines (to create a delay)

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("The EXACT name of the scene you want to load (e.g., 'Tutorial').")]
    public string sceneToLoad = "Tutorial";

    // --- NEW VARIABLES FOR SCRIPT-BASED FADE ---
    [Header("Fade Effect")]
    [Tooltip("Drag the black UI Image you created for the fade effect here.")]
    public Image fadeScreenImage;

    [Tooltip("How long, in seconds, the fade-to-black should take.")]
    public float fadeDuration = 1.0f;
    // --- END OF NEW VARIABLES ---

    // This function is still called by your "Start" button's OnClick() event.
    public void StartGame()
    {
        // Start the coroutine that handles the fade and scene loading sequence.
        StartCoroutine(FadeAndLoadScene());
    }

    // This coroutine will handle the entire fade process from start to finish.
    private IEnumerator FadeAndLoadScene()
    {
        // Make sure the fade screen is ready to be used.
        if (fadeScreenImage == null)
        {
            Debug.LogError("Fade Screen Image is not assigned in the Inspector!");
            // If the image is missing, just load the scene immediately to avoid getting stuck.
            SceneManager.LoadScene(sceneToLoad);
            yield break; // Stop the coroutine here.
        }

        // --- THE FADE LOGIC ---
        float timer = 0f;
        Color originalColor = fadeScreenImage.color;

        // This loop will run until the timer reaches the desired fade duration.
        while (timer < fadeDuration)
        {
            // Increase the timer by the time that has passed since the last frame.
            timer += Time.deltaTime;

            // Calculate how far along the fade is (a value from 0 to 1).
            float alpha = Mathf.Clamp01(timer / fadeDuration);

            // Apply the new alpha value to the image's color.
            fadeScreenImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // Wait until the next frame before continuing the loop.
            yield return null;
        }
        // --- END OF FADE LOGIC ---

        // After the loop is finished, the screen is fully black. Now, load the scene.
        Debug.Log($"Attempting to load scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    // This function is still called by your "Quit" button.
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
