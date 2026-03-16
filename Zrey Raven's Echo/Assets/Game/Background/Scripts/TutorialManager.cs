using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Mode")]
    public bool InTutorialMode = false;

    [Header("Parry Prompt Canvas")]
    [SerializeField] private CanvasGroup parryPromptCanvasGroup;
    [SerializeField] private float fadeInSpeed = 3f;
    [SerializeField] private float fadeOutSpeed = 3f;

    [Header("Slow Time Settings")]
    [SerializeField] private float slowTimeScale = 0.2f;
    [SerializeField] private float slowTimeRestoreSpeed = 5f;
    [Tooltip("Safety timeout — restores time if player never parries.")]
    [SerializeField] private float slowTimeTimeout = 4f;

    private bool isSlowTimeActive = false;
    private Coroutine slowTimeCoroutine;
    private Coroutine fadeCoroutine;
    public bool IsTutorialParryWindowOpen { get; private set; } = false;
    private bool hasPlayerLearnedParry = false;
    public bool HasPlayerLearnedParry => hasPlayerLearnedParry;
    // Stores pending damage to apply if player fails to parry
    private int pendingDamageAmount = 0;
    private Transform pendingDamageAttacker;
    private ImpactData pendingDamageImpact;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Make sure canvas starts invisible
        if (parryPromptCanvasGroup != null)
        {
            parryPromptCanvasGroup.alpha = 0f;
            parryPromptCanvasGroup.gameObject.SetActive(false);
        }
    }

    // Called from Reaper counter animation event
    public void TriggerParrySlowTime()
    {
        if (!InTutorialMode) return;
        if (isSlowTimeActive) return;
        if (hasPlayerLearnedParry) return;
        if (slowTimeCoroutine != null) StopCoroutine(slowTimeCoroutine);
        slowTimeCoroutine = StartCoroutine(SlowTimeSequence());
    }

    // Called from PlayerHealth when a successful parry fires
    public void OnPlayerParriedSuccessfully()
    {
        if (!InTutorialMode || !isSlowTimeActive) return;

        // Cancel any pending damage — player earned the parry
        pendingDamageAmount = 0;
        pendingDamageAttacker = null;
        pendingDamageImpact = null;

        // Player learned to parry — never slow time again
        hasPlayerLearnedParry = true;
        Debug.Log("<color=lime>Tutorial: Player learned parry. Slow-time will not trigger again.</color>");

        RestoreTime();
    }

    // Called from PlayerHealth to queue damage instead of applying it immediately
    public void QueueTutorialDamage(int damage, Transform attacker, ImpactData impact)
    {
        pendingDamageAmount = damage;
        pendingDamageAttacker = attacker;
        pendingDamageImpact = impact;
        Debug.Log($"<color=orange>Tutorial: Damage queued ({damage}) — waiting for parry window to resolve.</color>");
    }
    public bool HasQueuedDamage()
    {
        return pendingDamageAmount > 0 && pendingDamageAttacker != null;
    }

    // Called by PlayerHealth when block is pressed with queued damage — treat as parry
    public void ProcessQueuedDamageAsParry(PlayerHealth playerHealth, Transform playerTransform)
    {
        if (!HasQueuedDamage()) return;

        Transform attacker = pendingDamageAttacker;
        ImpactData impact = pendingDamageImpact;

        // Clear the queue before processing so it can't double-fire
        pendingDamageAmount = 0;
        pendingDamageAttacker = null;
        pendingDamageImpact = null;

        // Tell tutorial system parry succeeded — stops timeout, restores time
        OnPlayerParriedSuccessfully();

        // Fire the actual parry success logic on PlayerHealth
        playerHealth.TriggerParrySuccess(attacker, impact);
    }
    // Called internally after timeout to flush pending damage
    private void FlushPendingDamage()
    {
        if (pendingDamageAmount <= 0 || pendingDamageAttacker == null) return;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log($"<color=red>Tutorial: Parry failed — applying queued damage ({pendingDamageAmount}).</color>");
            playerHealth.TakeDamage(pendingDamageAmount, pendingDamageAttacker, pendingDamageImpact);
        }
        pendingDamageAmount = 0;
        pendingDamageAttacker = null;
        pendingDamageImpact = null;
    }

    private IEnumerator SlowTimeSequence()
    {

        isSlowTimeActive = true;
        IsTutorialParryWindowOpen = true;
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Fade in prompt AFTER slowing time (uses unscaled time so still works)
        if (parryPromptCanvasGroup != null)
        {
            parryPromptCanvasGroup.gameObject.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvas(parryPromptCanvasGroup, 0f, 1f, fadeInSpeed));
        }

        // Safety timeout using unscaled time
        float elapsed = 0f;
        while (isSlowTimeActive && elapsed < slowTimeTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isSlowTimeActive)
        {
            RestoreTime();
            // Small delay so time is restored before damage lands
            yield return new WaitForSecondsRealtime(0.1f);
            FlushPendingDamage();
        }
    }

    private void RestoreTime()
    {
        isSlowTimeActive = false;
        IsTutorialParryWindowOpen = false;
        // Smoothly restore time scale
        StartCoroutine(RestoreTimeCoroutine());

        // Fade out prompt
        if (parryPromptCanvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasThenDisable(parryPromptCanvasGroup, 1f, 0f, fadeOutSpeed));
        }
    }

    private IEnumerator RestoreTimeCoroutine()
    {
        // Use unscaled delta time so this works even when game is slowed
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards(
                Time.timeScale, 1f, slowTimeRestoreSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float speed)
    {
        cg.alpha = from;
        // Use unscaled time so fade works during slow motion
        while (!Mathf.Approximately(cg.alpha, to))
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, to, speed * Time.unscaledDeltaTime);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeCanvasThenDisable(CanvasGroup cg, float from, float to, float speed)
    {
        yield return StartCoroutine(FadeCanvas(cg, from, to, speed));
        cg.gameObject.SetActive(false);
    }
}