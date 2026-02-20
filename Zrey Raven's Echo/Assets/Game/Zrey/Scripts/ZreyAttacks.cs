using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Animator))]
public class ZreyAttacks : MonoBehaviour
{
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
    private InputSystem_Actions inputActions;

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
    [Header("Aerial Combo Settings")]
    [Tooltip("The maximum number of attacks in the aerial combo chain.")]
    [SerializeField] private int maxAerialComboSteps = 3;

    [Tooltip("How long the player must hold the attack button in the air to trigger a down slam.")]
    [SerializeField] private float downSlamHoldTime = 0.3f;

    // --- Private state for the aerial combo ---
    private int aerialComboStep = 0;
    private bool isDownSlamPrimed = false;
    private float originalGravityScale;
    // --- New Animation Hashes ---
    private readonly int aerialAttackStepHash = Animator.StringToHash("aerialAttackStep");
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
    }

    void Update()
    {
        // Master shield: If we are busy, do nothing.
        if (isAttacking || IsInCinematicState)
        {
            return;
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

            // Grounded Tap Logic (Normal Combo or Block Attack)
            if (attackReleased && !isChargeAttackPrimed)
            {
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
            if (attackReleased && !isDownSlamPrimed)
            {
                PerformAerialAttack();
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
    private void PerformAerialAttack()
    {
        // Failsafe: If we are busy, do nothing.
        if (isAttacking || isDownSlamming || IsInCinematicState)
        {
            return;
        }

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
    private void HandleAttack()
    {
        if (playerHealth != null && playerHealth.IsBlocking())
        {
            // 2. If YES, perform the new Block Special Attack.
            Debug.Log("<color=lime>--- BLOCK SPECIAL ATTACK TRIGGERED ---</color>");

            // Set the master attacking flag to true.
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
    public bool IsDownSlamming()
    {
        return isDownSlamming;
    }
    /// <summary>
    /// Triggers the correct attack animation based on the combo step.
    /// </summary>

    private void PerformAttack(int step)
    {
        isCustomKnockbackPrimed = false;
        isAttacking = true;
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
        if (isAttacking || isDownSlamming || (playerHealth != null && playerHealth.IsBlocking()) || (playerHealth != null && playerHealth.isStunned))
        {
            return;
        }

        Debug.Log("<color=yellow>--- UPPER ATTACK TRIGGERED ---</color>");
        isAttacking = true;
        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdogRoutine());
        animator.SetTrigger(upperAttackTriggerHash);
        if (playerMovement != null)
    {
        // IMPORTANT: You must find the duration of your RootUpperAttack animation clip
        // and put that exact value here. For example, if it's 0.75 seconds long:
        float upperAttackDuration = 0.75f; // <--- CHANGE THIS TO YOUR ANIMATION'S DURATION

        // We call the public method on ZreyMovements that we already built.
        playerMovement.InitiateRootMotion(rootUpperAttackTriggerHash, upperAttackDuration);
    }
    }

    public void DealUpperAttackDamage()
    {
        AttackEnemy(upperAttackData);
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

                // We only want to hit one enemy, so we break the loop.
                break;
            }
        }
        // --- END OF FIX ---
    }
    public bool IsAttacking()
    {
        return isAttacking;
    }
    public void PerformLunge()
    {
        if (playerMovement == null) return;
        lungeCoroutine = StartCoroutine(LungeCoroutine());
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
    private IEnumerator LungeCoroutine()
    {
        float timer = 0f;
        Vector2 direction = playerMovement.IsFacingRight() ? Vector2.right : Vector2.left;

        while (timer < lungeDuration)
        {
            // Calculate the movement for this frame.
            Vector2 moveStep = direction * lungeSpeed * Time.deltaTime;
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
        if (attackWatchdogCoroutine != null)
        {
            StopCoroutine(attackWatchdogCoroutine);
            attackWatchdogCoroutine = null;
        }
        isAttacking = false;
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
       
        animator.SetInteger(attackStepHash, 0);
        animator.SetInteger(aerialAttackStepHash, 0);
        isChargeAttackPrimed = false;
        comboResetCoroutine = StartCoroutine(ComboResetRoutine());
        Debug.Log($"Attack {comboStep} finished. Combo reset timer started.");
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
        // Find all enemies in the attack box.
        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
                // Call the new, all-in-one function on the knight, passing the data container.
                enemyHealth.ApplyDamageAndKnockback(attackData);
                break; // Hit one enemy and stop.
            }
            SpearHealth spearHealth = enemy.GetComponent<SpearHealth>();
            if (spearHealth != null)
            {
                // Call the new, all-in-one function on the knight, passing the data container.
                spearHealth.ApplyDamageAndKnockback(attackData);
                break; // Hit one enemy and stop.
            }
        }
    }
    private IEnumerator ComboResetRoutine()
    {
        yield return new WaitForSeconds(comboResetTime);

        // If we get here, it means the player didn't press the attack button in time.
        Debug.Log("<color=orange>Combo Reset Timer Expired.</color>");
        comboStep = 0;
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
        // 3. Reset physics and animator states.
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        animator.SetInteger(attackStepHash, 0);
        // You can also add ResetTrigger for any attack animations here if needed.
        animator.ResetTrigger(upperAttackTriggerHash);
        animator.ResetTrigger(specialAttackBlockTriggerHash);
    }
    public void StartKnightCounter()
    {
        isCountering = true;
        IsInCinematicState = true;
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

    }
    // An event on the final 'knightCounter' animation should call a method to give control back.
    public void FinishKnightCounter()
    {
        isCountering = false;
        IsInCinematicState = false;
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
        // This method is called every time the 'C' button is pressed.
        // It decides what to do based on priority.

        // PRIORITY #1: FINISHER
        // First, we try to perform a finisher. We will create a new method
        // that returns 'true' if it succeeds and 'false' if it fails.
        bool finisherSuccess =  AttemptFinisher();

        // If the finisher was successful, we do nothing else.
        if (finisherSuccess)
        {
            return;
        }

        // PRIORITY #2: COUNTER
        // If the finisher failed, we then attempt a counter by firing our
        // own event for any nearby enemies to hear.
        OnPlayerCounterAttempt?.Invoke();
    }

    // MODIFY AttemptFinisher to return a boolean.
   public bool AttemptFinisher()
// --- END OF FIX ---
{
    if (isAttacking || IsInCinematicState || (playerHealth != null && playerHealth.isStunned))
    {
        return false; // Finisher fails because we are busy.
    }

    Debug.Log($"--- Attempting Finisher. Checking circle with radius {finisherRange} on layer {LayerMask.LayerToName(enemyLayer.value)} ---");
    Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, finisherRange, enemyLayer);

    if (nearbyEnemies.Length == 0)
    {
        Debug.LogWarning("Finisher check found ZERO colliders on the specified enemy layer.");
    }

    foreach (Collider2D enemyCollider in nearbyEnemies)
    {
        Debug.Log($"Found potential target: {enemyCollider.name}");
        SpearHealth enemyHealth = enemyCollider.GetComponent<SpearHealth>();

        if (enemyHealth != null && enemyHealth.isFinishable)
        {
            Debug.LogError("--- SUCCESS! Found finishable Spear Enemy. Starting sequence. ---");
            StartCoroutine(ExecuteFinisherSequence(enemyHealth));
            return true; // --- THIS IS THE FIX: Report that the finisher was successful.
        }
        else if (enemyHealth != null)
        {
            Debug.LogWarning($"Found Spear Enemy '{enemyCollider.name}', but it is not finishable. (isFinishable = {enemyHealth.isFinishable})");
        }
    }

    // --- THIS IS THE FIX ---
    // If we get through the whole loop and find no target, the finisher fails.
    return false;
    // --- END OF FIX ---
}

    // MODIFY the coroutine to accept the INTERFACE, not the specific script.
    private IEnumerator ExecuteFinisherSequence(SpearHealth target) // Or SpearHealth, if you reverted
    {
        // --- 1. LOCK EVERYTHING (This is correct) ---
        IsInCinematicState = true;
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
    public void FinishFinisherSequence()
    {
        Debug.Log("<color=green>--- Finisher Sequence Finished. Player control restored. ---</color>");

        // Give control back to the player.
        IsInCinematicState = false;
        if (playerMovement != null)
        {
            playerMovement.CanMove = true;
        }
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
