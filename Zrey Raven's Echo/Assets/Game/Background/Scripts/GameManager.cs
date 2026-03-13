using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI; // --- NEW: We need this to control the Image component ---
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Pause Panel UI")]
    public GameObject pausePanel;
    [SerializeField] private Image deathFadeImage;
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float startScale = 0.8f;

    // --- NEW: Variables for the scene transition fade ---
    [Header("Scene Fade Effect")]
    [Tooltip("Drag the black UI Image for the scene transition fade here.")]
    public Image sceneFadeImage;
    [Tooltip("How long the fade-to-black should take before changing scenes.")]
    public float sceneFadeDuration = 0.5f;
    // --- END OF NEW VARIABLES ---
    [Header("Audio")]
    [Tooltip("Drag the AudioManager object here.")]
    public AudioManager audioManager;
    private CanvasGroup panelCanvasGroup;
    private RectTransform panelRectTransform;

    public static bool isPaused = false;

    void Start()
    {
        if (pausePanel != null)
        {
            panelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            panelRectTransform = pausePanel.GetComponent<RectTransform>();
            panelCanvasGroup.alpha = 0f;
            pausePanel.SetActive(false);
        }

        // --- NEW: Ensure the scene fade image is transparent at the start ---
        if (sceneFadeImage != null)
        {
            sceneFadeImage.color = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 0f);
        }
        // --- END OF NEW CODE ---

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
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

    // --- Your PauseGame() and ResumeGame() functions remain the same ---
    public void PauseGame()
    {
        // --- ADD THIS LINE ---
        if (audioManager != null) audioManager.MuffleMusic();
        // --- END OF ADDED LINE ---
        if (CursorManager.Instance != null) CursorManager.Instance.RequestShowCursor();
        if (pausePanel == null) return;
        StartCoroutine(AnimatePanel(true));
        Time.timeScale = 0f;
        ZreyMovements.inputActions.Player.Disable();
        ZreyMovements.inputActions.UI.Enable();
        // AudioListener.pause = true; // <-- IMPORTANT: DELETE OR COMMENT OUT THIS LINE!
        isPaused = true;
    }

    public void ResumeGame()
    {
        // --- ADD THIS LINE ---
        if (audioManager != null) audioManager.UnmuffleMusic();
        // --- END OF ADDED LINE ---
        if (CursorManager.Instance != null) CursorManager.Instance.ForceHide();
        if (pausePanel == null) return;
        StartCoroutine(AnimatePanel(false));
        Time.timeScale = 1f;
        ZreyMovements.inputActions.UI.Disable();
        ZreyMovements.inputActions.Player.Enable();
        // AudioListener.pause = false; // <-- IMPORTANT: DELETE OR COMMENT OUT THIS LINE!
        isPaused = false;
    }

    // --- Your AnimatePanel() coroutine remains the same ---
    private IEnumerator AnimatePanel(bool show)
    {
        // ... (This coroutine is unchanged) ...
        float timer = 0f;
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        Vector3 startScaleVector = Vector3.one * startScale;
        Vector3 endScaleVector = Vector3.one;
        if (!show) { Vector3 temp = startScaleVector; startScaleVector = endScaleVector; endScaleVector = temp; }
        if (show) { pausePanel.SetActive(true); }
        while (timer < animationDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / animationDuration);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            panelRectTransform.localScale = Vector3.Lerp(startScaleVector, endScaleVector, progress);
            yield return null;
        }
        if (!show) { pausePanel.SetActive(false); }
    }

    // --- MODIFIED: RestartLevel now calls the fade coroutine ---
    public void RestartLevel()
    {
        
        Time.timeScale = 1f;
        ZreyMovements.NukeInputSystem();
        StartCoroutine(FadeAndLoadScene(SceneManager.GetActiveScene().name));
    }

    public void LoadMainMenu()
    {
       
        Time.timeScale = 1f;
        ZreyMovements.NukeInputSystem();
        StartCoroutine(FadeAndLoadScene("MainMenu"));
    }
    // --- NEW: The Coroutine that handles fading and loading a scene ---
    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        isPaused = false;
        // AudioListener.pause stays true here so sounds remain stopped during fade

        if (deathFadeImage == null)
        {
            AudioListener.pause = false;
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        deathFadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color c = deathFadeImage.color;
        c.a = 0f;
        deathFadeImage.color = c;

        while (timer < sceneFadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Clamp01(timer / sceneFadeDuration);
            deathFadeImage.color = c;
            yield return null;
        }

        // Only unpause audio right before loading so sounds stay dead during the fade
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }
}
