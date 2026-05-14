using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // We need this to control UI elements like Image
using System.Collections; // We need this for Coroutines (to create a delay)
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("The EXACT name of the scene you want to load (e.g., 'Tutorial').")]
    public string sceneToLoad = "Tutorial";
    [Header("Menu Buttons")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject tutorialButton;
    [SerializeField] private GameObject bossButton;
    [SerializeField] private float buttonFadeDuration = 0.5f;
    // --- NEW VARIABLES FOR SCRIPT-BASED FADE ---
    [Header("Fade Effect")]
    [Tooltip("Drag the black UI Image you created for the fade effect here.")]
    public Image fadeScreenImage;
    [Header("Scene Effects")]
    [Tooltip("Drag all the parent GameObjects of the particle effects you want to hide here.")]
    public List<GameObject> effectsToHide;
    [Tooltip("How long, in seconds, the fade-to-black should take.")]
    public float fadeDuration = 1.0f;
    // --- END OF NEW VARIABLES ---
    [Header("Music")]
    [Tooltip("Drag the main menu music clip here.")]
    public AudioClip menuMusicClip;
    [Tooltip("How long the music fade out takes (should match or be shorter than fadeDuration).")]
    public float musicFadeDuration = 1.0f;
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    private AudioSource musicSource;
    // This function is still called by your "Start" button's OnClick() event.
    void Start()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = menuMusicClip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.playOnAwake = false;

        if (menuMusicClip != null)
            musicSource.Play();
        if (fadeScreenImage != null)
        {
            fadeScreenImage.color = new Color(0f, 0f, 0f, 1f); // Start fully black
            StartCoroutine(FadeInOnStart());
        }
        if (tutorialButton != null) tutorialButton.SetActive(false);
        if (bossButton != null) bossButton.SetActive(false);
    }

    public void StartGame()
    {
        StartCoroutine(SwapToLevelButtons());
    }

    private IEnumerator SwapToLevelButtons()
    {
        // Fade out the start button
        CanvasGroup startCG = startButton.GetComponent<CanvasGroup>();
        if (startCG == null) startCG = startButton.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < buttonFadeDuration)
        {
            t += Time.deltaTime;
            startCG.alpha = Mathf.Lerp(1f, 0f, t / buttonFadeDuration);
            yield return null;
        }
        startButton.SetActive(false);

        // Fade in the two level buttons
        tutorialButton.SetActive(true);
        bossButton.SetActive(true);

        CanvasGroup tutCG = tutorialButton.GetComponent<CanvasGroup>();
        if (tutCG == null) tutCG = tutorialButton.AddComponent<CanvasGroup>();
        CanvasGroup bossCG = bossButton.GetComponent<CanvasGroup>();
        if (bossCG == null) bossCG = bossButton.AddComponent<CanvasGroup>();

        tutCG.alpha = 0f;
        bossCG.alpha = 0f;

        t = 0f;
        while (t < buttonFadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / buttonFadeDuration);
            tutCG.alpha = alpha;
            bossCG.alpha = alpha;
            yield return null;
        }
    }

    // This coroutine will handle the entire fade process from start to finish.
    private IEnumerator FadeAndLoadScene()
    {
        if (CursorManager.Instance != null) CursorManager.Instance.ForceHide();
        if (effectsToHide != null)
        {
            Debug.Log($"Hiding {effectsToHide.Count} scene effects.");
            foreach (GameObject effect in effectsToHide)
            {
                if (effect != null)
                {
                    effect.SetActive(false);
                }
            }
        }
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
        StartCoroutine(FadeOutMusic());
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
    public void LoadTutorial()
    {
        sceneToLoad = "SampleScene";
        StartCoroutine(FadeAndLoadScene());
    }

    public void LoadBossRoom()
    {
        sceneToLoad = "BossRoom";
        StartCoroutine(FadeAndLoadScene());
    }
    private IEnumerator FadeOutMusic()
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < musicFadeDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / musicFadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }
    // This function is still called by your "Quit" button.
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
    private IEnumerator FadeInOnStart()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
            fadeScreenImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeScreenImage.color = new Color(0f, 0f, 0f, 0f); // Fully transparent at the end
    }
}
