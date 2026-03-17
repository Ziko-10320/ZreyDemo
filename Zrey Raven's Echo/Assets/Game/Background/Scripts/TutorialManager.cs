using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    private bool hasPlayerLearnedCounter = false;
    // Stores pending damage to apply if player fails to parry
    private int pendingDamageAmount = 0;
    private Transform pendingDamageAttacker;
    private ImpactData pendingDamageImpact;

    [Header("Direct Combo Canvas")]
    [SerializeField] private CanvasGroup directComboCanvasGroup;
    [SerializeField] private float directComboFadeInSpeed = 3f;
    [SerializeField] private float directComboFadeOutSpeed = 3f;
    [SerializeField] private float directComboDisplayTime = 3f;
    private bool hasShownDirectCombo = false;
    private Coroutine directComboCoroutine;

    [Header("Dash Attack Canvas")]
    [SerializeField] private CanvasGroup dashAttackCanvasGroup;
    [SerializeField] private float dashAttackFadeInSpeed = 3f;
    [SerializeField] private float dashAttackFadeOutSpeed = 3f;
    [SerializeField] private float dashAttackDisplayTime = 3f;
    [Tooltip("Minimum distance between player and Reaper to trigger the dash attack hint.")]
    [SerializeField] private float dashAttackTriggerDistance = 5f;
    private bool hasShownDashAttack = false;
    private Coroutine dashAttackCoroutine;

    [Header("Dash Attack Slow Time")]
    [SerializeField] private float dashAttackSlowTimeScale = 0.3f;
    [SerializeField] private float dashAttackSlowTimeRestoreSpeed = 3f;

    [Header("UpperCut Canvas")]
    [SerializeField] private CanvasGroup upperCutCanvasGroup;
    [SerializeField] private float upperCutFadeInSpeed = 3f;
    [SerializeField] private float upperCutFadeOutSpeed = 3f;
    [SerializeField] private float upperCutDisplayTime = 3f;
    private bool hasShownUpperCut = false;
    private Coroutine upperCutCoroutine;
    [Tooltip("Delay in real seconds between dash attack canvas finishing and uppercut canvas appearing.")]
    [SerializeField] private float upperCutDelayAfterDashCanvas = 2f;
    [Header("Aerial Combo Canvas")]
    [SerializeField] private CanvasGroup aerialComboCanvasGroup;
    [SerializeField] private float aerialComboFadeInSpeed = 3f;
    [SerializeField] private float aerialComboFadeOutSpeed = 3f;
    [SerializeField] private float aerialComboDisplayTime = 3f;
    [SerializeField] private float aerialComboSlowTimeScale = 0.3f;
    [SerializeField] private float aerialComboSlowTimeRestoreSpeed = 3f;
    private bool hasShownAerialCombo = false;
    private Coroutine aerialComboCoroutine;

    [Header("Color Adjustment (Parry Greyscale)")]
    [SerializeField] private Volume colorAdjustmentVolume;
    [SerializeField] private float saturationFadeSpeed = 8f;
    private ColorAdjustments colorAdjustments;
    private Coroutine saturationCoroutine;

    [Header("Counter Tutorial (Special Attack)")]
    [SerializeField] private float counterSlowTimeScale = 0.2f;
    [SerializeField] private float counterSlowTimeRestoreSpeed = 5f;
    [SerializeField] private float counterSlowTimeTimeout = 4f;
    private bool isCounterSlowTimeActive = false;
    private Coroutine counterSlowTimeCoroutine;

    // Pending special attack damage — held until counter resolves
    private int pendingCounterDamageAmount = 0;
    private Transform pendingCounterDamageAttacker;
    private ImpactData pendingCounterDamageImpact;

    // True once all tutorial hint canvases have been displayed at least once
    public bool HasShownAllCanvases =>
        hasShownDirectCombo && hasShownDashAttack && hasShownUpperCut && hasShownAerialCombo;

    private bool hasTriggeredCounterSlowTime = false; // Only fires once

    [Header("Counter Attack Canvas")]
    [SerializeField] private CanvasGroup counterAttackCanvasGroup;
    [SerializeField] private float counterAttackFadeInSpeed = 3f;
    [SerializeField] private float counterAttackFadeOutSpeed = 3f;
    [SerializeField] private float counterAttackDisplayTime = 3f;
    private bool hasShownCounterAttack = false;
    private Coroutine counterAttackCoroutine;
    [Header("Jump Attack Canvas")]
    [SerializeField] private CanvasGroup jumpAttackCanvasGroup;
    [SerializeField] private float jumpAttackFadeInSpeed = 3f;
    [SerializeField] private float jumpAttackFadeOutSpeed = 3f;
    [SerializeField] private float jumpAttackDisplayTime = 3f;
    [SerializeField] private float jumpAttackDelayAfterCounter = 2f;
    private bool hasShownJumpAttack = false;
    private Coroutine jumpAttackCoroutine;

    [Header("Special Attack Canvas")]
    [SerializeField] private CanvasGroup specialAttackCanvasGroup;
    [SerializeField] private float specialAttackFadeInSpeed = 3f;
    [SerializeField] private float specialAttackFadeOutSpeed = 3f;
    [SerializeField] private float specialAttackDisplayTime = 3f;
    [SerializeField] private float specialAttackDelayAfterJumpAttack = 2f;
    private bool hasShownSpecialAttack = false;
    private Coroutine specialAttackCoroutine;

    [Header("Finisher Canvas")]
    [SerializeField] private CanvasGroup finisherCanvasGroup;
    [SerializeField] private float finisherFadeInSpeed = 3f;
    [SerializeField] private float finisherFadeOutSpeed = 3f;
    [SerializeField] private float finisherDisplayTime = 3f;
    private bool hasShownFinisher = false;
    private Coroutine finisherCoroutine;
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

        if (directComboCanvasGroup != null)
        {
            directComboCanvasGroup.alpha = 0f;
            directComboCanvasGroup.gameObject.SetActive(false);
        }
        if (dashAttackCanvasGroup != null)
        {
            dashAttackCanvasGroup.alpha = 0f;
            dashAttackCanvasGroup.gameObject.SetActive(false);
        }
        if (upperCutCanvasGroup != null)
        {
            upperCutCanvasGroup.alpha = 0f;
            upperCutCanvasGroup.gameObject.SetActive(false);
        }
        if (aerialComboCanvasGroup != null)
        {
            aerialComboCanvasGroup.alpha = 0f;
            aerialComboCanvasGroup.gameObject.SetActive(false);
        }
        if (counterAttackCanvasGroup != null)
        {
            counterAttackCanvasGroup.alpha = 0f;
            counterAttackCanvasGroup.gameObject.SetActive(false);
        }
        if (jumpAttackCanvasGroup != null)
        {
            jumpAttackCanvasGroup.alpha = 0f;
            jumpAttackCanvasGroup.gameObject.SetActive(false);
        }

        if (specialAttackCanvasGroup != null)
        {
            specialAttackCanvasGroup.alpha = 0f;
            specialAttackCanvasGroup.gameObject.SetActive(false);
        }

        if (finisherCanvasGroup != null)
        {
            finisherCanvasGroup.alpha = 0f;
            finisherCanvasGroup.gameObject.SetActive(false);
        }
        // Find ColorAdjustments override in the volume
        if (colorAdjustmentVolume == null)
            colorAdjustmentVolume = FindObjectOfType<Volume>();
        if (colorAdjustmentVolume != null &&
         colorAdjustmentVolume.profile.TryGet(out ColorAdjustments ca))
        {
            colorAdjustments = ca;
            colorAdjustments.active = false;
            // Must set overrideState = true or runtime value changes are ignored by URP
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 0f;
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

        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.value = 0f;
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            saturationCoroutine = StartCoroutine(FadeSaturation(0f, -100f, saturationFadeSpeed));
        }

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
    private IEnumerator FadeSaturation(float from, float to, float speed)
    {
        if (colorAdjustments == null) yield break;
        colorAdjustments.saturation.value = from;
        while (!Mathf.Approximately(colorAdjustments.saturation.value, to))
        {
            colorAdjustments.saturation.value = Mathf.MoveTowards(
                colorAdjustments.saturation.value, to, speed * Time.unscaledDeltaTime);
            yield return null;
        }
        colorAdjustments.saturation.value = to;
        saturationCoroutine = null;
    }
    private void RestoreTime()
    {
        isSlowTimeActive = false;
        IsTutorialParryWindowOpen = false;
        if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }
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
    public void TriggerDirectComboCanvas()
    {
        if (!InTutorialMode) return;
        if (hasShownDirectCombo) return;

        hasShownDirectCombo = true;

        if (directComboCoroutine != null) StopCoroutine(directComboCoroutine);
        directComboCoroutine = StartCoroutine(DirectComboSequence());
    }

    private IEnumerator DirectComboSequence()
    {
        if (directComboCanvasGroup == null) yield break;

        directComboCanvasGroup.gameObject.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeCanvas(directComboCanvasGroup, 0f, 1f, directComboFadeInSpeed));

        // Hold
        yield return new WaitForSecondsRealtime(directComboDisplayTime);

        // Fade out
        yield return StartCoroutine(FadeCanvasThenDisable(directComboCanvasGroup, 1f, 0f, directComboFadeOutSpeed));

        directComboCoroutine = null;
    }
    public void TryTriggerDashAttackCanvas(Transform player, Transform reaper)
    {
        if (!InTutorialMode) return;
        if (hasShownDashAttack) return;
        if (!hasShownDirectCombo) return; // Only show after direct combo was taught first

        float distance = Vector2.Distance(player.position, reaper.position);
        if (distance < dashAttackTriggerDistance) return;

        hasShownDashAttack = true;

        if (dashAttackCoroutine != null) StopCoroutine(dashAttackCoroutine);
        dashAttackCoroutine = StartCoroutine(DashAttackCanvasSequence());
    }

    public void OnPlayerPerformedDashAttack()
    {
        if (!InTutorialMode || !hasShownDashAttack) return;
        if (dashAttackCoroutine == null && Time.timeScale >= 1f) return;

        Debug.Log("<color=lime>Tutorial: Dash attack performed — restoring time and fading canvas.</color>");

        // Stop the display sequence so it doesn't keep running
        if (dashAttackCoroutine != null)
        {
            StopCoroutine(dashAttackCoroutine);
            dashAttackCoroutine = null;
        }

        // Fade out canvas immediately
        if (dashAttackCanvasGroup != null && dashAttackCanvasGroup.gameObject.activeSelf)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasThenDisable(
                dashAttackCanvasGroup, dashAttackCanvasGroup.alpha, 0f, dashAttackFadeOutSpeed));
        }

        // Restore time then trigger UpperCut canvas after delay
        StartCoroutine(RestoreAndTriggerUpperCut());
    }

    private IEnumerator RestoreAndTriggerUpperCut()
    {
        if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }

        // Restore time first
        yield return StartCoroutine(RestoreDashAttackSlowTime());

        // Delay before showing UpperCut canvas
        yield return new WaitForSecondsRealtime(upperCutDelayAfterDashCanvas);

        TriggerUpperCutCanvas();
    }

    private IEnumerator DashAttackCanvasSequence()
    {
        if (dashAttackCanvasGroup == null) yield break;

        // Greyscale
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.value = 0f;
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            saturationCoroutine = StartCoroutine(FadeSaturation(0f, -100f, saturationFadeSpeed));
        }

        Time.timeScale = dashAttackSlowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        dashAttackCanvasGroup.gameObject.SetActive(true);

        // Fade in using unscaled time so it works during slow motion
        yield return StartCoroutine(FadeCanvas(dashAttackCanvasGroup, 0f, 1f, dashAttackFadeInSpeed));

        // Hold for display time in real seconds
        yield return new WaitForSecondsRealtime(dashAttackDisplayTime);

        // Fade out
        yield return StartCoroutine(FadeCanvasThenDisable(dashAttackCanvasGroup, 1f, 0f, dashAttackFadeOutSpeed));

        if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }
        // Restore time smoothly after canvas fades out
        yield return StartCoroutine(RestoreDashAttackSlowTime());

        dashAttackCoroutine = null;

        // Delay before showing UpperCut canvas
        yield return new WaitForSecondsRealtime(upperCutDelayAfterDashCanvas);

        // Trigger UpperCut canvas after delay
        TriggerUpperCutCanvas();
    }

    private IEnumerator RestoreDashAttackSlowTime()
    {
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards(
                Time.timeScale, 1f, dashAttackSlowTimeRestoreSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public void TriggerUpperCutCanvas()
    {
        if (!InTutorialMode) return;
        if (hasShownUpperCut) return;

        hasShownUpperCut = true;

        if (upperCutCoroutine != null) StopCoroutine(upperCutCoroutine);
        upperCutCoroutine = StartCoroutine(UpperCutCanvasSequence());
    }

    private IEnumerator UpperCutCanvasSequence()
    {
        if (upperCutCanvasGroup == null) yield break;

        upperCutCanvasGroup.gameObject.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeCanvas(upperCutCanvasGroup, 0f, 1f, upperCutFadeInSpeed));

        // Hold
        yield return new WaitForSecondsRealtime(upperCutDisplayTime);

        // Fade out
        yield return StartCoroutine(FadeCanvasThenDisable(upperCutCanvasGroup, 1f, 0f, upperCutFadeOutSpeed));

        upperCutCoroutine = null;
    }
    public void TriggerAerialComboCanvas()
    {
        if (!InTutorialMode) return;
        if (hasShownAerialCombo) return;
        if (!hasShownUpperCut) return; // Only after uppercut canvas was shown

        hasShownAerialCombo = true;

        if (aerialComboCoroutine != null) StopCoroutine(aerialComboCoroutine);
        aerialComboCoroutine = StartCoroutine(AerialComboCanvasSequence());
    }

    // Called by ZreyAttacks when player taps attack in the air during aerial combo slow window
    public void OnPlayerPerformedAerialAttack()
    {
        if (!InTutorialMode || !hasShownAerialCombo) return;
        if (aerialComboCoroutine == null && Time.timeScale >= 1f) return;

        Debug.Log("<color=lime>Tutorial: Aerial attack performed — restoring time.</color>");

        if (aerialComboCoroutine != null)
        {
            StopCoroutine(aerialComboCoroutine);
            aerialComboCoroutine = null;
        }

        // Fade out canvas immediately
        if (aerialComboCanvasGroup != null && aerialComboCanvasGroup.gameObject.activeSelf)
        {
            StartCoroutine(FadeCanvasThenDisable(
                aerialComboCanvasGroup, aerialComboCanvasGroup.alpha, 0f, aerialComboFadeOutSpeed));
        }
          if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }
        // Restore time
        StartCoroutine(RestoreAerialComboSlowTime());
    }

    private IEnumerator AerialComboCanvasSequence()
    {
        if (aerialComboCanvasGroup == null) yield break;

        // Greyscale
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.value = 0f;
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            saturationCoroutine = StartCoroutine(FadeSaturation(0f, -100f, saturationFadeSpeed));
        }

        Time.timeScale = aerialComboSlowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        aerialComboCanvasGroup.gameObject.SetActive(true);

        yield return StartCoroutine(FadeCanvas(aerialComboCanvasGroup, 0f, 1f, aerialComboFadeInSpeed));

        yield return new WaitForSecondsRealtime(aerialComboDisplayTime);

        yield return StartCoroutine(FadeCanvasThenDisable(
            aerialComboCanvasGroup, 1f, 0f, aerialComboFadeOutSpeed));

        // Restore greyscale before restoring time
        if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }

        yield return StartCoroutine(RestoreAerialComboSlowTime());

        aerialComboCoroutine = null;
    }

    private IEnumerator RestoreAerialComboSlowTime()
    {
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards(
                Time.timeScale, 1f, aerialComboSlowTimeRestoreSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
    public void TriggerCounterSlowTime()
    {
        if (!InTutorialMode) return;
        if (isCounterSlowTimeActive) return;
        if (!HasShownAllCanvases) return;
        if (hasPlayerLearnedCounter) return; // Player already learned — no more slow time

        if (counterSlowTimeCoroutine != null) StopCoroutine(counterSlowTimeCoroutine);
        counterSlowTimeCoroutine = StartCoroutine(CounterSlowTimeSequence());
    }

    // Called when player successfully counters — restores time, clears queued damage
    public void OnPlayerCounteredSuccessfully()
    {
        if (!InTutorialMode || !isCounterSlowTimeActive) return;

        pendingCounterDamageAmount = 0;
        pendingCounterDamageAttacker = null;
        pendingCounterDamageImpact = null;

        hasPlayerLearnedCounter = true; // Never slow time again for counter
        Debug.Log("<color=lime>Tutorial: Counter learned — slow time will not trigger again.</color>");

        RestoreCounterTime();

        // After time restores, chain Jump Attack then Special Attack canvases
        StartCoroutine(ChainPostCounterCanvases());
    }

    private IEnumerator ChainPostCounterCanvases()
    {
        // Wait for time to fully restore before showing canvases
        while (Time.timeScale < 1f)
            yield return null;

        // Delay before Jump Attack canvas
        yield return new WaitForSecondsRealtime(jumpAttackDelayAfterCounter);
        if (!hasShownJumpAttack && jumpAttackCanvasGroup != null)
        {
            hasShownJumpAttack = true;
            jumpAttackCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvas(jumpAttackCanvasGroup, 0f, 1f, jumpAttackFadeInSpeed));
            yield return new WaitForSecondsRealtime(jumpAttackDisplayTime);
            yield return StartCoroutine(FadeCanvasThenDisable(jumpAttackCanvasGroup, 1f, 0f, jumpAttackFadeOutSpeed));
        }

        // Delay before Special Attack canvas
        yield return new WaitForSecondsRealtime(specialAttackDelayAfterJumpAttack);
        if (!hasShownSpecialAttack && specialAttackCanvasGroup != null)
        {
            hasShownSpecialAttack = true;
            specialAttackCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvas(specialAttackCanvasGroup, 0f, 1f, specialAttackFadeInSpeed));
            yield return new WaitForSecondsRealtime(specialAttackDisplayTime);
            yield return StartCoroutine(FadeCanvasThenDisable(specialAttackCanvasGroup, 1f, 0f, specialAttackFadeOutSpeed));
        }
    }

  
    public void TryTriggerFinisherCanvas()
    {
        if (!InTutorialMode) return;
        if (hasShownFinisher) return;
        if (finisherCoroutine != null) StopCoroutine(finisherCoroutine);
        finisherCoroutine = StartCoroutine(FinisherCanvasSequence());
    }

    // Called when player executes the finisher — cancels the canvas display
    public void OnPlayerExecutedFinisher()
    {
        if (finisherCoroutine != null)
        {
            StopCoroutine(finisherCoroutine);
            finisherCoroutine = null;
        }
        if (finisherCanvasGroup != null && finisherCanvasGroup.gameObject.activeSelf)
            StartCoroutine(FadeCanvasThenDisable(finisherCanvasGroup,
                finisherCanvasGroup.alpha, 0f, finisherFadeOutSpeed));
    }

    private IEnumerator FinisherCanvasSequence()
    {
        if (finisherCanvasGroup == null) yield break;
        hasShownFinisher = true;

        finisherCanvasGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvas(finisherCanvasGroup, 0f, 1f, finisherFadeInSpeed));
        yield return new WaitForSecondsRealtime(finisherDisplayTime);
        yield return StartCoroutine(FadeCanvasThenDisable(finisherCanvasGroup,
            1f, 0f, finisherFadeOutSpeed));

        finisherCoroutine = null;
    }
    // Called from ReaperAttack to queue special attack damage instead of applying it
    public void QueueCounterDamage(int damage, Transform attacker, ImpactData impact)
    {
        if (!InTutorialMode || !isCounterSlowTimeActive) return;
        pendingCounterDamageAmount = damage;
        pendingCounterDamageAttacker = attacker;
        pendingCounterDamageImpact = impact;
        Debug.Log($"<color=orange>Tutorial: Counter damage queued ({damage}).</color>");
    }

    public bool IsCounterSlowTimeActive => isCounterSlowTimeActive;

    private IEnumerator CounterSlowTimeSequence()
    {
        isCounterSlowTimeActive = true;

        // Greyscale
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.value = 0f;
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            saturationCoroutine = StartCoroutine(FadeSaturation(0f, -100f, saturationFadeSpeed));
        }

        Time.timeScale = counterSlowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Show the counter attack canvas during the slow time window
        TriggerCounterAttackCanvas();

        float elapsed = 0f;
        while (isCounterSlowTimeActive && elapsed < counterSlowTimeTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isCounterSlowTimeActive)
        {
            RestoreCounterTime();
            yield return new WaitForSecondsRealtime(0.1f);
            FlushCounterDamage();
        }
    }

    private void TriggerCounterAttackCanvas()
    {
        if (counterAttackCanvasGroup == null) return;
        if (hasShownCounterAttack) return;

        hasShownCounterAttack = true;
        if (counterAttackCoroutine != null) StopCoroutine(counterAttackCoroutine);
        counterAttackCoroutine = StartCoroutine(CounterAttackCanvasSequence());
    }

    private IEnumerator CounterAttackCanvasSequence()
    {
        if (counterAttackCanvasGroup == null) yield break;

        counterAttackCanvasGroup.gameObject.SetActive(true);

        yield return StartCoroutine(FadeCanvas(counterAttackCanvasGroup, 0f, 1f, counterAttackFadeInSpeed));

        yield return new WaitForSecondsRealtime(counterAttackDisplayTime);

        yield return StartCoroutine(FadeCanvasThenDisable(
            counterAttackCanvasGroup, 1f, 0f, counterAttackFadeOutSpeed));

        counterAttackCoroutine = null;
    }

    private void RestoreCounterTime()
    {
        isCounterSlowTimeActive = false;

        if (colorAdjustments != null)
        {
            if (saturationCoroutine != null) StopCoroutine(saturationCoroutine);
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.active = false;
        }

        StartCoroutine(RestoreCounterTimeCoroutine());
    }

    private IEnumerator RestoreCounterTimeCoroutine()
    {
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards(
                Time.timeScale, 1f, counterSlowTimeRestoreSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void FlushCounterDamage()
    {
        if (pendingCounterDamageAmount <= 0 || pendingCounterDamageAttacker == null) return;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log($"<color=red>Tutorial: Counter failed — applying queued damage ({pendingCounterDamageAmount}).</color>");
            playerHealth.TakeUnblockableDamage(
                pendingCounterDamageAmount, pendingCounterDamageAttacker, pendingCounterDamageImpact);
        }
        pendingCounterDamageAmount = 0;
        pendingCounterDamageAttacker = null;
        pendingCounterDamageImpact = null;
    }

}