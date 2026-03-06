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
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider healthDelayedFill;
    // ADD a CanvasGroup on the health bar's ROOT GameObject in the Inspector
    [SerializeField] private CanvasGroup healthBarCanvasGroup;
    [SerializeField] private GameObject deathPanel;
    private int currentHealth;

    [Header("Posture/Shield UI")]
    [SerializeField] private int maxShieldHealth = 100;
    [SerializeField] private float shieldRegenDelay = 2.5f;
    [SerializeField] private float shieldRegenRate = 20f;
    [SerializeField] private float guardBreakStunDuration = 3f;
    [SerializeField] private Slider shieldSlider;
    [Tooltip("The secondary (background) SLIDER for the delayed posture drop effect.")]
    [SerializeField] private Slider shieldDelayedFill;
    // ADD a CanvasGroup on the shield/posture bar's ROOT GameObject in the Inspector
    [SerializeField] private CanvasGroup shieldBarCanvasGroup;

    [Header("UI Animation Settings")]
    [SerializeField] private float healthFillSpeed = 5f;
    [SerializeField] private float healthFillDelay = 0.5f;
    [SerializeField] private float postureFillSpeed = 8f;
    [SerializeField] private float postureFillDelay = 0.2f;
    [Tooltip("How long the slider takes to fade in/out when hitting 0.")]
    [SerializeField] private float sliderFadeDuration = 0.5f;

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

    private Coroutine healthUpdateCoroutine;
    private Coroutine postureUpdateCoroutine;
    private Coroutine healthFadeCoroutine;
    private Coroutine shieldFadeCoroutine;

    private int currentShieldHealth;
    private bool isShieldBroken = false;
    private Coroutine shieldRegenCoroutine;

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

        // --- FIX: Initialize sliders properly with min=0, max=1 ---
        if (healthSlider != null) { healthSlider.minValue = 0f; healthSlider.maxValue = 1f; }
        if (healthDelayedFill != null) { healthDelayedFill.minValue = 0f; healthDelayedFill.maxValue = 1f; }
        if (shieldSlider != null) { shieldSlider.minValue = 0f; shieldSlider.maxValue = 1f; }
        if (shieldDelayedFill != null) { shieldDelayedFill.minValue = 0f; shieldDelayedFill.maxValue = 1f; }

        currentShieldHealth = maxShieldHealth;
        TriggerPostureUpdate();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Block.started += HandleBlockInput;
        inputActions.Player.Block.canceled += HandleBlockInput;
    }

    #region UI Update Logic

    private void TriggerHealthUpdate()
    {
        if (healthUpdateCoroutine != null) StopCoroutine(healthUpdateCoroutine);
        healthUpdateCoroutine = StartCoroutine(UpdateHealthBarRoutine());

        // Handle fade based on value
        if (healthBarCanvasGroup != null)
        {
            bool shouldBeVisible = currentHealth > 0;
            if (healthFadeCoroutine != null) StopCoroutine(healthFadeCoroutine);
            healthFadeCoroutine = StartCoroutine(FadeCanvasGroup(healthBarCanvasGroup, shouldBeVisible ? 1f : 0f, sliderFadeDuration));
        }
    }

    private void TriggerPostureUpdate()
    {
        if (postureUpdateCoroutine != null) StopCoroutine(postureUpdateCoroutine);
        postureUpdateCoroutine = StartCoroutine(UpdatePostureBarRoutine());

        // Handle fade based on value
        if (shieldBarCanvasGroup != null)
        {
            bool shouldBeVisible = currentShieldHealth > 0;
            if (shieldFadeCoroutine != null) StopCoroutine(shieldFadeCoroutine);
            shieldFadeCoroutine = StartCoroutine(FadeCanvasGroup(shieldBarCanvasGroup, shouldBeVisible ? 1f : 0f, sliderFadeDuration));
        }
    }

    // FIX BUG 1 & 2: All slider values are now normalized (0.0 - 1.0).
    // The raw int value was being set directly before, clamping the slider at max.
    private IEnumerator UpdateHealthBarRoutine()
    {
        if (healthSlider == null) yield break;

        float targetFill = (float)currentHealth / maxHealth;

        // Main slider snaps instantly
        healthSlider.value = targetFill;

        // Handle fade based on value
        if (healthBarCanvasGroup != null)
        {
            bool shouldBeVisible = currentHealth > 0;
            if (healthFadeCoroutine != null) StopCoroutine(healthFadeCoroutine);
            healthFadeCoroutine = StartCoroutine(FadeCanvasGroup(healthBarCanvasGroup, shouldBeVisible ? 1f : 0f, sliderFadeDuration));
        }

        if (healthDelayedFill != null)
        {
            yield return new WaitForSeconds(healthFillDelay);
            float currentFill = healthDelayedFill.value;
            while (Mathf.Abs(currentFill - targetFill) > 0.001f)
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
        if (shieldSlider == null) yield break;

        float targetFill = (float)currentShieldHealth / maxShieldHealth;

        // Handle fade based on value
        if (shieldBarCanvasGroup != null)
        {
            bool shouldBeVisible = currentShieldHealth > 0;
            if (shieldFadeCoroutine != null) StopCoroutine(shieldFadeCoroutine);
            shieldFadeCoroutine = StartCoroutine(FadeCanvasGroup(shieldBarCanvasGroup, shouldBeVisible ? 1f : 0f, sliderFadeDuration));
        }

        // Animate the MAIN posture slider
        float currentSliderValue = shieldSlider.value;
        while (Mathf.Abs(currentSliderValue - targetFill) > 0.001f)
        {
            currentSliderValue = Mathf.Lerp(currentSliderValue, targetFill, Time.deltaTime * postureFillSpeed);
            shieldSlider.value = currentSliderValue;
            yield return null;
        }
        shieldSlider.value = targetFill;

        // Animate the DELAYED posture slider
        if (shieldDelayedFill != null)
        {
            yield return new WaitForSeconds(postureFillDelay);
            float currentDelayedFill = shieldDelayedFill.value;
            while (Mathf.Abs(currentDelayedFill - targetFill) > 0.001f)
            {
                currentDelayedFill = Mathf.Lerp(currentDelayedFill, targetFill, Time.deltaTime * postureFillSpeed);
                shieldDelayedFill.value = currentDelayedFill;
                yield return null;
            }
            shieldDelayedFill.value = targetFill;
        }
    }

    // NEW: Generic fade coroutine for any CanvasGroup
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    #endregion

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
        TriggerHealthUpdate();
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
                TriggerPostureUpdate(); // FIX: This now correctly sets slider via normalized value

                if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

                ImpactData parryImpact = ScriptableObject.CreateInstance<ImpactData>();
                parryImpact.knockbackDistance = impact.knockbackDistance;
                parryImpact.knockbackDuration = impact.knockbackDuration;
                parryImpact.hitReactionType = "none";
                knockbackCoroutine = StartCoroutine(HitReactionRoutine(attacker, parryImpact));

                if (shieldRegenCoroutine != null) StopCoroutine(shieldRegenCoroutine);

                if (currentShieldHealth <= 0)
                    StartCoroutine(GuardBreakRoutine());
                else
                    shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());

                return;
            }
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        TriggerHealthUpdate(); // FIX: This now correctly sets slider via normalized value

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
        TriggerHealthUpdate(); // FIX: normalized update

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
        TriggerHealthUpdate(); // FIX: normalized update

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

        StopAllCoroutines();
        if (playerMovements != null) playerMovements.CanMove = true;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private IEnumerator GuardBreakRoutine()
    {
        isShieldBroken = true;
        isStunned = true;
        StopBlocking();
        if (playerMovements != null) playerMovements.CanMove = false;

        animator.SetTrigger(guardBrokenTriggerHash);
        yield return null;
        animator.SetBool(isWeakBoolHash, true);

        yield return new WaitForSeconds(guardBreakStunDuration);

        animator.SetBool(isWeakBoolHash, false);
        animator.SetTrigger(recoverShieldTriggerHash);
        isStunned = false;
        isShieldBroken = false;
        if (playerMovements != null) playerMovements.CanMove = true;

        currentShieldHealth = maxShieldHealth;
        TriggerPostureUpdate(); // Will also trigger fade-in since value > 0 now

        Debug.Log("<color=green>Shield recovered after guard break!</color>");
    }

    private IEnumerator ShieldRegenRoutine()
    {
        yield return new WaitForSeconds(shieldRegenDelay);

        // This loop now smoothly regenerates the shield AND updates the UI correctly.
        while (currentShieldHealth < maxShieldHealth)
        {
            // Regenerate the value
            currentShieldHealth += Mathf.RoundToInt(shieldRegenRate * Time.deltaTime);
            currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, maxShieldHealth);

            // Animate the UI to the new value
            TriggerPostureUpdate();

            yield return null;
        }

        Debug.Log("<color=cyan>Shield fully regenerated.</color>");
    }

    private void Die(Transform killer)
    {
        Debug.Log("<color=black>PLAYER IS DEAD.</color>");
        if (checkpointManager != null) checkpointManager.RespawnAtMajorCheckpoint();

        currentHealth = maxHealth;
        TriggerHealthUpdate();

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
        StopAllCoroutines();
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1.5f);
        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}