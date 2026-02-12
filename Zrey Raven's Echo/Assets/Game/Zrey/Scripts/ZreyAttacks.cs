using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Using the new Input System

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
   

  
    void Awake()
    {
        // Automatically get components if they aren't assigned.
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<ZreyMovements>();
        rb = GetComponent<Rigidbody2D>();
         if (playerTrail == null) playerTrail = GetComponent<ZreyTrail>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
    }

    void Update()
    {
        // 1. If we are already attacking, do nothing.
        if (isAttacking)
        {
            return;
        }

        // 2. Read the raw input state from the InputManager.
        bool attackHeld = InputManager.Instance.isAttackButtonPressed;
        float heldTime = InputManager.Instance.attackButtonHeldTime;
        bool attackReleased = InputManager.Instance.justReleasedAttack;

        // --- THIS IS THE FINAL, GUARANTEED FIX ---

        // 3. CHARGE LOGIC
        // If the button is being held, we haven't already primed a charge, AND we are on the ground...
        if (attackHeld && !isChargeAttackPrimed && playerMovement.IsGrounded())
        {
            // ...check if the hold time has been met.
            if (heldTime >= chargeAttackHoldTime)
            {
                // If YES, perform the Upper Attack immediately.
                PerformUpperAttack();

                // CRITICAL FIX #1: Immediately reset the primed flag.
                // This allows you to release and immediately start a new charge
                // without any "cooldown". The charge is now a "one-shot" event per press.
                isChargeAttackPrimed = true;
            }
        }

        // 4. TAP LOGIC
        // If the button was just released AND we didn't already do a charge attack...
        if (attackReleased && !isChargeAttackPrimed)
        {
            // ...then it was a TAP. Perform a normal attack.
            // The HandleAttack() method already contains its own grounded check.
            HandleAttack();
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
        
        // 2. If we are not attacking, THEN decide what to do.
        if (!playerMovement.IsGrounded())
        {
            // We are in the air and not busy, so perform a down slam.
            PerformDownSlam();
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
        int variant = Random.Range(0, 2); // Min is inclusive, Max is exclusive. So this gives 0 or 1.

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

        // --- THIS IS THE FINAL, GUARANTEED FIX FOR THE COUNTER ---
        // 2. Check if the playerMovement script exists.
        if (playerMovement != null)
        {
            if (playerMovement.IsFacingRight())
            {
                playerMovement.InitiateRootMotion(rootKnightCounterTriggerHash, 2.0f); // Call the renamed method
            }
            else
            {
                playerMovement.InitiateRootMotion(rootKnightCounterLeftTriggerHash, 2.0f); // Call the renamed method
            }
        }
        else
        {
            Debug.LogError("Cannot start counter root motion! ZreyMovements script is not assigned!", this);
        }
        // --- END OF FIX ---
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
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
