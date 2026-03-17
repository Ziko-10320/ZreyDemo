using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
[RequireComponent(typeof(Animator))]
public class ZreyAttacks : MonoBehaviour
{
    [Header("Special Attack Cooldown UI")]
    [SerializeField] private float specialAttackCooldown = 15f;
    [SerializeField] private TMPro.TextMeshProUGUI cooldownText15;
    [SerializeField] private TMPro.TextMeshProUGUI cooldownText10;
    [SerializeField] private TMPro.TextMeshProUGUI cooldownText5;
    [SerializeField] private UnityEngine.UI.Image readyImage;

    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color inactiveImageColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color readyImageActiveColor = Color.white;
    [SerializeField] private float colorTransitionSpeed = 3f;

    private float specialAttackCooldownTimer = 0f;
    private bool isSpecialAttackOnCooldown = false;

    // Target colors we lerp towards
    private Color target15Color;
    private Color target10Color;
    private Color target5Color;
    private Color targetImageColor;
    [SerializeField] private AudioClip[] counterClips;
    [Header("Components")]
    [SerializeField] private Animator animator;
    // We need a reference to the movement script to check its state.
    [SerializeField] private ZreyMovements playerMovement;

    [Header("Combo Settings")]
    [Tooltip("How long the player has to press the next attack button to continue the combo.")]
    [SerializeField] private float comboResetTime = 1f;

    // --- Private State Variables ---
    private int comboStep = 0;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    private readonly int attackStepHash = Animator.StringToHash("attackStep");
    private readonly int attackVariantHash = Animator.StringToHash("attackVariant");
    private Rigidbody2D rb;
    [SerializeField] private float lungeSpeed = 8f;
    [Tooltip("How long the lunge lasts (in seconds).")]
    [SerializeField] private float lungeDuration = 0.15f;
    private Coroutine comboResetCoroutine;
    public ShakeData CameraShakeLight;
    public ShakeData CameraShakeMid;
    public ShakeData CameraShakeHeavy;

    [Header("Damage Settings")]
    [Tooltip("The amount of damage each attack deals.")]
    [SerializeField] private int attackDamage = 10; 

    [Tooltip("An empty GameObject marking the center of the player's damage area.")]
    [SerializeField] private Transform attackPoint; 

    [Tooltip("The size of the damage area (Width, Height).")]
    [SerializeField] private Vector2 attackAreaSize = new Vector2(1.5f, 1f); 

    [Tooltip("The layer the enemies are on, so we know who to damage.")]
    [SerializeField] private LayerMask enemyLayer; 

    // --- Private State Variables for Damage ---
    private bool isDamageFrameActive = false;
    private bool hasDealtDamageThisAttack = false;
    private Coroutine lungeCoroutine;
    private string currentHitReactionType = "";
    private bool isCustomKnockbackPrimed = false;
    private float primedKnockbackDistance;
    private float primedKnockbackDuration;
    [Header("Collision Settings")]
    [Tooltip("The integer value of the Player's layer.")]
    [SerializeField] private int playerLayerValue = 6; // Example: Change this to your actual Player layer number"
    [Tooltip("The integer value of the Enemy's layer.")]
    [SerializeField] private int enemyLayerValue = 7;
    [SerializeField] private float attackTimeout = 2f;
    [Header("Attack Sounds")]
    [SerializeField] private AudioSource attackSfxSource;
    [Range(0f, 1f)][SerializeField] private float attackSfxVolume = 1f;
    [SerializeField] private AudioClip downSlamLoopClip;
    [Range(0f, 1f)][SerializeField] private float downSlamLoopVolume = 1f;
    private AudioSource downSlamLoopSource;
    private Coroutine attackWatchdogCoroutine;
    [Header("Down Slam Settings")]
    [Tooltip("The downward force applied to the player during the down slam.")]
    [SerializeField] private float downSlamForce = 20f; 
    [Tooltip("The Scriptable Object defining the damage and impact of the down slam.")]
    [SerializeField] private AttackData downSlamAttackData; 

    private bool isDownSlamming = false;

    private readonly int downSlamLoopTriggerHash = Animator.StringToHash("downSlamLoop");
    private readonly int downSlamImpactTriggerHash = Animator.StringToHash("downSlamImpact");
    private readonly int counterBurstTriggerHash = Animator.StringToHash("counterBurst");
    private readonly int knightCounterTriggerHash = Animator.StringToHash("knightCounter");

    [Header("Root Motion Components")]
    [Tooltip("The Animator that controls the root motion proxy object.")]
    [SerializeField] private Animator rootZreyAnimator;
    private readonly int rootKnightCounterTriggerHash = Animator.StringToHash("RootKnightCounter");
    private readonly int rootKnightCounterLeftTriggerHash = Animator.StringToHash("RootKnightCounterLeft");
    private bool isCountering = false;

    [SerializeField] private ZreyTrail playerTrail;

    [Header("Counter Attack Settings")] 
    [Tooltip("The amount of pure damage the knight counter deals, with no knockback.")]
    [SerializeField] private int counterDamage = 50;
    public bool IsInCinematicState { get; private set; } = false;
    public static bool PlayerInCinematic { get; private set; } = false;
    [SerializeField] private PlayerHealth playerHealth;
    private readonly int specialAttackBlockTriggerHash = Animator.StringToHash("specialAttackBlock");
    [SerializeField] private float guardCrushStunDuration = 1.0f;
    [Header("Charged Attack Settings")]
    [Tooltip("The specific AttackData for the UpperAttack.")]
    [SerializeField] private AttackData upperAttackData; 


    private float attackButtonHeldTime = 0f;
    private bool isChargingAttack = false;

    // --- New Animation Hashes ---
    private readonly int upperAttackTriggerHash = Animator.StringToHash("UpperAttack");
    private readonly int rootUpperAttackTriggerHash = Animator.StringToHash("RootUpperAttack");
    private bool isChargeAttackPrimed = false;
    [SerializeField] private float chargeAttackHoldTime = 0.4f;

    [Header("Cinematic Camera Settings")]
    [Tooltip("The target orthographic size for the camera during a cinematic zoom.")]
    [SerializeField]  private float zoomInSize = 3.5f; 

    [Tooltip("How long it takes to zoom in and out (in seconds).")]
    [SerializeField] private float zoomDuration = 0.2f; 

    // --- Private state for camera control ---
    private Camera mainCamera;
    private float originalCameraSize;
    private Coroutine cameraZoomCoroutine;
    [Header("Finisher Settings")]
    [Tooltip("The range within which the player can initiate a finisher.")]
    [SerializeField] private float finisherRange = 2.5f; 

    [Tooltip("The offset from the player to snap the enemy to before the finisher.")]
    [SerializeField] private Vector3 finisherSnapOffset = new Vector3(1.2f, 0, 0); 

    private readonly int spearFinisherTriggerHash = Animator.StringToHash("SpearFinisher");
    public static event Action OnPlayerCounterAttempt;
    public static event Action<Transform> OnPlayerCinematicStarted;
    [Header("Aerial Combo Settings")]
    [Tooltip("The maximum number of attacks in the aerial combo chain.")]
    [SerializeField] private int maxAerialComboSteps = 3;

    [Tooltip("How long the player must hold the attack button in the air to trigger a down slam.")]
    [SerializeField] private float downSlamHoldTime = 0.3f;

    // --- Private state for the aerial combo ---
    private int aerialComboStep = 0;
    private bool isDownSlamPrimed = false;
    private float originalGravityScale;
    private int aerialInputBuffer = 0;
    // --- New Animation Hashes ---
    private readonly int aerialAttackStepHash = Animator.StringToHash("aerialAttackStep");

    [Header("Vagabond Counter (Grab)")]
    [Tooltip("The range within which the player will broadcast their counter attempt.")]
    [SerializeField]  private float counterBroadcastRange = 5f; 
    [Tooltip("The stun duration to apply to the enemy after a successful grab counter.")]
    [SerializeField] public float grabCounterStunDuration = 2.5f; 

 
    private readonly int vagabondCounterTriggerHash = Animator.StringToHash("VagabondCounter");

    private readonly int vagabondFinisherTriggerHash = Animator.StringToHash("VagabondFinisher");
    [Tooltip("The offset from the player to snap the KNIGHT to before the Vagabond Finisher.")]
    [SerializeField] private Vector3 vagabondFinisherSnapOffset = new Vector3(1.5f, 0, 0);

    private readonly int rootUpperAttackLeftTriggerHash = Animator.StringToHash("RootUpperAttackLeft");

    private readonly int isAttackingBoolHash = Animator.StringToHash("isAttacking");

    [Header("Attack Jump Settings")]
    [Tooltip("Max times the same AttackJump variant can play in a row.")]
    [SerializeField] private int maxSameAttackJumpInRow = 2;

    private readonly int attackJumpV1TriggerHash = Animator.StringToHash("AttackJumpV1");
   

    private int lastAttackJumpVariant = -1; // -1 = none played yet
    private int sameAttackJumpCount = 0;
    private bool isAttackJumping = false;

    private readonly int reaperFinisherTriggerHash = Animator.StringToHash("ReaperFinisher");

    [Tooltip("The offset from the player to snap the REAPER to before the Reaper Finisher.")]
    [SerializeField] private Vector3 reaperFinisherSnapOffset = new Vector3(1.5f, 0, 0);

    [Header("Finisher Vignette Settings")]
    [Tooltip("The Global Volume that contains the Vignette override.")]
    [SerializeField] private Volume globalVolume;
    [Tooltip("The target vignette intensity during a finisher.")]
    [SerializeField] private float vignetteTargetIntensity = 0.45f;
    [Tooltip("How fast the vignette fades in and out.")]
    [SerializeField] private float vignetteFadeSpeed = 3f;

    [Header("Dash Attack Settings")]
    [Tooltip("The AttackData for the dash attack hit.")]
    [SerializeField] private AttackData dashAttackData;
    [Tooltip("X offset from the enemy where the player snaps to (negative = behind).")]
    [SerializeField] private float dashAttackSnapOffsetX = -1.2f;
    [Tooltip("Speed at which the player travels to the enemy during dash attack.")]
    [SerializeField] private float dashAttackTravelSpeed = 60f;
    [Tooltip("Minimum distance from the enemy required to perform a dash attack.")]
    [SerializeField] private float dashAttackMinDistance = 2.5f;
    private readonly int dashAttackTriggerHash = Animator.StringToHash("AttackDash");
    public bool isDashAttacking = false;

    private Vignette vignette;
    private Coroutine vignetteCoroutine;
    [Tooltip("The trail effect prefab to spawn during the dash attack.")]
    [SerializeField] private GameObject dashAttackTrailPrefab;
    [Tooltip("The spawn point where the trail will be spawned and parented to.")]
    [SerializeField] private Transform dashAttackTrailSpawnPoint;

    private readonly int reaperCounterTriggerHash = Animator.StringToHash("ReaperCounter");
    private bool isReaperCountering = false;

    [Header("Reaper Counter Settings")]
    [Tooltip("Duration of the Reaper counter animation sequence.")]
    [SerializeField] private float reaperCounterDuration = 2.5f;

    private void OnEnable()
    {
        InputManager.OnInteractPressed += HandleInteractionInput;
    }

    private void OnDisable()
    {
        InputManager.OnInteractPressed -= HandleInteractionInput;
    }
    void Awake()
    {
        target15Color = inactiveColor;
        target10Color = inactiveColor;
        target5Color = inactiveColor;
        targetImageColor = inactiveImageColor;
        isSpecialAttackOnCooldown = true;
        specialAttackCooldownTimer = specialAttackCooldown;
        if (cooldownText15 != null) cooldownText15.color = inactiveColor;
        if (cooldownText10 != null) cooldownText10.color = inactiveColor;
        if (cooldownText5 != null) cooldownText5.color = inactiveColor;
        if (readyImage != null) readyImage.color = inactiveImageColor;
        downSlamLoopSource = gameObject.AddComponent<AudioSource>();
        downSlamLoopSource.playOnAwake = false;
        downSlamLoopSource.loop = true;
        downSlamLoopSource.spatialBlend = 0f;
        if (attackSfxSource == null)
        {
            attackSfxSource = gameObject.AddComponent<AudioSource>();
            attackSfxSource.playOnAwake = false;
            attackSfxSource.spatialBlend = 0f;
        }
        // Automatically get components if they aren't assigned.
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<ZreyMovements>();
        rb = GetComponent<Rigidbody2D>();
         if (playerTrail == null) playerTrail = GetComponent<ZreyTrail>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Store the camera's original size so we can always return to it.
            originalCameraSize = mainCamera.orthographicSize;
        }
        else
        {
            Debug.LogError("FATAL ERROR: No main camera found in the scene!", this);
        }
        originalGravityScale = rb.gravityScale;
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();
        if (globalVolume != null && globalVolume.profile.TryGet(out Vignette vig))
        {
            vignette = vig;
            vignette.active = false;
            vignette.intensity.value = 0f;
        }
    }
    public void UpdateVolume(float masterVolume)
    {
        attackSfxVolume = masterVolume;
        downSlamLoopVolume = masterVolume;          // ? add this

        // Update live if currently playing
        if (downSlamLoopSource != null)
            downSlamLoopSource.volume = downSlamLoopVolume;
    }
    void Update()
    {
        // Master shield: If we are busy, do nothing.
        if (isAttacking || IsInCinematicState)
        {
            return;
        }
        if (cooldownText15 != null) cooldownText15.color = Color.Lerp(cooldownText15.color, target15Color, colorTransitionSpeed * Time.deltaTime);
        if (cooldownText10 != null) cooldownText10.color = Color.Lerp(cooldownText10.color, target10Color, colorTransitionSpeed * Time.deltaTime);
        if (cooldownText5 != null) cooldownText5.color = Color.Lerp(cooldownText5.color, target5Color, colorTransitionSpeed * Time.deltaTime);
        if (readyImage != null) readyImage.color = Color.Lerp(readyImage.color, targetImageColor, colorTransitionSpeed * Time.deltaTime);

        if (isSpecialAttackOnCooldown)
        {
            specialAttackCooldownTimer -= Time.deltaTime;

            // Light up text15 when countdown hits 15 (i.e. just started)
            if (specialAttackCooldownTimer <= 15f)
                target15Color = activeColor;

            // Light up text10 when 10 seconds remain
            if (specialAttackCooldownTimer <= 10f)
                target10Color = activeColor;

            // Light up text5 when 5 seconds remain
            if (specialAttackCooldownTimer <= 5f)
                target5Color = activeColor;

            if (specialAttackCooldownTimer <= 0f)
            {
                isSpecialAttackOnCooldown = false;
                specialAttackCooldownTimer = 0f;
                // Light up the ready image
                target15Color = inactiveColor;
                target10Color = inactiveColor;
                target5Color = inactiveColor;
                targetImageColor = readyImageActiveColor;
                target15Color = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, 0f);
                target10Color = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, 0f);
                target5Color = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, 0f);
            }
        }
        // Read the raw input state from the InputManager.
        bool attackHeld = InputManager.Instance.isAttackButtonPressed;
        float heldTime = InputManager.Instance.attackButtonHeldTime;
        bool attackReleased = InputManager.Instance.justReleasedAttack;

        // --- THE NEW DUAL-STATE LOGIC ---
        if (playerMovement.IsGrounded())
        {
            // --- STATE: ON THE GROUND ---
            // Grounded Charge Logic (Upper Attack)
            if (attackHeld && !isChargeAttackPrimed)
            {
                if (heldTime >= chargeAttackHoldTime)
                {
                    PerformUpperAttack();
                    isChargeAttackPrimed = true;
                }
            }
            if (attackReleased
              && playerMovement.IsDashing()
              && playerMovement.IsInCombatMode
              && playerMovement.LockedOnTarget != null
              && !playerMovement.IsBackwardDashing
              && !isDashAttacking
              && !IsInCinematicState)
            {
                float distToTarget = Vector2.Distance(
                    transform.position,
                    playerMovement.LockedOnTarget.position);

                if (distToTarget >= dashAttackMinDistance)
                {
                    StartCoroutine(ExecuteDashAttack(playerMovement.LockedOnTarget));
                    return;
                }
                else
                {
                    Debug.Log($"<color=orange>Dash attack blocked: too close to enemy ({distToTarget:F2} < {dashAttackMinDistance}).</color>");
                }
            }
            // Grounded Tap Logic (Normal Combo or Block Attack)
            if (attackReleased && !isChargeAttackPrimed && !playerMovement.IsDashing())
            {
                if (AttemptFinisher()) return;
                HandleAttack(); // This method already handles the block-attack check.
            }
          
        }
        else
        {
            // --- STATE: IN THE AIR ---
            // Aerial Hold Logic (Down Slam)
            if (attackHeld && !isDownSlamPrimed)
            {
                if (heldTime >= downSlamHoldTime)
                {
                    PerformDownSlam();
                    isDownSlamPrimed = true; // Mark that we've started the slam.
                }
            }

            // Aerial Tap Logic (Aerial Combo)
            if (attackReleased && !isDownSlamPrimed && !playerMovement.IsDashing())
            {
                if (isAttacking)
                {
                    // Player tapped during uppercut — buffer it for when uppercut ends
                    if (aerialInputBuffer < maxAerialComboSteps)
                    {
                        aerialInputBuffer++;
                        Debug.Log($"<color=cyan>Aerial tap buffered during uppercut. Buffer: {aerialInputBuffer}</color>");
                    }
                }
                else
                {
                    PerformAerialAttack();
                }
            }
        }

        // Reset the "primed" flags on release, regardless of state.
        if (attackReleased)
        {
            isChargeAttackPrimed = false;
            isDownSlamPrimed = false;
        }
    }
    void FixedUpdate()
    {
        // If we are in the down slam state...
        if (isDownSlamming)
        {
            // ...constantly apply a downward force to the Rigidbody.
            rb.linearVelocity = new Vector2(0, -downSlamForce);
        }
    }
    private IEnumerator ExecuteDashAttack(Transform target)
    {
        if (isDashAttacking || IsInCinematicState || isAttacking) yield break;

        isDashAttacking = true;
        isAttacking = true;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnPlayerPerformedDashAttack();
        playerMovement.CancelGroundDash();
        // Stop current dash immediately
        playerMovement.CanMove = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        // Ignore collisions with enemies during travel so player passes through cleanly
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);

        // Play animation
        animator.SetTrigger(dashAttackTriggerHash);

        // Determine which side the player is coming from BEFORE any movement
        bool playerIsOnLeft = transform.position.x < target.position.x;

        // Snap target X — place player on the OPPOSITE side of where they started
        // If player was on the left, they end up on the right side of the enemy and vice versa
        float sideMultiplier = playerIsOnLeft ? 1f : -1f;
        float snapTargetX = target.position.x + (dashAttackSnapOffsetX * sideMultiplier);

        // Determine the correct facing direction AFTER snapping
        // Player snapped to right side of enemy → face left (toward enemy)
        // Player snapped to left side of enemy → face right (toward enemy)
        bool shouldFaceRightAfterSnap = snapTargetX < target.position.x;

        float elapsed = 0f;
        float startX = transform.position.x;

        while (elapsed < dashAttackTravelSpeed)
        {
            // Always use unscaled delta time so slow-motion tutorial
            // doesn't affect the snap distance or travel feel
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dashAttackTravelSpeed);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = new Vector3(
                Mathf.Lerp(startX, snapTargetX, smoothT),
                transform.position.y,
                transform.position.z
            );

            yield return null;
        }

        // Guarantee clean X snap, Y untouched
        transform.position = new Vector3(
            snapTargetX,
            transform.position.y,
            transform.position.z
        );

        // Restore gravity and physics
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
            rb.linearVelocity = Vector2.zero;
        }

        // Force correct facing direction explicitly — this is the source of truth
        // Do this BEFORE re-enabling CanMove so combat mode auto-flip
        // doesn't race against us on the next FixedUpdate
        playerMovement.ForceFaceDirection(shouldFaceRightAfterSnap);

    }
    public void EVENT_DashAttackDamage()
    {
        if (dashAttackData != null)
        {
            AttackEnemy(dashAttackData);
        }
        Debug.Log("<color=lime>Dash Attack damage dealt.</color>");
    }
    public void EVENT_EndDashAttack()
    {
        isDashAttacking = false;
        // IsDashAttacking is now false — ZreyMovements combat mode
        // resumes auto-facing the locked enemy on the next frame naturally
        EndAttack();
        Debug.Log("<color=lime>Dash Attack complete.</color>");
    }
    public void EVENT_SpawnDashAttackTrail()
    {
        if (dashAttackTrailPrefab == null || dashAttackTrailSpawnPoint == null)
        {
            Debug.LogWarning("Dash attack trail prefab or spawn point not assigned!");
            return;
        }

        Instantiate(
            dashAttackTrailPrefab,
            dashAttackTrailSpawnPoint.position,
            dashAttackTrailSpawnPoint.rotation,
            dashAttackTrailSpawnPoint
        );

        Debug.Log("<color=cyan>Dash attack trail spawned.</color>");
    }

    public void EVENT_TriggerAerialComboCanvas()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.TriggerAerialComboCanvas();
    }
    private void PerformAerialAttack()
    {
        // Failsafe: If we are busy, do nothing.
        if (isAttacking || isDownSlamming || IsInCinematicState)
        {
            return;
        }
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnPlayerPerformedAerialAttack();
        Debug.Log($"<color=cyan>--- AERIAL ATTACK {aerialComboStep + 1} TRIGGERED ---</color>");

        // --- Start the attack state ---
        isAttacking = true;
        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);

        // Stop any previous combo reset timer.
        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);

        // Increment the aerial combo step.
        aerialComboStep++;

        // --- Set the Animator ---
        // We use a new parameter, "aerialAttackStep", to distinguish from the ground combo.
        animator.SetInteger(aerialAttackStepHash, aerialComboStep);

        // If we have reached the end of the combo, reset the step counter for the next chain.
        if (aerialComboStep >= maxAerialComboSteps)
        {
            aerialComboStep = 0;
        }
    }
    public void StartSpecialAttackCooldown()
    {
        isSpecialAttackOnCooldown = true;
        specialAttackCooldownTimer = specialAttackCooldown;

        // Reset everything back to inactive when skill is used
        target15Color = inactiveColor;
        target10Color = inactiveColor;
        target5Color = inactiveColor;
        targetImageColor = inactiveImageColor;
    }

    public bool IsSpecialAttackReady()
    {
        return !isSpecialAttackOnCooldown;
    }
    private void HandleAttack()
    {
        if (playerMovement != null && playerMovement.IsDashing()) return;
        if (playerHealth != null && playerHealth.IsBlocking())
        {
            if (isSpecialAttackOnCooldown) return;
            Debug.Log("<color=lime>--- BLOCK SPECIAL ATTACK TRIGGERED ---</color>");
            isAttacking = true;
            if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
            attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
            // Trigger the new animation.
            animator.SetTrigger(specialAttackBlockTriggerHash);
            Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);

            
            // 3. CRITICAL: Exit the method immediately.
            //    We do not want to proceed to the normal ground/air attack logic.
            return;
        }
        if (isAttacking || isDownSlamming || IsInCinematicState)
        {
            return;
        }
        
      
        else // We are on the ground
        {
            // Perform a normal ground combo attack.
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            comboStep++;
            PerformAttack(comboStep);
            if (comboStep >= 4) comboStep = 0;
        }
    }
 
    private void PerformDownSlam()
    {
        Debug.Log("<color=magenta>DOWN SLAM STARTED!</color>");
        isDownSlamming = true;
        isAttacking = true;
        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
        // Tell the movement script to stop normal control.
        playerMovement.CanMove = false;
        animator.ResetTrigger(downSlamImpactTriggerHash);
        // Play the looping "falling" part of the slam.
        animator.SetTrigger(downSlamLoopTriggerHash);
        if (downSlamLoopClip != null)
        {
            downSlamLoopSource.clip = downSlamLoopClip;
            downSlamLoopSource.volume = downSlamLoopVolume;
            downSlamLoopSource.Play();
        }

    }

    /// <summary>
    /// Called by ZreyMovements when the player lands during a down slam.
    /// </summary>
    public void EndDownSlam()
    {
        if (!playerMovement.IsGrounded())
        {
            Debug.LogError("EndDownSlam was called, but player is NOT grounded! Aborting impact.");
            return;
        }
        if (!isDownSlamming) return; // Failsafe
        downSlamLoopSource.Stop();
        Debug.Log("<color=magenta>DOWN SLAM IMPACT!</color>");
        isDownSlamming = false;

        // Play the impact animation.
        animator.SetTrigger(downSlamImpactTriggerHash);

        // Deal damage in an area around the player.
        // We can reuse the AttackEnemy method for this.
        if (downSlamAttackData != null)
        {
            AttackEnemy(downSlamAttackData);
        }

        // Give control back to the player after a short delay.
        StartCoroutine(DownSlamRecoveryRoutine());
    }

    private IEnumerator DownSlamRecoveryRoutine()
    {
        // Wait for the impact animation to have some weight.
        yield return new WaitForSeconds(0.15f); // Adjust this delay as needed.
       
        playerMovement.CanMove = true;
    }
    public void EVENT_DownSlamBreakWall()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f);
        foreach (Collider2D hit in hits)
        {
            BreakableWall wall = hit.GetComponent<BreakableWall>();
            if (wall != null)
            {
                wall.TakeDownSlamDamage(999);
            }
        }
    }
    public bool IsDownSlamming()
    {
        return isDownSlamming;
    }
    /// <summary>
    /// Triggers the correct attack animation based on the combo step.
    /// </summary>
    public void EVENT_OnDashComplete()
    {
        Debug.Log("<color=lime>ZreyAttacks: Dash complete cleanup.</color>");

        // If an attack fired during the dash it must be cancelled cleanly
        if (isAttacking)
        {
            isAttacking = false;
            SetCinematicState(false);
            isChargeAttackPrimed = false;
            comboStep = 0;

            Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);

            if (attackWatchdogCoroutine != null)
            {
                StopCoroutine(attackWatchdogCoroutine);
                attackWatchdogCoroutine = null;
            }

            animator.SetBool(isAttackingBoolHash, false);
            animator.SetInteger(attackStepHash, 0);
        }
    }
    private void PerformAttack(int step)
    {
        if (playerMovement.IsDashing() || playerMovement.IsInRootMotionState())
        {
            Debug.LogWarning("Attack blocked: Player is dashing or in root motion.");
            return;
        }
        playerMovement.SetAttacking(true);
        playerMovement.CanMove = false;
        isCustomKnockbackPrimed = false;
        isAttacking = true;
        animator.SetBool(isAttackingBoolHash, true);
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
        // --- THIS IS THE NEW RANDOM LOGIC ---
        // 1. Generate a random number: 0 or 1.
        int variant = UnityEngine.Random.Range(0, 2); // Min is inclusive, Max is exclusive. So this gives 0 or 1.

        // 2. Set the Animator parameters.
        animator.SetInteger(attackStepHash, step);
        animator.SetInteger(attackVariantHash, variant);
        // --- END OF NEW LOGIC ---

        Debug.Log($"<color=green>ATTACK {step} TRIGGERED! (Variant: {variant})</color>");
    }
    private void PerformUpperAttack()
    {
        playerMovement.CanMove = false;
        if (playerMovement != null && playerMovement.IsDashing())
        {
            Debug.LogError("Upper Attack blocked: Player is still in a dash state!");
            return;
        }
        if (isAttacking || isDownSlamming || (playerHealth != null && playerHealth.IsBlocking()) || (playerHealth != null && playerHealth.isStunned))
        {
            return;
        }

        Debug.Log("<color=yellow>--- UPPER ATTACK TRIGGERED ---</color>");
        isAttacking = true;
       
        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
        animator.SetTrigger(upperAttackTriggerHash);
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);

    }
    public void TriggerRootUpper()
    {

        if (playerMovement != null)
        {
            // IMPORTANT: You must find the duration of your RootUpperAttack animation clip
            // and put that exact value here.
            float upperAttackDuration = 0.7f; // <--- CHANGE THIS TO YOUR ANIMATION'S DURATION

            // --- THIS IS THE NEW DIRECTIONAL LOGIC ---
            // 1. Ask the movement script which way we are facing.
            if (playerMovement.IsFacingRight())
            {
                // 2A. If facing RIGHT, play the normal root motion animation.
                playerMovement.InitiateRootMotion(rootUpperAttackTriggerHash, upperAttackDuration);
            }
            else
            {
                // 2B. If facing LEFT, play the mirrored "Left" root motion animation.
                playerMovement.InitiateRootMotion(rootUpperAttackLeftTriggerHash, upperAttackDuration);
            }
            // --- END OF NEW LOGIC ---
        }
    }
    public void DealUpperAttackDamage()
    {
        // --- THIS IS THE FIX ---
        // Instead of calling the generic AttackEnemy, we will find the enemy
        // and call a new, specialized method on its health script.

        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            // We check for SpearHealth specifically for now.
            SpearHealth spearHealth = enemy.GetComponent<SpearHealth>();
            if (spearHealth != null)
            {
                // Call the new, specialized method.
                spearHealth.TakeUpperAttack(upperAttackData);
                break; // Hit one enemy and stop.
            }
            KnightHealth knightHealth = enemy.GetComponent<KnightHealth>();
            if (knightHealth != null)
            {
                // Call the new, specialized method.
                knightHealth.TakeUpperAttack(upperAttackData);
                break; // Hit one enemy and stop.
            }
            ReaperHealth ReaperHealth = enemy.GetComponent<ReaperHealth>();
            if (ReaperHealth != null)
            {
                // Call the new, specialized method.
                ReaperHealth.TakeUpperAttack(upperAttackData);
                break; // Hit one enemy and stop.
            }
        }
      
    }
    /// <summary>
    /// Resets the combo state. Called by the timer in Update().
    /// </summary>
    private void ResetCombo()
    {
        Debug.Log("<color=orange>Combo Reset.</color>");
        comboStep = 0;
        aerialComboStep = 0;
    }
    public bool TryAttackJump()
    {
        // Only trigger if currently in a ground combo
        if (!isAttacking || isAttackJumping || IsInCinematicState || isDownSlamming) return false;
        if (!playerMovement.IsGrounded()) return false;

        // Pick a variant, respecting the max-same-in-a-row rule
        int chosenVariant;

        if (lastAttackJumpVariant == -1)
        {
            // First time — purely random
            chosenVariant = UnityEngine.Random.Range(0, 2);
        }
        else if (sameAttackJumpCount >= maxSameAttackJumpInRow)
        {
            // Forced to switch — pick the OTHER variant
            chosenVariant = lastAttackJumpVariant == 0 ? 1 : 0;
        }
        else
        {
            // Random but track repeats
            chosenVariant = UnityEngine.Random.Range(0, 2);
        }

        // Update tracking
        if (chosenVariant == lastAttackJumpVariant)
            sameAttackJumpCount++;
        else
            sameAttackJumpCount = 1;

        lastAttackJumpVariant = chosenVariant;

        // Execute
        isAttackJumping = true;
        Debug.Log($"<color=lime>ATTACK JUMP V{chosenVariant + 1} TRIGGERED!</color>");

        animator.SetTrigger(attackJumpV1TriggerHash);
        return true;
    }

    // Call this from the Animation Event on the last frame of both AttackJump animations
    public void EVENT_OnAttackJumpComplete()
    {
        isAttackJumping = false;
        EndAttack();
        Debug.Log("<color=lime>Attack Jump complete. Player restored.</color>");
    }
    public void OnPlayerLanded()
    {
        // If we were in the middle of an aerial combo, this landing cancels it.
        if (aerialComboStep > 0)
        {
            Debug.LogWarning("--- Player Landed: Resetting Aerial Combo ---");
            aerialComboStep = 0;
            animator.SetInteger(aerialAttackStepHash, 0);

            // We can also call EndAttack() here to ensure a full state reset.
            EndAttack();
        }
    }
    private IEnumerator AttackWatchdogRoutine()
    {
        // Wait for the specified timeout duration.
        yield return new WaitForSeconds(attackTimeout);

        // If we get here, it means EndAttack() was never called cleanly.
        // We check if isAttacking is STILL true.
        if (isAttacking)
        {
            Debug.LogWarning($"<color=orange>PLAYER ATTACK TIMEOUT! Forcibly resetting state.</color>");
            // Force the attack to end.
            EndAttack();
        }
    }
    public void DealCounterDamage()
    {
        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        Debug.LogWarning("--- DEALING COUNTER DAMAGE NOW ---");

        // 1. Find all enemies in the attack box.
        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            // 2. Get the enemy's health component.
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
                // 3. Call the enemy's TakeDamage method DIRECTLY.
                //    We are NOT using ApplyDamageAndKnockback. We are bypassing it
                //    to avoid any knockback or hit reactions.
                enemyHealth.TakeDamageCounter(counterDamage);

                // Optional: Add a special camera shake or blood effect here for the counter.
                // CameraShakerHandler.Shake(counterShakeData);
                // Instantiate(counterBloodEffect, ...);
                if (playerHealth != null) playerHealth.HealFromCounter(counterDamage);
                // We only want to hit one enemy, so we break the loop.
                break;
            }
        }
        // --- END OF FIX ---
    }
    public void PlayRandomCounterSound()
    {
        PlayRandomAttackSound(counterClips);
    }
    public void PlayAttackSound(AudioClip clip)
    {
        if (clip == null || attackSfxSource == null) return;
        attackSfxSource.PlayOneShot(clip, attackSfxVolume);
    }
    public void PlayRandomAttackSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || attackSfxSource == null) return;
        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip != null) attackSfxSource.PlayOneShot(clip, attackSfxVolume);
    }
    public bool IsAttacking()
    {
        return isAttacking;
    }
    public void PerformLunge()
    {
        if (playerMovement == null) return;
        lungeCoroutine = StartCoroutine(LungeCoroutine(1f));
    }
    public void PerformLungeBackward()
    {
        if (playerMovement == null) return;
        // Stop any previous lunge to be safe
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        // Start the backward lunge
        lungeCoroutine = StartCoroutine(LungeCoroutine(-1f)); // Backward direction is -1
    }

    public void ForceResetState()
    {
        Debug.LogError("--- ZREYATTACKS: FORCIBLY RESETTING ALL STATES! ---");

        // --- Unlock all state flags ---
        isAttacking = false;
        SetCinematicState(false);
        isDownSlamming = false;
        isCountering = false;
        isChargeAttackPrimed = false;
        isDownSlamPrimed = false;
        comboStep = 0;
        aerialComboStep = 0;
        lastAttackJumpVariant = -1;
        sameAttackJumpCount = 0;
        // --- Reset Physics/Collisions ---
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        RestoreNormalGravity(); // Use your existing method to be safe.

        // --- Reset Animators ---
        if (animator != null)
        {
            animator.SetInteger(attackStepHash, 0);
            animator.SetInteger(aerialAttackStepHash, 0);
        }

        // --- Kill All Coroutines in this script ---
        StopAllCoroutines();
    }
    public void PerformTransformLunge()
    {
        // If a lunge is already happening, stop it first.
        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
        }
        // Start the new, transform-based lunge coroutine.
        lungeCoroutine = StartCoroutine(TransformLungeCoroutine());
    }

    // ADD THIS NEW COROUTINE
    private IEnumerator TransformLungeCoroutine()
    {
        Debug.Log("<color=orange>--- Performing TRANSFORM-BASED Lunge ---</color>");

        float timer = 0f;
        Vector3 direction = playerMovement.IsFacingRight() ? Vector3.right : Vector3.left;

        while (timer < lungeDuration)
        {
            // Calculate the movement for this frame.
            float moveStep = lungeSpeed * Time.deltaTime;

            // Apply the movement directly to the transform.
            transform.position += direction * moveStep;

            timer += Time.deltaTime;
            yield return null;
        }

        lungeCoroutine = null; // Mark the coroutine as finished.
    }
    private IEnumerator LungeCoroutine(float directionMultiplier)
    {
        float timer = 0f;
        Vector2 baseDirection = playerMovement.IsFacingRight() ? Vector2.right : Vector2.left;

        // Apply the multiplier. If multiplier is 1, it's forward. If -1, it's backward.
        Vector2 finalDirection = baseDirection * directionMultiplier;

        while (timer < lungeDuration)
        {
            // Calculate the movement for this frame.
            Vector2 moveStep = finalDirection * lungeSpeed * Time.deltaTime;
            // Apply the movement using MovePosition.
            rb.MovePosition(rb.position + moveStep);

            timer += Time.deltaTime;
            yield return null;
        }
    }
    public void GuardCrush()
    {
        Debug.LogWarning("--- PLAYER EXECUTING GUARD CRUSH ---");

        // 1. Find all enemies in the attack box.
        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            // 2. Get the enemy's health component.
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
               
                enemyHealth.TriggerStun(guardCrushStunDuration);
               
                break;
            }
            SpearHealth spearHealth = enemy.GetComponent<SpearHealth>();
            if (spearHealth != null)
            {

                spearHealth.TriggerStun(guardCrushStunDuration);

                break;
            }
        }
    }
    public void EndAttack()
    {
        playerMovement.SetAttacking(false);
        playerMovement.CanMove = true;
        animator.SetBool(isAttackingBoolHash, false);
        if (attackWatchdogCoroutine != null)
        {
            StopCoroutine(attackWatchdogCoroutine);
            attackWatchdogCoroutine = null;
        }
        isAttacking = false;
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        isAttackJumping = false;
        animator.SetInteger(attackStepHash, 0);
        animator.SetInteger(aerialAttackStepHash, 0);
        isChargeAttackPrimed = false;
        comboResetCoroutine = StartCoroutine(ComboResetRoutine());
        Debug.Log($"Attack {comboStep} finished. Combo reset timer started.");

        // Drain aerial buffer — if the player spammed attack during uppercut and is airborne, fire them now
        if (aerialInputBuffer > 0 && !playerMovement.IsGrounded() && !isDownSlamming)
        {
            Debug.Log($"<color=cyan>Draining aerial buffer: {aerialInputBuffer} tap(s) queued.</color>");
            int taps = aerialInputBuffer;
            aerialInputBuffer = 0;
            StartCoroutine(DrainAerialBuffer(taps));
        }
        else
        {
            aerialInputBuffer = 0; // Clear stale buffer if grounded
        }
    }

    private IEnumerator DrainAerialBuffer(int taps)
    {
        for (int i = 0; i < taps; i++)
        {
            // Wait one frame so the state has fully reset before firing the next attack
            yield return null;

            // Safety checks — if something changed between taps, stop draining
            if (isAttacking || isDownSlamming || IsInCinematicState || playerMovement.IsGrounded())
            {
                Debug.Log("<color=orange>Aerial buffer drain stopped early — state changed.</color>");
                aerialInputBuffer = 0;
                yield break;
            }

            PerformAerialAttack();

            // Wait for this attack to finish before firing the next buffered tap
            yield return new WaitUntil(() => !isAttacking);
        }
    }
    public void StopLunge()
    {
        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
            Debug.Log("<color=orange>Player lunge interrupted by taking damage!</color>");
        }
    }
    public void CameraShake()
    {
        CameraShakerHandler.Shake(CameraShakeLight);
    }
    public void CameraShakeMiid()
    {
        CameraShakerHandler.Shake(CameraShakeMid);
    }
    public void CameraShakeheavy()
    {
        CameraShakerHandler.Shake(CameraShakeHeavy);
    }
    public void AttackEnemy(AttackData attackData)
    {
        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
                bool wasBlocking = enemyHealth.isBlocking;
                enemyHealth.ApplyDamageAndKnockback(attackData);
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break;
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break;
            }
            SpearHealth spearHealth = enemy.GetComponent<SpearHealth>();
            if (spearHealth != null)
            {
                bool wasBlocking = spearHealth.isBlocking;
                spearHealth.ApplyDamageAndKnockback(attackData);
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break;
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break; 
            }
            ReaperHealth reaperHealth = enemy.GetComponent<ReaperHealth>();
            if (reaperHealth != null)
            {
                bool wasBlocking = reaperHealth.isBlocking;
                reaperHealth.ApplyDamageAndKnockback(attackData);
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break;
                if (playerHealth != null && !wasBlocking) playerHealth.HealFromLifeSteal(attackData.damage);
                break;
            }
        }
    }
    private IEnumerator ComboResetRoutine()
    {
        yield return new WaitForSeconds(comboResetTime);

        // If we get here, it means the player didn't press the attack button in time.
        Debug.Log("<color=orange>Combo Reset Timer Expired.</color>");
        comboStep = 0;
        lastAttackJumpVariant = -1;
        sameAttackJumpCount = 0;
    }

    public void ApplyKnockback(Transform attacker, float knockbackDistance, float knockbackDuration)
    {
        // Stop any previous knockback to handle rapid hits.
        // You might already have a knockback coroutine reference; if so, use it.
        // For now, we'll just start a new one.
        StartCoroutine(PlayerKnockbackRoutine(attacker, knockbackDistance, knockbackDuration));
    }

    private IEnumerator PlayerKnockbackRoutine(Transform attacker, float knockbackDistance, float knockbackDuration)
    {
        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
        }

        // --- THIS IS THE DIAGNOSTIC FIX ---

        Debug.Log($"<color=yellow>--- COMMAND RECEIVED ---</color>\n" +
                  $"EXECUTOR: ZreyAttacks.PlayerKnockbackRoutine\n" +
                  $"SOURCE (Attacker): {attacker.name} at position {attacker.position}\n" +
                  $"PLAYER: {this.transform.name} at position {this.transform.position}");

        // 1. Calculate the direction vector.
        Vector2 directionVector = (transform.position - attacker.position).normalized;
        Debug.Log($"Step 1: Raw Direction Vector = {directionVector}");

        // 2. Isolate the horizontal component.
        Vector2 horizontalKnockbackDirection = new Vector2(directionVector.x, 0).normalized;
        Debug.Log($"Step 2: Horizontal Direction = {horizontalKnockbackDirection}");

        // 3. Calculate the final velocity.
        Vector2 knockbackVelocity = horizontalKnockbackDirection * (knockbackDistance / knockbackDuration);
        Debug.Log($"Step 3: Final Knockback Velocity = {knockbackVelocity}");

        // --- END OF FIX ---

        if (horizontalKnockbackDirection == Vector2.zero)
        {
            Debug.LogError("Knockback direction was zero! Aborting knockback.", this);
            yield break;
        }

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            // We will add a log here too, to see if the velocity is being applied.
            if (rb != null)
            {
                rb.linearVelocity = knockbackVelocity;
                Debug.Log($"Frame {Time.frameCount}: Applying velocity {rb.linearVelocity}");
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null && rb.linearVelocity.x == knockbackVelocity.x)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    public void TelegraphAttack()
    {
        // Define how far away enemies should be able to "see" the attack starting.
        float notificationRange = 10f;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, notificationRange, enemyLayer);

        foreach (Collider2D enemy in nearbyEnemies)
        {
            KnightAI enemyAI = enemy.GetComponent<KnightAI>();
            if (enemyAI != null)
            {
                // Tell the AI BRAIN that we are starting an attack.
                enemyAI.OnPlayerAttackTelegraphed(this.transform);
            }
            SpearAI enemyAi = enemy.GetComponent<SpearAI>();
            if (enemyAi != null)
            {
                // Tell the AI BRAIN that we are starting an attack.
                enemyAi.OnPlayerAttackTelegraphed(this.transform);
            }
            ReaperAI ReaperAi = enemy.GetComponent<ReaperAI>();
            if (ReaperAi != null)
            {
                // Tell the AI BRAIN that we are starting an attack.
                ReaperAi.OnPlayerAttackTelegraphed(this.transform);
            }
        }
    }
    public void CancelAttack()
    {
        // If we are not attacking, there's nothing to cancel.
        if (!isAttacking) return;

        Debug.LogWarning("<color=orange>ATTACK CANCELLED by a higher priority action (e.g., Block)!</color>");

        // We call the same EndAttack() method that our animation events use.
        // This ensures the state is cleaned up correctly (isAttacking = false, collisions reset, etc.).
        EndAttack();
        isAttacking = false;
        isChargeAttackPrimed = false;
        isDownSlamming = false;
        comboStep = 0; // An interruption always breaks the combo.
        aerialComboStep = 0;
        lastAttackJumpVariant = -1;
        sameAttackJumpCount = 0;
        // 3. Reset physics and animator states.
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        animator.SetInteger(attackStepHash, 0);
        // You can also add ResetTrigger for any attack animations here if needed.
        animator.ResetTrigger(upperAttackTriggerHash);
        animator.ResetTrigger(specialAttackBlockTriggerHash);
    }
    public void StartKnightCounter(Transform counterTarget = null)
    {
        isCountering = true;
        PlayRandomAttackSound(counterClips);
        SetCinematicState(true, counterTarget);
        if (playerTrail != null)
        {
            playerTrail.StartTrail();
        }
        // 1. BRUTALLY INTERRUPT whatever the player was doing.
        CancelAttack(); // Cancel any normal combo.
        if (playerMovement != null)
        {
            // You might need a StopDash() method on your movement script if the dash is a coroutine.
            // For now, let's just lock movement.
            playerMovement.CanMove = false;
        }

        // 2. PLAY THE BURST. This is the initial "teleport" or "flash" animation.
        animator.SetTrigger(counterBurstTriggerHash);

    }

    public void TriggerRootAndCounterAnimations()
    {
        Debug.LogWarning("!!! ANIMATION EVENT: TriggerRootAndCounterAnimations() CALLED !!!");

        // 1. Trigger the final VISUAL attack on the main animator.
        animator.SetTrigger(knightCounterTriggerHash);

        // if (rootZreyAnimator != null && playerMovement != null)
        //  {
        // 3. ASK the movement script which way the player is facing.
        // if (playerMovement.IsFacingRight())
        //{
        // 4A. If facing RIGHT, trigger the normal RootKnightCounter.
        //  Debug.Log("<color=cyan>Playing RootKnightCounter (Right)</color>");
        //rootZreyAnimator.SetTrigger(rootKnightCounterTriggerHash);
    }
    //  else
    // {
    // 4B. If facing LEFT, trigger the mirrored RootKnightCounterLeft.
    //  Debug.Log("<color=cyan>Playing RootKnightCounterLeft (Left)</color>");
    // rootZreyAnimator.SetTrigger(rootKnightCounterLeftTriggerHash);
    // }

    // 5. INITIATE THE ROOT MOTION SYNCHRONIZATION
    //    We use the same robust system we built for the other root motion attacks.
    //    IMPORTANT: You must find the duration of your RootKnightCounter animation
    //    and put that exact value here.
    // float knightCounterDuration = 2.0f; // <--- CHANGE THIS TO YOUR ANIMATION'S DURATION
    //  playerMovement.InitiateRootMotion(0, knightCounterDuration); // We pass 0 for the trigger hash because we already triggered it.
    //}
    //}
    // An event on the final 'knightCounter' animation should call a method to give control back.
    public void FinishKnightCounter()
    {
        isCountering = false;
        SetCinematicState(false);
        if (playerMovement != null)
        {
            playerMovement.CanMove = true;
        }
        // You might also want to call EndAttack() here to clean up any attack state.
        EndAttack();
    }
    public bool IsCountering()
    {
        return isCountering;
    }
    public void StartCinematicZoom()
    {
        // Failsafe: if there's no camera, do nothing.
        if (mainCamera == null) return;

        // If a zoom is already happening, stop it first.
        if (cameraZoomCoroutine != null)
        {
            StopCoroutine(cameraZoomCoroutine);
        }

        // Start the new zoom-in coroutine.
        cameraZoomCoroutine = StartCoroutine(ZoomCamera(mainCamera.orthographicSize, zoomInSize, zoomDuration));
    }
   
    /// <summary>
    /// Called by an Animation Event to start the smooth zoom-out effect.
    /// </summary>
    public void EndCinematicZoom()
    {
        if (mainCamera == null) return;

        if (cameraZoomCoroutine != null)
        {
            StopCoroutine(cameraZoomCoroutine);
        }

        // Start the new zoom-out coroutine, returning to the original size.
        cameraZoomCoroutine = StartCoroutine(ZoomCamera(mainCamera.orthographicSize, originalCameraSize, zoomDuration));
    }

    /// <summary>
    /// The coroutine that handles the smooth transition of the camera's size.
    /// </summary>
    private IEnumerator ZoomCamera(float startSize, float endSize, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // Use Mathf.Lerp to smoothly interpolate between the start and end sizes.
            mainCamera.orthographicSize = Mathf.Lerp(startSize, endSize, timer / duration);

            timer += Time.deltaTime;
            yield return null;
        }

        // After the loop, guarantee the camera is at the exact end size.
        mainCamera.orthographicSize = endSize;
        cameraZoomCoroutine = null; // Mark the coroutine as finished.
    }
    private void HandleInteractionInput()
    {
        // Block ALL counter input if player is stunned
        if (playerHealth != null && playerHealth.isStunned)
        {
            Debug.Log("<color=orange>Counter Input Ignored: Player is stunned.</color>");
            return;
        }
        if (playerHealth != null && playerHealth.IsGrabbed) return;
        if (isAttacking || IsInCinematicState) return;



        // PRIORITY 2: Vagabond Counter — Knight enemies ONLY via direct call
        if (BroadcastVagabondCounter()) return;

        // PRIORITY 3: Reaper Counter — Reaper enemies ONLY
        if (BroadcastReaperCounter()) return;

        // PRIORITY 4: Knight Counter — Spear enemies ONLY via direct call
        BroadcastSpearCounter();
    }
    private bool BroadcastReaperCounter()
    {
        if (playerHealth != null && playerHealth.isStunned) return false;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            transform.position, counterBroadcastRange, enemyLayer);

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Skip Knight and Spear enemies — this broadcast is Reaper only
            KnightAI knightCheck = enemyCollider.GetComponentInParent<KnightAI>();
            if (knightCheck == null) knightCheck = enemyCollider.GetComponentInChildren<KnightAI>();
            if (knightCheck != null) continue;

            SpearAI spearCheck = enemyCollider.GetComponentInParent<SpearAI>();
            if (spearCheck == null) spearCheck = enemyCollider.GetComponentInChildren<SpearAI>();
            if (spearCheck != null) continue;

            ReaperAI reaperAI = enemyCollider.GetComponentInParent<ReaperAI>();
            if (reaperAI == null) reaperAI = enemyCollider.GetComponentInChildren<ReaperAI>();
            if (reaperAI != null)
            {
                Debug.Log($"<color=magenta>Found ReaperAI on {enemyCollider.name}. Firing counter attempt.</color>");
                // Fire the same event — ReaperAI is subscribed and will handle it
                OnPlayerCounterAttempt?.Invoke();
                return true;
            }
        }

        Debug.Log("<color=grey>Reaper counter broadcast: no reaper enemy nearby.</color>");
         return false;
    }

    private bool BroadcastVagabondCounter()
    {
        if (playerHealth != null && playerHealth.isStunned) return false;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, counterBroadcastRange, enemyLayer);

        Debug.Log($"<color=yellow>BroadcastVagabondCounter: checking {nearbyEnemies.Length} nearby enemies.</color>");

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Search parent and children, not just the exact object
            SpearAI spearCheck = enemyCollider.GetComponentInParent<SpearAI>();
            if (spearCheck == null) spearCheck = enemyCollider.GetComponentInChildren<SpearAI>();
            if (spearCheck != null) continue; // Skip spear enemies

            KnightAI knightAI = enemyCollider.GetComponentInParent<KnightAI>();
            if (knightAI == null) knightAI = enemyCollider.GetComponentInChildren<KnightAI>();
            if (knightAI != null)
            {
                Debug.Log($"<color=yellow>Found KnightAI on {enemyCollider.name}. Calling OnPlayerCounterAttempt.</color>");
                bool accepted = knightAI.OnPlayerCounterAttempt(this);
                if (accepted) return true;
            }
        }
        return false;
    }

    private void BroadcastSpearCounter()
    {
        if (playerHealth != null && playerHealth.isStunned) return;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, counterBroadcastRange, enemyLayer);

        Debug.Log($"<color=cyan>BroadcastSpearCounter: checking {nearbyEnemies.Length} nearby enemies.</color>");

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Search parent and children
            KnightAI knightCheck = enemyCollider.GetComponentInParent<KnightAI>();
            if (knightCheck == null) knightCheck = enemyCollider.GetComponentInChildren<KnightAI>();
            if (knightCheck != null) continue; // Skip knight enemies

            SpearAI spearAI = enemyCollider.GetComponentInParent<SpearAI>();
            if (spearAI == null) spearAI = enemyCollider.GetComponentInChildren<SpearAI>();
            if (spearAI != null)
            {
                Debug.Log($"<color=cyan>Found SpearAI on {enemyCollider.name}. Firing event.</color>");
                OnPlayerCounterAttempt?.Invoke();
                return;
            }
        }
        Debug.Log("<color=grey>Spear counter broadcast: no spear enemy nearby.</color>");
    }
    public void ExecuteVagabondCounter(float sequenceDuration, Transform counterTarget = null)
    {
        StartCoroutine(VagabondCounterSequence(sequenceDuration, counterTarget));
    }

    private IEnumerator VagabondCounterSequence(float duration, Transform counterTarget = null)
    {
        // --- 1. LOCK PLAYER ---
        SetCinematicState(true, counterTarget);
        StartFinisherVignette();
        playerMovement.CanMove = false;
        rb.linearVelocity = Vector2.zero;

        // --- 2. PLAY ANIMATION ---
        animator.SetTrigger(vagabondCounterTriggerHash);

        // --- 3. WAIT ---
        yield return new WaitForSeconds(duration);

        // --- 4. RELEASE PLAYER ---
        ForceResetState();
        playerMovement.ForceResetState();
        playerHealth.ForceResetState();
    }
    // MODIFY AttemptFinisher to return a boolean.
    private System.Collections.Generic.HashSet<GameObject> activeFinisherTargets
        = new System.Collections.Generic.HashSet<GameObject>();

    public bool AttemptFinisher()
    {
        if (isAttacking || IsInCinematicState || (playerHealth != null && playerHealth.isStunned))
        {
            return false;
        }
        if (!playerMovement.IsGrounded())
        {
            return false;
        }
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, finisherRange, enemyLayer);

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Skip enemies already being finished
            if (activeFinisherTargets.Contains(enemyCollider.gameObject)) continue;

            SpearHealth spearHealth = enemyCollider.GetComponent<SpearHealth>();
            if (spearHealth != null && spearHealth.isFinishable)
            {
                Debug.LogError("--- SUCCESS! Found finishable Spear Enemy. ---");
                activeFinisherTargets.Add(enemyCollider.gameObject);
                StartCoroutine(ExecuteFinisherSequence(spearHealth));
                return true;
            }

            KnightHealth knightHealth = enemyCollider.GetComponent<KnightHealth>();
            if (knightHealth != null && knightHealth.isFinishable)
            {
                Debug.LogError("--- SUCCESS! Found finishable Knight Enemy. ---");
                activeFinisherTargets.Add(enemyCollider.gameObject);
                StartCoroutine(ExecuteVagabondFinisherSequence(knightHealth));
                return true;
            }

            ReaperHealth reaperHealth = enemyCollider.GetComponent<ReaperHealth>();
            if (reaperHealth != null && reaperHealth.isFinishable)
            {
                Debug.LogError("--- SUCCESS! Found finishable Reaper Enemy. ---");
                activeFinisherTargets.Add(enemyCollider.gameObject);
                StartCoroutine(ExecuteReaperFinisherSequence(reaperHealth));
                return true;
            }
        }

        return false;
    }
    public void StartReaperCounter(Transform reaperTransform = null)
    {
        if (isReaperCountering || IsInCinematicState) return;

        isReaperCountering = true;
        SetCinematicState(true, reaperTransform);
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnPlayerCounteredSuccessfully();
        if (playerTrail != null) playerTrail.StartTrail();

        CancelAttack();

        if (playerMovement != null) playerMovement.CanMove = false;

        // Play the player's dedicated Reaper counter animation
        animator.SetTrigger(reaperCounterTriggerHash);

        StartCoroutine(ReaperCounterSequence());
    }

    private IEnumerator ReaperCounterSequence()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(reaperCounterDuration);

        FinishReaperCounter();
    }

    // Call this from the Animation Event on the last frame of ReaperCounter animation
    // OR it fires automatically after reaperCounterDuration as a fallback
    public void FinishReaperCounter()
    {
        if (!isReaperCountering) return;

        isReaperCountering = false;
        SetCinematicState(false);

        if (playerMovement != null) playerMovement.CanMove = true;

        EndAttack();
        Debug.Log("<color=cyan>Reaper Counter finished. Player control restored.</color>");
    }
    public bool IsReaperCountering() => isReaperCountering;
    private IEnumerator ExecuteVagabondFinisherSequence(KnightHealth targetKnight)
    {
        // --- 1. LOCK EVERYTHING ---
        SetCinematicState(true, targetKnight.transform);
        StartFinisherVignette();
        if (playerMovement != null) playerMovement.CanMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (targetKnight.GetComponent<KnightAI>().finisherPromptUI != null)
        {
            targetKnight.GetComponent<KnightAI>().finisherPromptUI.SetActive(false);
        }


        // --- 2. ALIGN THE ENEMY TO THE PLAYER ---
        KnightFollow knightFollow = targetKnight.GetComponent<KnightFollow>();
        if (knightFollow != null)
        {
            // Force the Knight to face the player before the snap.
            knightFollow.FacePlayer();
        }

        // Make the player face the Knight.
        playerMovement.ForceFaceDirection(targetKnight.transform.position.x > transform.position.x);

        float direction = playerMovement.IsFacingRight() ? 1f : -1f;
        Vector3 snapPosition = transform.position + new Vector3(
            vagabondFinisherSnapOffset.x * direction,
            vagabondFinisherSnapOffset.y,
            vagabondFinisherSnapOffset.z
        );
        targetKnight.transform.position = snapPosition;

        // --- 3. PLAY ANIMATIONS ---
        targetKnight.ExecuteFinisher(); // Command the Knight to play "GetFinished"
        animator.SetTrigger(vagabondFinisherTriggerHash); // Player plays "VagabondFinisher"

        // The rest of the sequence (damage, cleanup) will be handled by animation events.
        yield return null;
    }

    // MODIFY the coroutine to accept the INTERFACE, not the specific script.
    private IEnumerator ExecuteFinisherSequence(SpearHealth target) // Or SpearHealth, if you reverted
    {
        // --- 1. LOCK EVERYTHING (This is correct) ---
        SetCinematicState(true, target.transform);
        StartFinisherVignette();
        if (playerMovement != null) playerMovement.CanMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // --- 2. SNAP POSITION ONLY ---
        // (Face the target logic can go here if you need it)

        SpearFollow enemyFollow = target.transform.GetComponent<SpearFollow>();

        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        // A. COMMAND the enemy to face the player BEFORE we snap positions.
        if (enemyFollow != null)
        {
            enemyFollow.FacePlayer();
        }

        // B. Make the PLAYER face the enemy.
        if ((target.transform.position.x > transform.position.x && !playerMovement.IsFacingRight()) ||
            (target.transform.position.x < transform.position.x && playerMovement.IsFacingRight()))
        {
            // You need a public Flip() method on ZreyMovements for this to work.
            // playerMovement.Flip();
        }

        // C. Calculate the snap position.
        float direction = playerMovement.IsFacingRight() ? 1f : -1f;
        Vector3 snapPosition = transform.position + new Vector3(finisherSnapOffset.x * direction, finisherSnapOffset.y, finisherSnapOffset.z);
        target.transform.position = snapPosition;

        // D. COMMAND the enemy to face the player AGAIN after the snap.
        //    This is a brutal guarantee that the final orientation is correct.
        if (enemyFollow != null)
        {
            enemyFollow.FacePlayer();
        }

        // --- 3. PLAY ANIMATIONS (This is correct) ---
        target.ExecuteFinisher();
        animator.SetTrigger(spearFinisherTriggerHash);

        yield return null;
    }
    private IEnumerator ExecuteReaperFinisherSequence(ReaperHealth targetReaper)
    {
        // --- 1. LOCK EVERYTHING ---
        SetCinematicState(true, targetReaper.transform);
        StartFinisherVignette();
        if (playerMovement != null) playerMovement.CanMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // --- 2. ALIGN THE REAPER TO THE PLAYER ---
        ReaperFollow reaperFollow = targetReaper.GetComponent<ReaperFollow>();

        // A. Force the Reaper to face the player before the snap.
        if (reaperFollow != null) reaperFollow.FacePlayer();

        // B. Make the player face the Reaper.
        playerMovement.ForceFaceDirection(targetReaper.transform.position.x > transform.position.x);

        // C. Snap the Reaper to the fixed offset from the player.
        float direction = playerMovement.IsFacingRight() ? 1f : -1f;
        Vector3 snapPosition = transform.position + new Vector3(
            reaperFinisherSnapOffset.x * direction,
            reaperFinisherSnapOffset.y,
            reaperFinisherSnapOffset.z
        );
        targetReaper.transform.position = snapPosition;

        // D. Re-face guarantee after snap.
        if (reaperFollow != null) reaperFollow.FacePlayer();
        playerMovement.ForceFaceDirection(targetReaper.transform.position.x > transform.position.x);

        yield return null;

        // --- 3. PLAY ANIMATIONS ---
        targetReaper.ExecuteFinisher();          // Reaper plays "TakeFinisher"
        animator.SetTrigger(reaperFinisherTriggerHash); // Player plays "ReaperFinisher"

        // Cleanup is handled by FinishFinisherSequence() called from animation event.
    }
    private void StartFinisherVignette()
    {
        if (vignette == null) return;
        if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(FadeVignette(vignetteTargetIntensity));
    }

    private void StopFinisherVignette()
    {
        if (vignette == null) return;
        if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(FadeVignette(0f, disableOnComplete: true));
    }

    private IEnumerator FadeVignette(float targetIntensity, bool disableOnComplete = false)
    {
        vignette.active = true;
        float start = vignette.intensity.value;

        while (!Mathf.Approximately(vignette.intensity.value, targetIntensity))
        {
            vignette.intensity.value = Mathf.MoveTowards(
                vignette.intensity.value, targetIntensity, vignetteFadeSpeed * Time.deltaTime);
            yield return null;
        }

        vignette.intensity.value = targetIntensity;

        if (disableOnComplete)
        {
            vignette.active = false;
        }

        vignetteCoroutine = null;
    }
    public void FinishFinisherSequence()
    {
        Debug.Log("<color=green>--- Finisher Sequence Finished. Player control restored. ---</color>");

        // Give control back to the player.
        SetCinematicState(false);
        if (playerMovement != null)
        {
            playerMovement.CanMove = true;
        }
        StopFinisherVignette();
    }
    public void SetGravityToZero()
    {
        if (rb != null)
        {
            Debug.Log("<color=cyan>--- Gravity Scale set to 0 ---</color>");
            rb.gravityScale = 0f;
            // Also, it's a good idea to kill any downward velocity when this happens.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    /// <summary>
    /// Called by an Animation Event to restore the player's normal gravity.
    /// </summary>
    public void RestoreNormalGravity()
    {
        if (rb != null)
        {
            Debug.Log("<color=green>--- Gravity Scale restored to normal ---</color>");
            rb.gravityScale = originalGravityScale;
        }
    }
    public void IsInCinematicState_ForceSet(bool value)
    {
        IsInCinematicState = value;
        PlayerInCinematic = value;
    }

    private void SetCinematicState(bool value, Transform cinematicTarget = null)
    {
        IsInCinematicState = value;
        PlayerInCinematic = value;

        if (value)
        {
            playerHealth?.MakeInvincible();
            OnPlayerCinematicStarted?.Invoke(cinematicTarget);
        }
        else
        {
            playerHealth?.MakeVulnerable();
        }
    }
    private void OnDrawGizmosSelected()
    {
        // --- THIS IS THE FIX ---
        // 1. Set the color for the gizmo. Yellow is good for ranges.
        Gizmos.color = Color.yellow;

        // 2. Draw a wireframe sphere (which looks like a circle in 2D)
        //    at the player's current position, with a radius equal to the finisherRange.
        Gizmos.DrawWireSphere(transform.position, finisherRange);
        // --- END OF FIX ---

        // You can also re-add the gizmo for your attack point here if it was removed.
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
        }
    }

}
