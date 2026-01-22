using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class KnightAttack : MonoBehaviour
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
    public ShakeData CameraShakeParry;
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
    private KnightFollow followAI;
    void Awake() 
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        followAI = GetComponent<KnightFollow>();
    }

    void Update()
    {
        // If the damage window is not open, do nothing.
        if (!isDamageWindowOpen) return;

        // --- If the window IS open, check for a hit ---
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPoint.position, attackAreaSize, 0f, playerLayer);

        foreach (Collider2D player in hitPlayers)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log("<color=red>Knight hit Player!</color>");
                playerHealth.TakeDamage(attackDamage, transform, currentImpactData);

                // CRITICAL: Immediately close the damage window after a successful hit.
                // This prevents a single swing from hitting the player 50 times.
                isDamageWindowOpen = false;
                break; // Exit the loop.
            }
        }
    }
    public void StartDamage()
    {
        Debug.Log("<color=orange>Knight Damage Window OPEN</color>");
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
        isDamageWindowOpen = true;
    }

    /// <summary>
    /// Called by an Animation Event to CLOSE the damage window.
    /// </summary>
    public void StopDamage()
    {
        Debug.Log("<color=grey>Knight Damage Window CLOSED</color>");
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        isDamageWindowOpen = false;
    }
    public bool IsAttacking()
    {
        return isAttacking;
    }

    // **MODIFIED:** This is now a public method that the KnightAI script will call.
    public void StartCombo()
    {
      
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
    public bool IsFinalComboAttack()
    {
        return currentComboStep == 3;
    }
    public void CameraShake()
    {
        CameraShakerHandler.Shake(CameraShakeParry);
    }
    public void Lunge()
    {
        StartCoroutine(LungeCoroutine());
    }

    // --- ADD THIS NEW COROUTINE ---
    private IEnumerator LungeCoroutine()
    {
        float direction = Mathf.Sign(transform.localScale.x);
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
            if (rb != null) rb.velocity = new Vector2(knockbackVelocity.x, rb.velocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
        knockbackCoroutine = null;
    }
    public void StartCounterCombo()
    {
        // We only check if we are already attacking. We IGNORE the cooldown timer.
        if (isAttacking) return;

        // The rest of the logic is the same as StartCombo.
        isAttacking = true;
        currentComboStep = 1;
        animator.SetTrigger("attack1");

        if (comboWatchdogCoroutine != null) StopCoroutine(comboWatchdogCoroutine);
        comboWatchdogCoroutine = StartCoroutine(ComboWatchdogRoutine());

       
    }
   
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
