using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EndLevelManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The parent panel that holds everything.")]
    public GameObject endingPanel;
    [Tooltip("The background image that will fade in first.")]
    public Image backgroundImage;

    // --- MODIFIED: Added slots for the new images ---
    [Tooltip("The text that fades in second.")]
    public TextMeshProUGUI endingText;
    [Tooltip("The 'cadre' or border image for the text.")]
    public Image textBackgroundImage; // NEW

    [Tooltip("The button that fades in last.")]
    public Button mainMenuButton;
    [Tooltip("The image that is a child of the button.")]
    public Image buttonChildImage; // NEW

    [Header("Fade Settings")]
    [Tooltip("How long each fade (background, text, button) should take.")]
    public float fadeInDuration = 2f;

    // --- NEW: Variables for the quit fade ---
    [Header("Quit Fade Elements")]
    [Tooltip("The black screen image used to fade out when quitting.")]
    public Image quitFadeImage;
    [Tooltip("How long the fade-to-black should take before changing scenes.")]
    public float fadeOutDuration = 1f;

    [Header("Scene Settings")]
    [Tooltip("The name of your Main Menu scene (e.g., 'MainMenu').")]
    public string mainMenuSceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has reached the end! Starting ending sequence.");
            StartEndingSequence();
            GetComponent<Collider2D>().enabled = false;
        }
    }

    private void Start()
    {
        // --- NEW: Ensure quit fade screen is transparent at start ---
        if (quitFadeImage != null)
        {
            quitFadeImage.color = new Color(quitFadeImage.color.r, quitFadeImage.color.g, quitFadeImage.color.b, 0f);
        }
    }

    private void StartEndingSequence()
    {
        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        StartCoroutine(ChainedFadeInSequence());
    }

    private IEnumerator ChainedFadeInSequence()
    {
        // --- SEQUENCE 1: FADE IN BACKGROUND ---
        Debug.Log("Fading in background...");
        yield return StartCoroutine(FadeImage(backgroundImage, 0f, 1f, fadeInDuration));
        Debug.Log("Background fade complete.");

        // --- SEQUENCE 2: FADE IN TEXT AND ITS IMAGE ---
        Debug.Log("Fading in text and its background...");
        // Start both fades at the same time
        StartCoroutine(FadeText(endingText, 0f, 1f, fadeInDuration));
        yield return StartCoroutine(FadeImage(textBackgroundImage, 0f, 1f, fadeInDuration)); // Wait for this one to finish
        Debug.Log("Text fade complete.");

        // --- SEQUENCE 3: FADE IN BUTTON AND ITS IMAGE ---
        Debug.Log("Fading in button and its child image...");
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
            Image buttonImage = mainMenuButton.GetComponent<Image>();
            TextMeshProUGUI buttonText = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();

            // Start all three fades at the same time
            StartCoroutine(FadeImage(buttonImage, 0f, 1f, fadeInDuration));
            StartCoroutine(FadeText(buttonText, 0f, 1f, fadeInDuration));
            yield return StartCoroutine(FadeImage(buttonChildImage, 0f, 1f, fadeInDuration)); // Wait for this one to finish
        }
        Debug.Log("Button fade complete. Sequence finished.");
    }

    // --- Reusable coroutine to fade any Image (Unchanged) ---
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

    // --- Reusable coroutine to fade any TextMeshProUGUI (Unchanged) ---
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

    // --- MODIFIED: This function now starts the fade-out coroutine ---
    public void GoToMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            StartCoroutine(FadeAndLoadScene(mainMenuSceneName));
        }
        else
        {
            Debug.LogError("Main Menu Scene Name is not set in the EndLevelManager!");
        }
    }

    // --- NEW: The coroutine that handles fading out and loading the scene ---
    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        Debug.Log("Fading out to load Main Menu...");

        // Use the reusable FadeImage coroutine to fade the quit screen IN.
        yield return StartCoroutine(FadeImage(quitFadeImage, 0f, 1f, fadeOutDuration));

        // After the fade is complete, load the scene.
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
