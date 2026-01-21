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
   
    private string hitReactionType = "back";
    private ImpactData currentImpactData;

    void Awake() 
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
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
        isDamageWindowOpen = true;
    }

    /// <summary>
    /// Called by an Animation Event to CLOSE the damage window.
    /// </summary>
    public void StopDamage()
    {
        Debug.Log("<color=grey>Knight Damage Window CLOSED</color>");
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
        animator.SetTrigger("attack1");
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

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackPoint.position, attackAreaSize);
    }
}
