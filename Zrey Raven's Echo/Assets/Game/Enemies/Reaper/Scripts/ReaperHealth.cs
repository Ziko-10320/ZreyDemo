using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
[RequireComponent(typeof(Rigidbody2D))]
public class ReaperHealth : MonoBehaviour
{
    [Header("Health UI")]
    [Tooltip("The parent Canvas object for the health bars.")]
    [SerializeField] private Transform healthBarCanvasTransform;
    [Tooltip("The vertical offset to position the canvas above the enemy's head.")]
    [SerializeField] private Vector3 canvasOffset = new Vector3(0, 2.5f, 0);

    [Tooltip("The main (top) UI Slider for health.")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("The 'Fill' child object of the Health Slider.")]
    [SerializeField] private GameObject healthFillObject;
    [Tooltip("The secondary (background) Image for the delayed health drop effect.")]
    [SerializeField] private Slider healthDelayedFill;

    [Tooltip("The main (top) UI Slider for posture.")]
    [SerializeField] private Slider postureSlider;
    [Tooltip("The 'Fill' child object of the Posture Slider.")]
    [SerializeField] private GameObject postureFillObject;
    [Tooltip("The secondary (background) Image for the delayed posture drop effect.")]
    [SerializeField] private Slider postureDelayedFill;

    [Header("UI Animation Settings")]
    [SerializeField] private float healthFillSpeed = 5f;
    [Tooltip("How long to wait before the HEALTH delayed-fill bar starts moving.")]
    [SerializeField] private float healthFillDelay = 0.5f;
    [SerializeField] private float postureFillSpeed = 8f;
    [Tooltip("How long to wait before the POSTURE delayed-fill bar starts moving.")]
    [SerializeField] private float postureFillDelay = 0.2f;
    [Tooltip("How long it takes for the health bars to fade out on death.")]
    [SerializeField] private float uiFadeOutDuration = 0.5f;

    private Coroutine healthUpdateCoroutine;
    private Coroutine postureUpdateCoroutine;
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
    [HideInInspector] public bool isBlocking = false; // Is the block window currently active?
    private bool canBlock = true; // Can the knight attempt another block?
    private float blockCooldown = 1.5f; // How long the knight must wait between blocks.

    // --- Components & Animation Hashes ---
    private Animator animator; // We need the animator now.
    private readonly int blockTriggerHash = Animator.StringToHash("block");
    private readonly int block2TriggerHash = Animator.StringToHash("block2");
    private readonly int block3TriggerHash = Animator.StringToHash("block3");
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
    private bool isGuardBroken = false;
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


    private ReaperFollow followAI;

    [Header("Flash Damage Effect")]
    [Tooltip("The SkinnedMeshRenderer of the knight's 3D model.")]
    [SerializeField] private SkinnedMeshRenderer knightMeshRenderer;

    [Tooltip("The special material that has the flash effect shader.")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("How fast the flash effect happens (e.g., 0.2 seconds).")]
    [SerializeField] private float flashDuration = 0.2f;

    private Material originalMaterial;
    private Coroutine flashCoroutine;
    private ReaperAI ReaperAI;
    [Header("Counter Attack Settings")]

    [SerializeField] private int minBlocksToCounter = 3;
    [Tooltip("The maximum number of blocks before a counter attack is possible.")]
    [SerializeField] private int maxBlocksToCounter = 6;

    // 3. ADD BACK the variable to store the current random threshold.
    private int blocksNeededForNextCounter = 0;

    private int blocksSinceLastCounter = 0;
    [HideInInspector] public bool isUnbreakable = false;
    private ReaperAttack ReaperAttack;
    private readonly int isWeakAndDamageableBoolHash = Animator.StringToHash("isWeakAndDamageable");

    // This will be a trigger for the recovery animation.
    private readonly int recoverPostureTriggerHash = Animator.StringToHash("recoverPosture");

    [SerializeField] private float counterStunDuration = 1.5f;
    [SerializeField] private float guardCrushStunDuration = 1.0f;

    [Header("Finisher Settings")]
    [Tooltip("Is the enemy currently in a state where they can be finished?")]
    public bool isFinishable { get; private set; } = false;

    [Tooltip("The animation trigger for when the enemy is being finished.")]
    private readonly int takeFinisherTriggerHash = Animator.StringToHash("TakeFinisher");
    [Header("Decapitation Finisher Settings")]
    [Tooltip("The separate Head prefab to be spawned when decapitated.")]
    [SerializeField] private GameObject headPrefab;

    [Tooltip("The point from which the new head prefab will be spawned.")]
    [SerializeField] private Transform headSpawnPoint;

    [Header("Head Ejection Force")]
    [Tooltip("The upward force applied to the severed head.")]
    [SerializeField] private float headUpwardForce = 5f;

    [Tooltip("The horizontal force applied to the severed head.")]
    [SerializeField] private float headSidewaysForce = 3f;

    [Tooltip("The rotational force (torque) applied to the severed head.")]
    [SerializeField] private float headTorque = 10f;

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
    private bool isDying = false;
    private readonly int finishableStateTriggerHash = Animator.StringToHash("FinishableState");
    [Header("Hit Sounds")]
    [Range(0f, 1f)][SerializeField] private float hitSfxVolume = 1f;
    [SerializeField] private AudioClip[] hitSoundClips;
    [SerializeField] private AudioClip[] blockSoundClips;
    private AudioSource hitSfxSource;

    private bool hasHadFirstGuardBreak = false;
    void Awake()
    {
        currentHealth = maxHealth;
        currentGuard = maxGuard;

        if (healthBarCanvasTransform != null)
        {
            if (healthSlider != null) healthSlider.value = (float)currentHealth / maxHealth;
            if (healthDelayedFill != null) healthDelayedFill.value = (float)currentHealth / maxHealth;
            if (postureSlider != null) postureSlider.value = currentGuard / maxGuard;
            if (postureDelayedFill != null) postureDelayedFill.value = currentGuard / maxGuard;
            UpdateHealthUI(); // Call this to set the initial fill visibility
            UpdatePostureUI();

        }
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentGuard = maxGuard;
        followAI = GetComponent<ReaperFollow>();
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
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
        ReaperAI = GetComponent<ReaperAI> ();
        ReaperAttack = GetComponent<ReaperAttack>();
        if (ReaperAI == null)
        {
            Debug.LogError("FATAL ERROR: KnightAI script is missing! The knight will have no brain.", this);
        }
        SetNewCounterThreshold();
        if (groundCheck == null)
        {
            Debug.LogError("FATAL ERROR: Ground Check transform is not assigned on SpearHealth!", this);
        }
        UpdateHealthUI();
        UpdatePostureUI();
        hitSfxSource = gameObject.AddComponent<AudioSource>();
        hitSfxSource.playOnAwake = false;
        hitSfxSource.spatialBlend = 0f;
    }
    void LateUpdate()
    {
        if (healthBarCanvasTransform != null)
        {
            healthBarCanvasTransform.position = transform.position + canvasOffset;
            healthBarCanvasTransform.rotation = Quaternion.identity;
        }
    }

    // --- NEW: All the UI update logic ---
    #region UI Update Logic

    private void UpdateHealthUI()
    {
        if (healthSlider == null) return;
        float healthPercent = (float)currentHealth / maxHealth;
        healthSlider.value = healthPercent;
        if (healthFillObject != null)
        {
            healthFillObject.SetActive(healthPercent > 0);
        }
    }

    private void UpdatePostureUI()
    {
        if (postureSlider == null) return;
        float guardPercent = currentGuard / maxGuard;
        postureSlider.value = guardPercent;
        if (postureFillObject != null)
        {
            postureFillObject.SetActive(guardPercent > 0);
        }
    }

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
        if (healthSlider != null) healthSlider.value = targetFill;
        UpdateHealthUI();

        if (healthDelayedFill != null)
        {
            // Use the specific health delay
            yield return new WaitForSeconds(healthFillDelay);
            float currentFill = healthDelayedFill.value;

            while (Mathf.Abs(currentFill - targetFill) > 0.01f)
            {
                // Use the specific health speed
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
    #endregion
    void Update()
    {
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
                TriggerPostureUpdate();
            }

        }
        if (isGuardBroken && playerTarget != null && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.TryTriggerDashAttackCanvas(playerTarget, transform);
        }
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
        bool tutorialHealthLocked = TutorialManager.Instance != null
     && TutorialManager.Instance.InTutorialMode
     && !TutorialManager.Instance.TutorialCombatUnlocked;

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
           
            if (!tutorialHealthLocked)
                currentHealth -= damage;
            Debug.Log("<color=red>GUARD BROKEN! Dealt " + damage + " direct damage.</color>");
            SpawnBloodVFX();
            PlayRandomHitSound();
            // Apply the normal hit knockback
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, knockbackDistance, knockbackDuration, 0, 0));

            if (!tutorialHealthLocked && currentHealth <= 0)
                    Die();
            return; // Exit the function.
        }

        if (isBlocking)
        {
            Debug.Log("<color=cyan>ATTACK BLOCKED!</color>");
            TriggerPostureUpdate();
            // Spawn sparks effect.
            if (blockSparksPrefab != null && blockSparksPoint != null)
            {
                Instantiate(blockSparksPrefab, blockSparksPoint.position, blockSparksPoint.rotation);
            }
            PlayRandomBlockSound();
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
            blocksSinceLastCounter++;
            if (blocksSinceLastCounter >= blocksNeededForNextCounter)
            {
                if (ReaperAI != null)
                {
                    ReaperAI.TriggerCounterAttack();
                }
            }
            if (currentGuard <= 0)
            {
                StartCoroutine(GuardBrokenSequence()); // GUARD BREAK!
            }
            // IMPORTANT: Exit the function. Do not take damage or get knocked back.
            return;
        }
        PlayHitReaction(hitType);
      

        if (!tutorialHealthLocked)
            currentHealth -= damage;
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
        if (!tutorialHealthLocked && currentHealth <= 0)
            Die();
    }
    public void TakeDamageCounter(int damage)
    {
        bool tutorialHealthLocked = TutorialManager.Instance != null
    && TutorialManager.Instance.InTutorialMode
    && !TutorialManager.Instance.TutorialCombatUnlocked;

        if (!tutorialHealthLocked)
            currentHealth -= damage;// Fixed damage for counter hits.
        Debug.Log(transform.name + " took 10 damage from counter. Health is now: " + currentHealth);
        TriggerHealthUpdate();
        UpdateHealthUI();
        if (!tutorialHealthLocked && currentHealth <= 0)
            Die();
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
        if (isDying || isFinishable) return;
        currentHealth = 0;
        Debug.LogWarning($"--- {transform.name} has been defeated! ---");
        TriggerHealthUpdate();
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
        if (healthBarCanvasTransform != null)
        {
            StartCoroutine(FadeOutUI());
        }
        currentGuard = 0;
        TriggerPostureUpdate();
        // Set the state flags.
        isFinishable = true;
        isDying = false; // The dying process is complete.
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.TryTriggerFinisherCanvas();
        if (animator != null)
        {
            animator.SetTrigger(finishableStateTriggerHash);
        }
        if (ReaperAI != null) ReaperAI.enabled = false;
        if (ReaperAttack != null)  ReaperAttack.enabled = false;
        if (followAI != null) followAI.enabled = false;

        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        // Instead of disabling the simulation, we make the Rigidbody kinematic.
        if (rb != null)
        {
            // 1. Make it kinematic. This stops it from reacting to gravity or forces.
            rb.bodyType = RigidbodyType2D.Kinematic;

            // 2. Kill all existing velocity.
            rb.linearVelocity = Vector2.zero;

            // By doing this, the colliders remain active and can be detected
            // by the player's OverlapCircleAll check.
        }
    }
    public void ExecuteFinisher()
    {
        if (!isFinishable) return;

        Debug.LogError($"--- {transform.name} IS BEING FINISHED! ---");

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnPlayerExecutedFinisher();

        animator.SetTrigger(takeFinisherTriggerHash);

        // ? add this
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnReaperFinisherComplete();

        Destroy(gameObject, 7.0f);
    }
    public void TakePostureDamageOnParry()
    {
        // Failsafe: If the guard is already broken, we can't break it again.
        if (isGuardBroken) return;
        UpdatePostureUI();
        Debug.Log($"<color=orange>KNIGHT'S POSTURE DAMAGED BY PARRY! Taking {guardDamageOnParried} guard damage.</color>");

        // Subtract the damage from the guard meter.
        currentGuard -= guardDamageOnParried;
        TriggerPostureUpdate();
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
                nextBlockIndex = Random.Range(1, 4); // Pick a number from 1, 2, or 3.
            } while (nextBlockIndex == lastBlockAnimationIndex); // Keep picking until it's a new one.
        }
        else
        {
            // B. If NO, we can pick any animation randomly.
            nextBlockIndex = Random.Range(1, 4); // Pick a number from 1, 2, or 3.
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
                animator.SetTrigger(blockTriggerHash);
                break;
            case 2:
                animator.ResetTrigger(block2TriggerHash);
                animator.SetTrigger(block2TriggerHash);
                break;
            case 3:
                animator.ResetTrigger(block3TriggerHash);
                animator.SetTrigger(block3TriggerHash);
                break;
        }
    }

    public void PerformBlock(Transform player)
    {
        if (isDying || isFinishable) return;
        // This is the same logic that used to be in OnPlayerAttackTelegraphed.
        // We still check for stun and range as a final safety measure.
        if (isGuardBroken || isBeingKnockedBack || Vector2.Distance(transform.position, player.position) > blockRange)
        {
            return;
        }

        // Because the brain has given the command, we execute the block.
        TriggerRandomBlock();
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
        Debug.Log("<color=red>KNIGHT'S GUARD IS BROKEN!</color>");
        animator.SetTrigger(guardBrokenTriggerHash);
        currentGuard = 0;
        TriggerPostureUpdate();
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.TriggerDirectComboCanvas();

        // --- THIS IS THE FIX ---
        // Instead of containing all the logic, it now just calls the new universal method
        // with the correct duration for a guard break.
        TriggerStun(guardBrokenStunDuration);
        // --- END OF FIX ---

        // We yield for a tiny moment to ensure the trigger has time to fire before the sequence ends.
        yield return null;
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
        animator.ResetTrigger(UpTriggerHash);
        animator.ResetTrigger(RightUpTriggerHash);
        animator.ResetTrigger(LeftUpTriggerHash);
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
        if (ReaperAttack != null && ReaperAttack.IsAttacking())
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
        // ADD HERE:
        bool tutorialHealthLocked = TutorialManager.Instance != null
            && TutorialManager.Instance.InTutorialMode
            && !TutorialManager.Instance.TutorialCombatUnlocked;
        Debug.Log($"<color=red>--- ATTACK DATA RECEIVED ---</color>\n" +
                  $"Damage: {damage}, HitType: {hitType}, Knockback: {distance}, Duration: {duration}");

        // --- 2. APPLY THE LOGIC (This part is exactly the same as before) ---
        if (isGuardBroken)
        {
            PlayHitReaction(hitType);
           

            if (!tutorialHealthLocked)
                currentHealth -= damage;
            TriggerHealthUpdate();
            SpawnBloodVFX();
            PlayRandomHitSound();
            SpawnWoundEffect();
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDamageEffect());
            knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, distance, duration, upward, downward));
            if (!tutorialHealthLocked && currentHealth <= 0)
                Die();
            return;
        }
        TriggerHealthUpdate();
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
            PlayRandomBlockSound();
            CameraShakerHandler.Shake(CameraShakeParry);
            blocksSinceLastCounter++;
            if (blocksSinceLastCounter >= blocksNeededForNextCounter)
            {
                if (ReaperAI != null)
                {
                    ReaperAI.TriggerCounterAttack();
                }
            }
            currentGuard -= guardDamagePerBlock;
            if (currentGuard <= 0) StartCoroutine(GuardBrokenSequence());
            return;
        }

        PlayHitReaction(hitType);
       

        if (!tutorialHealthLocked)
            currentHealth -= damage;
        SpawnBloodVFX();
        PlayRandomHitSound();
        SpawnWoundEffect();
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamageEffect());
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, distance, duration, upward, downward));
        if (!tutorialHealthLocked && currentHealth <= 0)
            Die();
    }
    private void SetNewCounterThreshold()
    {
        // 1. Pick a new random number between the min and max values.
        blocksNeededForNextCounter = Random.Range(minBlocksToCounter, maxBlocksToCounter + 1);

        // 2. Reset the current count to zero.
        blocksSinceLastCounter = 0;

        Debug.Log($"<color=purple>Knight AI: New counter threshold set. Will counter after {blocksNeededForNextCounter} blocks.</color>");
    }
    public void TriggerStun(float stunDuration)
    {
        // Failsafe: If already stunned, don't start another stun.
        if (isGuardBroken) return;

        StartCoroutine(StunSequence(stunDuration));
    }

    // This new coroutine contains the logic that used to be in GuardBrokenSequence.
    private IEnumerator StunSequence(float stunDuration)
    {
        Debug.LogWarning($"--- SPEAR ENEMY STUN SEQUENCE STARTED (Duration: {stunDuration}s) ---");

        // --- PHASE 1 & 2 (Unchanged) ---
        bool isFirstBreak = !hasHadFirstGuardBreak;
        hasHadFirstGuardBreak = true;

        isGuardBroken = true;
        isUnbreakable = false;
        isBlocking = false;
        animator.SetBool(isWeakAndDamageableBoolHash, true);
        if (TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode)
        {
            yield return new WaitForSeconds(stunDuration);
            // Hold here until dash attack happens
            while (!TutorialManager.Instance.HasPlayerDashAttacked)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(stunDuration);
        }


        if (isFinishable)
        {
            yield break;
        }

        // --- PHASE 3: THE RECOVERY ---
        Debug.Log("<color=green>Spear Enemy has recovered from stun.</color>");
        animator.SetBool(isWeakAndDamageableBoolHash, false);
        animator.SetTrigger(recoverPostureTriggerHash);

        isGuardBroken = false;
        timeSinceLastBlock = 0f;
        if (isFirstBreak && ReaperAI != null)
            ReaperAI.OnFirstGuardBreakRecovered();
        // --- NEW: DYNAMIC POSTURE RECOVERY ANIMATION ---
        float recoveryStartTime = Time.time;
        float recoveryDuration = 1.0f; // How long the recovery animation takes
        float startingGuard = currentGuard;

        while (Time.time < recoveryStartTime + recoveryDuration)
        {
            float progress = (Time.time - recoveryStartTime) / recoveryDuration;
            currentGuard = Mathf.Lerp(startingGuard, maxGuard, progress);
            // Use the trigger function to animate the UI smoothly
            TriggerPostureUpdate();
            yield return null;
        }

        // Snap to final value and update UI one last time
        currentGuard = maxGuard;
        TriggerPostureUpdate();
    }
    public float GetCounterStunDuration()
    {
        return counterStunDuration;
    }
    /// <summary>
    /// This is the PUBLIC command that the KnightAI brain will call after a counter-attack is finished.
    /// </summary>
    public void ResetBlockCounter()
    {
        Debug.Log("<color=purple>Knight AI: Brain has commanded a counter reset.</color>");
        SetNewCounterThreshold();
    }
    public void GetParried(Transform playerTransform)
    {
        Debug.Log("<color=orange>KNIGHT HAS BEEN PARRIED!</color>");

        // Play a stunned/parried animation.
        animator.SetTrigger("getParried"); // Make sure you have this trigger in your Knight's Animator.

    }
    public void ApplyParryKnockback(Transform playerTransform)
    {
        // We use hardcoded values here for the small, reactive knockback.
        float parryKnockbackDistance = 2f;
        float parryKnockbackDuration = 0.2f;

        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(playerTransform, parryKnockbackDistance, parryKnockbackDuration, 0, 0));
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
    public void Decapitate()
    {
        // --- BRUTAL DEBUG ---
        Debug.LogError("--- ANIMATION EVENT: DECAPITATE! (Spawn-Only Version) ---");

        // --- 1. SAFETY CHECKS ---
        // We only check for the prefab and the spawn point now.
        if (headPrefab == null || headSpawnPoint == null)
        {
            Debug.LogError("Decapitation failed: Head Prefab or Head Spawn Point is not assigned in the Inspector!", this);
            return;
        }

        // --- 2. SPAWN THE NEW HEAD PREFAB ---
        GameObject newHead = Instantiate(headPrefab, headSpawnPoint.position, headSpawnPoint.rotation);
       

        // --- 3. APPLY THE FORCE ---
        Rigidbody2D headRb = newHead.GetComponent<Rigidbody2D>();
        if (headRb != null)
        {
            // Determine the direction based on the player's position.
            float direction = (playerTarget.position.x > transform.position.x) ? -1f : 1f;
            Vector2 force = new Vector2(headSidewaysForce * direction, headUpwardForce);

            // Apply the forces.
            headRb.AddForce(force, ForceMode2D.Impulse);
            headRb.AddTorque(headTorque, ForceMode2D.Impulse);

            Debug.Log($"Applied force ({force}) and torque ({headTorque}) to severed head.");
        }
        else
        {
            Debug.LogWarning("Spawned head prefab does not have a Rigidbody2D component! Cannot apply force.", newHead);
        }
        Destroy(newHead, 3.5f); // Destroy the original body after spawning the head.
    }
    public bool IsGrounded()
    {
        // We already calculate this 'isGrounded' boolean in the Update loop.
        // We just need to expose its value.
        return isGrounded;
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
