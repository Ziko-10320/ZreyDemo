using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Required for Slider

public class PlayerHealth : MonoBehaviour
{
    [Header("Health & UI")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject deathPanel;
    private int currentHealth;

    [Header("Impact & VFX")]
    [SerializeField] private GameObject bloodVFX;
    [SerializeField] private Transform bloodSpawnPoint;

    // --- Private Components & State ---
    private Rigidbody2D rb;
    private Animator animator;
    private ZreyAttacks playerAttacks;
    private Coroutine knockbackCoroutine;

    // --- Animation Hashes (for performance) ---
    private readonly int getHitBackTriggerHash = Animator.StringToHash("getHitBack");
    private readonly int getHitDownTriggerHash = Animator.StringToHash("getHitDown");
    private readonly int getHitFinalBackTriggerHash = Animator.StringToHash("finalBack");
    private readonly int getHitFallTriggerHash = Animator.StringToHash("Hitfall");
    private readonly int deathTriggerHash = Animator.StringToHash("death"); // For death animation
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
    // --- ADD NEW PRIVATE STATE VARIABLES ---
    private bool isBlocking = false;
    private bool isParryWindowActive = false;
   
    private Coroutine parryWindowCoroutine;
    // --- ADD NEW ANIMATION HASHES ---
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
    [Tooltip("The blood particle effect for the stab part of the grab.")]
    [SerializeField] private GameObject stabBloodVFX; 
    [Tooltip("The point where the stab blood should spawn.")]
    [SerializeField] private Transform stabBloodSpawnPoint;
    public bool IsGrabbed { get; private set; } = false;
    public bool IsInvincible { get; private set; } = false;
    void Awake()
    {
        // --- THIS IS THE GUARANTEE ---
        // We get the components automatically from this GameObject.
        // This is not optional. It is required.
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAttacks = GetComponent<ZreyAttacks>();
        playerMovements = GetComponent<ZreyMovements>();
        // Check if anything is missing and scream if it is.
        if (rb == null) Debug.LogError("FATAL ERROR: Rigidbody2D is missing on Player!", this);
        if (animator == null) Debug.LogError("FATAL ERROR: Animator is missing on Player!", this);
        if (playerAttacks == null) Debug.LogError("FATAL ERROR: ZreyAttacks script is missing on Player!", this);
        inputActions = new InputSystem_Actions();
        if (checkpointManager == null) checkpointManager = FindFirstObjectByType<CheckpointManager>();
    }
    private void OnEnable()
    {
        // 1. Enable this script's own private input system.
        inputActions.Enable();

        // 2. Subscribe to the Block action on that private system.
        inputActions.Player.Block.started += HandleBlockInput;
        inputActions.Player.Block.canceled += HandleBlockInput;
    }

    private void OnDisable()
    {
        // 1. Unsubscribe from the events.
        inputActions.Player.Block.started -= HandleBlockInput;
        inputActions.Player.Block.canceled -= HandleBlockInput;

        // 2. Disable this script's private input system.
        inputActions.Disable();
    }
    private void HandleBlockInput(InputAction.CallbackContext context)
    {
        // context.ReadValueAsButton() is true if the button is down, false if it's up.
        if (context.ReadValueAsButton())
        {
            StartBlocking();
        }
        else
        {
            StopBlocking();
        }
    }
    public void UpdateBlockBinding(string newBindingPath)
    {
        // Failsafe: If our private input system doesn't exist, do nothing.
        if (inputActions == null) return;

        Debug.Log($"<color=yellow>PLAYER HEALTH RECEIVED A NEW BINDING: {newBindingPath}</color>");

        // This is the magic. We are manually overriding the binding on our private input system.
        // We are changing the "Block" action (index 0) to the new path we received.
        inputActions.Player.Block.ApplyBindingOverride(0, newBindingPath);
    }
    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        if (deathPanel != null) deathPanel.SetActive(false);
    }
    public void MakeInvincible()
    {
        Debug.Log("<color=cyan>--- PLAYER IS NOW INVINCIBLE ---</color>");
        IsInvincible = true;
    }

    /// <summary>
    /// Called by an Animation Event to make the player vulnerable again.
    /// </summary>
    public void MakeVulnerable()
    {
        Debug.Log("<color=grey>--- Player is now VULNERABLE ---</color>");
        IsInvincible = false;
    }

    // --- THE ONLY WAY TO TAKE DAMAGE ---
    public void TakeDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        if (IsInvincible)
        {
            Debug.Log("Damage ignored: Player is invincible.");
            return;
        }
        // If already dead, do nothing.
        if (currentHealth <= 0) return;
        if (isParryWindowActive)
        {
            Debug.Log("<color=lime>PARRY SUCCESSFUL!</color>");
            CameraShakerHandler.Shake(CameraShakeParry); // Shake the camera

            // Force the animator out of the block state to play the parry anim
            isBlocking = false;


            // Randomly choose between parry1 and parry2
            int parryAnim = Random.Range(0, 2);
            animator.SetTrigger(parryAnim == 0 ? parry1TriggerHash : parry2TriggerHash);

            if (parryVFX != null) Instantiate(parryVFX, defenseVFXSpawnPoint.position, Quaternion.identity);

            // Let the player move again after a parry
            playerMovements.CanMove = true;

            ImpactData parryRecoilImpact = ScriptableObject.CreateInstance<ImpactData>();
            // 2. We read the NEW parry-specific values from the original 'impact' data.
            parryRecoilImpact.knockbackDistance = impact.parryKnockbackDistance;
            parryRecoilImpact.knockbackDuration = impact.parryKnockbackDuration;
            parryRecoilImpact.hitReactionType = "none"; // A parry never plays a "get hit" animation.

            // 3. We apply this parry-specific recoil to the PLAYER.
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryRecoilImpact));

            KnightAttack enemyAttack = attacker.GetComponent<KnightAttack>();
            KnightHealth enemyHealth = attacker.GetComponent<KnightHealth>();

            if (enemyHealth != null)
            {
                // 2. ALWAYS apply the small knockback to the knight on ANY parry.

                enemyHealth.TakePostureDamageOnParry();
                // 3. ASK if the attack was the final one.
                if (enemyAttack != null && enemyAttack.IsFinalComboAttack())
                {
                    // 4. If YES, ALSO command the enemy to play the stunned animation.
                    Debug.Log("<color=lime>PARRIED THE FINAL ATTACK! Stunning the knight!</color>");
                    enemyHealth.GetParried(transform);
                }
                else
                {
                    Debug.Log("<color=yellow>Parried a normal attack. Knight is knocked back but not stunned.</color>");
                }
            }
            SpearAttack SAttack = attacker.GetComponent<SpearAttack>();
            SpearHealth SHealth = attacker.GetComponent<SpearHealth>();

            if (SHealth != null)
            {
                // 2. ALWAYS apply the small knockback to the knight on ANY parry.

                SHealth.TakePostureDamageOnParry();
                // 3. ASK if the attack was the final one.
                if (SAttack != null &&SAttack.IsFinalComboAttack())
                {
                    // 4. If YES, ALSO command the enemy to play the stunned animation.
                    Debug.Log("<color=lime>PARRIED THE FINAL ATTACK! Stunning the knight!</color>");
                    SHealth.GetParried(transform);
                }
                else
                {
                    Debug.Log("<color=yellow>Parried a normal attack. Knight is knocked back but not stunned.</color>");
                }
            }
            return; // Stop all further execution. No damage, no knockback.
        }

        // --- 2. BLOCK LOGIC (from your old script) ---
        if (isBlocking)
        {
            Debug.Log("<color=cyan>BLOCK SUCCESSFUL!</color>");
            CameraShakerHandler.Shake(CameraShakeParry); // Shake on block too

            int reducedDamage = Mathf.RoundToInt(damageAmount * (1f - damageReduction));
            currentHealth -= reducedDamage;
            if (healthSlider != null) healthSlider.value = currentHealth;

            if (blockVFX != null) Instantiate(blockVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

            // Create a temporary ImpactData for the parry knockback
            ImpactData parryImpact = ScriptableObject.CreateInstance<ImpactData>();
            parryImpact.knockbackDistance = impact.knockbackDistance; // Half distance
            parryImpact.knockbackDuration = impact.knockbackDuration;
            parryImpact.hitReactionType = "none"; // No hit animation
            SpawnBlood();
            knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryImpact));

            if (currentHealth <= 0) Die(attacker);
            return; // Stop all further execution.
        }

        currentHealth -= damageAmount;
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (impact == null)
        {
            Debug.LogWarning("TakeDamage was called with null ImpactData!");
            return;
        }
        Debug.Log($"<color=red>PLAYER TOOK DAMAGE. Health: {currentHealth}/{maxHealth}</color>");

        // --- CHECK FOR DEATH FIRST ---
        if (currentHealth <= 0)
        {
            Die(attacker); // Pass the attacker for a final knockback
            return; // Stop everything else.
        }
        SpawnBlood();
        // --- IF NOT DEAD, DO ALL THE REACTIONS ---
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, impact));

    }
    public void TakeUnblockableDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        if (IsInvincible)
        {
            Debug.Log("Damage ignored: Player is invincible.");
            return;
        }
        // If already dead, do nothing.
        if (currentHealth <= 0) return;

        Debug.LogWarning($"<color=red>!!! PLAYER TOOK UNBLOCKABLE DAMAGE: {damageAmount} !!!</color>");

        // --- BYPASS ALL DEFENSES ---
        // We do NOT check for isParryWindowActive.
        // We do NOT check for isBlocking.

        // --- APPLY DAMAGE DIRECTLY ---
        currentHealth -= damageAmount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        // --- CHECK FOR DEATH ---
        if (currentHealth <= 0)
        {
            Die(attacker);
            return;
        }

        // --- APPLY ALL NORMAL HIT REACTIONS ---
        // Even though it's unblockable, it should still cause hit stun, knockback, and blood.
        SpawnBlood();
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, impact));
    }
    public void TakeHazardDamage(int damageAmount)
    {

        if (IsInvincible)
        {
            Debug.Log("Damage ignored: Player is invincible.");
            return;
        }
        // If already dead, do nothing.
        if (currentHealth <= 0) return;

        Debug.LogWarning($"<color=orange>PLAYER HIT A HAZARD! Taking {damageAmount} damage.</color>");

        currentHealth -= damageAmount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        // --- CHECK FOR DEATH FIRST ---
        if (currentHealth <= 0)
        {
            // If the hazard damage kills the player, trigger the full death sequence.
            Die(null); // We pass null because there is no specific "killer" transform.
        }
        else
        {
            // --- IF NOT DEAD, RESPAWN AT MINI CHECKPOINT ---
            // This is the "slap on the wrist."
            Debug.Log("Player is hurt by hazard. Respawning at MINI checkpoint.");
            if (checkpointManager != null)
            {
                checkpointManager.RespawnAtMiniCheckpoint();
            }
        }
    }
    private IEnumerator ParryKnockbackRoutine(Transform attacker, ImpactData impact)
    {
        isBeingKnockedBack = true;
        // Calculate the small knockback velocity.
        float horizontalDirection = Mathf.Sign(transform.position.x - attacker.position.x);
        Vector2 knockbackVelocity = new Vector2(horizontalDirection * (impact.knockbackDistance / impact.knockbackDuration), 0);

        // Apply the knockback over its short duration.
        float timer = 0f;
        while (timer < impact.knockbackDuration)
        {
            if (rb != null) rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        // After the knockback, reset horizontal velocity but allow the player to keep moving.
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isBeingKnockedBack = false;
    }
    private IEnumerator HitReactionRoutine(Transform attacker, ImpactData impact)
    {
        if (playerAttacks != null)
        {
            playerAttacks.CancelAttack();
        }
        isBeingKnockedBack = true;
        isStunned = true;
        if (playerMovements != null) playerMovements.CanMove = false;
        // 2. PLAY ANIMATION
        PlayHitReaction(impact.hitReactionType);

        float horizontalVelocity = 0f;
        if (impact.knockbackDistance > 0 && impact.knockbackDuration > 0)
        {
            horizontalVelocity = (impact.knockbackDistance / impact.knockbackDuration) * Mathf.Sign(transform.position.x - attacker.position.x);
        }

        // B. Calculate the VERTICAL velocity component.
        //    This is the upward or downward "explosion." It will be 0 if the forces are 0.
        float verticalVelocity = 0f;
        if (impact.upwardForce > 0)
        {
            verticalVelocity = impact.upwardForce;
        }
        else if (impact.downwardForce > 0)
        {
            verticalVelocity = -impact.downwardForce;
        }

        Debug.Log($"<color=lime>--- UNIFIED KNOCKBACK CALCULATION ---</color>\n" +
                  $"Horizontal Velocity Component: {horizontalVelocity}\n" +
                  $"Vertical Velocity Component: {verticalVelocity}");

        // --- 3. THE UNIFIED PHYSICS APPLICATION ---

        // We use a timer that runs for the LONGER of the two durations: the hit stun or the knockback.
        float maxDuration = Mathf.Max(hitStunDuration, impact.knockbackDuration);
        float timer = 0f;

        // Apply the initial vertical impulse ONCE.
        if (rb != null && verticalVelocity != 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalVelocity);
        }

        // Now, we loop. In every frame of the loop, we apply the SUSTAINED horizontal force.
        while (timer < maxDuration)
        {
            if (rb != null)
            {
                // We continuously set the horizontal velocity, but we let the physics engine
                // handle the vertical velocity (gravity will take over after the initial impulse).
                rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
            }

            // We stop applying the horizontal force after its duration is over.
            if (timer >= impact.knockbackDuration)
            {
                horizontalVelocity = 0;
            }

            timer += Time.deltaTime;
            yield return null;
        }
        // 4. APPLY HIT STUN (wait for the remaining time)
        float remainingStunTime = hitStunDuration - impact.knockbackDuration;
        if (remainingStunTime > 0)
        {
            // Stop moving during the stun.
            if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            yield return new WaitForSeconds(remainingStunTime);
        }
        isStunned = false;
        // 5. RELINQUISH CONTROL
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
        {
            Instantiate(bloodVFX, bloodSpawnPoint.position, bloodVFX.transform.rotation);
        }
    }


    private void StartBlocking()
    {
        if (IsGrabbed)
        {
            Debug.LogWarning("Block Input Ignored: Player is GRABBED.");
            return;
        }
        if (playerAttacks != null && playerAttacks.IsInCinematicState)
        {
            Debug.Log("Block Input Ignored: In Cinematic State.");
            return;
        }
        if (playerAttacks != null)
        {
            playerAttacks.CancelAttack();
        }
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
        // --- 1. CANCEL EVERYTHING THE PLAYER IS DOING ---
        isStunned = true; // Use the existing stun flag.
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (playerMovements != null) playerMovements.CanMove = false;
        StopBlocking(); // Force the player to stop blocking.
        if (rb != null) rb.linearVelocity = Vector2.zero; // Kill momentum.
        transform.position = targetPosition;

        // Force the player to face the enemy.
        if (playerMovements != null)
        {
            // We tell the movement script to look at the enemy's X position.
            playerMovements.ForceFaceDirection(enemyTransform.position.x > transform.position.x);
        }
        // --- 2. PLAY THE "GET GRABBED" ANIMATION ---
        animator.SetTrigger(getGrabbedTriggerHash);
    }
    public void SpawnStabBlood()
    {
        if (stabBloodVFX != null && stabBloodSpawnPoint != null)
        {
            Debug.Log("--- Spawning Stab Blood VFX ---");
            // Instantiate the prefab at the spawn point's position, but using the PREFAB's own original rotation.
            Instantiate(stabBloodVFX, stabBloodSpawnPoint.position, stabBloodVFX.transform.rotation);
        }
    }

    // --- ADD THIS NEW PUBLIC METHOD (called by Animation Event) ---
    // You will call this from an Animation Event at the END of your player's "GetGrabbed" animation.
    public void ReleaseFromGrab()
    {
        Debug.Log("<color=green>--- Player released from grab. Regaining control. ---</color>");
        IsGrabbed = false;
        isStunned = false;
        isBeingKnockedBack = false;
        if (playerMovements != null)
        {
            playerMovements.ForceResetState();
        }
        else
        {
            Debug.LogError("ReleaseFromGrab failed: ZreyMovements script not found!");
        }
        if (playerAttacks != null)
        {
            // It's good practice to reset the attack script too.
            playerAttacks.ForceResetState(); // We will create this method.
        }
        StopAllCoroutines();
        if (playerMovements != null) playerMovements.CanMove = true;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Reset horizontal velocity but keep vertical.
            Debug.Log("Reset Rigidbody horizontal velocity.");
        }
    }
    private void Die(Transform killer)
    {
        Debug.Log("<color=black>PLAYER IS DEAD.</color>");
        if (checkpointManager != null)
        {
            checkpointManager.RespawnAtMajorCheckpoint();
        }
        // 2. Restore health to full AFTER respawning.
        currentHealth = maxHealth;
        if (healthSlider != null) healthSlider.value = currentHealth;

        // Play a death animation.
        animator.SetTrigger(deathTriggerHash);
        // Disable all player control scripts immediately.
        playerAttacks.enabled = false;
        GetComponent<ZreyMovements>().enabled = false;
        this.enabled = false; // Disable this script.

        // Use a coroutine to show the death panel and freeze time AFTER a delay.
        StartCoroutine(DeathSequence());
    }
    public bool resetisStunned()
    {
        isStunned = false;
        return isStunned;
    }
    public bool IsBlocking()
    {
        // The 'isBlocking' variable already controls the block state.
        // We just need to expose its value.
        return isBlocking;
    }
    public void ForceResetState()
    {
        isBlocking = false;
        isParryWindowActive = false;
        isStunned = false;
        isBeingKnockedBack = false;

        // We also need to ensure the animator's block state is reset.
        if (animator != null)
        {
            animator.SetTrigger(stopBlockTriggerHash);
        }

        // Stop any lingering coroutines in this script
        StopAllCoroutines();
    }
    private IEnumerator DeathSequence()
    {
        // Wait for the death animation/knockback to have some impact.
        yield return new WaitForSeconds(1.5f);

        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze the game.
    }
}