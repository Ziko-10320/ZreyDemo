using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health & UI")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject deathPanel;
    private int currentHealth;

    [Header("Posture/Shield UI")]
    [SerializeField] private int maxShieldHealth = 100;
    [SerializeField] private float shieldRegenDelay = 2.5f;
    [SerializeField] private float shieldRegenRate = 20f;
    [SerializeField] private float guardBreakStunDuration = 3f;



    [Header("Impact & VFX")]
    [SerializeField] private GameObject bloodVFX;
    [SerializeField] private Transform bloodSpawnPoint;

    // --- Private Components & State ---
    private Rigidbody2D rb;
    private Animator animator;
    private ZreyAttacks playerAttacks;
    private Coroutine knockbackCoroutine;

    // --- Animation Hashes ---
    private readonly int getHitBackTriggerHash = Animator.StringToHash("getHitBack");
    private readonly int getHitDownTriggerHash = Animator.StringToHash("getHitDown");
    private readonly int getHitFinalBackTriggerHash = Animator.StringToHash("finalBack");
    private readonly int getHitFallTriggerHash = Animator.StringToHash("Hitfall");
    private readonly int deathTriggerHash = Animator.StringToHash("death");
    private ZreyMovements playerMovements;

    [Header("Defense & Parry")]
    [Tooltip("How much damage is blocked (e.g., 0.75 means 75% of damage is ignored).")]
    [Range(0f, 1f)] public float damageReduction = 0.75f;
    [Tooltip("How long the parry window stays open after starting a block (in seconds).")]
    public float parryWindow = 0.15f;
    [Tooltip("How long the player is stunned and cannot move after taking a normal hit.")]
    public float hitStunDuration = 0.5f;
    [Tooltip("The VFX to spawn on a successful block.")]
    public GameObject blockVFX;
    [Tooltip("The VFX to spawn on a successful parry.")]
    public GameObject parryVFX;
    [Tooltip("The point where block/parry VFX should spawn.")]
    public Transform defenseVFXSpawnPoint;
    private InputSystem_Actions inputActions;
    private bool isBlocking = false;
    private bool isParryWindowActive = false;
    private Coroutine parryWindowCoroutine;

    private readonly int startBlockTriggerHash = Animator.StringToHash("startBlock");
    private readonly int stopBlockTriggerHash = Animator.StringToHash("stopBlock");
    private readonly int parry1TriggerHash = Animator.StringToHash("parry1");
    private readonly int parry2TriggerHash = Animator.StringToHash("parry2");
    public ShakeData CameraShakeParry;

    [SerializeField] private CheckpointManager checkpointManager;
    public bool isStunned = false;
    public bool isBeingKnockedBack { get; private set; } = false;
    private readonly int getGrabbedTriggerHash = Animator.StringToHash("GetGrabbed");

    [Header("Grab VFX")]
    [SerializeField] private GameObject stabBloodVFX;
    [SerializeField] private Transform stabBloodSpawnPoint;
    public bool IsGrabbed { get; private set; } = false;
    public bool IsInvincible { get; private set; } = false;

 

    private int currentShieldHealth;
    private bool isShieldBroken = false;
    private Coroutine shieldRegenCoroutine;
    private Coroutine guardBreakCoroutine;
    private readonly int guardBrokenTriggerHash = Animator.StringToHash("guardBroken");
    private readonly int isWeakBoolHash = Animator.StringToHash("isWeak");
    private readonly int recoverShieldTriggerHash = Animator.StringToHash("recoverShield");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAttacks = GetComponent<ZreyAttacks>();
        playerMovements = GetComponent<ZreyMovements>();

        if (rb == null) Debug.LogError("FATAL ERROR: Rigidbody2D is missing on Player!", this);
        if (animator == null) Debug.LogError("FATAL ERROR: Animator is missing on Player!", this);
        if (playerAttacks == null) Debug.LogError("FATAL ERROR: ZreyAttacks script is missing on Player!", this);

        inputActions = new InputSystem_Actions();
        if (checkpointManager == null) checkpointManager = FindFirstObjectByType<CheckpointManager>();
 
        currentShieldHealth = maxShieldHealth;
      
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Block.started += HandleBlockInput;
        inputActions.Player.Block.canceled += HandleBlockInput;
    }

   

    private void OnDisable()
    {
        inputActions.Player.Block.started -= HandleBlockInput;
        inputActions.Player.Block.canceled -= HandleBlockInput;
        inputActions.Disable();
    }

    private void HandleBlockInput(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
            StartBlocking();
        else
            StopBlocking();
    }

    public void UpdateBlockBinding(string newBindingPath)
    {
        if (inputActions == null) return;
        Debug.Log($"<color=yellow>PLAYER HEALTH RECEIVED A NEW BINDING: {newBindingPath}</color>");
        inputActions.Player.Block.ApplyBindingOverride(0, newBindingPath);
    }

    void Start()
    {
        currentHealth = maxHealth;
       
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void MakeInvincible()
    {
        Debug.Log("<color=cyan>--- PLAYER IS NOW INVINCIBLE ---</color>");
        IsInvincible = true;
    }

    public void MakeVulnerable()
    {
        Debug.Log("<color=grey>--- Player is now VULNERABLE ---</color>");
        IsInvincible = false;
    }

    public void TakeDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        if (IsInvincible) { Debug.Log("Damage ignored: Player is invincible."); return; }
        if (currentHealth <= 0) return;

        if (isParryWindowActive)
        {
            Debug.Log("<color=lime>PARRY SUCCESSFUL!</color>");
            CameraShakerHandler.Shake(CameraShakeParry);
            isBlocking = false;

            int parryAnim = Random.Range(0, 2);
            animator.SetTrigger(parryAnim == 0 ? parry1TriggerHash : parry2TriggerHash);

            if (parryVFX != null) Instantiate(parryVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
            playerMovements.CanMove = true;

            ImpactData parryRecoilImpact = ScriptableObject.CreateInstance<ImpactData>();
            parryRecoilImpact.knockbackDistance = impact.parryKnockbackDistance;
            parryRecoilImpact.knockbackDuration = impact.parryKnockbackDuration;
            parryRecoilImpact.hitReactionType = "none";

            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryRecoilImpact));

            KnightAttack enemyAttack = attacker.GetComponent<KnightAttack>();
            KnightHealth enemyHealth = attacker.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakePostureDamageOnParry();
                if (enemyAttack != null && enemyAttack.IsFinalComboAttack())
                {
                    Debug.Log("<color=lime>PARRIED THE FINAL ATTACK! Stunning the knight!</color>");
                    enemyHealth.GetParried(transform);
                }
                else Debug.Log("<color=yellow>Parried a normal attack. Knight is knocked back but not stunned.</color>");
            }

            SpearAttack SAttack = attacker.GetComponent<SpearAttack>();
            SpearHealth SHealth = attacker.GetComponent<SpearHealth>();
            if (SHealth != null)
            {
                SHealth.TakePostureDamageOnParry();
                if (SAttack != null && SAttack.IsFinalComboAttack())
                {
                    Debug.Log("<color=lime>PARRIED THE FINAL ATTACK! Stunning the knight!</color>");
                    SHealth.GetParried(transform);
                }
                else Debug.Log("<color=yellow>Parried a normal attack. Knight is knocked back but not stunned.</color>");
            }
            return;
        }

        if (isBlocking)
        {
            if (isShieldBroken)
            {
                // Shield is broken, fall through to take full damage
            }
            else
            {
                Debug.Log("<color=cyan>BLOCK SUCCESSFUL!</color>");
                CameraShakerHandler.Shake(CameraShakeParry);

                currentShieldHealth -= damageAmount;
                currentShieldHealth = Mathf.Max(0, currentShieldHealth);
                // FIX: This now correctly sets slider via normalized value
                if (blockVFX != null) Instantiate(blockVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
                if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

                ImpactData parryImpact = ScriptableObject.CreateInstance<ImpactData>();
                parryImpact.knockbackDistance = impact.knockbackDistance;
                parryImpact.knockbackDuration = impact.knockbackDuration;
                parryImpact.hitReactionType = "none";
                knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryImpact));

                if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);

                if (currentShieldHealth <= 0)
                {
                    StartCoroutine(GuardBreakRoutine());
                }
                else
                {
                    // --- THIS IS THE GUARANTEED FIX ---
                    // ALWAYS start a new regen timer after taking shield damage.
                    shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
                    // --- END OF THE GUARANTEED FIX ---
                }

                return;
            }
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
       

        if (impact == null) { Debug.LogWarning("TakeDamage was called with null ImpactData!"); return; }
        Debug.Log($"<color=red>PLAYER TOOK DAMAGE. Health: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0) { Die(attacker); return; }

        SpawnBlood();
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, impact));
    }

    public void TakeUnblockableDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        if (IsInvincible) { Debug.Log("Damage ignored: Player is invincible."); return; }
        if (currentHealth <= 0) return;

        Debug.LogWarning($"<color=red>!!! PLAYER TOOK UNBLOCKABLE DAMAGE: {damageAmount} !!!</color>");

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
       

        if (currentHealth <= 0) { Die(attacker); return; }

        SpawnBlood();
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, impact));
    }

    public void TakeHazardDamage(int damageAmount)
    {
        if (IsInvincible) { Debug.Log("Damage ignored: Player is invincible."); return; }
        if (currentHealth <= 0) return;

        Debug.LogWarning($"<color=orange>PLAYER HIT A HAZARD! Taking {damageAmount} damage.</color>");

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        

        if (currentHealth <= 0)
            Die(null);
        else
        {
            Debug.Log("Player is hurt by hazard. Respawning at MINI checkpoint.");
            if (checkpointManager != null) checkpointManager.RespawnAtMiniCheckpoint();
        }
    }

    private IEnumerator ParryKnockbackRoutine(Transform attacker, ImpactData impact)
    {
        isBeingKnockedBack = true;
        float horizontalDirection = Mathf.Sign(transform.position.x - attacker.position.x);
        Vector2 knockbackVelocity = new Vector2(horizontalDirection * (impact.knockbackDistance / impact.knockbackDuration), 0);

        float timer = 0f;
        while (timer < impact.knockbackDuration)
        {
            if (rb != null) rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isBeingKnockedBack = false;
    }

    private IEnumerator HitReactionRoutine(Transform attacker, ImpactData impact)
    {
        if (playerAttacks != null) playerAttacks.CancelAttack();
        isBeingKnockedBack = true;
        isStunned = true;
        if (playerMovements != null) playerMovements.CanMove = false;
        PlayHitReaction(impact.hitReactionType);

        float horizontalVelocity = 0f;
        if (impact.knockbackDistance > 0 && impact.knockbackDuration > 0)
            horizontalVelocity = (impact.knockbackDistance / impact.knockbackDuration) * Mathf.Sign(transform.position.x - attacker.position.x);

        float verticalVelocity = 0f;
        if (impact.upwardForce > 0) verticalVelocity = impact.upwardForce;
        else if (impact.downwardForce > 0) verticalVelocity = -impact.downwardForce;

        Debug.Log($"<color=lime>--- UNIFIED KNOCKBACK CALCULATION ---</color>\n" +
                  $"Horizontal Velocity Component: {horizontalVelocity}\n" +
                  $"Vertical Velocity Component: {verticalVelocity}");

        float maxDuration = Mathf.Max(hitStunDuration, impact.knockbackDuration);
        float timer = 0f;

        if (rb != null && verticalVelocity != 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalVelocity);

        while (timer < maxDuration)
        {
            if (rb != null) rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
            if (timer >= impact.knockbackDuration) horizontalVelocity = 0;
            timer += Time.deltaTime;
            yield return null;
        }

        float remainingStunTime = hitStunDuration - impact.knockbackDuration;
        if (remainingStunTime > 0)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            yield return new WaitForSeconds(remainingStunTime);
        }

        isStunned = false;
        playerMovements.CanMove = true;
        knockbackCoroutine = null;
        isBeingKnockedBack = false;
    }

    private void PlayHitReaction(string hitType)
    {
        animator.ResetTrigger(getHitBackTriggerHash);
        animator.ResetTrigger(getHitDownTriggerHash);
        animator.ResetTrigger(getHitFinalBackTriggerHash);

        switch (hitType.ToLower())
        {
            case "down": animator.SetTrigger(getHitDownTriggerHash); break;
            case "finalback": animator.SetTrigger(getHitFinalBackTriggerHash); break;
            case "back": animator.SetTrigger(getHitBackTriggerHash); break;
            case "fall": animator.SetTrigger(getHitFallTriggerHash); break;
        }
    }

    private void SpawnBlood()
    {
        if (bloodVFX != null && bloodSpawnPoint != null)
            Instantiate(bloodVFX, bloodSpawnPoint.position, bloodVFX.transform.rotation);
    }

    private void StartBlocking()
    {
        if (isShieldBroken || isStunned) { Debug.LogWarning("Block ignored: Shield broken or stunned."); return; }

        if (IsGrabbed) { Debug.LogWarning("Block Input Ignored: Player is GRABBED."); return; }
        if (playerAttacks != null && playerAttacks.IsInCinematicState) { Debug.Log("Block Input Ignored: In Cinematic State."); return; }
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (isBlocking) return;

        animator.ResetTrigger(stopBlockTriggerHash);
        isBlocking = true;
        animator.SetTrigger(startBlockTriggerHash);
        playerMovements.CanMove = false;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
        parryWindowCoroutine = StartCoroutine(ParryWindowCoroutine());
    }

    private void StopBlocking()
    {
        if (!isBlocking) return;
        isBlocking = false;
        animator.SetTrigger(stopBlockTriggerHash);
        playerMovements.CanMove = true;
        isParryWindowActive = false;
    }

    private IEnumerator ParryWindowCoroutine()
    {
        isParryWindowActive = true;
        yield return new WaitForSeconds(parryWindow);
        isParryWindowActive = false;
    }

    public void GetGrabbedByEnemy(Vector3 targetPosition, Transform enemyTransform)
    {
        Debug.LogError("--- PLAYER HAS BEEN GRABBED! LOSING CONTROL. ---");
        IsGrabbed = true;
        isStunned = true;
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (playerMovements != null) playerMovements.CanMove = false;
        StopBlocking();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = targetPosition;

        if (playerMovements != null)
            playerMovements.ForceFaceDirection(enemyTransform.position.x > transform.position.x);

        animator.SetTrigger(getGrabbedTriggerHash);
    }

    public void SpawnStabBlood()
    {
        if (stabBloodVFX != null && stabBloodSpawnPoint != null)
        {
            Debug.Log("--- Spawning Stab Blood VFX ---");
            Instantiate(stabBloodVFX, stabBloodSpawnPoint.position, stabBloodVFX.transform.rotation);
        }
    }

    public void ReleaseFromGrab()
    {
        Debug.Log("<color=green>--- Player released from grab. Regaining control. ---</color>");
        IsGrabbed = false;
        isStunned = false;
        isBeingKnockedBack = false;
        if (playerMovements != null) playerMovements.ForceResetState();
        else Debug.LogError("ReleaseFromGrab failed: ZreyMovements script not found!");
        if (playerAttacks != null) playerAttacks.ForceResetState();

        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        if (guardBreakCoroutine != null) StopCoroutine(guardBreakCoroutine);
        if (playerMovements != null) playerMovements.CanMove = true;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private IEnumerator GuardBreakRoutine()
    {
        if (isShieldBroken)
        {
            yield break; // Exit the coroutine immediately. Do nothing.
        }
        // --- PHASE 1: THE PUNISHMENT (This part is correct) ---
        Debug.LogError("--- PLAYER GUARD BROKEN! STUNNED! ---");
        isShieldBroken = true;
        isStunned = true;
        isBeingKnockedBack = true; // Use this to lock movement

        StopBlocking(); // Force the block to end

        if (playerMovements != null) playerMovements.CanMove = false;
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(guardBrokenTriggerHash);
        yield return null; // Wait a frame for the trigger to register
        animator.SetBool(isWeakBoolHash, true);
        Debug.Log("<color=yellow>--- Starting Parallel Shield Regen during Stun ---</color>");

        float timer = 0f;
        float startShieldHealth = 0; // We always start from zero after a break.
        currentShieldHealth = 0; // Set it to 0 at the beginning.

        // This loop runs for the entire duration of the stun.
        while (timer < guardBreakStunDuration)
        {
            // Calculate how far through the stun we are (a value from 0 to 1).
            float progress = timer / guardBreakStunDuration;

            // Use Lerp to smoothly move the shield health from 0 to max.
            currentShieldHealth = (int)Mathf.Lerp(startShieldHealth, maxShieldHealth, progress);

            // Increment the timer.
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }
        currentShieldHealth = maxShieldHealth;

        animator.SetBool(isWeakBoolHash, false);
        animator.SetTrigger(recoverShieldTriggerHash);

        // Wait for the "get up" animation to have some time to play
        yield return new WaitForSeconds(0.3f);

        // --- THE FORCED AWAKENING ---
        // 1. Unlock all the state flags.
        isStunned = false;
        isBeingKnockedBack = false;
        isShieldBroken = false;

        // 2. Give control back to the player.
        if (playerMovements != null) playerMovements.CanMove = true;

        if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);
        shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
    }
    private IEnumerator ShieldRegenRoutine()
    {
        Debug.Log("<color=yellow>--- SHIELD REGEN ROUTINE STARTED. Waiting " + shieldRegenDelay + " seconds... ---</color>");
        yield return new WaitForSeconds(shieldRegenDelay);
        Debug.Log("<color=yellow>--- SHIELD REGEN DELAY OVER. Starting to fill... Current: " + currentShieldHealth + " ---</color>");


        float regenAccumulator = currentShieldHealth; // Use a float accumulator

        while (regenAccumulator < maxShieldHealth)
        {
            regenAccumulator += shieldRegenRate * Time.deltaTime;
            currentShieldHealth = Mathf.Clamp(Mathf.FloorToInt(regenAccumulator), 0, maxShieldHealth);
            yield return null;
        }

        currentShieldHealth = maxShieldHealth;
        Debug.Log("<color=cyan>Shield fully regenerated.</color>");
    }

    private void Die(Transform killer)
    {
        Debug.Log("<color=black>PLAYER IS DEAD.</color>");
        if (checkpointManager != null) checkpointManager.RespawnAtMajorCheckpoint();

        currentHealth = maxHealth;


        animator.SetTrigger(deathTriggerHash);
        playerAttacks.enabled = false;
        GetComponent<ZreyMovements>().enabled = false;
        this.enabled = false;

        StartCoroutine(DeathSequence());
    }

    public bool resetisStunned()
    {
        isStunned = false;
        return isStunned;
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    public void ForceResetState()
    {
        isBlocking = false;
        isParryWindowActive = false;
        isStunned = false;
        isBeingKnockedBack = false;
        isShieldBroken = false;
        animator.SetBool(isWeakBoolHash, false);
        if (animator != null) animator.SetTrigger(stopBlockTriggerHash);
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
        if (guardBreakCoroutine != null) StopCoroutine(guardBreakCoroutine);
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1.5f);
        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}