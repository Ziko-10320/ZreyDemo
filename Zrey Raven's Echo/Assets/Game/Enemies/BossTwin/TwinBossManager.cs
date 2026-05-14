using System.Collections;
using UnityEngine;

public class TwinBossManager : MonoBehaviour
{
    [Header("Twin References")]
    [SerializeField] private BootTwinAttack bootAttack;
    [SerializeField] private BootTwinHealth bootHealth;
    [SerializeField] private GauntletTwinAttack gauntletAttack;
    [SerializeField] private GauntletTwinHealth gauntletHealth;

    [Header("Proximity Push Settings")]
    [SerializeField] private float tooCloseDistance = 2.5f;

    [Header("Turn Settings")]
    [SerializeField] private float turnTimeoutDuration = 1.5f; // if token holder doesn't attack within this, pass it

    [Header("Turn Delay")]
    [SerializeField] private float turnHandoffDelay = 0.5f;

    [Header("Sync Combo Settings")]
    [SerializeField] private float syncComboHealthThreshold = 0.5f;
    [Tooltip("How close the twins must be to trigger sync combo.")]
    [SerializeField] private float syncComboMaxDistance = 6f;
    [Tooltip("Cooldown between sync combos.")]
    [SerializeField] private float syncComboCooldown = 20f;
    [Tooltip("Chance per check to trigger sync combo.")]
    [Range(0f, 1f)]
    [SerializeField] private float syncComboChance = 0.4f;
    [Tooltip("How often (seconds) to check if sync combo can trigger.")]
    [SerializeField] private float syncComboCheckInterval = 5f;

    [Header("Sync Combo Snap Offsets")]
    [Tooltip("Where Boot snaps to (world-space offset from center between twins).")]
    [SerializeField] private Vector3 bootSyncSnapOffset = new Vector3(-1.5f, 0f, 0f);
    [Tooltip("Where Gauntlet snaps to (world-space offset from center between twins).")]
    [SerializeField] private Vector3 gauntletSyncSnapOffset = new Vector3(0.5f, 0f, 0f);

    [SerializeField] private BossFightCutscene bossFightCutscene;

    private float turnHandoffTimer = 0f;
    private bool isWaitingToHandoff = false;

    private float turnTimeoutTimer = 0f;
    private bool tokenHolderIsActuallyBusy = false;

    // -1 = nobody has token yet, 0 = boot has token, 1 = gauntlet has token
    private int attackTokenHolder = -1;
    private bool airLaunchTurnBoot = true;
    private bool isCinematicLockActive = false;

    private float syncComboCooldownTimer = 0f;
    private float syncComboCheckTimer = 0f;
    private bool isSyncComboActive = false;

    // Add these animation hash caches
    private static readonly int SyncComboBootHash = Animator.StringToHash("SyncCombo");
    private static readonly int SyncComboGauntletHash = Animator.StringToHash("SyncCombo");

    private bool openingSequenceComplete = false;

    private bool defeatTriggered = false;
    private void Start()
    {
        // Do NOT give a random token or push anything here.
        // The cutscene calls StartFightWithBootLaunch() which sets everything up.
        // Just validate health values.
        if (bootHealth != null && gauntletHealth != null)
        {
            if (bootHealth.GetMaxHealth() != gauntletHealth.GetMaxHealth())
                Debug.LogWarning("[TwinBossManager] Boot and Gauntlet have different maxHealth values!");
        }
    }

    private void Update()
    {  if (!openingSequenceComplete) return;
        if (bootAttack == null || gauntletAttack == null) return;
        if (isCinematicLockActive) return;
        if (!openingSequenceComplete) return;
        bool bootBusy = IsBootBusy();
        bool gauntletBusy = IsGauntletBusy();

        if (attackTokenHolder == 0 && bootHealth != null && bootHealth.isGuardBroken && !bootAttack.IsAirLaunching())
        {
            attackTokenHolder = 1;
            isWaitingToHandoff = false;
            turnHandoffTimer = 0f;
            turnTimeoutTimer = 0f;
            tokenHolderIsActuallyBusy = false;
            PushTokenToAttackScripts();
            Debug.Log("[TwinBossManager] Boot guard broken — token stolen by Gauntlet.");
        }
        else if (attackTokenHolder == 1 && gauntletHealth != null && gauntletHealth.isGuardBroken && !gauntletAttack.IsAirLaunching())
        {
            attackTokenHolder = 0;
            isWaitingToHandoff = false;
            turnHandoffTimer = 0f;
            turnTimeoutTimer = 0f;
            tokenHolderIsActuallyBusy = false;
            PushTokenToAttackScripts();
            Debug.Log("[TwinBossManager] Gauntlet guard broken — token stolen by Boot.");
        }
        syncComboCooldownTimer -= Time.deltaTime;
        syncComboCheckTimer -= Time.deltaTime;

        if (!isSyncComboActive && !isCinematicLockActive && syncComboCooldownTimer <= 0f && syncComboCheckTimer <= 0f)
        {
            syncComboCheckTimer = syncComboCheckInterval;
            TryTriggerSyncCombo();
        }

        // Track if the current token holder started doing something
        if (attackTokenHolder == 0 && bootBusy) tokenHolderIsActuallyBusy = true;
        if (attackTokenHolder == 1 && gauntletBusy) tokenHolderIsActuallyBusy = true;

        // ── Token handoff: if the token holder just FINISHED an attack, pass it ──
        // ── Token handoff: if the token holder just FINISHED an attack, start the delay ──
        if (attackTokenHolder == 0 && tokenHolderIsActuallyBusy && !bootBusy && !isWaitingToHandoff)
        {
            isWaitingToHandoff = true;
            turnHandoffTimer = 0f;
            tokenHolderIsActuallyBusy = false;
            bootAttack.HoldAttack(true); // keep boot locked during delay
        }
        else if (attackTokenHolder == 1 && tokenHolderIsActuallyBusy && !gauntletBusy && !isWaitingToHandoff)
        {
            isWaitingToHandoff = true;
            turnHandoffTimer = 0f;
            tokenHolderIsActuallyBusy = false;
            gauntletAttack.HoldAttack(true); // keep gauntlet locked during delay
        }

        // ── Count down the handoff delay then actually pass the token ──
        if (isWaitingToHandoff)
        {
            turnTimeoutTimer = 0f;
            turnHandoffTimer += Time.deltaTime;
            if (turnHandoffTimer >= turnHandoffDelay)
            {
                attackTokenHolder = GetNextTokenHolder();
                turnHandoffTimer = 0f;
                tokenHolderIsActuallyBusy = false;
                isWaitingToHandoff = false;
                turnTimeoutTimer = 0f; // ← ADD: Gauntlet gets fresh full timeout window
                PushTokenToAttackScripts();
                Debug.Log("[TwinBossManager] Token handed off to: " + (attackTokenHolder == 0 ? "Boot" : "Gauntlet"));
            }
        }

        // ── Timeout: only count down if NOT waiting for handoff ──
        bool currentHolderIdle = (attackTokenHolder == 0 && !bootBusy) ||
                          (attackTokenHolder == 1 && !gauntletBusy);

        // ← ADD: don't timeout if we're mid-handoff OR if holder just finished (tokenHolderIsActuallyBusy was true)
        if (currentHolderIdle && !tokenHolderIsActuallyBusy && !isWaitingToHandoff)
        {
            turnTimeoutTimer += Time.deltaTime;
            if (turnTimeoutTimer >= turnTimeoutDuration)
            {
                attackTokenHolder = GetNextTokenHolder();
                turnTimeoutTimer = 0f;
                tokenHolderIsActuallyBusy = false;
                isWaitingToHandoff = false;
                PushTokenToAttackScripts();
                Debug.Log("[TwinBossManager] Turn timeout — passing token to other twin.");
            }
        }
        else if (!isWaitingToHandoff) // ← only reset timer when NOT in handoff
        {
            turnTimeoutTimer = 0f;
        }

        // ── Proximity push ──
        if (!isSyncComboActive)
        {
            float dist = Vector2.Distance(bootAttack.transform.position, gauntletAttack.transform.position);
            if (dist < tooCloseDistance)
            {
                if (IsGauntletAttackingOrLaunching() && !IsBootBusy())
                    bootAttack.TryEvasiveRetreat();
                else if (IsBootAttackingOrLaunching() && !IsGauntletBusy())
                    gauntletAttack.TryEvasiveRetreat();
            }
        }
    }
    private int GetNextTokenHolder()
    {
        // Gauntlet (1) gets token 65% of the time, Boot (0) gets 35%
        if (attackTokenHolder == 0) return 1;           // Boot just went → always give Gauntlet
        return Random.value < 0.4f ? 0 : 1;            // Gauntlet just went → 65% chance Gauntlet again
    }
    /// <summary>Tells each attack script whether they are allowed to attack right now.</summary>
    private void PushTokenToAttackScripts()
    {
        bootAttack.HoldAttack(false);
        gauntletAttack.HoldAttack(false);

        bootAttack.HoldAttack(attackTokenHolder != 0);
        gauntletAttack.HoldAttack(attackTokenHolder != 1);

        Debug.Log($"[Token] Boot held={attackTokenHolder != 0}  Gauntlet held={attackTokenHolder != 1}");
    }

    /// <summary>Called by either attack script before starting an air launch.</summary>
    public bool RequestAirLaunch(bool isBootTwin)
    {
        // If the OTHER twin is already air launching, block it completely
        if (isBootTwin && gauntletAttack.IsAirLaunching()) return false;
        if (!isBootTwin && bootAttack.IsAirLaunching()) return false;

        // If the other twin is busy with something else, always allow
        bool otherIsBusy = isBootTwin ? IsGauntletBusy() : IsBootBusy();
        if (otherIsBusy) return true;

        // Alternate turns
        if (isBootTwin && airLaunchTurnBoot) { airLaunchTurnBoot = false; return true; }
        if (!isBootTwin && !airLaunchTurnBoot) { airLaunchTurnBoot = true; return true; }

        bool otherCanAirLaunch = isBootTwin
            ? (gauntletHealth == null || (!gauntletHealth.isGuardBroken && !gauntletHealth.isFinishable))
            : (bootHealth == null || (!bootHealth.isGuardBroken && !bootHealth.isFinishable));

        if (!otherCanAirLaunch) return true;

        return false;
    }

    // ── State helpers ──
    private bool IsBootAttackingOrLaunching()
    {
        return bootAttack.IsAttacking() || bootAttack.IsLaunching() ||
               bootAttack.IsAirLaunching() || bootAttack.IsThrowingRocks() ||
               bootAttack.IsSpecialAttacking();
    }

    private bool IsGauntletAttackingOrLaunching()
    {
        return gauntletAttack.IsAttacking() || gauntletAttack.IsLaunching() ||
               gauntletAttack.IsAirLaunching();
    }

    private bool IsBootBusy()
    {
        return IsBootAttackingOrLaunching()
            || (bootHealth != null && (bootAttack.isBeingCountered || bootHealth.isGuardBroken))
            || (bootHealth != null && bootAttack.isBeingCountered); // ADD — covers health-side flag too
    }

    private bool IsGauntletBusy()
    {
        return IsGauntletAttackingOrLaunching()
            || (gauntletHealth != null && (gauntletHealth.isBeingCountered || gauntletHealth.isGuardBroken))
            || (gauntletAttack != null && gauntletAttack.isBeingCountered); // ADD
    }
    public void NotifyAttackEnded(bool wasBootTwin, bool forced = false)
    {
        if (isCinematicLockActive) return;
        if (isWaitingToHandoff && !forced) return;

        isWaitingToHandoff = false;
        turnHandoffTimer = 0f;
        turnTimeoutTimer = 0f;          // ← fresh window for new holder
        tokenHolderIsActuallyBusy = false;
        attackTokenHolder = wasBootTwin ? 1 : GetNextTokenHolder();
        PushTokenToAttackScripts();
        Debug.Log("[TwinBossManager] Attack ended — token now: " + (attackTokenHolder == 0 ? "Boot" : "Gauntlet"));
    }
    public void SetCinematicLock(bool locked, bool suppressTokenPush = false)
    {
        isCinematicLockActive = locked;
        if (locked)
        {
            isWaitingToHandoff = false;
            turnHandoffTimer = 0f;
            bootAttack.HoldAttack(true);
            gauntletAttack.HoldAttack(true);
            Debug.Log("[TwinBossManager] CINEMATIC LOCK — both twins held.");
        }
        else
        {
            if (!suppressTokenPush)
                PushTokenToAttackScripts();
            Debug.Log("[TwinBossManager] CINEMATIC LOCK released.");
        }
    }

    private void TryTriggerSyncCombo()
    {
        if (bootHealth == null || gauntletHealth == null) return;
        if (bootHealth.isFinishable || gauntletHealth.isFinishable) return;
        if (bootHealth.isGuardBroken || gauntletHealth.isGuardBroken) return;

        // Check health threshold — use boot's health since it's shared
        float healthPercent = (float)bootHealth.GetCurrentHealth() / bootHealth.GetMaxHealth();
        if (healthPercent > syncComboHealthThreshold) return;

        // Check distance
        float dist = Vector2.Distance(bootAttack.transform.position, gauntletAttack.transform.position);
        if (dist > syncComboMaxDistance) return;

        if (Random.value > syncComboChance) return;

        StartCoroutine(ExecuteSyncCombo());
    }

    private IEnumerator ExecuteSyncCombo()
    {
        isSyncComboActive = true;
        syncComboCooldownTimer = syncComboCooldown;

        SetCinematicLock(true);

        bootAttack.ForceResetAttackState();
        gauntletAttack.ForceResetAttackState();

        yield return null;

        // ── Snap Boot behind Gauntlet, relative to where Gauntlet already is ──
        bool gauntletFacingRight = gauntletAttack.isFacingRight;
        float behindOffset = gauntletFacingRight ? -1.5f : 1.5f; // Boot is behind Gauntlet
        bootAttack.transform.position = new Vector3(
            gauntletAttack.transform.position.x + behindOffset,
            gauntletAttack.transform.position.y,
            gauntletAttack.transform.position.z
        );

        // ── Force BOTH to face the same direction as Gauntlet already faces ──
        bootAttack.SetFacingDirect(gauntletFacingRight);
        gauntletAttack.SetFacingDirect(gauntletFacingRight);

        yield return null;

        bootAttack.PlaySyncCombo();
        gauntletAttack.PlaySyncCombo();

        Debug.Log("[TwinBossManager] SYNC COMBO triggered!");
    }

    public void NotifySyncComboFinished()
    {
        isSyncComboActive = false;
        SetCinematicLock(false);
        Debug.Log("[TwinBossManager] Sync combo finished.");
    }

    public bool IsSyncComboActive() => isSyncComboActive;
    public void StartFightWithBootLaunch()
    {
        openingSequenceComplete = false;
        isCinematicLockActive = false;
        attackTokenHolder = 0;
        tokenHolderIsActuallyBusy = false;
        isWaitingToHandoff = false;
        turnHandoffTimer = 0f;
        turnTimeoutTimer = 0f;

        bootAttack.HoldAttack(false);
        gauntletAttack.HoldAttack(true);

        // ADD: slam gauntlet's air launch cooldown so it physically cannot fire during boot's opening
        gauntletAttack.BlockAirLaunchDuringOpening(); // NEW method below

        Debug.Log("[TwinBossManager] Fight started — Boot launching, Gauntlet held.");
    }
    public void NotifyBootOpeningLaunchFinished()
    {
        openingSequenceComplete = true;
        attackTokenHolder = 1;
        tokenHolderIsActuallyBusy = false;
        isWaitingToHandoff = false;
        turnTimeoutTimer = 0f;
        gauntletAttack.NotifyOpeningSequenceComplete();
        gauntletAttack.ResetCooldownForFightStart();
        // Give Gauntlet a zeroed cooldown so it attacks immediately
        PushTokenToAttackScripts();
        Debug.Log("[TwinBossManager] Boot opening launch done — Gauntlet now free.");
    }
    public void ResetGauntletCooldownForFightStart()
    {
        gauntletAttack.ResetCooldownForFightStart();
    }

    public void NotifyTwinDefeated()
    {
        if (defeatTriggered) return;
        if (bootHealth == null || gauntletHealth == null) return;

        // Use <= 0 check with a 1-frame delay to let shared damage propagate
        StartCoroutine(CheckBothDefeated());
    }
    private IEnumerator CheckBothDefeated()
    {
        // Wait one frame so shared damage has propagated to both twins
        yield return null;

        if (bootHealth.GetCurrentHealth() <= 0 && gauntletHealth.GetCurrentHealth() <= 0)
        {
            if (defeatTriggered) yield break;
            defeatTriggered = true;

            // Disable BOTH attack scripts here in the manager
            if (bootAttack != null) bootAttack.enabled = false;
            if (gauntletAttack != null) gauntletAttack.enabled = false;

            Debug.Log("[TwinBossManager] Both twins defeated — triggering defeat sequence.");
            if (bossFightCutscene != null)
                bossFightCutscene.TriggerDefeatSequence();
        }
    }
    public bool BothTwinsDefeated()
    {
        if (bootHealth == null || gauntletHealth == null) return false;
        return bootHealth.GetCurrentHealth() <= 0 && gauntletHealth.GetCurrentHealth() <= 0;
    }
}