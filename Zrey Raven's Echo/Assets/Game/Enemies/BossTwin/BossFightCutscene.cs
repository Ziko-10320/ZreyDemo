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
}