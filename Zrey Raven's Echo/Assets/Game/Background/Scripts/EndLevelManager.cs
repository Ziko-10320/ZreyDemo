// --- EndLevelManager.cs (Upgraded) ---

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // Needed to load scenes

public class EndLevelManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The parent panel that holds everything.")]
    public GameObject endingPanel;
    [Tooltip("The background image that will fade in first.")]
    public Image backgroundImage;
    [Tooltip("The text that fades in second.")]
    public TextMeshProUGUI endingText;
    [Tooltip("The button that fades in last.")]
    public Button mainMenuButton;

    [Header("Fade Settings")]
    [Tooltip("How long each fade (background, text, button) should take.")]
    public float fadeDuration = 2f;
    [Header("Quit Fade Elements")]
  
    [Header("Scene Settings")]
    [Tooltip("The name of your Main Menu scene (e.g., 'MainMenu').")]
    public string mainMenuSceneName;

    // This function is called by Unity when another collider enters this one.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has reached the end! Starting ending sequence.");
            StartEndingSequence();
            GetComponent<Collider2D>().enabled = false;
        }
    }

    private void StartEndingSequence()
    {
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
        }
        // Hook up the button's OnClick event through code.
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        StartCoroutine(ChainedFadeInSequence());
    }

    // This is the main coroutine that controls the sequence.
    private IEnumerator ChainedFadeInSequence()
    {
        // --- SEQUENCE 1: FADE IN BACKGROUND ---
        Debug.Log("Fading in background...");
        yield return StartCoroutine(FadeImage(backgroundImage, 0f, 1f, fadeDuration));
        Debug.Log("Background fade complete.");

        // --- SEQUENCE 2: FADE IN TEXT ---
        Debug.Log("Fading in text...");
        yield return StartCoroutine(FadeText(endingText, 0f, 1f, fadeDuration));
        Debug.Log("Text fade complete.");

        // --- SEQUENCE 3: FADE IN BUTTON ---
        Debug.Log("Fading in button...");
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true); // Enable the button object first
            // Get the button's image and text components
            Image buttonImage = mainMenuButton.GetComponent<Image>();
            TextMeshProUGUI buttonText = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();

            // Fade them both in at the same time
            StartCoroutine(FadeImage(buttonImage, 0f, 1f, fadeDuration));
            yield return StartCoroutine(FadeText(buttonText, 0f, 1f, fadeDuration));
        }
        Debug.Log("Button fade complete. Sequence finished.");
    }

    // A reusable coroutine to fade any Image
    private IEnumerator FadeImage(Image targetImage, float startAlpha, float endAlpha, float duration)
    {
        if (targetImage == null) yield break;
        float elapsedTime = 0f;
        Color color = targetImage.color;
        while (elapsedTime < duration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            targetImage.color = new Color(color.r, color.g, color.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    // A reusable coroutine to fade any TextMeshProUGUI
    private IEnumerator FadeText(TextMeshProUGUI targetText, float startAlpha, float endAlpha, float duration)
    {
        if (targetText == null) yield break;
        float elapsedTime = 0f;
        Color color = targetText.color;
        while (elapsedTime < duration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            targetText.color = new Color(color.r, color.g, color.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetText.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    // This function is called when the MainMenuButton is clicked.
    public void GoToMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"Loading scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main Menu Scene Name is not set in the EndLevelManager!");
        }
    }
}
