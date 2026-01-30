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
    public ShakeData CameraShakeParry;

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
    private string currentHitReactionType = "back";
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
    void Awake()
    {
        // Automatically get components if they aren't assigned.
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<ZreyMovements>();
        rb = GetComponent<Rigidbody2D>();
        // Set up the new Input System.
        inputActions = new InputSystem_Actions();
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // When the "Fire" action (Left Mouse Button) is performed, call our HandleAttack method.
        inputActions.Player.Attack.performed += HandleAttack;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Attack.performed -= HandleAttack;
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

    private void HandleAttack(InputAction.CallbackContext context)
    {
        if (isAttacking || isDownSlamming)
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
    public void EndAttack()
    {
        if (attackWatchdogCoroutine != null)
        {
            StopCoroutine(attackWatchdogCoroutine);
            attackWatchdogCoroutine = null;
        }
        isAttacking = false;
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        // --- THIS IS THE NEW, CRITICAL PART ---
        // Reset the attackStep so the Animator can exit the Attack_Router state.
        animator.SetInteger(attackStepHash, 0);
        // --- END OF NEW PART ---

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
        CameraShakerHandler.Shake(CameraShakeParry);
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
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
