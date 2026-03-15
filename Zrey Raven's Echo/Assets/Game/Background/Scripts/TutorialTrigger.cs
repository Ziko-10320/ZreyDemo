using UnityEngine;
using UnityEngine.UI; // Needed for Image
using TMPro;          // Needed for TextMeshPro
using System.Collections;

// This script will be placed on an empty GameObject with a trigger collider.
[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The parent Canvas that holds all the tutorial UI for this trigger.")]
    [SerializeField] private CanvasGroup tutorialCanvasGroup;
    [Tooltip("How long to wait before the tutorial can trigger again. 0 = never repeats.")]
    [SerializeField] private float repeatDelay = 10f;
    [Header("Timing Settings")]
    [Tooltip("How long it takes for the UI to fade in (in seconds).")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("How long the UI stays visible on screen before fading out.")]
    [SerializeField] private float displayDuration = 5.0f;
    [Tooltip("How long it takes for the UI to fade out (in seconds).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Trigger Settings")]
    [Tooltip("Should this trigger only activate once?")]
    [SerializeField] private bool triggerOnlyOnce = true;

    // --- Internal State ---
    private bool hasBeenTriggered = false;
    private Coroutine activeCoroutine = null;

    // Ensure the UI is hidden when the game starts.
    private void Awake()
    {
        if (tutorialCanvasGroup != null)
        {
            // Set the alpha to 0 to make it invisible.
            tutorialCanvasGroup.alpha = 0f;

            // We will now control visibility with the CanvasGroup's "interactable" and "blocksRaycasts" properties.
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("Tutorial Canvas Group is not assigned on " + gameObject.name, this);
        }
    }

    // This function is called by Unity when another collider enters our trigger zone.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasBeenTriggered) return;
            if (hasBeenTriggered) return; // Still in cooldown or running

            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(ShowTutorialSequence());
        }
    }

    // This is the main coroutine that controls the entire fade-in, wait, and fade-out sequence.
    private IEnumerator ShowTutorialSequence()
    {
        hasBeenTriggered = true;

        // --- FADE IN ---
        tutorialCanvasGroup.interactable = true;
        tutorialCanvasGroup.blocksRaycasts = true;
        yield return StartCoroutine(FadeCanvasGroup(tutorialCanvasGroup, 0f, 1f, fadeInDuration));

        // --- WAIT ---
        yield return new WaitForSeconds(displayDuration);

        // --- FADE OUT ---
        tutorialCanvasGroup.interactable = false;
        tutorialCanvasGroup.blocksRaycasts = false;
        yield return StartCoroutine(FadeCanvasGroup(tutorialCanvasGroup, 1f, 0f, fadeOutDuration));

        activeCoroutine = null;

        // --- REPEAT DELAY (only if not a one-time trigger) ---
        if (!triggerOnlyOnce && repeatDelay > 0f)
        {
            yield return new WaitForSeconds(repeatDelay);
            hasBeenTriggered = false; // Now it can trigger again
        }
    }

    // A reusable coroutine to fade any CanvasGroup.
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = endAlpha; // Ensure it ends at the exact target alpha.
    }
}
