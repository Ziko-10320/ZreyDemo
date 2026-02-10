using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
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

    // --- ADD NEW PRIVATE STATE VARIABLES ---
    private bool isBlocking = false;
    private bool isParryWindowActive = false;
    private InputSystem_Actions inputActions; // For the block input
    private Coroutine parryWindowCoroutine;
    // --- ADD NEW ANIMATION HASHES ---
    private readonly int startBlockTriggerHash = Animator.StringToHash("startBlock");
    private readonly int stopBlockTriggerHash = Animator.StringToHash("stopBlock");
    private readonly int parry1TriggerHash = Animator.StringToHash("parry1");
    private readonly int parry2TriggerHash = Animator.StringToHash("parry2");
    public ShakeData CameraShakeParry;
    [SerializeField] private CheckpointManager checkpointManager;
    public bool isStunned = false;
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
        inputActions.Enable();
        // When the "Block" action is started (Right-click pressed)
        inputActions.Player.Block.started += ctx => StartBlocking();
        // When the "Block" action is canceled (Right-click released)
        inputActions.Player.Block.canceled += ctx => StopBlocking();
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Block.started -= ctx => StartBlocking();
        inputActions.Player.Block.canceled -= ctx => StopBlocking();
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

    // --- THE ONLY WAY TO TAKE DAMAGE ---
    public void TakeDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
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

            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

            // Create a temporary ImpactData for the parry knockback
            ImpactData parryImpact = ScriptableObject.CreateInstance<ImpactData>();
            parryImpact.knockbackDistance = impact.knockbackDistance * 0.1f; // Half distance
            parryImpact.knockbackDuration = impact.knockbackDuration * 0.4f;
            parryImpact.hitReactionType = "none"; // No hit animation

            knockbackCoroutine = StartCoroutine(ParryKnockbackRoutine(attacker, parryImpact));

            KnightAttack enemyAttack = attacker.GetComponent<KnightAttack>();
            KnightHealth enemyHealth = attacker.GetComponent<KnightHealth>();

            if (enemyHealth != null)
            {
                // 2. ALWAYS apply the small knockback to the knight on ANY parry.
                enemyHealth.ApplyParryKnockback(transform);
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
            parryImpact.knockbackDistance = impact.knockbackDistance * 0.5f; // Half distance
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
    }
    private IEnumerator HitReactionRoutine(Transform attacker, ImpactData impact)
    {
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
        if (playerAttacks != null && playerAttacks.IsInCinematicState)
        {
            Debug.Log("Block Input Ignored: In Cinematic State.");
            return;
        }
        if ( isBlocking) return;
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
    private IEnumerator DeathSequence()
    {
        // Wait for the death animation/knockback to have some impact.
        yield return new WaitForSeconds(1.5f);

        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze the game.
    }
}
