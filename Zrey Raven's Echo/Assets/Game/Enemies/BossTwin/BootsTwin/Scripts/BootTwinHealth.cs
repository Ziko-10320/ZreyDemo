using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class BootTwinHealth : MonoBehaviour
{
    [Header("Health UI")]
    [Tooltip("The UI Slider that displays the knight's health.")]
    [SerializeField] private Transform healthBarCanvasTransform;
    [SerializeField] private Vector3 canvasOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float uiFadeOutDuration = 0.5f;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider healthDelayedFill; // NEW
    [Tooltip("The UI Slider that displays the knight's posture/guard.")]
    [SerializeField] private Slider postureSlider;
    [SerializeField] private Slider postureDelayedFill;
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    [Header("UI Animation Settings")] // <-- ADD THIS ENTIRE HEADER
    [SerializeField] private float healthFillSpeed = 5f;
    [Tooltip("How long to wait before the HEALTH delayed-fill bar starts moving.")]
    [SerializeField] private float healthFillDelay = 0.5f;
    [SerializeField] private float postureFillSpeed = 8f;
    [Tooltip("How long to wait before the POSTURE delayed-fill bar starts moving.")]
    [SerializeField] private float postureFillDelay = 0.2f;

    [Header("Knockback Settings")]
    [Tooltip("How far the knight is knocked back when hit.")]
    [SerializeField] private float knockbackDistance = 1.5f;
    [Tooltip("How long the knockback effect lasts (in seconds).")]
    [SerializeField] private float knockbackDuration = 0.2f;
    private Coroutine healthUpdateCoroutine;
    private Coroutine postureUpdateCoroutine;
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
    [Header("Enemy Reaction to Being Parried")]
    [Tooltip("How far the KNIGHT is knocked back when the PLAYER parries its attack.")]
    [SerializeField] private float knockbackOnParriedDistance = 2f;
    [Tooltip("How long the KNIGHT's knockback lasts after being parried.")]
    [SerializeField] private float knockbackOnParriedDuration = 0.2f;
    [HideInInspector] public bool isBlocking = false; // Is the block window currently active?
    private bool canBlock = true; // Can the knight attempt another block?
    private float blockCooldown = 0.2f; // How long the knight must wait between blocks.

    // --- Components & Animation Hashes ---
    private Animator animator; // We need the animator now.
    private readonly int blockTriggerHash = Animator.StringToHash("block");
    private readonly int block2TriggerHash = Animator.StringToHash("block2");
    
    private int lastBlockAnimationIndex = -1; // -1 means no block has been played yet.
    private int consecutivePlayCount = 0;
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

    [SerializeField] private float guardDamageOnParried = 50f;

    private float currentGuard;
    public bool isGuardBroken = false;
    [HideInInspector] public float timeSinceLastBlock = 0f;

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

    [Tooltip("The prefab for the persistent wound effect (decal, etc.).")]
    [SerializeField] private GameObject woundEffectPrefab;

    [Tooltip("The specific point on the knight's body where the wound should appear.")]
    [SerializeField] private Transform woundSpawnPoint;
    private readonly int fallTriggerHash = Animator.StringToHash("fall");
    private readonly int UpTriggerHash = Animator.StringToHash("Up");
    private readonly int RightUpTriggerHash = Animator.StringToHash("RightUp");
    private readonly int LeftUpTriggerHash = Animator.StringToHash("LeftUp");
    private readonly int finalBackTriggerHash = Animator.StringToHash("finalBack");

    // --- ADD this new section for the custom knockback system ---
    [Header("Custom Knockback Override")]
    [Tooltip("A flag to check if a custom knockback is primed.")]
    private bool useCustomKnockback = false;
    private float customKnockbackDistance;
    private float customKnockbackDuration;
    private ZreyAttacks zreyAttacks;
    public Transform playerTarget;


    private BootTwinAttack followAI;

    [Header("Flash Damage Effect")]
    [Tooltip("The SkinnedMeshRenderer of the knight's 3D model.")]
    [SerializeField] private SkinnedMeshRenderer knightMeshRenderer;

    [Tooltip("The special material that has the flash effect shader.")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("How fast the flash effect happens (e.g., 0.2 seconds).")]
    [SerializeField] private float flashDuration = 0.2f;

    private Material originalMaterial;
    private Coroutine flashCoroutine;
    
    
   
    [HideInInspector] public bool isUnbreakable = false;
   
    private readonly int isWeakAndDamageableBoolHash = Animator.StringToHash("isWeakAndDamageable");

    // This will be a trigger for the recovery animation.
    private readonly int recoverPostureTriggerHash = Animator.StringToHash("recoverPosture");

    
    [SerializeField] private float guardCrushStunDuration = 1.0f;

    [Header("Juggling & Ground Check")]
    [Tooltip("An empty GameObject at the enemy's feet to check for the ground.")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("The radius of the ground check circle.")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [Tooltip("The layer(s) that should be considered ground.")]
    [SerializeField] private LayerMask groundLayer;

    // --- Private state for juggling ---
    private bool isGrounded = true;
    private bool wasGrounded = true;
    private bool isInJuggleState = false;

    // --- New Animation Hashes ---
    private readonly int landHitTriggerHash = Animator.StringToHash("LandHit");

    public bool isFinishable { get; private set; } = false;

    // --- ADD THIS NEW ANIMATION HASH ---
    private readonly int getFinishedTriggerHash = Animator.StringToHash("GetFinished");
    private bool isDying = false;
    private readonly int finishableStateTriggerHash = Animator.StringToHash("FinishableState");
    [Header("Hit Sounds")]
    [Range(0f, 1f)][SerializeField] private float hitSfxVolume = 1f;
    [SerializeField] private AudioClip[] hitSoundClips;
    [SerializeField] private AudioClip[] blockSoundClips;
    private AudioSource hitSfxSource;

    [Header("Transition Lock")]
    [SerializeField] private string[] protectedTriggers; // fill in inspector with all your Any State trigger names
    private bool isTransitionLocked = false;

    private Coroutine stunSequenceCoroutine;

    private static readonly int GetCounteredLaunchHash = Animator.StringToHash("GetCounteredLaunch");
    private static readonly int GetCounteredAimDownHash = Animator.StringToHash("GetCounteredAimDown");
    [Header("Counter Blood VFX")]
    [SerializeField] private GameObject counterBloodPrefab;
    [SerializeField] private Transform counterBloodSpawnPoint;

    [Header("Twin Boss Shared Health")]
    [SerializeField] private MonoBehaviour otherTwinHealth; // drag the other twin's health here
    private bool isSharedDamageCall = false;
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    void Awake()
    {
        currentHealth = maxHealth;
        currentGuard = maxGuard;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentGuard = maxGuard;
        followAI = GetComponent<BootTwinAttack>();
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }

        }

        if (healthSlider != null)
        {
            // Calculate the health percentage (a value from 0.0 to 1.0)
            healthSlider.value = (float)currentHealth / maxHealth;
        }
        // --- THIS IS THE FIX. THIS IS THE MISSING PIECE. ---
        // If we have a player target, get their attack script so we can talk to it.
        if (playerTarget != null)
        {
            zreyAttacks = playerTarget.GetComponent<ZreyAttacks>();
        }
        if (knightMeshRenderer == null)
        {
            // Try to find it automatically if not assigned.
            knightMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (knightMeshRenderer != null)
        {
            originalMaterial = knightMeshRenderer.material;
        }
        else
        {
            Debug.LogError("FATAL ERROR: Knight Mesh Renderer is not assigned and could not be found!", this);
        }
       
        TriggerHealthUpdate();
        TriggerPostureUpdate();
       
        hitSfxSource = gameObject.AddComponent<AudioSource>();
        hitSfxSource.playOnAwake = false;
        hitSfxSource.spatialBlend = 0f;
    }
    private void UpdateHealthUI()
    {
        if (healthSlider == null) return;

        // Calculate the target value
        float healthPercent = (float)currentHealth / maxHealth;
        healthSlider.value = healthPercent;

        // --- NEW LOGIC: Hide the fill if health is zero ---
        if (healthSlider == null) return;
    }
    void LateUpdate()
    {
        // If we have a reference to the canvas...
        if (healthBarCanvasTransform != null)
        {
            // --- THE "MANUAL FOLLOW" LOGIC ---

            // 1. Force its WORLD position to be the knight's position PLUS our desired offset.
            // Because it's no longer a child, this is the ONLY thing making it move.
            healthBarCanvasTransform.position = transform.position + canvasOffset;

            // 2. We still force its rotation to be zero, just as a final guarantee
            // that nothing else in the scene can accidentally rotate it.
            healthBarCanvasTransform.rotation = Quaternion.identity;
        }
    }
    // --- REPLACE your old UpdatePostureUI method with this one ---
    private void UpdatePostureUI()
    {
        if (postureSlider == null) return;

        // Calculate the target value
        float guardPercent = currentGuard / maxGuard;
        postureSlider.value = guardPercent;

        // --- NEW LOGIC: Hide the fill if posture is zero ---
        if (postureSlider == null) return;
    }
    private void PlayRandomHitSound()
    {
        if (hitSoundClips == null || hitSoundClips.Length == 0 || hitSfxSource == null) return;
        AudioClip clip = hitSoundClips[Random.Range(0, hitSoundClips.Length)];
        if (clip != null) hitSfxSource.PlayOneShot(clip, hitSfxVolume);
    }
    private void PlayRandomBlockSound()
    {
        if (blockSoundClips == null || blockSoundClips.Length == 0 || hitSfxSource == null) return;
        AudioClip clip = blockSoundClips[Random.Range(0, blockSoundClips.Length)];
        if (clip != null) hitSfxSource.PlayOneShot(clip, hitSfxVolume);
    }
    public void UpdateVolume(float masterVolume)
    {
        hitSfxVolume = masterVolume;
    }
    void Update()
    {
        if (isFinishable)
        {
            return; // Stop the rest of the Update function from running.
        }
        if (!isGuardBroken && !isBlocking)
        {
            timeSinceLastBlock += Time.deltaTime;
            if (timeSinceLastBlock >= guardRecoveryDelay)
            {
                currentGuard = Mathf.MoveTowards(currentGuard, maxGuard, guardRecoveryRate * Time.deltaTime);
                TriggerPostureUpdate(); // Call the UI update function here
            }
        }
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isDying && !wasGrounded && isGrounded)
        {
            // If yes, this is the moment to transition to the finishable state.
            Debug.LogWarning("--- Dying enemy has landed. Transitioning to finishable state. ---");
            TransitionToFinishable();
        }
        // 2. Check if we are in a juggle state.
        if (isInJuggleState)
        {
            // A. If we are falling (Y velocity is negative)...
            if (rb.linearVelocity.y < -0.1f)
            {
                // ...play the falling hit animation.
                // We can call PlayHitReaction because it already handles resetting other triggers.
                PlayHitReaction("fall");
            }

            // B. If we have just landed (we were NOT grounded, but now we ARE)...
            if (!wasGrounded && isGrounded)
            {
                Debug.LogWarning("--- Enemy has LANDED from juggle ---");
                // ...play the landing animation.
                animator.SetTrigger(landHitTriggerHash);
                // ...and exit the juggle state.
                isInJuggleState = false;
            }
        }
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

    #region UI Update Logic

    private void TriggerHealthUpdate()
    {
        if (healthUpdateCoroutine != null) StopCoroutine(healthUpdateCoroutine);
        healthUpdateCoroutine = StartCoroutine(UpdateHealthBarRoutine());
    }

    private void TriggerPostureUpdate()
    {
        if (postureUpdateCoroutine != null) StopCoroutine(postureUpdateCoroutine);
        postureUpdateCoroutine = StartCoroutine(UpdatePostureBarRoutine());
    }

    private IEnumerator UpdateHealthBarRoutine()
    {
        float targetFill = (float)currentHealth / maxHealth;

        // Main health slider snaps instantly
        if (healthSlider != null)
        {
            healthSlider.value = targetFill;
        }

        // Update the visibility of the fill object
        if (healthSlider.fillRect != null)
        {
            healthSlider.fillRect.gameObject.SetActive(targetFill > 0);
        }

        // Animate the delayed fill
        if (healthDelayedFill != null)
        {
            yield return new WaitForSeconds(healthFillDelay);
            float currentFill = healthDelayedFill.value;

            while (Mathf.Abs(currentFill - targetFill) > 0.01f)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * healthFillSpeed);
                healthDelayedFill.value = currentFill;
                yield return null;
            }
            healthDelayedFill.value = targetFill;
        }
    }

    private IEnumerator UpdatePostureBarRoutine()
    {
        float targetFill = currentGuard / maxGuard;
        UpdatePostureUI(); // This instantly updates the main fill's visibility

        // --- Part 1: Animate the MAIN posture slider ---
        if (postureSlider != null)
        {
            float currentSliderValue = postureSlider.value;
            // This loop makes the main yellow bar animate smoothly
            while (Mathf.Abs(currentSliderValue - targetFill) > 0.01f)
            {
                // Use the specific posture speed
                currentSliderValue = Mathf.Lerp(currentSliderValue, targetFill, Time.deltaTime * postureFillSpeed);
                postureSlider.value = currentSliderValue;
                yield return null;
            }
            postureSlider.value = targetFill; // Snap to final value
        }

        // --- Part 2: Animate the DELAYED posture slider ---
        if (postureDelayedFill != null)
        {
            // Use the specific posture delay
            yield return new WaitForSeconds(postureFillDelay);
            float currentDelayedFill = postureDelayedFill.value;

            // This loop makes the background bar catch up.
            // It works for both going down AND going up.
            while (Mathf.Abs(currentDelayedFill - targetFill) > 0.01f)
            {
                // Use the specific posture speed
                currentDelayedFill = Mathf.Lerp(currentDelayedFill, targetFill, Time.deltaTime * postureFillSpeed);
                postureDelayedFill.value = currentDelayedFill;
                yield return null;
            }
            postureDelayedFill.value = targetFill; // Snap to final value
        }
    }

    #endregion
    public void KillAllMomentum()
    {
        // Failsafe: If there is no Rigidbody, do nothing.
        if (rb == null) return;

        // This is the brake slam. It sets the velocity to zero for this frame.
        rb.linearVelocity = Vector2.zero;

        // Optional: You can also kill angular (rotational) velocity if needed.
        rb.angularVelocity = 0f;

        Debug.LogWarning("--- BRAKE SLAM! Knight momentum killed by animation event. ---");
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
            NotifyOtherTwin(damage);
            TriggerHealthUpdate();
            Debug.Log("<color=red>GUARD BROKEN! Dealt " + damage + " direct damage.</color>");
            SpawnBloodVFX();
            PlayRandomHitSound();
            // Apply the normal hit knockback
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, knockbackDistance, knockbackDuration, 0, 0));

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
            PlayRandomBlockSound(); // ADD THIS
            CameraShakerHandler.Shake(CameraShakeParry);
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, blockRecoilDistance, blockRecoilDuration, 0, 0));

            timeSinceLastBlock = 0f; // Reset the recovery timer.
            currentGuard -= guardDamagePerBlock; // SUBTRACT damage from the guard meter.
            Debug.Log("Current Guard: " + currentGuard + " / " + maxGuard);



            // Apply knockback to the PLAYER.
            ZreyAttacks playerAttacks = attacker.GetComponent<ZreyAttacks>();
            if (playerAttacks != null)
            {
                playerAttacks.ApplyKnockback(transform, playerKnockbackOnBlock, playerKnockbackDurationOnBlock);
            }
            
            if (currentGuard <= 0)
            {
                StartCoroutine(GuardBrokenSequence()); // GUARD BREAK!
            }
            // IMPORTANT: Exit the function. Do not take damage or get knocked back.
            return;
        }
        PlayHitReaction(hitType);
        currentHealth -= damage;
        NotifyOtherTwin(damage);
        TriggerHealthUpdate();
        SpawnBloodVFX();
        PlayRandomHitSound();
        Debug.Log(transform.name + " took " + damage + " damage. Health is now: " + currentHealth);

        // --- KNOCKBACK LOGIC ---
        // Stop any previous knockback to handle rapid hits.
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, knockbackDistance, knockbackDuration, 0f, 0f));
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void TakeDamageCounter(int damage)
    {
        currentHealth -= damage; // Fixed damage for counter hits.
        NotifyOtherTwin(damage);
        TriggerHealthUpdate();
        Debug.Log(transform.name + " took 10 damage from counter. Health is now: " + currentHealth);
        TriggerHealthUpdate();
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void TakeUpperAttack(AttackData attackData)
    {
        if (isFinishable || isUnbreakable || isDying) return;

        Transform attacker = playerTarget;

        // If guard is already broken — just launch him, no block check needed
        if (isGuardBroken)
        {
            PlayHitReaction(attackData.hitType);
            currentHealth -= attackData.damage;
            NotifyOtherTwin(attackData.damage);
            TriggerHealthUpdate();
            SpawnBloodVFX();
            PlayRandomHitSound();
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDamageEffect());
            TriggerHealthUpdate();
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(
                attacker,
                attackData.knockbackDistance,
                attackData.knockbackDuration,
                attackData.upwardForce,
                attackData.downwardForce
            ));
            if (currentHealth <= 0) Die();
            return;
        }

        if (isBlocking)
        {
            TriggerPostureUpdate();
            Debug.LogWarning("--- Upper Attack BLOCKED! Applying Guard Damage. ---");
            currentGuard -= attackData.guardDamage;
            timeSinceLastBlock = 0f;
            animator.SetTrigger(block2TriggerHash);
            CameraShakerHandler.Shake(CameraShakeParry);
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, blockRecoilDistance, blockRecoilDuration, 0, 0));
            if (currentGuard <= 0) StartCoroutine(GuardBrokenSequence());
        }
        else
        {
            Debug.Log("<color=yellow>--- Upper Attack LANDED! Launching enemy. ---</color>");
            PlayHitReaction(attackData.hitType);
            currentHealth -= attackData.damage;
            NotifyOtherTwin(attackData.damage);
            TriggerHealthUpdate();
            SpawnBloodVFX();
            PlayRandomHitSound();
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDamageEffect());
            TriggerPostureUpdate();
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(
                attacker,
                attackData.knockbackDistance,
                attackData.knockbackDuration,
                attackData.upwardForce,
                attackData.downwardForce
            ));
            if (currentHealth <= 0) Die();
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
    private IEnumerator KnockbackRoutine(Transform attacker, float distance, float duration, float upwardForce, float downwardForce)
    {
        isBeingKnockedBack = true;

        // --- THIS IS THE FINAL, FUCKING, GUARANTEED FIX ---

        // 1. Failsafe: If we don't have a reference to our own AI brain, we can't get direction.
        if (followAI != null)
        {
            followAI.FacePlayer();
        }
        else
        {
            Debug.LogError("KnightFollow script (followAI) is not assigned! Knockback will fail.", this);
            isBeingKnockedBack = false;
            yield break;
        }

        // 2. THE REAL LOGIC: ASK OUR OWN BRAIN which way we are facing.
        //    The knockback direction is the OPPOSITE of our facing direction.
        //    If we are facing right (+1), knockback is left (-1).
        //    If we are facing left (-1), knockback is right (+1).
        float knockbackDirectionX = followAI.IsFacingRight() ? -1f : 1f;

        // 3. Create the final, clean knockback direction vector.
        Vector2 knockbackDirection = new Vector2(knockbackDirectionX, 0);
        float horizontalVelocity = (distance / duration) * knockbackDirectionX;

        if (rb != null)
        {
            float initialYVelocity = 0f;
            if (upwardForce > 0) initialYVelocity = upwardForce;
            if (downwardForce > 0) initialYVelocity = -downwardForce;

            // We set the velocity once at the beginning.
            rb.linearVelocity = new Vector2(horizontalVelocity, initialYVelocity);
        }

        Debug.Log($"<color=lime>--- KNIGHT KNOCKBACK ---</color>\n" +
                  $"Knight is facing right: {followAI.IsFacingRight()}\n" +
                  $"Final Knockback Direction: {knockbackDirection.x}");

        Vector2 knockbackVelocity = knockbackDirection * (distance / duration);

        float timer = 0f;
        while (timer < duration)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }


        knockbackCoroutine = null;
        isBeingKnockedBack = false;
    }

    private void Die()
    {
        currentHealth = 0;
        TriggerHealthUpdate();
        if (isDying || isFinishable) return;

        Debug.LogWarning($"--- {transform.name} has been defeated! ---");

        // --- THIS IS THE NEW, CONTEXT-AWARE LOGIC ---
        if (isGrounded)
        {
            // CASE 1: We are on the ground. Transition to finishable immediately.
            Debug.Log("Enemy died on the ground. Becoming finishable now.");
            TransitionToFinishable();
        }
        else
        {
            // CASE 2: We are in the air. Just mark ourselves as "dying".
            // The Update() method will handle the rest upon landing.
            Debug.Log("Enemy died in the air. Will become finishable upon landing.");
            isDying = true;
            // We DO NOT make the Rigidbody kinematic here. We let it fall.
        }
    }
    private void TransitionToFinishable()
    {
        // --- Start the fade-out coroutine ---
        if (healthBarCanvasTransform != null)
        {
            StartCoroutine(FadeOutUI());
        }

        // Set the state flags
        isFinishable = true;
        isDying = false;
        if (animator != null)
        {
            animator.SetTrigger(finishableStateTriggerHash);
        }

        
        if (followAI != null) followAI.enabled = false;

        // Make the Rigidbody kinematic to freeze it in place
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // --- THE LINE TO DELETE WAS HERE ---
        // if (healthBarCanvasTransform != null)
        // {
        //     healthBarCanvasTransform.gameObject.SetActive(false); // <--
        //     DELETE THIS BLOCK
        // }

        // Force the guard to 0 and update the UI one last time to ensure it's hidden.
        currentGuard = 0;
        TriggerPostureUpdate();
    }

    public void TakePostureDamageOnParry()
    {
        // Failsafe: If the guard is already broken, we can't break it again.
        if (isGuardBroken) return;
        TriggerPostureUpdate();
        Debug.Log($"<color=orange>KNIGHT'S POSTURE DAMAGED BY PARRY! Taking {guardDamageOnParried} guard damage.</color>");

        // Subtract the damage from the guard meter.
        currentGuard -= guardDamageOnParried;
        timeSinceLastBlock = 0f; // A parry is a form of block, so reset the recovery timer.

        // Check if this parry was the one that broke the guard.
        if (currentGuard <= 0)
        {
            // GUARD BREAK!
            // We don't need to call GetParried() here, because the GuardBrokenSequence
            // already plays a stun animation.
            StartCoroutine(GuardBrokenSequence());
        }
    }
    private IEnumerator FadeOutUI()
    {
        if (healthBarCanvasTransform == null) yield break;
        CanvasGroup canvasGroup = healthBarCanvasTransform.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            healthBarCanvasTransform.gameObject.SetActive(false);
            yield break;
        }

        float timer = 0f;
        float startAlpha = canvasGroup.alpha;
        while (timer < uiFadeOutDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / uiFadeOutDuration);
            yield return null;
        }
        healthBarCanvasTransform.gameObject.SetActive(false);
    }

    private void TriggerRandomBlock()
    {
        int nextBlockIndex;

        // --- THE "SMART RANDOMNESS" LOGIC ---

        // 1. Check if the last animation has been played twice in a row.
        if (consecutivePlayCount >= 1)
        {
            // A. If YES, we MUST pick a DIFFERENT animation.
            Debug.Log("<color=orange>Forcing a different block animation!</color>");
            do
            {
                nextBlockIndex = Random.Range(1, 3); // Pick a number from 1, 2, or 3.
            } while (nextBlockIndex == lastBlockAnimationIndex); // Keep picking until it's a new one.
        }
        else
        {
            // B. If NO, we can pick any animation randomly.
            nextBlockIndex = Random.Range(1, 3); // Pick a number from 1, 2, or 3.
        }

        // --- UPDATE THE STATE FOR THE NEXT BLOCK ---

        // 2. Check if the new pick is the same as the last one.
        if (nextBlockIndex == lastBlockAnimationIndex)
        {
            // If it's the same, increment the consecutive counter.
            consecutivePlayCount++;
        }
        else
        {
            // If it's different, reset the counter to 1.
            consecutivePlayCount = 1;
        }

        // 3. Remember this new animation as the "last one" for the next time.
        lastBlockAnimationIndex = nextBlockIndex;

        Debug.Log($"Playing Block Animation #{nextBlockIndex}. Consecutive Count: {consecutivePlayCount}");

        // --- TRIGGER THE CHOSEN ANIMATION ---

        // 4. Use a switch to fire the correct trigger based on our smart decision.
        switch (nextBlockIndex)
        {
            case 1:
                animator.ResetTrigger(blockTriggerHash);
                SafeSetTrigger(blockTriggerHash, "block");
                break;
            case 2:
                animator.ResetTrigger(block2TriggerHash);
                SafeSetTrigger(block2TriggerHash, "block2");
                break;
          
        }
    }

    public void PerformBlock(Transform player)
    {
        if (isDying || isFinishable) return;
        if (isGuardBroken) return;
        GetComponent<BootTwinAttack>()?.ForceResetAttackState();
        // This is the same logic that used to be in OnPlayerAttackTelegraphed.
        // We still check for stun and range as a final safety measure.
        if (isGuardBroken || isBeingKnockedBack || Vector2.Distance(transform.position, player.position) > blockRange)
        {
            return;
        }

        // Because the brain has given the command, we execute the block.
        TriggerRandomBlock();
    }
    public void OnPlayerAttackTelegraphed(Transform player)
    {
        
        // --- THE HYPER ARMOR LOGIC ---
        // If the Follow brain is already locked in an attack, DO NOTHING.
        if (!isInJuggleState && followAI != null && (followAI.IsAttacking() || followAI.IsLaunching() || followAI.IsAirLaunching() || followAI.IsThrowingRocks() || followAI.IsSpecialAttacking()))
        {
            Debug.Log("<color=red>AI is ATTACKING. Ignoring player attack telegraph.</color>");
            return;
        }

        PerformBlock(player);
    }
    private IEnumerator FlashDamageEffect()
    {
        // 1. SWAP to the flash material.
        knightMeshRenderer.material = flashMaterial;

        // 2. ANIMATE the flash amount from 0 -> 1.
        float elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(0f, 1f, elapsedTime / (flashDuration / 2));
            flashMaterial.SetFloat("_FlashAmount", flashAmount);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 3. ANIMATE the flash amount from 1 -> 0.
        elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(1f, 0f, elapsedTime / (flashDuration / 2));
            flashMaterial.SetFloat("_FlashAmount", flashAmount);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 4. RESTORE the original material.
        knightMeshRenderer.material = originalMaterial;
        flashCoroutine = null; // Mark the coroutine as finished.
    }
    private IEnumerator GuardBrokenSequence()
    {
        if (stunSequenceCoroutine != null)
        {
            StopCoroutine(stunSequenceCoroutine);
            stunSequenceCoroutine = null;
        }

        isGuardBroken = false;
        isBlocking = false;

        // DON'T reset the bool yet — fire the trigger first
        yield return null;

        animator.SetTrigger(guardBrokenTriggerHash);  // fires cleanly
        animator.SetBool(isWeakAndDamageableBoolHash, false); // reset AFTER
        currentGuard = 0;
        TriggerPostureUpdate();

        stunSequenceCoroutine = StartCoroutine(StunSequence(guardBrokenStunDuration));
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
        bool isMidAirJuggleReaction = (hitType.ToLower() == "rightup" || hitType.ToLower() == "leftup");

        // If it's a MID-AIR juggle reaction BUT we are currently on the ground...
        if (isMidAirJuggleReaction && isGrounded)
        {
            // ...OVERRIDE the hit type and force a standard, grounded reaction.
            // We DO NOT check for "hitup" here. "hitup" is the launcher and is allowed.
            Debug.LogWarning($"Hit reaction '{hitType}' overridden to 'back' because enemy is grounded.");
            hitType = "back";
        }
        // 3. Now, set the new trigger.
        switch (hitType.ToLower())
        {
            case "up":
                SafeSetTrigger(getHitUpTriggerHash, "getHitUp");
                break;
            case "down":
                SafeSetTrigger(getHitDownTriggerHash, "getHitDown");
                break;
            case "back":
                SafeSetTrigger(getHitBackTriggerHash, "getHitBack");
                break;
            case "fall":
                animator.SetTrigger(fallTriggerHash);
                break;
            case "finalback":
                animator.SetTrigger(finalBackTriggerHash);
                break;

            case "hitup":
                animator.SetTrigger(UpTriggerHash);
                isInJuggleState = true;
                break;
            case "rightup":
                animator.SetTrigger(RightUpTriggerHash);
                isInJuggleState = true;
                break;
            case "leftup":
                animator.SetTrigger(LeftUpTriggerHash);
                isInJuggleState = true;
                break;
            default:
                animator.SetTrigger(getHitBackTriggerHash);
                break;
        }
    }
    private void SpawnWoundEffect()
    {
        // --- Safety Checks ---
        if (woundEffectPrefab == null || woundSpawnPoint == null)
        {
            // If either the prefab or the spawn point is missing, do nothing.
            // This prevents errors if you don't want to use the effect.
            return;
        }

        // --- The Spawn Logic ---
        // Instantiate the wound prefab at the spawn point's position and rotation.
        // We make it a child of the spawn point so that if the knight moves, the wound moves with him.
        Instantiate(woundEffectPrefab, woundSpawnPoint.position, woundSpawnPoint.rotation, woundSpawnPoint);

        Debug.Log("<color=purple>Wound Effect Spawned!</color>");
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
    public void ApplyDamageAndKnockback(AttackData attackData)
    {
        if (isDying || isFinishable)
        {
            Debug.Log("Damage ignored: Knight is already defeated.");
            return;
        }
        if (!isGuardBroken && !isInJuggleState &&  followAI != null && (followAI.IsAttacking() || followAI.IsLaunching() || followAI.IsAirLaunching() || followAI.IsThrowingRocks() || followAI.IsSpecialAttacking()))
        {
            // 2. If YES, do NOTHING. The knight is invincible during his combo.
            Debug.Log("<color=red>KNIGHT IS INVINCIBLE (mid-combo)! Damage ignored.</color>");
            // Optionally, you can spawn a "clank" effect here to show the attack was ineffective.
            return;
        }
        if (isUnbreakable)
        {
            Debug.Log("<color=red>KNIGHT IS INVINCIBLE! Damage ignored.</color>");
            return;
        }
        // --- 1. READ THE DATA from the Scriptable Object ---
        int damage = attackData.damage;
        string hitType = attackData.hitType;
        float distance = attackData.knockbackDistance;
        float duration = attackData.knockbackDuration;
        float upward = attackData.upwardForce;
        float downward = attackData.downwardForce;
        Transform attacker = GameObject.FindGameObjectWithTag("Player").transform;
        if (attacker == null) return;

        Debug.Log($"<color=red>--- ATTACK DATA RECEIVED ---</color>\n" +
                  $"Damage: {damage}, HitType: {hitType}, Knockback: {distance}, Duration: {duration}");

        // --- 2. APPLY THE LOGIC (This part is exactly the same as before) ---
        if (isGuardBroken)
        {
            TriggerHealthUpdate();
            PlayHitReaction(hitType);
            currentHealth -= damage;
            NotifyOtherTwin(damage);
            TriggerHealthUpdate();
            SpawnBloodVFX();
            PlayRandomHitSound();
            SpawnWoundEffect();
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDamageEffect());
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, distance, duration, upward, downward));
            if (currentHealth <= 0) Die();
            return;
        }

        if (isBlocking)
        {
            TriggerPostureUpdate();
            ZreyAttacks playerAttacks = attacker.GetComponent<ZreyAttacks>();
            if (playerAttacks != null)
            {
                Debug.Log($"<color=red>--- COMMAND SENT ---</color>\n" +
                    $"COMMANDER: KnightHealth (isBlocking)\n" +
                    $"TARGET: ZreyAttacks.ApplyKnockback\n" +
                    $"SOURCE (Attacker): {this.transform.name} at position {this.transform.position}\n" +
                    $"Knockback Force: {playerKnockbackOnBlock}, Duration: {playerKnockbackDurationOnBlock}");
                // --- END OF FIX ---
                // 2. We call ApplyKnockback and pass THIS knight's transform as the source of the knockback.
                playerAttacks.ApplyKnockback(this.transform, playerKnockbackOnBlock, playerKnockbackDurationOnBlock);
            }
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, blockRecoilDistance, blockRecoilDuration, upward, downward));
            timeSinceLastBlock = 0f;

            if (blockSparksPrefab != null && blockSparksPoint != null)
            {
                Instantiate(blockSparksPrefab, blockSparksPoint.position, blockSparksPoint.rotation);
            }
            PlayRandomBlockSound(); // ADD THIS
            CameraShakerHandler.Shake(CameraShakeParry);
            
            currentGuard -= guardDamagePerBlock;
            if (currentGuard <= 0) StartCoroutine(GuardBrokenSequence());
            return;
        }
        TriggerHealthUpdate();
        PlayHitReaction(hitType);
        currentHealth -= damage;
        NotifyOtherTwin(damage);
        TriggerHealthUpdate();
        SpawnBloodVFX();
        PlayRandomHitSound();
        SpawnWoundEffect();
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamageEffect());
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, distance, duration, upward, downward));
        if (currentHealth <= 0) Die();
    }
    private void NotifyOtherTwin(int damage)
    {
        if (isSharedDamageCall) return;
        if (otherTwinHealth == null) return;

        isSharedDamageCall = true;

        // Try both types
        BootTwinHealth boot = otherTwinHealth as BootTwinHealth;
        GauntletTwinHealth gauntlet = otherTwinHealth as GauntletTwinHealth;

        if (boot != null) boot.ReceiveSharedDamage(damage);
        else if (gauntlet != null) gauntlet.ReceiveSharedDamage(damage);

        isSharedDamageCall = false;
    }
    public void ReceiveSharedDamage(int damage)
    {
        if (isSharedDamageCall) return;
        isSharedDamageCall = true;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        TriggerHealthUpdate(); // this already exists

        if (currentHealth <= 0) Die();

        isSharedDamageCall = false;
    }
    public void IgnoreAnyTransition()
    {
        if (isTransitionLocked) return;
        isTransitionLocked = true;
        foreach (string t in protectedTriggers)
            animator.ResetTrigger(t);
    }

    public void AcceptAnyTransition()
    {
        isTransitionLocked = false;
    }
    private void SafeSetTrigger(int hash, string triggerName)
    {
        if (isTransitionLocked)
        {
            Debug.Log($"[BootTwin] Trigger '{triggerName}' blocked — transition locked.");
            return;
        }
        animator.SetTrigger(hash);
    }
    public void TriggerStun(float stunDuration)
    {
        if (isGuardBroken) return;
        stunSequenceCoroutine = StartCoroutine(StunSequence(stunDuration));
    }

    // This new coroutine contains the logic that used to be in GuardBrokenSequence.
    private IEnumerator StunSequence(float stunDuration)
    {
        Debug.LogWarning($"--- KNIGHT STUN SEQUENCE STARTED (Duration: {stunDuration}s) ---");

        // --- PHASE 1: ENTER THE STUNNED STATE (Unchanged) ---
        isGuardBroken = true;
        isUnbreakable = false;
        isBlocking = false;
        animator.SetBool(isWeakAndDamageableBoolHash, true);

        // --- PHASE 2: THE VULNERABLE LOOP (Unchanged) ---
        yield return new WaitForSeconds(stunDuration);

        // --- THIS IS THE CRITICAL FIX ---
        // Before we start the recovery, check if the knight has been defeated and is finishable.
        if (isFinishable)
        {
            Debug.LogWarning("Knight is finishable. Aborting posture recovery.");
            animator.SetBool(isWeakAndDamageableBoolHash, false);
            isGuardBroken = false;
            yield break;
        }
        // --- END OF FIX ---

        // --- PHASE 3: THE RECOVERY (This code will now only run if the knight is NOT finishable) ---
        Debug.Log("<color=green>Knight has recovered from stun.</color>");
        animator.SetBool(isWeakAndDamageableBoolHash, false);
        animator.SetTrigger(recoverPostureTriggerHash);

        isGuardBroken = false;
        timeSinceLastBlock = 0f;

        // Dynamic posture recovery animation
        float recoveryStartTime = Time.time;
        float recoveryDuration = 1.0f;
        float startingGuard = currentGuard;

        while (Time.time < recoveryStartTime + recoveryDuration)
        {
            float progress = (Time.time - recoveryStartTime) / recoveryDuration;
            currentGuard = Mathf.Lerp(startingGuard, maxGuard, progress);
            TriggerPostureUpdate();
            yield return null;
        }

        currentGuard = maxGuard;
        TriggerPostureUpdate();
    }
 
    public void GetParried(Transform playerTransform)
    {
        Debug.Log("<color=orange>KNIGHT HAS BEEN PARRIED!</color>");

        // Play a stunned/parried animation.
        animator.SetTrigger("getParried"); // Make sure you have this trigger in your Knight's Animator.


    }
    private IEnumerator ParryKnockbackRoutine(Transform player, float distance, float duration)
    {
        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        Debug.LogWarning($"--- Knight Executing PARRY KNOCKBACK. Distance: {distance}, Duration: {duration} ---");

        // 1. Set the state flag.
        isBeingKnockedBack = true;

        // 2. Determine direction. The knockback is always AWAY from the player who parried.
        float direction = (transform.position.x > player.position.x) ? 1f : -1f;
        Vector2 knockbackVelocity = new Vector2(direction * (distance / duration), 0);

        // 3. Apply the velocity for the specified duration.
        float timer = 0f;
        while (timer < duration)
        {
            if (rb != null)
            {
                rb.linearVelocity = knockbackVelocity;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. Clean up the state.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        isBeingKnockedBack = false;

    }

    public void MakeFinishable()
    {
        if (isFinishable) return; // Already finishable, do nothing.

        Debug.LogError("--- KNIGHT IS NOW FINISHABLE! ---");
        isFinishable = true;
        // You might want to play a "dazed" or "posture broken" animation here.
        // animator.SetTrigger("postureBroken");
    }

    // --- ADD THIS NEW PUBLIC METHOD ---
    /// <summary>
    /// Called by the player's attack script during the finisher sequence.
    /// </summary>
    public void ExecuteFinisher()
    {
        Debug.LogError("--- KNIGHT: ExecuteFinisher command received! ---");
        isFinishable = false; // No longer finishable
        animator.SetTrigger(getFinishedTriggerHash);
        Destroy(gameObject, 7f);
    }
    public bool IsStunned()
    {
        // The knight is considered "stunned" if their guard is broken OR if they are being knocked back by a hit.
        return isGuardBroken || isBeingKnockedBack;
    }
    /// <summary>
    /// Called by an Animation Event at the END of the block animation.
    /// </summary>
    public void CloseBlockWindow()
    {
        isBlocking = false;
        Debug.Log("Knight: Block Window CLOSED");
    }
    public void BecomeInvincible()
    {
        isUnbreakable = true;
        Debug.LogWarning("--- KNIGHT IS NOW INVINCIBLE (Animation Event) ---");
    }

    /// <summary>
    /// Called by an Animation Event to make the knight vulnerable again.
    /// </summary>
    public void BecomeVulnerable()
    {
        isUnbreakable = false;
        Debug.Log("<color=green>--- Knight is now VULNERABLE (Animation Event) ---</color>");
    }
    public bool IsGrounded()
    {
        // We already calculate this 'isGrounded' boolean in the Update loop.
        // We just need to expose its value.
        return isGrounded;
    }

    public void PlayGetCounteredLaunch()
    {
        followAI.ForceResetAttackState();
        followAI.isBeingCountered = true;        // ← lock BEFORE animation plays
        followAI.LockFlip();
        animator.SetTrigger(GetCounteredLaunchHash);
    }
    public void PlayGetCounteredAimDown()
    {
        followAI.ForceResetAttackState();
        followAI.isBeingCountered = true;
        followAI.LockFlip();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
        }
        animator.SetTrigger(GetCounteredAimDownHash);
    }
    public void EndCounter()
    {
        followAI.isBeingCountered = false;
        followAI.UnlockFlip();
    }
    public void SpawnCounterBlood()
    {
        if (counterBloodPrefab == null || counterBloodSpawnPoint == null) return;
        Instantiate(
            counterBloodPrefab,
            counterBloodSpawnPoint.position,
            counterBloodPrefab.transform.rotation
        );
    }

   
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
