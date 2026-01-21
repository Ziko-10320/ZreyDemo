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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
