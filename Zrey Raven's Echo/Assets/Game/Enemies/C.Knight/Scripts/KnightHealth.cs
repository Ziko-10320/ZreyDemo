using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnightHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Knockback Settings")]
    [Tooltip("How far the knight is knocked back when hit.")]
    [SerializeField] private float knockbackDistance = 1.5f;
    [Tooltip("How long the knockback effect lasts (in seconds).")]
    [SerializeField] private float knockbackDuration = 0.2f;

    // --- Components ---
    private Rigidbody2D rb;
    private Coroutine knockbackCoroutine;

    [Header("Parry/Block Settings")]
    [Tooltip("How close the player must be for the knight to attempt a block.")]
    [SerializeField] private float blockRange = 2.5f; 

    [Tooltip("The prefab for the spark effect when an attack is blocked.")]
    [SerializeField] private GameObject blockSparksPrefab; 

    [Tooltip("An empty GameObject marking where the sparks should spawn.")]
    [SerializeField] private Transform blockSparksPoint; 

    [Tooltip("How far the PLAYER is knocked back when their attack is blocked.")]
    [SerializeField] private float playerKnockbackOnBlock = 2f; 

    [Tooltip("How long the PLAYER's knockback lasts.")]
    [SerializeField] private float playerKnockbackDurationOnBlock = 0.2f;
    [SerializeField] private float blockRecoilDistance = 0.5f;
    [SerializeField] private float blockRecoilDuration = 0.15f;
    private bool isBlocking = false; // Is the block window currently active?
    private bool canBlock = true; // Can the knight attempt another block?
    private float blockCooldown = 1.5f; // How long the knight must wait between blocks.

    // --- Components & Animation Hashes ---
    private Animator animator; // We need the animator now.
    private readonly int blockTriggerHash = Animator.StringToHash("block");
    public ShakeData CameraShakeParry;
    private bool isBeingKnockedBack = false;


    [Header("Shield/Guard System")]
    [Tooltip("The maximum value of the knight's guard meter. Starts full.")]
    [SerializeField] private float maxGuard = 100f; 

    [Tooltip("How much guard meter is LOST each time the knight blocks an attack.")]
    [SerializeField] private float guardDamagePerBlock = 35f; 

    [Tooltip("How long the knight is stunned and vulnerable after their guard breaks.")]
    [SerializeField] private float guardBrokenStunDuration = 3f; 

    [Tooltip("How long the knight must be out of combat before their guard starts to recover.")]
    [SerializeField] private float guardRecoveryDelay = 4f; 

    [Tooltip("How fast the knight's guard meter recovers per second.")]
    [SerializeField] private float guardRecoveryRate = 15f; 

   
    private float currentGuard;
    private bool isGuardBroken = false;
    private float timeSinceLastBlock = 0f;

    // --- New Animation Hash ---
    private readonly int guardBrokenTriggerHash = Animator.StringToHash("guardBroken");
    private readonly int getHitUpTriggerHash = Animator.StringToHash("getHitUp");
    private readonly int getHitDownTriggerHash = Animator.StringToHash("getHitDown");
    private readonly int getHitBackTriggerHash = Animator.StringToHash("getHitBack");
    [Header("VFX Settings")]
    [Tooltip("An array of blood particle effect prefabs to spawn on hit.")]
    [SerializeField] private GameObject[] bloodVFXPrefabs; 

    [Tooltip("The specific point on the knight's body where blood VFX will spawn.")]
    [SerializeField] private Transform bloodSpawnPoint;
    private readonly int fallTriggerHash = Animator.StringToHash("fall");
    private readonly int finalBackTriggerHash = Animator.StringToHash("finalBack");

    // --- ADD this new section for the custom knockback system ---
    [Header("Custom Knockback Override")]
    [Tooltip("A flag to check if a custom knockback is primed.")]
    private bool useCustomKnockback = false;
    private float customKnockbackDistance;
    private float customKnockbackDuration;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentGuard = maxGuard;
    }
    void Update()
    {
        // --- THIS IS THE GUARD RECOVERY LOGIC ---
        // If our guard is not broken and we are not currently blocking...
        if (!isGuardBroken && !isBlocking)
        {
            // ...increment the timer.
            timeSinceLastBlock += Time.deltaTime;

            // If enough time has passed since the last block...
            if (timeSinceLastBlock >= guardRecoveryDelay)
            {
                // ...start recovering the guard meter back towards its maximum value.
                currentGuard = Mathf.MoveTowards(currentGuard, maxGuard, guardRecoveryRate * Time.deltaTime);
            }
        }
    }
    /// <summary>
    /// This is the public method that the player's attack script will call.
    /// </summary>
    public void TakeDamage(int damage, Transform attacker, string hitType)
    {
        float distanceToUse;
        float durationToUse;

        // --- THIS IS THE CUSTOM KNOCKBACK LOGIC ---
        // 1. Check if a custom knockback is primed.
        if (useCustomKnockback)
        {
            // If yes, use the custom values for this hit.
            distanceToUse = customKnockbackDistance;
            durationToUse = customKnockbackDuration;
            Debug.Log("<color=yellow>Applying CUSTOM knockback!</color>");

            // CRITICAL: Reset the flag so the next hit uses the default values again.
            useCustomKnockback = false;
        }
        else
        {
            // If no, use the default values from the Inspector.
            distanceToUse = knockbackDistance;
            durationToUse = knockbackDuration;
        }
        if (isGuardBroken)
        {
            PlayHitReaction(hitType);
            currentHealth -= damage;
            Debug.Log("<color=red>GUARD BROKEN! Dealt " + damage + " direct damage.</color>");
            SpawnBloodVFX();
            // Apply the normal hit knockback
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, knockbackDistance, knockbackDuration));

            if (currentHealth <= 0) Die();
            return; // Exit the function.
        }

        if (isBlocking)
        {
            Debug.Log("<color=cyan>ATTACK BLOCKED!</color>");

            // Spawn sparks effect.
            if (blockSparksPrefab != null && blockSparksPoint != null)
            {
                Instantiate(blockSparksPrefab, blockSparksPoint.position, blockSparksPoint.rotation);
            }
            CameraShakerHandler.Shake(CameraShakeParry);
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, blockRecoilDistance, blockRecoilDuration));

            timeSinceLastBlock = 0f; // Reset the recovery timer.
            currentGuard -= guardDamagePerBlock; // SUBTRACT damage from the guard meter.
            Debug.Log("Current Guard: " + currentGuard + " / " + maxGuard);

            // Check if the guard meter has been DEPLETED.
            if (currentGuard <= 0)
            {
                StartCoroutine(GuardBrokenSequence()); // GUARD BREAK!
            }

            // Apply knockback to the PLAYER.
            ZreyAttacks playerAttacks = attacker.GetComponent<ZreyAttacks>();
            if (playerAttacks != null)
            {
                playerAttacks.ApplyKnockback(transform, playerKnockbackOnBlock, playerKnockbackDurationOnBlock);
            }

            // IMPORTANT: Exit the function. Do not take damage or get knocked back.
            return;
        }
        PlayHitReaction(hitType);
        currentHealth -= damage;
        SpawnBloodVFX();
        Debug.Log(transform.name + " took " + damage + " damage. Health is now: " + currentHealth);

        // --- KNOCKBACK LOGIC ---
        // Stop any previous knockback to handle rapid hits.
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, knockbackDistance, knockbackDuration));
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void SpawnBloodVFX() // MODIFIED: No longer needs the 'attacker' parameter.
    {
        // --- Safety Check #1: Is the array empty? ---
        if (bloodVFXPrefabs == null || bloodVFXPrefabs.Length == 0)
        {
            return;
        }

        // --- Safety Check #2: Is the spawn point assigned? ---
        if (bloodSpawnPoint == null)
        {
            Debug.LogError("Blood Spawn Point is not assigned on the KnightHealth script! Using knight's own position as a fallback.", this);
            bloodSpawnPoint = this.transform; // Use our own transform as a last resort.
        }

        // --- The Random Logic ---
        int randomIndex = Random.Range(0, bloodVFXPrefabs.Length);
        GameObject randomPrefab = bloodVFXPrefabs[randomIndex];

        // --- THIS IS THE MASTER FIX ---
        // 1. We get the PREFAB's own rotation. This respects the rotation you set in the prefab file.
        Quaternion prefabRotation = randomPrefab.transform.rotation;

        // 2. We Instantiate the prefab at our DEDICATED spawn point, using the PREFAB's rotation.
        // Unity will automatically use the prefab's scale.
        Instantiate(randomPrefab, bloodSpawnPoint.position, prefabRotation);
        // --- END OF MASTER FIX ---
    }
    private IEnumerator KnockbackRoutine(Transform attacker, float distance, float duration)
    {
        isBeingKnockedBack = true;
        // Determine the direction of the knockback.
        Vector2 knockbackDirection = (transform.position - attacker.position).normalized;

        // Calculate the knockback velocity.
        Vector2 knockbackVelocity = knockbackDirection * (distance / duration);

        float timer = 0f;
        while (timer < duration)
        {
            // Apply the velocity directly. This overrides other movement.
            rb.linearVelocity = knockbackVelocity;
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }

        // After the duration, reset the velocity to prevent sliding.
        rb.linearVelocity = Vector2.zero;
        knockbackCoroutine = null;
        isBeingKnockedBack = false;
    }

    private void Die()
    {
        Debug.Log(transform.name + " has been defeated!");
        // Here you would play a death animation, spawn particles, etc.
        // For now, we'll just destroy the GameObject.
        Destroy(gameObject, 0.1f);
    }
    public void OnPlayerAttackTelegraphed(Transform player)
    {
        if (isGuardBroken)
        {
            return;
        }
        // --- THIS IS THE FIX ---
        // 1. If we are already being hit or the player is too far, do nothing.
        if (isBeingKnockedBack || Vector2.Distance(transform.position, player.position) > blockRange)
        {
            return;
        }

        // 2. Reset the trigger before setting it again. This is a robust way
        //    to ensure the Animator is ready to receive the signal.
        animator.ResetTrigger(blockTriggerHash);
        animator.SetTrigger(blockTriggerHash);
        // --- END OF FIX ---

        Debug.Log("Knight is attempting to block NOW.");
    }
    private IEnumerator GuardBrokenSequence()
    {
        Debug.Log("<color=red>KNIGHT'S GUARD IS BROKEN!</color>");

        // --- 1. ENTER THE BROKEN STATE ---
        isGuardBroken = true; // The master switch that makes the knight vulnerable.
        isBlocking = false; // Can't block if your guard is broken.
        animator.SetTrigger(guardBrokenTriggerHash); // Play the "guardBroken" animation.

        // --- 2. WAIT FOR THE STUN DURATION ---
        // The knight is now stuck in this vulnerable state.
        yield return new WaitForSeconds(guardBrokenStunDuration);

        // --- 3. RECOVER FROM THE BROKEN STATE ---
        Debug.Log("<color=green>Knight has recovered their guard.</color>");
        isGuardBroken = false; // No longer vulnerable.
        currentGuard = maxGuard; // Refill the guard meter completely.
        timeSinceLastBlock = 0f; // Reset the recovery timer.
    }
    public void PlayHitReaction(string hitType)
    {
  // --- PRIORITY #2: Brutal Interrupt Logic ---
        // 1. Stop any ongoing knockback coroutine. A new hit gets priority.
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            isBeingKnockedBack = false; // Manually reset the state flag.
            Debug.Log("<color=orange>Previous knockback interrupted by new hit.</color>");
        }

        // 2. Reset ALL other animation triggers. This is CRITICAL.
        // It prevents the animator from getting "stuck" waiting for another animation to finish.
        animator.ResetTrigger(getHitUpTriggerHash);
        animator.ResetTrigger(getHitDownTriggerHash);
        animator.ResetTrigger(getHitBackTriggerHash);
        animator.ResetTrigger(blockTriggerHash); // Also reset the block trigger, just in case.
        animator.ResetTrigger(fallTriggerHash);
        animator.ResetTrigger(finalBackTriggerHash);

        Debug.Log($"<color=cyan>Knight received AGGRESSIVE hit reaction command: {hitType}</color>");

        // 3. Now, set the new trigger.
        switch (hitType.ToLower())
        {
            case "up":
                animator.SetTrigger(getHitUpTriggerHash);
                break;
            case "down":
                animator.SetTrigger(getHitDownTriggerHash);
                break;
            case "back":
                animator.SetTrigger(getHitBackTriggerHash);
                break;
            case "fall":
                animator.SetTrigger(fallTriggerHash);
                break;
            case "finalback":
                animator.SetTrigger(finalBackTriggerHash);
                break;
            default:
                animator.SetTrigger(getHitBackTriggerHash);
                break;
        }
    }
    private IEnumerator BlockCooldownRoutine()
    {
        canBlock = false;
        yield return new WaitForSeconds(blockCooldown);
        canBlock = true;
    }
    /// <summary>
    /// Called by an Animation Event at the START of the block animation.
    /// </summary>
    public void SetCustomKnockback(float distance, float duration)
    {
        Debug.Log($"<color=yellow>Custom Knockback Primed! Distance: {distance}, Duration: {duration}</color>");
        useCustomKnockback = true;
        customKnockbackDistance = distance;
        customKnockbackDuration = duration;
    }
    public void OpenBlockWindow()
    {
        isBlocking = true;
        Debug.Log("Knight: Block Window OPEN");
    }

    /// <summary>
    /// Called by an Animation Event at the END of the block animation.
    /// </summary>
    public void CloseBlockWindow()
    {
        isBlocking = false;
        Debug.Log("Knight: Block Window CLOSED");
    }
}
