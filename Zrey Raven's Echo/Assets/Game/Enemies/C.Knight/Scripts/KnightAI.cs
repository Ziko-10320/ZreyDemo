using UnityEngine;
using System.Collections;

// Ensure the enemy has the components we need to function.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(KnightAttack))] // Make sure the attack script is also on this enemy

public class KnightAI : MonoBehaviour
{
    [Header("AI References")]
    [Tooltip("The target the knight will follow (the player).")]
    public Transform playerTarget;
    private Animator animator;
    private Rigidbody2D rb;
    private KnightAttack knightAttack; // A reference to the attack script

    [Header("AI Behavior")]
    [Tooltip("The distance at which the knight will start attacking the player.")]
    public float attackRange = 2f;
    [Tooltip("The distance at which the knight will stop following the player.")]
    public float chaseRange = 10f;
    [Tooltip("How much force to apply with each 'step' during the walk animation.")]
    public float moveForce = 4f;

    // --- State Control ---
    private bool isWalking = false;

    void Awake()
    {
        // Get all the components we need.
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        knightAttack = GetComponent<KnightAttack>();

        // Try to find the player automatically if no target is set.
        // This looks for a GameObject with the "Player" tag.
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }
    }

    void Update()
    {
        // If we don't have a target, do nothing.
        if (playerTarget == null) return;

        // Calculate the distance to the player.
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // --- DECISION MAKING ---

        // If the player is within attack range AND we are not already attacking...
        if (distanceToPlayer <= attackRange && !knightAttack.IsAttacking())
        {
            // Stop walking and tell the KnightAttack script to start its combo.
            StopWalking();
            knightAttack.StartCombo();
        }
        // Else, if the player is within chase range AND we are not attacking...
        else if (distanceToPlayer <= chaseRange && !knightAttack.IsAttacking())
        {
            // Start walking towards the player.
            StartWalking();
        }
        // Otherwise (player is too far away or we are busy attacking)...
        else
        {
            // Stop walking.
            StopWalking();
        }

        // Always make the knight face the player.
        FacePlayer();
    }

    private void StartWalking()
    {
        // If we are already walking, do nothing.
        if (isWalking) return;

        isWalking = true;
        // Tell the Animator to start the walking animation loop.
        animator.SetBool("isWalking", true);
    }

    private void StopWalking()
    {
        // If we are already stopped, do nothing.
        if (!isWalking) return;

        isWalking = false;
        // Tell the Animator to stop the walking animation loop.
        animator.SetBool("isWalking", false);
    }

    private void FacePlayer()
    {
        // If the player is to the right of the knight and the knight is facing left...
        if (playerTarget.position.x > transform.position.x && transform.localScale.x < 0)
        {
            // Flip the knight to face right.
            transform.localScale = new Vector3(1, 1, 1);
        }
        // If the player is to the left of the knight and the knight is facing right...
        else if (playerTarget.position.x < transform.position.x && transform.localScale.x > 0)
        {
            // Flip the knight to face left.
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    // --- ANIMATION EVENT METHOD ---
    // This public method will be called from the walk animation itself.
    public void TakeStep()
    {
        // If we are not supposed to be walking, don't take a step.
        if (!isWalking) return;

        // Determine direction and apply a force pulse to move the knight forward.
        float direction = Mathf.Sign(transform.localScale.x);
        rb.AddForce(new Vector2(direction * moveForce, 0f), ForceMode2D.Impulse);
    }

    // This is called by Unity to draw Gizmos in the Scene view for easy debugging.
    void OnDrawGizmosSelected()
    {
        // Set the color for the Gizmos.
        Gizmos.color = Color.red;
        // Draw a wire sphere representing the attack range.
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        // Draw a wire sphere representing the chase range.
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
