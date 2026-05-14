using System.Collections;
using UnityEngine;

public class BossFightCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZreyMovements playerMovement;
    [SerializeField] private ZreyAttacks playerAttacks;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TwinBossManager twinBossManager;
    [SerializeField] private BootTwinAttack bootAttack;
    [SerializeField] private Animator bootAnimator;
    [SerializeField] private Animator gauntletAnimator;

    [Header("Cutscene Settings")]
    [SerializeField] private Transform fightStartPoint;
    [SerializeField] private float playerAutoRunSpeed = 5f;
    [SerializeField] private float delayBeforeBootLaunch = 1.5f;

    [Header("Defeat Cutscene")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;
    [SerializeField] private float blackScreenFadeInDuration = 0.5f;
    [SerializeField] private float blackScreenHoldDuration = 1.5f;
    [SerializeField] private float blackScreenFadeOutDuration = 0.5f;
    [SerializeField] private float delayAfterFadeOutBeforeAnims = 0.4f; // the 0.3-0.5s delay you want
    [SerializeField] private float delayBeforeFlip = 1.5f; // 1-2s after GetKnockback finishes
    [SerializeField] private float delayBeforeFinalCanvas = 5f;
    [SerializeField] private CanvasGroup finalCanvasGroup;
    [SerializeField] private float finalCanvasFadeInDuration = 1f;

    [Header("Defeat Snap Points (X only)")]
    [SerializeField] private Transform playerSnapPoint;
    [SerializeField] private Transform bootSnapPoint;
    [SerializeField] private Transform gauntletSnapPoint;
    [SerializeField] private Transform bootTransform;
    [SerializeField] private Transform gauntletTransform;

    [Header("Defeat Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private BootTwinAttack bootAttackRef;
    [SerializeField] private GauntletTwinAttack gauntletAttackRef;

    private static readonly int ParryLongHash = Animator.StringToHash("ParryLong");
    private static readonly int GetKnockbackHash = Animator.StringToHash("GetKnockback");
    private static readonly int RunAwayHash = Animator.StringToHash("RunAway");

    private bool defeatSequenceStarted = false;

    private static readonly int BeginCutSceneHash = Animator.StringToHash("BeginCutScene");
    private void Awake()
    {
        // Lock both twins IMMEDIATELY — before any Update() runs on them
        if (twinBossManager != null)
            twinBossManager.SetCinematicLock(true);
    }
    private void Start()
    {
        StartCoroutine(PlayOpeningCutscene());
    }

    private IEnumerator PlayOpeningCutscene()
    {
      
        // --- 1. LOCK PLAYER ---
        playerAttacks.IsInCinematicState_ForceSet(true);
        if (playerHealth != null) playerHealth.MakeInvincible();

        // --- 2. LOCK BOTH TWINS ---
        twinBossManager.SetCinematicLock(true);

        // --- 3. PLAY ENEMY CUTSCENE ANIMATIONS ---
        bootAnimator.SetTrigger(BeginCutSceneHash);
        gauntletAnimator.SetTrigger(BeginCutSceneHash);

        // --- 4. FORCE COMBAT MODE SO isMovingForward WORKS ---
        bool shouldFaceRight = fightStartPoint.position.x > playerMovement.transform.position.x;
        playerMovement.ForceAutoRun(shouldFaceRight);
        playerMovement.ForceEnterCombatRunAnimation(shouldFaceRight); // NEW — see ZreyMovements change

        // Wait until close enough on X
        while (Mathf.Abs(playerMovement.transform.position.x - fightStartPoint.position.x) > 0.15f)
        {
            yield return null;
        }

        // --- 5. STOP AND CLEAR COMBAT RUN ANIMATION ---
        playerMovement.StopAutoRun();
        playerMovement.ForceExitCombatRunAnimation(); // NEW — see ZreyMovements change

        // --- 6. WAIT BEFORE BOOT LAUNCHES ---
        yield return new WaitForSeconds(delayBeforeBootLaunch);

        // --- 7. UNLOCK PLAYER ---
        playerAttacks.IsInCinematicState_ForceSet(false);
        if (playerHealth != null) playerHealth.MakeVulnerable();

        playerAttacks.ForceResetState();       // clears ALL attack flags
        playerMovement.ForceResetState();
        // --- 8. START FIGHT ---
        twinBossManager.StartFightWithBootLaunch();
        yield return null;
        bootAttack.ForceStartAnticipationLaunch();
    }

    public void TriggerDefeatSequence()
    {
        if (defeatSequenceStarted) return;
        defeatSequenceStarted = true;
        StartCoroutine(PlayDefeatCutscene());
    }

    private IEnumerator PlayDefeatCutscene()
    {
        // --- 1. LOCK PLAYER INPUT/MOVEMENT ---
        playerAttacks.IsInCinematicState_ForceSet(true);
        playerMovement.CanMove = false;
        playerMovement.canFlip = false;
        if (playerHealth != null) playerHealth.MakeInvincible();
        Rigidbody2D bootRb = bootTransform.GetComponent<Rigidbody2D>();
        Rigidbody2D gauntletRb = gauntletTransform.GetComponent<Rigidbody2D>();

        if (bootRb != null)
        {
            bootRb.linearVelocity = Vector2.zero;
            bootRb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (gauntletRb != null)
        {
            gauntletRb.linearVelocity = Vector2.zero;
            gauntletRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Stop ALL coroutines on the attack scripts to kill any mid-air dashes,
        // grabs, backsteps etc that are still running
        if (bootAttackRef != null)
        {
            bootAttackRef.StopAllCoroutines();
            bootAttackRef.ForceResetAttackState();
            bootAttackRef.enabled = false;
        }
        if (gauntletAttackRef != null)
        {
            gauntletAttackRef.StopAllCoroutines();
            gauntletAttackRef.ForceResetAttackState();
            gauntletAttackRef.enabled = false;
        }

        // Also stop coroutines on the health scripts so no stun/knockback
        // sequences are still running and fighting the animator
        BootTwinHealth bootHealthComp = bootTransform.GetComponent<BootTwinHealth>();
        GauntletTwinHealth gauntletHealthComp = gauntletTransform.GetComponent<GauntletTwinHealth>();
        if (bootHealthComp != null) bootHealthComp.StopAllCoroutines();
        if (gauntletHealthComp != null) gauntletHealthComp.StopAllCoroutines();


        // Attack scripts already disabled by TwinBossManager
        if (twinBossManager != null) twinBossManager.SetCinematicLock(true);

        // --- 3. FADE IN BLACK SCREEN ---
        if (blackScreenCanvasGroup == null)
        {
            Debug.LogError("BossFightCutscene: blackScreenCanvasGroup is not assigned!");
            yield break;
        }
        blackScreenCanvasGroup.gameObject.SetActive(true);
        blackScreenCanvasGroup.alpha = 0f; // ADD THIS — guarantee it starts at 0
        yield return StartCoroutine(FadeCanvasGroup(blackScreenCanvasGroup, 0f, 1f, blackScreenFadeInDuration));

        // --- 4. HOLD BLACK SCREEN & SNAP POSITIONS (X only) ---
        yield return new WaitForSeconds(blackScreenHoldDuration);

        if (playerSnapPoint != null)
            playerMovement.transform.position = new Vector3(playerSnapPoint.position.x, playerMovement.transform.position.y, playerMovement.transform.position.z);
        if (bootSnapPoint != null && bootTransform != null)
            bootTransform.position = new Vector3(bootSnapPoint.position.x, bootTransform.position.y, bootTransform.position.z);
        if (gauntletSnapPoint != null && gauntletTransform != null)
            gauntletTransform.position = new Vector3(gauntletSnapPoint.position.x, gauntletTransform.position.y, gauntletTransform.position.z);

        // --- 5. FADE OUT BLACK SCREEN ---
        if (playerSnapPoint != null)
            playerMovement.transform.position = new Vector3(playerSnapPoint.position.x, playerMovement.transform.position.y, playerMovement.transform.position.z);
        if (bootSnapPoint != null && bootTransform != null)
            bootTransform.position = new Vector3(bootSnapPoint.position.x, bootTransform.position.y, bootTransform.position.z);
        if (gauntletSnapPoint != null && gauntletTransform != null)
            gauntletTransform.position = new Vector3(gauntletSnapPoint.position.x, gauntletTransform.position.y, gauntletTransform.position.z);

        // Start fade-out coroutine WITHOUT yielding — let it run in parallel
        StartCoroutine(FadeCanvasGroup(blackScreenCanvasGroup, 1f, 0f, blackScreenFadeOutDuration));

        // Wait the short delay DURING the fade-out, then fire animations
        yield return new WaitForSeconds(delayAfterFadeOutBeforeAnims);

        if (playerAnimator != null) playerAnimator.SetTrigger(ParryLongHash);
        if (bootAnimator != null) bootAnimator.SetTrigger(GetKnockbackHash);
        if (gauntletAnimator != null) gauntletAnimator.SetTrigger(GetKnockbackHash);

        // Wait for fade-out to fully finish before continuing (subtract the delay already waited)
        float remainingFadeTime = blackScreenFadeOutDuration - delayAfterFadeOutBeforeAnims;
        if (remainingFadeTime > 0f)
            yield return new WaitForSeconds(remainingFadeTime);

        // --- 7. WAIT FOR KNOCKBACK ANIMS TO FINISH THEN FLIP + RUNAWAY ---
        yield return new WaitForSeconds(delayBeforeFlip);

        // Flip boot away from player
        if (bootAttackRef != null)
        {
            bool bootShouldFaceAwayRight = bootTransform.position.x > playerMovement.transform.position.x;
            bootAttackRef.SetFacingDirect(bootShouldFaceAwayRight); // faces AWAY = same side as their position relative to player
        }
        if (gauntletAttackRef != null)
        {
            bool gauntletShouldFaceAwayRight = gauntletTransform.position.x > playerMovement.transform.position.x;
            gauntletAttackRef.SetFacingDirect(gauntletShouldFaceAwayRight);
        }

        if (bootAnimator != null) bootAnimator.SetTrigger(RunAwayHash);
        if (gauntletAnimator != null) gauntletAnimator.SetTrigger(RunAwayHash);

        // --- 8. WAIT THEN SHOW FINAL CANVAS ---
        yield return new WaitForSeconds(delayBeforeFinalCanvas);

        if (finalCanvasGroup != null)
        {
            finalCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(finalCanvasGroup, 0f, 1f, finalCanvasFadeInDuration));
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float timer = 0f;
        cg.alpha = from;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}