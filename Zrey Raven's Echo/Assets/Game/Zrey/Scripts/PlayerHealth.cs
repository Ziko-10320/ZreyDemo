// PASTE THIS ENTIRE SCRIPT INTO PlayerHealth.cs

using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for Slider

public class PlayerHealth : MonoBehaviour
{
    [Header("Health & UI")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject deathPanel;
    private int currentHealth;

    [Header("Impact & VFX")]
    [SerializeField] private GameObject bloodVFX;
    [SerializeField] private Transform bloodSpawnPoint;

    // --- Private Components & State ---
    private Rigidbody2D rb;
    private Animator animator;
    private ZreyAttacks playerAttacks;
    private Coroutine knockbackCoroutine;

    // --- Animation Hashes (for performance) ---
    private readonly int getHitBackTriggerHash = Animator.StringToHash("getHitBack");
    private readonly int getHitDownTriggerHash = Animator.StringToHash("getHitDown");
    private readonly int getHitFinalBackTriggerHash = Animator.StringToHash("finalBack");
    private readonly int deathTriggerHash = Animator.StringToHash("death"); // For death animation
    private ZreyMovements playerMovements;
    void Awake()
    {
        // --- THIS IS THE GUARANTEE ---
        // We get the components automatically from this GameObject.
        // This is not optional. It is required.
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAttacks = GetComponent<ZreyAttacks>();
        playerMovements = GetComponent<ZreyMovements>();
        // Check if anything is missing and scream if it is.
        if (rb == null) Debug.LogError("FATAL ERROR: Rigidbody2D is missing on Player!", this);
        if (animator == null) Debug.LogError("FATAL ERROR: Animator is missing on Player!", this);
        if (playerAttacks == null) Debug.LogError("FATAL ERROR: ZreyAttacks script is missing on Player!", this);
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    // --- THE ONLY WAY TO TAKE DAMAGE ---
    public void TakeDamage(int damageAmount, Transform attacker, ImpactData impact)
    {
        // If already dead, do nothing.
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (impact == null)
        {
            Debug.LogWarning("TakeDamage was called with null ImpactData!");
            return;
        }
        Debug.Log($"<color=red>PLAYER TOOK DAMAGE. Health: {currentHealth}/{maxHealth}</color>");

        // --- CHECK FOR DEATH FIRST ---
        if (currentHealth <= 0)
        {
            Die(attacker); // Pass the attacker for a final knockback
            return; // Stop everything else.
        }

        // --- IF NOT DEAD, DO ALL THE REACTIONS ---

        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(attacker, impact.knockbackDistance, impact.knockbackDuration));

        PlayHitReaction(impact.hitReactionType);
        // 3. BLOOD VFX
        SpawnBlood();
    }

    private void PlayHitReaction(string hitType)
    {
        animator.ResetTrigger(getHitBackTriggerHash);
        animator.ResetTrigger(getHitDownTriggerHash);
        animator.ResetTrigger(getHitFinalBackTriggerHash);

        switch (hitType.ToLower())
        {
            case "down": animator.SetTrigger(getHitDownTriggerHash); break;
            case "finalback": animator.SetTrigger(getHitFinalBackTriggerHash); break;
            case "back": animator.SetTrigger(getHitBackTriggerHash); break;
        }
    }

    private void SpawnBlood()
    {
        if (bloodVFX != null && bloodSpawnPoint != null)
        {
            Instantiate(bloodVFX, bloodSpawnPoint.position, bloodVFX.transform.rotation);
        }
    }

    private IEnumerator KnockbackRoutine(Transform attacker, float distance, float duration)
    {
        if (playerMovements != null) playerMovements.CanMove = false;

        // --- THIS IS THE X-AXIS FIX ---
        // 1. Calculate the full direction first.
        Vector2 knockbackDirection = (transform.position - attacker.position).normalized;

        // 2. Create a new direction vector that ONLY has the X component.
        Vector2 horizontalDirection = new Vector2(knockbackDirection.x, 0).normalized;

        // 3. Calculate the velocity using the purely horizontal direction.
        Vector2 knockbackVelocity = horizontalDirection * (distance / duration);
        // --- END OF X-AXIS FIX ---

        Debug.Log($"<color=yellow>APPLYING KNOCKBACK! Velocity: {knockbackVelocity}</color>");

        float timer = 0f;
        while (timer < duration)
        {
            // We forcefully set the velocity every frame to override other scripts.
            // We preserve the current vertical velocity to allow for normal gravity.
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
            }

            timer += Time.deltaTime;
            yield return null;
        }
        if (playerMovements != null) playerMovements.CanMove = true;
        // After the knockback, set the horizontal velocity to zero, but again, leave the Y velocity alone.
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        knockbackCoroutine = null;
    }
    // --- THE NEW, BULLETPROOF DIE METHOD ---
    private void Die(Transform killer)
    {
        Debug.Log("<color=black>PLAYER IS DEAD.</color>");

        // Play a death animation.
        animator.SetTrigger(deathTriggerHash);

       

        // Disable all player control scripts immediately.
        playerAttacks.enabled = false;
        GetComponent<ZreyMovements>().enabled = false;
        this.enabled = false; // Disable this script.

        // Use a coroutine to show the death panel and freeze time AFTER a delay.
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Wait for the death animation/knockback to have some impact.
        yield return new WaitForSeconds(1.5f);

        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze the game.
    }
}
