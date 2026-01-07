using System.Collections;
using UnityEngine;

// Ensure the enemy has an Animator and Rigidbody2D component.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]

public class KnightAttack : MonoBehaviour
{
    [Header("References")]
    private Animator animator;
    private Rigidbody2D rb;

    [Header("Attack Settings")]
    [Tooltip("The time between the end of one combo and the start of the next.")]
    public float timeBetweenCombos = 3f;
    [Tooltip("The force of the lunge during an attack animation.")]
    public float lungeForce = 5f;

    // --- State Control ---
    private bool isAttacking = false;
    private Coroutine attackCoroutine;

    // This is called when the script first loads.
    void Start()
    {
        // Get the components we need from this GameObject.
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Start the main attack loop.
        attackCoroutine = StartCoroutine(AttackLoop());
    }

    /// <summary>
    /// The main loop that waits and then initiates an attack combo.
    /// </summary>
    private IEnumerator AttackLoop()
    {
        // This loop will run for the entire life of the enemy.
        while (true)
        {
            // Wait for the specified time before starting a new combo.
            yield return new WaitForSeconds(timeBetweenCombos);

            // Start the attack combo.
            StartCombo();
        }
    }

    /// <summary>
    /// Kicks off the 3-hit attack combo.
    /// </summary>
    private void StartCombo()
    {
        // If we are already in an attack combo, don't start a new one.
        if (isAttacking)
        {
            return;
        }

        isAttacking = true;
        // Trigger the first attack animation. The rest will be handled by animation events.
        animator.SetTrigger("attack1");
    }

    // --- ANIMATION EVENT METHODS ---
    // These methods are designed to be called directly from the animation timeline.

    /// <summary>
    /// Called by an animation event at the end of the first attack animation.
    /// </summary>
    public void TriggerAttack2()
    {
        if (!isAttacking) return;
        animator.SetTrigger("attack2");
    }

    /// <summary>
    /// Called by an animation event at the end of the second attack animation.
    /// </summary>
    public void TriggerAttack3()
    {
        if (!isAttacking) return;
        animator.SetTrigger("attack3");
    }

    /// <summary>
    /// Called by an animation event at the end of the final attack animation to reset the state.
    /// </summary>
    public void FinishCombo()
    {
        isAttacking = false;
        // The AttackLoop coroutine will now wait for 'timeBetweenCombos' and start again.
    }

    /// <summary>
    /// PUBLIC LUNGE METHOD: This is called by an animation event during an attack
    /// to move the enemy forward.
    /// </summary>
    public void Lunge()
    {
        // Determine the direction the enemy is facing.
        // transform.localScale.x > 0 means facing right.
        // transform.localScale.x < 0 means facing left.
        float direction = Mathf.Sign(transform.localScale.x);

        // Apply an instant force to the Rigidbody2D.
        rb.AddForce(new Vector2(direction * lungeForce, 0f), ForceMode2D.Impulse);
    }
}
