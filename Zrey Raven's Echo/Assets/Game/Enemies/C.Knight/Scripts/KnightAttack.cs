using System.Collections;
using UnityEngine;

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
    private float lastComboTime = -10f; // **NEW:** Track when the last combo finished.

    void Awake() // **MODIFIED:** Changed from Start() to Awake() to ensure references are set early.
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // **REMOVED:** We no longer start the AttackLoop automatically.
    // The KnightAI script will now control when attacks happen.

    // **NEW:** A public property to let other scripts know if the knight is attacking.
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
        animator.SetTrigger("attack1");
    }

    // --- ANIMATION EVENT METHODS (These remain the same) ---
    public void TriggerAttack2()
    {
        if (!isAttacking) return;
        animator.SetTrigger("attack2");
    }

    public void TriggerAttack3()
    {
        if (!isAttacking) return;
        animator.SetTrigger("attack3");
    }

    public void FinishCombo()
    {
        isAttacking = false;
        lastComboTime = Time.time; // **NEW:** Record the time this combo finished.
    }

    public void Lunge()
    {
        float direction = Mathf.Sign(transform.localScale.x);
        rb.AddForce(new Vector2(direction * lungeForce, 0f), ForceMode2D.Impulse);
    }
}
