using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Hazard Hit")]
    [SerializeField] private float hazardRespawnDelay = 2f;
    [SerializeField] private GameObject hazardHitVFX;
    [SerializeField] private Transform hazardVFXSpawnPoint;
    [Header("Health & UI")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject deathPanel;
    [Range(0f, 1f)]
    [SerializeField] private float lifeStealPercent = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float counterLifeStealPercent = 0.4f;
    private int currentHealth;
    [Header("UI Sliders")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider healthDelayedSlider;
    [SerializeField] private Slider postureSlider;
    [SerializeField] private Slider postureDelayedSlider;
    [SerializeField] private CanvasGroup healthCanvasGroup;
    [SerializeField] private CanvasGroup postureCanvasGroup;
    [SerializeField] private float healthDelayBeforeCatchUp = 0.8f;
    [SerializeField] private float healthCatchUpSpeed = 0.4f;
    [SerializeField] private float postureDelayBeforeCatchUp = 0.8f;
    [SerializeField] private float postureCatchUpSpeed = 0.4f;
    [SerializeField] private float guardBreakEmptyDisplayDuration = 1f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Posture/Shield UI")]
    [SerializeField] private int maxShieldHealth = 100;
    [SerializeField] private float shieldRegenDelay = 2.5f;
    [SerializeField] private float shieldRegenRate = 20f;
    [SerializeField] private float guardBreakStunDuration = 3f;
    [SerializeField] private int parryShieldCost = 20;


    [Header("Impact & VFX")]
    [SerializeField] private GameObject bloodVFX;
    [SerializeField] private Transform bloodSpawnPoint;

    // --- Private Components & State ---
    private Rigidbody2D rb;
    private Animator animator;
    private ZreyAttacks playerAttacks;
    private Coroutine knockbackCoroutine;
    private float healthDamageTimer;
    private float postureDamageTimer;
    // --- Animation Hashes ---
    private readonly int getHitBackTriggerHash = Animator.StringToHash("getHitBack");
    private readonly int getHitDownTriggerHash = Animator.StringToHash("getHitDown");
    private readonly int getHitFinalBackTriggerHash = Animator.StringToHash("finalBack");
    private readonly int getHitFallTriggerHash = Animator.StringToHash("Hitfall");
    private readonly int getHitUpwardTriggerHash = Animator.StringToHash("Upward");
    private readonly int getHitDownwardTriggerHash = Animator.StringToHash("Downward");
    private readonly int deathTriggerHash = Animator.StringToHash("death");
    private ZreyMovements playerMovements;

    [Header("Defense & Parry")]
    [Tooltip("How much damage is blocked (e.g., 0.75 means 75% of damage is ignored).")]
    [Range(0f, 1f)] public float damageReduction = 0.75f;
    [Tooltip("How long the parry window stays open after starting a block (in seconds).")]
    public float parryWindow = 0.15f;
      [Tooltip("How long the parry window stays open during the tutorial slow-time (real seconds).")]
    public float tutorialParryWindow = 2f;
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
    private Coroutine invincibilityWatchdogCoroutine;



    private int currentShieldHealth;
    private bool isShieldBroken = false;
    private Coroutine shieldRegenCoroutine;
    private Coroutine guardBreakCoroutine;
    private readonly int guardBrokenTriggerHash = Animator.StringToHash("guardBroken");
    private readonly int isWeakBoolHash = Animator.StringToHash("isWeak");
    private readonly int recoverShieldTriggerHash = Animator.StringToHash("recoverShield");
    [Header("Defense Sounds")]
    [SerializeField] private AudioSource defenseSfxSource;
    [Range(0f, 1f)][SerializeField] private float defenseSfxVolume = 1f;
    [SerializeField] private AudioClip blockStartClip;        // plays when entering block stance
    [SerializeField] private AudioClip[] blockHitClips;       // random pick when blocking an attack
    [SerializeField] private AudioClip[] parryClips;
    [SerializeField] private AudioClip[] bloodHitClips;
    [SerializeField] private Vector2 blockStartPitchRange = new Vector2(0.9f, 1.1f);
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
        if (defenseSfxSource == null)
        {
            defenseSfxSource = gameObject.AddComponent<AudioSource>();
            defenseSfxSource.playOnAwake = false;
            defenseSfxSource.spatialBlend = 0f;
        }
    }
    void Update()
    {
        float healthNorm = (float)currentHealth / maxHealth;
        float postureNorm = (float)currentShieldHealth / maxShieldHealth;

        // Snap main sliders
        healthSlider.value = healthNorm;
        postureSlider.value = postureNorm;

        // Delayed bars catch up after delay
        healthDamageTimer += Time.deltaTime;
        postureDamageTimer += Time.deltaTime;

        if (healthDamageTimer > healthDelayBeforeCatchUp)
            healthDelayedSlider.value = Mathf.MoveTowards(healthDelayedSlider.value, healthNorm, healthCatchUpSpeed * Time.deltaTime);

        if (postureDamageTimer > postureDelayBeforeCatchUp)
            postureDelayedSlider.value = Mathf.MoveTowards(postureDelayedSlider.value, postureNorm, postureCatchUpSpeed * Time.deltaTime);

        // Fading
        float targetHealthAlpha = currentHealth <= 0 ? 0f : 1f;
        float targetPostureAlpha = currentShieldHealth <= 0 ? 0f : 1f;
        healthCanvasGroup.alpha = Mathf.MoveTowards(healthCanvasGroup.alpha, targetHealthAlpha, fadeSpeed * Time.deltaTime);
        postureCanvasGroup.alpha = Mathf.MoveTowards(postureCanvasGroup.alpha, targetPostureAlpha, fadeSpeed * Time.deltaTime);
    }
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Block.started += HandleBlockInput;
        inputActions.Player.Block.canceled += HandleBlockInput;
    }


    public void HealFromCounter(int damageDealt)
    {
        if (currentHealth <= 0) return;
        int healAmount = Mathf.RoundToInt(damageDealt * counterLifeStealPercent);
        if (healAmount <= 0) return;
        StartCoroutine(SmoothHeal(healAmount));
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
        if (invincibilityWatchdogCoroutine != null)
        {
            StopCoroutine(invincibilityWatchdogCoroutine);
        }
        invincibilityWatchdogCoroutine = StartCoroutine(InvincibilityWatchdog());
    }

    public void MakeVulnerable()
    {
        Debug.Log("<color=grey>--- Player is now VULNERABLE ---</color>");
        IsInvincible = false;
        if (invincibilityWatchdogCoroutine != null)
        {
            StopCoroutine(invincibilityWatchdogCoroutine);
            invincibilityWatchdogCoroutine = null;
        }
    }
    private IEnumerator InvincibilityWatchdog()
    {
        yield return new WaitForSeconds(0.5f);

        if (IsInvincible)
        {
            Debug.LogWarning("<color=orange>INVINCIBILITY WATCHDOG: Flag stuck! Forcing vulnerable.</color>");
            IsInvincible = false;
        }

        invincibilityWatchdogCoroutine = null;
    }
    public void TakeDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        Debug.Log($"<color=yellow>TakeDamage called. Invincible={IsInvincible} | isParryWindowActive={isParryWindowActive} | isBlocking={isBlocking} | TutorialWindowOpen={TutorialManager.Instance?.IsTutorialParryWindowOpen}</color>");
        if (IsInvincible) { Debug.Log("Damage ignored: Player is invincible."); return; }
        if (currentHealth <= 0) return;
        if (TutorialManager.Instance != null
          && TutorialManager.Instance.InTutorialMode
          && TutorialManager.Instance.IsTutorialParryWindowOpen)
        {
            TutorialManager.Instance.QueueTutorialDamage(damageAmount, attacker, impact);
            return; // Don't apply damage yet — wait for parry or timeout
        }
        if (isParryWindowActive)
        {
            Debug.Log("<color=lime>PARRY SUCCESSFUL!</color>");
            CameraShakerHandler.Shake(CameraShakeParry);
            isBlocking = false;
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnPlayerParriedSuccessfully();
            int parryAnim = Random.Range(0, 2);
            animator.SetTrigger(parryAnim == 0 ? parry1TriggerHash : parry2TriggerHash);

            if (parryVFX != null) Instantiate(parryVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
            PlayRandomDefenseSound(parryClips);
            playerMovements.CanMove = true;
            currentShieldHealth -= parryShieldCost;
            currentShieldHealth = Mathf.Max(0, currentShieldHealth); // Don't go below zero
            postureDamageTimer = 0f;

            // After the cost is paid, check if the shield broke.
            if (currentShieldHealth <= 0)
            {
                // If the parry itself breaks the shield, trigger the guard break sequence.
                if (guardBreakCoroutine == null) // Check to prevent multiple calls
                {
                    guardBreakCoroutine = StartCoroutine(GuardBreakRoutine());
                }
            }
            else if (!isShieldBroken) // NEVER start regen if guard break is active
            {
                if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);
                shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
            }
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

            ReaperAttack RAttack = attacker.GetComponent<ReaperAttack>();
            ReaperHealth RHealth = attacker.GetComponent<ReaperHealth>();
            if (RHealth != null)
            {
                RHealth.TakePostureDamageOnParry();
                if (RAttack != null && RAttack.IsFinalComboAttack())
                {
                    Debug.Log("<color=lime>PARRIED THE FINAL ATTACK! Stunning the knight!</color>");
                    RHealth.GetParried(transform);
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
                postureDamageTimer = 0f;
                currentShieldHealth = Mathf.Max(0, currentShieldHealth);
                // FIX: This now correctly sets slider via normalized value
                if (blockVFX != null) Instantiate(blockVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
                PlayRandomDefenseSound(blockHitClips);
                if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

                ImpactData parryImpact = ScriptableObject.CreateInstance<ImpactData>();
                parryImpact.knockbackDistance = impact.knockbackDistance;
                parryImpact.knockbackDuration = impact.knockbackDuration;
                parryImpact.hitReactionType = "none";
                knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryImpact));

                if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);

                if (currentShieldHealth <= 0)
                {
                    if (guardBreakCoroutine != null) StopCoroutine(guardBreakCoroutine);
                    guardBreakCoroutine = StartCoroutine(GuardBreakRoutine());
                }
                else if (!isShieldBroken) // NEVER start regen if guard break is active
                {
                    if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);
                    shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
                }

                return;
            }
        }

        currentHealth -= damageAmount;
        healthDamageTimer = 0f;
        currentHealth = Mathf.Max(0, currentHealth);


        if (impact == null) { Debug.LogWarning("TakeDamage was called with null ImpactData!"); return; }
        Debug.Log($"<color=red>PLAYER TOOK DAMAGE. Health: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0) { Die(attacker); return; }

        SpawnBlood();
        if (!isShieldBroken) // Don't override guard break stun with a shorter hit stun
        {
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, impact));
        }
    }
    private void PlayDefenseSound(AudioClip clip)
    {
        if (clip == null || defenseSfxSource == null) return;
        defenseSfxSource.pitch = Random.Range(blockStartPitchRange.x, blockStartPitchRange.y);
        defenseSfxSource.PlayOneShot(clip, defenseSfxVolume);
        defenseSfxSource.pitch = 1f; // reset after so other sounds aren't affected
    }

    private void PlayRandomDefenseSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || defenseSfxSource == null) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) defenseSfxSource.PlayOneShot(clip, defenseSfxVolume);
    }
    private void PlayRandomBloodSound()
    {
        if (bloodHitClips == null || bloodHitClips.Length == 0 || defenseSfxSource == null) return;
        AudioClip clip = bloodHitClips[Random.Range(0, bloodHitClips.Length)];
        if (clip != null) defenseSfxSource.PlayOneShot(clip, defenseSfxVolume);
    }
    public void UpdateVolume(float masterVolume)
    {
        defenseSfxVolume = masterVolume;
    }
    public bool IsShieldBroken()
    {
        return isShieldBroken;
    }
    public void TakeUnblockableDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        if (IsInvincible) { Debug.Log("Damage ignored: Player is invincible."); return; }
        if (currentHealth <= 0) return;

        Debug.LogWarning($"<color=red>!!! PLAYER TOOK UNBLOCKABLE DAMAGE: {damageAmount} !!!</color>");

        currentHealth -= damageAmount;
        healthDamageTimer = 0f;
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
        healthDamageTimer = 0f;
        currentHealth = Mathf.Max(0, currentHealth);

        // Spawn the hazard hit VFX
        if (hazardHitVFX != null && hazardVFXSpawnPoint != null)
            Instantiate(hazardHitVFX, hazardVFXSpawnPoint.position, hazardHitVFX.transform.rotation);
        animator.SetTrigger("HasardHit");

        if (currentHealth <= 0)
            Die(null);
        else
            StartCoroutine(HazardRespawnRoutine());
    }
    public void RestoreFullHealth()
    {
        StartCoroutine(SmoothHealthRestore());
    }

    private IEnumerator SmoothHealthRestore()
    {
        Debug.Log("<color=green>Restoring health smoothly...</color>");
        while (currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(currentHealth + Mathf.CeilToInt(maxHealth * 0.5f * Time.deltaTime), maxHealth);
            healthDamageTimer = 0f; // keep delayed slider in sync
            yield return null;
        }
        currentHealth = maxHealth;
        Debug.Log("<color=green>Health fully restored!</color>");
    }
    private IEnumerator HazardRespawnRoutine()
    {
        Debug.Log($"<color=orange>Hazard hit! Respawning in {hazardRespawnDelay} seconds...</color>");

        // Freeze absolutely everything including gravity
        isStunned = true;
        isBeingKnockedBack = true;
        if (playerMovements != null) playerMovements.CanMove = false;
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (playerAttacks != null) playerAttacks.IsInCinematicState_ForceSet(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; // fully stops all physics
        }

        float timer = 0f;
        while (timer < hazardRespawnDelay)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero; // force zero every frame
            timer += Time.deltaTime;
            yield return null;
        }

        // Restore everything
        isStunned = false;
        isBeingKnockedBack = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }
        if (playerMovements != null) playerMovements.CanMove = true;
        if (playerAttacks != null) playerAttacks.IsInCinematicState_ForceSet(false);

        if (checkpointManager != null) checkpointManager.RespawnAtMiniCheckpoint();
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
    public void HealFromLifeSteal(int damageDealt)
    {
        if (currentHealth <= 0) return;
        int healAmount = Mathf.RoundToInt(damageDealt * lifeStealPercent);
        if (healAmount <= 0) return;
        StartCoroutine(SmoothHeal(healAmount));
    }

    private IEnumerator SmoothHeal(int amount)
    {
        int targetHealth = Mathf.Min(currentHealth + amount, maxHealth);
        while (currentHealth < targetHealth)
        {
            currentHealth = Mathf.Min(currentHealth + Mathf.CeilToInt(maxHealth * 0.5f * Time.deltaTime), targetHealth);
            yield return null;
        }
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
        if (!isShieldBroken)
        {
            isStunned = false;
            playerMovements.CanMove = true;
        }
        knockbackCoroutine = null;
        isBeingKnockedBack = false;
    }

    private void PlayHitReaction(string hitType)
    {
        animator.ResetTrigger(getHitBackTriggerHash);
        animator.ResetTrigger(getHitDownTriggerHash);
        animator.ResetTrigger(getHitFinalBackTriggerHash);
        animator.ResetTrigger(getHitFallTriggerHash);
        animator.ResetTrigger(getHitUpwardTriggerHash);
        animator.ResetTrigger(getHitDownwardTriggerHash);
            

        switch (hitType.ToLower())
        {
            case "down": animator.SetTrigger(getHitDownTriggerHash); break;
            case "finalback": animator.SetTrigger(getHitFinalBackTriggerHash); break;
            case "back": animator.SetTrigger(getHitBackTriggerHash); break;
            case "fall": animator.SetTrigger(getHitFallTriggerHash); break;
            case "upward": animator.SetTrigger(getHitUpwardTriggerHash); break;
            case "downward": animator.SetTrigger(getHitDownwardTriggerHash); break;
        }
    }

    private void SpawnBlood()
    {
        if (bloodVFX != null && bloodSpawnPoint != null)
            Instantiate(bloodVFX, bloodSpawnPoint.position, bloodVFX.transform.rotation);
        PlayRandomBloodSound();
    }
    public void TriggerParrySuccess(Transform attacker, ImpactData impact)
    {
        Debug.Log("<color=lime>TUTORIAL PARRY SUCCESSFUL!</color>");
        CameraShakerHandler.Shake(CameraShakeParry);
        isBlocking = false;
        isParryWindowActive = false;
        animator.ResetTrigger(startBlockTriggerHash);
        animator.SetTrigger(stopBlockTriggerHash); // Exit block stance in Animator
        if (parryWindowCoroutine != null) { StopCoroutine(parryWindowCoroutine); parryWindowCoroutine = null; }

        // Wait one frame then play parry anim so stopBlock transition clears first
        StartCoroutine(PlayParryAnimNextFrame(attacker, impact));

    }
    private IEnumerator PlayParryAnimNextFrame(Transform attacker, ImpactData impact)
    {
        yield return null; // One frame for stopBlock to register in Animator

        int parryAnim = Random.Range(0, 2);
        animator.SetTrigger(parryAnim == 0 ? parry1TriggerHash : parry2TriggerHash);

        if (parryVFX != null) Instantiate(parryVFX, defenseVFXSpawnPoint.position, Quaternion.identity);
        PlayRandomDefenseSound(parryClips);
        playerMovements.CanMove = true;

        currentShieldHealth -= parryShieldCost;
        currentShieldHealth = Mathf.Max(0, currentShieldHealth);
        postureDamageTimer = 0f;

        if (currentShieldHealth <= 0)
        {
            if (guardBreakCoroutine == null)
                guardBreakCoroutine = StartCoroutine(GuardBreakRoutine());
        }
        else if (!isShieldBroken)
        {
            if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);
            shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
        }

        if (impact != null)
        {
            ImpactData parryRecoilImpact = ScriptableObject.CreateInstance<ImpactData>();
            parryRecoilImpact.knockbackDistance = impact.parryKnockbackDistance;
            parryRecoilImpact.knockbackDuration = impact.parryKnockbackDuration;
            parryRecoilImpact.hitReactionType = "none";
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryRecoilImpact));
        }

        ReaperAttack reaperAttack = attacker.GetComponent<ReaperAttack>();
        ReaperHealth reaperHealth = attacker.GetComponent<ReaperHealth>();
        if (reaperHealth != null)
        {
            reaperHealth.TakePostureDamageOnParry();
            if (reaperAttack != null && reaperAttack.IsFinalComboAttack())
                reaperHealth.GetParried(transform);
        }
    }
    private void StartBlocking()
    {
        bool tutWindow = TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode && TutorialManager.Instance.IsTutorialParryWindowOpen;
        Debug.Log($"<color=cyan>StartBlocking called. isBlocking={isBlocking} | isShieldBroken={isShieldBroken} | InCinematic={playerAttacks?.IsInCinematicState} | TutorialWindowOpen={tutWindow} | CanMove={playerMovements?.CanMove} | isStunned={isStunned}</color>");
        if (isShieldBroken) { Debug.LogWarning("Block ignored: Shield broken or stunned."); return; }
        if (playerMovements != null && playerMovements.IsDashing()) { Debug.LogWarning("Block ignored: Currently dashing."); return; }
        if (IsGrabbed) { Debug.LogWarning("Block Input Ignored: Player is GRABBED."); return; }
        if (playerAttacks != null && playerAttacks.IsInCinematicState) { Debug.Log("Block Input Ignored: In Cinematic State."); return; }

        // During tutorial parry window — force the parry window open every time
        // block is pressed, even if already blocking, so the player can actually parry
        bool inTutorialParryWindow = TutorialManager.Instance != null
            && TutorialManager.Instance.InTutorialMode
            && TutorialManager.Instance.IsTutorialParryWindowOpen;

        if (inTutorialParryWindow)
        {
            // Force block state and re-open parry window on every press
            if (!isBlocking)
            {
                animator.ResetTrigger(stopBlockTriggerHash);
                isBlocking = true;
                PlayDefenseSound(blockStartClip);
                animator.SetTrigger(startBlockTriggerHash);
                playerMovements.CanMove = false;
                if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }

            // Force parry window open
            if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
            isParryWindowActive = true;
            parryWindowCoroutine = StartCoroutine(ParryWindowCoroutine());
            Debug.Log("<color=cyan>Tutorial: Parry window force-opened on block press.</color>");

            // KEY FIX: If damage is already queued, process it as a parry RIGHT NOW
            // The player pressed block — treat queued damage as the attack they're parrying
            if (TutorialManager.Instance.HasQueuedDamage())
            {
                Debug.Log("<color=lime>Tutorial: Block pressed with queued damage — triggering parry immediately!</color>");
                TutorialManager.Instance.ProcessQueuedDamageAsParry(this, transform);
            }
            return;
        }

        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (isBlocking) return;

        animator.ResetTrigger(stopBlockTriggerHash);
        isBlocking = true;
        PlayDefenseSound(blockStartClip);
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
        if (TutorialManager.Instance != null
           && TutorialManager.Instance.InTutorialMode
           && TutorialManager.Instance.IsTutorialParryWindowOpen)
        {
            yield return new WaitForSecondsRealtime(tutorialParryWindow);
        }
        else
        {
            yield return new WaitForSeconds(parryWindow);
        }

        isParryWindowActive = false;
    }

    public void GetGrabbedByEnemy(Vector3 targetPosition, Transform enemyTransform)
    {
        Debug.LogError("--- PLAYER HAS BEEN GRABBED! LOSING CONTROL. ---");
        if (isShieldBroken)
        {
            Debug.LogWarning("Grab ignored: Player is in guard break state.");
            return;
        }
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
        if (shieldRegenCoroutine != null)
        {
            StopCoroutine(shieldRegenCoroutine);
            shieldRegenCoroutine = null;
        }

        // --- PHASE 1: THE PUNISHMENT (This part is correct) ---
        Debug.LogError("--- PLAYER GUARD BROKEN! STUNNED! ---");
        isShieldBroken = true;
        isStunned = true;
        isBeingKnockedBack = true; // Use this to lock movement

        StopBlocking(); // Force the block to end
        animator.ResetTrigger(startBlockTriggerHash);
        animator.ResetTrigger(parry1TriggerHash);
        animator.ResetTrigger(parry2TriggerHash);
        isParryWindowActive = false;

        if (playerMovements != null) playerMovements.CanMove = false;
        if (playerAttacks != null) playerAttacks.CancelAttack();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(guardBrokenTriggerHash);
        yield return null; // Wait a frame for the trigger to register
        animator.SetBool(isWeakBoolHash, true);
        Debug.Log("<color=yellow>--- Starting Parallel Shield Regen during Stun ---</color>");

        float timer = 0f;
        float startShieldHealth = 0; // We always start from zero after a break.
        currentShieldHealth = 0;
        postureDelayedSlider.value = 0f;
        postureDamageTimer = 0f;
        yield return new WaitForSeconds(guardBreakEmptyDisplayDuration);// Set it to 0 at the beginning.

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

        isStunned = false;
        isBeingKnockedBack = false;
        isShieldBroken = false;
        guardBreakCoroutine = null; // Mark as finished so ForceResetState knows it's done

        if (playerMovements != null) playerMovements.CanMove = true;

        // Shield is already maxShieldHealth from the lerp — no regen needed
        // but reset the delayed slider to match
        postureDelayedSlider.value = 1f;

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
        // REMOVE the RespawnAtMajorCheckpoint line entirely
        IsInvincible = true; // Prevent any further damage or interactions
        rb.linearVelocity = Vector2.zero;
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
        if (guardBreakCoroutine == null)
        {
            isStunned = false;
            isShieldBroken = false;
            animator.SetBool(isWeakBoolHash, false);
        }
        if (animator != null) animator.SetTrigger(stopBlockTriggerHash);
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        if (parryWindowCoroutine != null) StopCoroutine(parryWindowCoroutine);
    }
    public void EVENT_LockGuardBreakState()
    {
        isShieldBroken = true;
        isStunned = true;
        IsGrabbed = false; // Cannot be grabbed while broken
        IsInvincible = false; // Not fully invincible, just grab-immune

        // Kill any regen that snuck in
        if (shieldRegenCoroutine != null)
        {
            StopCoroutine(shieldRegenCoroutine);
            shieldRegenCoroutine = null;
        }

        // Kill any parry/block coroutines
        if (parryWindowCoroutine != null)
        {
            StopCoroutine(parryWindowCoroutine);
            parryWindowCoroutine = null;
        }

        isBlocking = false;
        isParryWindowActive = false;
        if (playerMovements != null) playerMovements.CanMove = false;
        if (playerAttacks != null) playerAttacks.IsInCinematicState_ForceSet(true);

        Debug.LogError("--- EVENT_LockGuardBreakState: ALL INPUT LOCKED ---");
    }

    // Called by Animation Event when recovery is complete
    public void EVENT_UnlockGuardBreakState()
    {
        isShieldBroken = false;
        isStunned = false;
        currentShieldHealth = maxShieldHealth;
        guardBreakCoroutine = null;

        if (playerMovements != null) playerMovements.CanMove = true;
        if (playerAttacks != null) playerAttacks.IsInCinematicState_ForceSet(false);

        postureDelayedSlider.value = 1f;

        Debug.Log("<color=green>--- EVENT_UnlockGuardBreakState: PLAYER RESTORED ---</color>");
    }
    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            if (CursorManager.Instance != null) CursorManager.Instance.RequestShowCursor(); // ? add this
            CanvasGroup deathCanvasGroup = deathPanel.GetComponent<CanvasGroup>();
            if (deathCanvasGroup != null)
            {
                deathCanvasGroup.alpha = 0f;
                while (deathCanvasGroup.alpha < 1f)
                {
                    deathCanvasGroup.alpha += Time.unscaledDeltaTime * 2f; // 2f = fade speed
                    yield return null;
                }
                deathCanvasGroup.alpha = 1f;
            }
        }

        // Stop all sounds after fade completes
        AudioListener.pause = true;
        Time.timeScale = 0f;
    }
}