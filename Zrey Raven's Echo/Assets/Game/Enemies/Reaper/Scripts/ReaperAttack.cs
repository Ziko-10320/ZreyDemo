using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class ReaperAttack : MonoBehaviour
{
    [Header("References")]
    private Animator animator;
    private Rigidbody2D rb;
    [Header("Damage Settings")]
    [Tooltip("The amount of damage each attack deals.")]
    public int attackDamage = 10;
    [Tooltip("An empty GameObject marking the center of the knight's damage area.")]
    public Transform attackPoint;
    [Tooltip("The size of the damage area (Width, Height).")]
    public Vector2 attackAreaSize = new Vector2(1.5f, 2f);
    [Tooltip("The layer the player is on, so we know who to damage.")]
    public LayerMask playerLayer;

    // --- MODIFY THIS HEADER ---
    [Header("Attack Settings")]
    [Tooltip("The time between the end of one combo and the start of the next.")]
    public float timeBetweenCombos = 3f;
    [Tooltip("The force of the lunge during an attack animation.")]
    public float lungeForce = 5f;
    // --- ADD THIS NEW VARIABLE ---
    [Tooltip("How long the lunge lasts (in seconds).")]
    public float lungeDuration = 0.2f;

    // --- State Control ---
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private float lastComboTime = -10f;
    public ShakeData CameraShakeLight;
    public ShakeData CameraShakeMid;
    public ShakeData CameraShakeHeavy;
    private bool isDamageWindowOpen = false;
    private Coroutine comboWatchdogCoroutine;

    private string hitReactionType = "back";
    private ImpactData currentImpactData;
    [SerializeField] private int playerLayerValue = 6; // Example: Change this to your actual Player layer number"
    [Tooltip("The integer value of the Enemy's layer.")]
    [SerializeField] private int enemyLayerValue = 7;
    private Coroutine knockbackCoroutine;
    private int currentComboStep = 0;
    [SerializeField] private float comboTimeout = 4f;
    private ReaperFollow followAI;
    [Header("AI Integration")]
    [Tooltip("The total duration of the main attack combo.")]
    [SerializeField] private float comboDuration = 2.5f;
    [Tooltip("The total duration of the counter-attack animation.")]
    [SerializeField] private float counterAttackDuration = 1.5f;
    private ReaperHealth health;
    private readonly int counterAttackTriggerHash = Animator.StringToHash("counterAttack");
    private readonly int specialAttackTriggerHash = Animator.StringToHash("specialAttack");
    [Header("Special Attack Damage")]
    [Tooltip("The ImpactData for the unblockable special attack.")]
    [SerializeField] private ImpactData specialAttackImpactData;

    [Tooltip("The damage dealt by EACH TICK of the special attack.")]
    [SerializeField] private int specialAttackDamagePerTick = 5;

    [Tooltip("The time (in seconds) between each damage tick.")]
    [SerializeField] private float timeBetweenTicks = 0.2f;

    private Coroutine specialDamageCoroutine;
    private Coroutine lungeCoroutine;

    [Header("Backstep Settings")]
    [Tooltip("The force applied to the knight during the backstep.")]
    [SerializeField] private float backstepForce = 8f;

    [Tooltip("The duration of the backstep movement in seconds.")]
    [SerializeField] private float backstepDuration = 0.3f;


    private readonly int backstepTriggerHash = Animator.StringToHash("backstep");
    private ReaperAI ReaperAI;
    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        followAI = GetComponent<ReaperFollow>();
        health = GetComponent<ReaperHealth>();
        ReaperAI = GetComponent<ReaperAI>();
    }

    void Update()
    {
        if (isDamageWindowOpen)
        {
            Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, playerLayer);

            foreach (Collider2D player in hitPlayers)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log("<color=red>Knight hit Player with normal attack!</color>");

                    // This calls the normal TakeDamage, which CAN be blocked or parried.
                    playerHealth.TakeDamage(attackDamage, transform, currentImpactData);

                    // Immediately close the window to prevent hitting multiple times.
                    isDamageWindowOpen = false;
                    break; // Exit the loop.
                }
            }
        }
    }
    public void StartDamage()
    {
        Debug.Log("<color=orange>Knight Damage Window OPEN</color>");

        if (health != null) health.isUnbreakable = true;

        isDamageWindowOpen = true;
    }

    /// <summary>
    /// Called by an Animation Event to CLOSE the damage window.
    /// </summary>
    public void StopDamage()
    {
        Debug.Log("<color=grey>Knight Damage Window CLOSED</color>");

        if (health != null) health.isUnbreakable = false;

        isDamageWindowOpen = false;
    }
    public void StartCollisionWithPlayer()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
    }
    public void StopCollisionWithPlayer()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
    }
    public void StartSpecialDamage()
    {
        Debug.Log("<color=red>!!! Special Damage Over Time STARTED !!!</color>");
        if (ReaperAI != null)
        {
            ReaperAI.OpenCounterWindow();
        }
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
        // If a previous DOT is somehow still running, stop it first.
        if (specialDamageCoroutine != null)
        {
            StopCoroutine(specialDamageCoroutine);
        }
        // Start the new DOT coroutine.
        specialDamageCoroutine = StartCoroutine(SpecialDamageOverTimeRoutine());
    }

    /// <summary>
    /// Called by an Animation Event to STOP the unblockable Damage Over Time effect.
    /// </summary>
    public void StopSpecialDamage()
    {
        Debug.Log("<color=grey>Special Damage Over Time STOPPED</color>");
        if (ReaperAI != null)
        {
            ReaperAI.CloseCounterWindow();
        }
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        // If the DOT coroutine is running, stop it.
        if (specialDamageCoroutine != null)
        {
            StopCoroutine(specialDamageCoroutine);
            specialDamageCoroutine = null;
        }
    }
    private IEnumerator SpecialDamageOverTimeRoutine()
    {
        // This is an infinite loop that will run as long as the coroutine is active.
        // The StopSpecialDamage() method is what breaks us out of this loop.
        while (true)
        {
            // 1. CHECK for the player in the damage area.
            Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, playerLayer);

            foreach (Collider2D player in hitPlayers)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // 2. If we find the player, DEAL UNBLOCKABLE DAMAGE.
                    Debug.LogWarning("!!! Player hit by DOT tick! !!!");
                    playerHealth.TakeUnblockableDamage(specialAttackDamagePerTick, this.transform, specialAttackImpactData);

                    // We break here so we only damage the player once per tick, even if they have multiple colliders.
                    break;
                }
            }

            // 3. WAIT for the specified time before the next tick.
            yield return new WaitForSeconds(timeBetweenTicks);
        }
    }
    public bool IsAttacking()
    {
        return isAttacking;
    }

    // **MODIFIED:** This is now a public method that the KnightAI script will call.
    public void StartCombo()
    {
        if (health != null && !health.IsGrounded()) return;
        if (health != null && health.IsStunned())
        {
            // 2. If we ARE stunned, do NOTHING.
            //    Exit the Update loop immediately. No decisions will be made.
            //    The AI brain is effectively "paused".
            return;
        }
        // If we are already attacking OR if it hasn't been long enough since the last combo...
        if (isAttacking || Time.time < lastComboTime + timeBetweenCombos)
        {
            return; // ...then don't start a new combo.
        }

        isAttacking = true;
        currentComboStep = 1;
        animator.SetTrigger("attack1");
        if (comboWatchdogCoroutine != null) StopCoroutine(comboWatchdogCoroutine);
        comboWatchdogCoroutine = StartCoroutine(ComboWatchdogRoutine());
    }
    private IEnumerator ComboWatchdogRoutine()
    {
        // Wait for the specified timeout duration.
        yield return new WaitForSeconds(comboTimeout);

        // If we get here, it means FinishCombo() was never called cleanly.
        // We check if isAttacking is STILL true.
        if (isAttacking)
        {
            Debug.LogWarning($"<color=orange>COMBO TIMEOUT! Forcibly resetting state.</color>");
            // Force the combo to end.
            FinishCombo();
        }
    }
    public void SetImpactType(ImpactData impactData)
    {
        currentImpactData = impactData;
        Debug.Log($"<color=orange>Knight primed impact: {impactData.name}</color>");
    }
    // --- ANIMATION EVENT METHODS (These remain the same) ---
    public void TriggerAttack2()
    {
        if (!isAttacking) return;
        currentComboStep = 2;
        animator.SetTrigger("attack2");
    }

    public void TriggerAttack3()
    {
        if (!isAttacking) return;
        currentComboStep = 3;
        animator.SetTrigger("attack3");
    }

    public void FinishCombo()
    {
        if (comboWatchdogCoroutine != null)
        {
            StopCoroutine(comboWatchdogCoroutine);
            comboWatchdogCoroutine = null;
        }
        isAttacking = false;
        currentComboStep = 0;
        lastComboTime = Time.time; // **NEW:** Record the time this combo finished.
    }
    public void FinishCounterAttack()
    {
        // This method ONLY sets the isAttacking flag to false.
        // It does NOT touch the combo timer or any other combo logic.
        isAttacking = false;
        Debug.Log("<color=cyan>KnightAttack: Counter-Attack Animation Finished. Resetting isAttacking flag.</color>");
    }
    public bool IsFinalComboAttack()
    {
        return currentComboStep == 3;
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
    public void Lunge()
    {
        // --- THIS IS THE FIX ---
        // If a lunge is already happening, stop it first.
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(LungeCoroutine());
    }
    // --- ADD THIS NEW COROUTINE ---
    private IEnumerator LungeCoroutine()
    {

        if (followAI == null) yield break;
        float direction = followAI.IsFacingRight() ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * lungeForce, 0f); // Use velocity for smooth movement

        // Wait for the duration of the lunge.
        yield return new WaitForSeconds(lungeDuration);

        // Stop the lunge.
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    public void GetParried(Transform playerTransform)
    {
        Debug.Log("<color=orange>KNIGHT HAS BEEN PARRIED!</color>");

        // Play a stunned/parried animation.
        animator.SetTrigger("getParried"); // Make sure you have this trigger in your Knight's Animator.

        // --- THIS IS THE MUTUAL KNOCKBACK FIX ---
        // Apply a small knockback to the KNIGHT.
        // We use hardcoded values here for simplicity, as this is a reaction, not an attack.
        float parryKnockbackDistance = 2f;
        float parryKnockbackDuration = 0.2f;

        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(playerTransform, parryKnockbackDistance, parryKnockbackDuration));
        // --- END OF FIX ---
    }
    private IEnumerator KnockbackRoutine(Transform attacker, float distance, float duration)
    {
        // Tell the follow script to stop moving.
        KnightFollow followScript = GetComponent<KnightFollow>();
        if (followScript != null) followScript.StopMovement();

        Vector2 knockbackDirection = (transform.position - attacker.position).normalized;
        Vector2 knockbackVelocity = new Vector2(knockbackDirection.x, 0) * (distance / duration); // Horizontal only

        float timer = 0f;
        while (timer < duration)
        {
            if (rb != null) rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        knockbackCoroutine = null;
    }

    public void StartCounterAttack()
    {
        // --- THIS IS THE FIX ---
        // 1. COMMAND the health script to become unbreakable.
        if (health != null) health.isUnbreakable = true;
        // --- END OF FIX ---

        isAttacking = true;
        animator.SetTrigger(counterAttackTriggerHash);
    }

    public void StartSpecialAttack()
    {
        // We don't need many checks here because the AI brain has already decided.
        // We can cancel a normal combo if needed.
        if (isAttacking)
        {
            FinishCombo(); // Cleanly end the normal combo state.
        }

        isAttacking = true; // The knight is now busy with the special attack.
        animator.SetTrigger(specialAttackTriggerHash);
    }
    public void CancelAllAttacks()
    {
        CancelLunge();
        // Stop the DOT coroutine if it's running.
        if (specialDamageCoroutine != null)
        {
            StopCoroutine(specialDamageCoroutine);
            specialDamageCoroutine = null;
        }
        if (health != null)
        {
            health.BecomeVulnerable(); // This directly sets isUnbreakable = false.
        }
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        animator.ResetTrigger(specialAttackTriggerHash);
        animator.ResetTrigger(counterAttackTriggerHash);
        animator.ResetTrigger("attack1"); // Assuming you have these
        animator.ResetTrigger("attack2");
        animator.ResetTrigger("attack3");
        animator.ResetTrigger("getParried");
        // Stop the normal combo if it's running.
        if (isAttacking)
        {
            FinishCombo();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    public void CancelLunge()
    {
        if (lungeCoroutine != null)
        {
            // If it is, STOP IT. This immediately halts the lunge movement.
            StopCoroutine(lungeCoroutine);
            lungeCoroutine = null; // Set it to null so we know it's dead.
            Debug.LogWarning("--- LUNGE COROUTINE KILLED BY CancelAllAttacks() ---");
        }

    }

    public float GetComboDuration() { return comboDuration; }
    public float GetCounterAttackDuration() { return counterAttackDuration; }

    public void PerformBackstep()
    {
        if (health != null && !health.IsGrounded()) return;
        // Play the backstep animation.
        animator.SetTrigger(backstepTriggerHash);

        // Start the physical movement coroutine.
        StartCoroutine(BackstepMovementRoutine());
    }

    // This coroutine handles the actual physics of the backstep.
    private IEnumerator BackstepMovementRoutine()
    {
        // 1. Determine the direction. The backstep is ALWAYS away from the player.
        //    We ask the followAI which way it's facing. The backstep is the opposite direction.
        if (followAI == null) yield break;
        float direction = followAI.IsFacingRight() ? -1f : 1f;

        // 2. Calculate the velocity.
        Vector2 backstepVelocity = new Vector2(direction * backstepForce, 0f);

        // 3. Apply the velocity for the specified duration.
        float timer = 0f;
        while (timer < backstepDuration)
        {
            if (rb != null)
            {
                rb.linearVelocity = backstepVelocity;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. Stop all movement after the backstep is finished.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
