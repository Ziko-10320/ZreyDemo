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
    void Awake()
    {
        // Automatically get components if they aren't assigned.
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<ZreyMovements>();
        rb = GetComponent<Rigidbody2D>();
        // Set up the new Input System.
        inputActions = new InputSystem_Actions();
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
        isAttacking = true;

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

    void FixedUpdate()
    {
        // We only check for damage if the damage window is open.
        if (isDamageFrameActive && !hasDealtDamageThisAttack)
        {
            CheckForEnemyDamage();
        }
    }

    private void CheckForEnemyDamage()
    {
        // Create a box in front of the player to detect enemies.
        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, enemyLayer);

        // Loop through all the enemies we hit.
        foreach (Collider2D enemy in enemiesHit)
        {
            Debug.Log("Hit: " + enemy.name);
            KnightHealth enemyHealth = enemy.GetComponent<KnightHealth>();

            // If the enemy has a KnightHealth script...
            if (enemyHealth != null)
            {
                if (isCustomKnockbackPrimed)
                {
                    // 2. If yes, call the knight's SetCustomKnockback method RIGHT NOW.
                    enemyHealth.SetCustomKnockback(primedKnockbackDistance, primedKnockbackDuration);
                }
                enemyHealth.TakeDamage(attackDamage, transform, currentHitReactionType); // Pass the player's transform for knockback direction.
                hasDealtDamageThisAttack = true; // Mark that we've dealt damage so we don't hit again this swing.
                break; // Exit the loop after the first enemy is hit. Remove this line if you want one swing to hit multiple enemies.
            }
        }
        isCustomKnockbackPrimed = false;
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

        // --- THIS IS THE NEW, CRITICAL PART ---
        // Reset the attackStep so the Animator can exit the Attack_Router state.
        animator.SetInteger(attackStepHash, 0);
        // --- END OF NEW PART ---

        comboResetCoroutine = StartCoroutine(ComboResetRoutine());
        Debug.Log($"Attack {comboStep} finished. Combo reset timer started.");
    }
    public void StartDamage()
    {
        Debug.Log("<color=red>Damage Window OPEN</color>");
        isDamageFrameActive = true;
        hasDealtDamageThisAttack = false; // Reset this for the new attack.
    }

    /// <summary>
    /// Called by an Animation Event to END the damage window.
    /// </summary>
    public void StopDamage()
    {
        Debug.Log("<color=grey>Damage Window CLOSED</color>");
        isDamageFrameActive = false;
    }
    public void CameraShake()
    {
        CameraShakerHandler.Shake(CameraShakeParry);
    }
    public void PrimeCustomKnockback(string values)
    {
        // --- THIS IS THE FIX FOR THE STRING FORMAT ---
        // Unity's Animation Event field does not need extra quotes.
        // Just type: 4.5,0.3
        // We also use CultureInfo.InvariantCulture to ensure '.' is always the decimal separator.
        string[] splitValues = values.Split(',');
        if (splitValues.Length != 2) return;

        if (float.TryParse(splitValues[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float distance) &&
            float.TryParse(splitValues[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float duration))
        {
            isCustomKnockbackPrimed = true;
            primedKnockbackDistance = distance;
            primedKnockbackDuration = duration;
            Debug.Log($"<color=lime>Player has PRIMED a custom knockback! D:{distance}, T:{duration}</color>");
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
    public void SetHitReactionType(string hitType)
    {
        currentHitReactionType = hitType;
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
