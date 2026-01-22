using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(KnightAttack))]
public class KnightFollow : MonoBehaviour
{
    [Header("AI References")]
    public Transform playerTarget;
    private Animator animator;
    private Rigidbody2D rb;
    private KnightAttack knightAttack;
    private KnightHealth knightHealth;

    [Header("AI Behavior")]
    public float attackRange = 2f;
    public float chaseRange = 10f;
    public float moveSpeed = 3f;

    // --- State Control ---
    private bool shouldBeWalking = false;
    private bool isBlocking = false;
    private bool isPreparingCounter = false; // <<< --- ADD THIS NEW LINE
    private float timeSinceLastBlock = 0f;
    private readonly int counterAttackTriggerHash = Animator.StringToHash("counterAttack");
    private bool isActionLocked = false;
    [Header("Counter Attack Logic")]
    [Tooltip("How long to wait after the warning before lunging.")]
    [SerializeField] private float counterWarningDelay = 0.6f; 
    private bool isLocked = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        knightAttack = GetComponent<KnightAttack>();
        knightHealth = GetComponent<KnightHealth>();

        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) playerTarget = playerObject.transform;
        }
    }

    void Update()
    {
        if (isLocked)
        {
            return; // STOP EVERYTHING.
        }
        if (playerTarget == null || (knightHealth != null && knightHealth.IsStunned()))
        {
            StopWalking(); // Tell the animator to stop.
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= attackRange && !knightAttack.IsAttacking())
        {
            StopWalking();
            knightAttack.StartCombo();
        }
        else if (distanceToPlayer > attackRange && distanceToPlayer <= chaseRange && !knightAttack.IsAttacking())
        {
            StartWalking();
        }
        else
        {
            StopWalking();
        }

        FacePlayer();
    }

    // The FixedUpdate movement logic has been DELETED.

    private void StartWalking()
    {
        if (shouldBeWalking) return;
        shouldBeWalking = true;
        animator.SetBool("isWalking", true);
    }

    private void StopWalking()
    {
        if (!shouldBeWalking) return;
        shouldBeWalking = false;
        animator.SetBool("isWalking", false);
        // We also call StopMovement here as a failsafe to prevent sliding if the animation is interrupted.
        StopMovement();
    }

    // --- NEW PUBLIC METHODS FOR ANIMATION EVENTS ---

    /// <summary>
    /// Called by an Animation Event at the START of the walk cycle.
    /// Applies a continuous velocity.
    /// </summary>
    public void StartMovement()
    {
        // Determine direction towards the player.
        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
        // Apply a consistent velocity.
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        Debug.Log("<color=green>StartMovement Event Called</color>");
    }

    /// <summary>
    /// Called by an Animation Event at the END of the walk cycle.
    /// Stops all horizontal movement.
    /// </summary>
    public void StopMovement()
    {
        // CRITICAL: Set horizontal velocity to zero to prevent sliding.
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        Debug.Log("<color=red>StopMovement Event Called</color>");
    }

    private void FacePlayer()
    {
        if (playerTarget == null) return;
        if (playerTarget.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
    public void ForceCounterAttack()
    {
        // We don't check anything. We just start the sequence.
        StartCoroutine(CounterAttackSequence());
    }

    /// <summary>
    /// The sequence for the counter-attack, taken from your proven logic.
    /// </summary>
    private IEnumerator CounterAttackSequence()
    {
        // 1. LOCK THE BRAIN. The Update() loop is now disabled.
        isLocked = true;
        Debug.Log("<color=red>AI BRAIN LOCKED. Starting Counter Sequence.</color>");

        // Optional: Play a warning glint/sound here.

        // 2. Wait for the warning delay.
        yield return new WaitForSeconds(counterWarningDelay);

        // 3. Now, command the attack script to start the combo.
        // Because the brain is locked, nothing can interrupt this.
        if (knightAttack != null)
        {
            Debug.Log("Warning finished. Firing counter combo!");
            knightAttack.StartCounterCombo();
        }

        // 4. Wait for the attack to finish (adjust this time to your combo length).
        yield return new WaitForSeconds(1.5f);


        if (knightHealth != null)
        {
            knightHealth.ResetBlockCounter();
        }
        // 5. UNLOCK THE BRAIN. The Update() loop can now run again.
        isLocked = false;
        Debug.Log("<color=green>Counter sequence complete. AI BRAIN UNLOCKED.</color>");
    }
    public void OnPlayerAttackTelegraphed(Transform player)
    {
        // --- THIS IS THE FIX ---
        // If the knight is preparing a counter OR is already mid-combo,
        // he is UNBREAKABLE. Ignore everything.
        if (isPreparingCounter || knightAttack.IsAttacking())
        {
            Debug.Log("<color=red>KNIGHT IS UNBREAKABLE! Ignoring telegraph.</color>");
            return;
        }
        // --- END OF FIX ---

        // If not unbreakable, proceed with the normal block logic.
        isBlocking = true;
        timeSinceLastBlock = 0f;
        FacePlayer();
    }
 
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
