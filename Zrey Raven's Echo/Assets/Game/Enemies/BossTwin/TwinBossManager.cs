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
    private float turnHandoffTimer = 0f;
    private bool isWaitingToHandoff = false;

    private float turnTimeoutTimer = 0f;
    private bool tokenHolderIsActuallyBusy = false;

    // -1 = nobody has token yet, 0 = boot has token, 1 = gauntlet has token
    private int attackTokenHolder = -1;
    private bool airLaunchTurnBoot = true;
    private bool isCinematicLockActive = false;
    private void Start()
    {
        // Give the token to a random twin at start
        attackTokenHolder = Random.value < 0.5f ? 0 : 1;
        PushTokenToAttackScripts();

        if (bootHealth != null && gauntletHealth != null)
        {
            if (bootHealth.GetMaxHealth() != gauntletHealth.GetMaxHealth())
                Debug.LogWarning("[TwinBossManager] Boot and Gauntlet have different maxHealth values! Shared health will desync.");
        }
    }

    private void Update()
    {
        if (bootAttack == null || gauntletAttack == null) return;
        if (isCinematicLockActive) return;
        bool bootBusy = IsBootBusy();
        bool gauntletBusy = IsGauntletBusy();

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
            turnHandoffTimer += Time.deltaTime;
            if (turnHandoffTimer >= turnHandoffDelay)
            {
                attackTokenHolder = (attackTokenHolder == 0) ? 1 : 0;
                turnHandoffTimer = 0f;
                isWaitingToHandoff = false;
                PushTokenToAttackScripts();
            }
        }

        // Track if the current token holder started doing something
        if (attackTokenHolder == 0 && bootBusy) tokenHolderIsActuallyBusy = true;
        if (attackTokenHolder == 1 && gauntletBusy) tokenHolderIsActuallyBusy = true;

        // ── Timeout: if token holder hasn't attacked within the window, steal the turn ──
        bool currentHolderIdle = (attackTokenHolder == 0 && !bootBusy) ||
                                 (attackTokenHolder == 1 && !gauntletBusy);

        if (currentHolderIdle && !tokenHolderIsActuallyBusy)
        {
            turnTimeoutTimer += Time.deltaTime;
            if (turnTimeoutTimer >= turnTimeoutDuration)
            {
                // Pass the token to the other twin
                attackTokenHolder = (attackTokenHolder == 0) ? 1 : 0;
                turnTimeoutTimer = 0f;
                tokenHolderIsActuallyBusy = false;
                PushTokenToAttackScripts();
                Debug.Log("[TwinBossManager] Turn timeout — passing token to other twin.");
            }
        }
        else
        {
            turnTimeoutTimer = 0f;
        }

        // ── Proximity push ──
        float dist = Vector2.Distance(bootAttack.transform.position, gauntletAttack.transform.position);
        if (dist < tooCloseDistance)
        {
            if (IsGauntletAttackingOrLaunching() && !IsBootBusy())
                bootAttack.TryEvasiveRetreat();
            else if (IsBootAttackingOrLaunching() && !IsGauntletBusy())
                gauntletAttack.TryEvasiveRetreat();
        }
    }

    /// <summary>Tells each attack script whether they are allowed to attack right now.</summary>
    private void PushTokenToAttackScripts()
    {
        bootAttack.HoldAttack(attackTokenHolder != 0);
        gauntletAttack.HoldAttack(attackTokenHolder != 1);
    }

    /// <summary>Called by either attack script before starting an air launch.</summary>
    public bool RequestAirLaunch(bool isBootTwin)
    {
        // Always allow if the other twin is busy with something else
        bool otherIsBusy = isBootTwin ? IsGauntletBusy() : IsBootBusy();
        if (otherIsBusy) return true;

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
    public void NotifyAttackEnded(bool wasBootTwin)
    {
        if (isCinematicLockActive) return;
        attackTokenHolder = wasBootTwin ? 1 : 0;
        turnTimeoutTimer = 0f;
        tokenHolderIsActuallyBusy = false;
        PushTokenToAttackScripts();
    }
    public void SetCinematicLock(bool locked)
    {
        isCinematicLockActive = locked;
        if (locked)
        {
            isWaitingToHandoff = false; // ADD — cancel any pending handoff
            turnHandoffTimer = 0f;      // ADD — reset the timer too
            bootAttack.HoldAttack(true);
            gauntletAttack.HoldAttack(true);
            Debug.Log("[TwinBossManager] CINEMATIC LOCK — both twins held.");
        }
        else
        {
            PushTokenToAttackScripts();
            Debug.Log("[TwinBossManager] CINEMATIC LOCK released.");
        }
    }
}