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



    private void HandleAttack(InputAction.CallbackContext context)
    {
        if (isAttacking || !playerMovement.IsGrounded()) return;

        // --- THIS IS THE FIX ---
        // If a reset timer is running, stop it. We are continuing the combo.
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }
        // --- END OF FIX ---

        comboStep++;
        PerformAttack(comboStep);

        if (comboStep >= 4)
        {
            comboStep = 0;
        }
    }

    /// <summary>
    /// Triggers the correct attack animation based on the combo step.
    /// </summary>

    private void PerformAttack(int step)
    {
        isCustomKnockbackPrimed = false;
        isAttacking = true;
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
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
        isAttacking = false;
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        // --- THIS IS THE NEW, CRITICAL PART ---
        // Reset the attackStep so the Animator can exit the Attack_Router state.
        animator.SetInteger(attackStepHash, 0);
        // --- END OF NEW PART ---

        comboResetCoroutine = StartCoroutine(ComboResetRoutine());
        Debug.Log($"Attack {comboStep} finished. Combo reset timer started.");
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
            Debug.Log("<color=orange>Lunge interrupted by parry!</color>");
        }

        // --- THIS IS THE FIX ---
        // We are now using the knockbackDistance and knockbackDuration parameters
        // that were PASSED INTO this function from the KnightHealth script.
        Vector2 knockbackDirection = (transform.position - attacker.position).normalized;
        Vector2 knockbackVelocity = knockbackDirection * (knockbackDistance / knockbackDuration);
        // --- END OF FIX ---

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            rb.linearVelocity = knockbackVelocity;
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb.linearVelocity == knockbackVelocity)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    public void TelegraphAttack()
    {
        // Define how far away enemies should be able to "see" the attack starting.
        float notificationRange = 10f;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, notificationRange, enemyLayer);

        foreach (Collider2D enemy in nearbyEnemies)
        {
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();
            if (enemyHealth != null)
            {
                // Tell the enemy that we are starting an attack.
                enemyHealth.OnPlayerAttackTelegraphed(transform);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
