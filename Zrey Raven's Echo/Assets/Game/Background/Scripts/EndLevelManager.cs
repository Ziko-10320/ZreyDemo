// --- EndLevelManager.cs ---

using UnityEngine;
using UnityEngine.UI; // Needed for Image
using TMPro;          // Needed for TextMeshPro
using System.Collections; // Needed for Coroutines

public class EndLevelManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The parent panel that holds everything.")]
    public GameObject endingPanel;

    [Tooltip("The image that will fade in.")]
    public Image endingImage;

    [Tooltip("The text that appears after the fade.")]
    public GameObject endingText;

    [Header("Fade Settings")]
    [Tooltip("How long the image fade should take in seconds.")]
    public float fadeDuration = 2f;

    // This function is called by Unity when another collider enters this one.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered is the Player.
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has reached the end! Starting ending sequence.");

            // Start the sequence.
            StartEndingSequence();

            // Optional: Disable the trigger so it can't be activated again.
            GetComponent<Collider2D>().enabled = false;
        }
    }

    private void StartEndingSequence()
    {
        // Make the panel visible so the fade has something to draw on.
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
        }

        // Start the coroutine that handles the timed sequence.
        StartCoroutine(FadeInSequence());
    }

    private IEnumerator FadeInSequence()
    {
        // --- PART 1: FADE IN THE IMAGE ---
        float elapsedTime = 0f;
        Color imageColor = endingImage.color;

        while (elapsedTime < fadeDuration)
        {
            // Calculate the new alpha value.
            float newAlpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);

            // Apply the new alpha to the image color.
            endingImage.color = new Color(imageColor.r, imageColor.g, imageColor.b, newAlpha);

            // Wait for the next frame.
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the alpha is exactly 1 at the end.
        endingImage.color = new Color(imageColor.r, imageColor.g, imageColor.b, 1f);

        Debug.Log("Fade complete!");

        // --- PART 2: SHOW THE TEXT ---
        if (endingText != null)
        {
            endingText.SetActive(true);
            Debug.Log("Showing ending text.");
        }
    }
}
