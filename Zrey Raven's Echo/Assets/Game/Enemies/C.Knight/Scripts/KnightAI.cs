using UnityEngine;
using System.Collections;

[RequireComponent(typeof(KnightAttack))]
[RequireComponent(typeof(KnightHealth))]
public class KnightAI : MonoBehaviour
{
    // --- COMPONENT REFERENCES ---
    private KnightAttack attack;
    private KnightHealth health;
    [Header("Special Attack Settings")]
    [Tooltip("The chance (0 to 1) that the knight will perform a special attack when the cooldown is ready.")]
    [Range(0f, 1f)]
    [SerializeField] private float specialAttackChance = 0.5f; // 50% chance"

    [Tooltip("The minimum time to wait before another special attack can be attempted.")]
    [SerializeField] private float minSpecialAttackCooldown = 8f; 

    [Tooltip("The maximum time to wait before another special attack can be attempted.")]
    [SerializeField] private float maxSpecialAttackCooldown = 15f; 

    private float specialAttackCooldownTimer = 0f;
    private bool isPerformingSpecialAttack = false;

    // --- ADD THIS NEW ANIMATION HASH ---
    private readonly int specialAttackTriggerHash = Animator.StringToHash("specialAttack");
    private bool isActionLocked = false;
    // --- STATE LOCK ---
    private bool isCounterSequenceRunning = false;

    void Awake()
    {
        attack = GetComponent<KnightAttack>();
        health = GetComponent<KnightHealth>();
    }

    void Update()
    {
        if (specialAttackCooldownTimer > 0)
        {
            specialAttackCooldownTimer -= Time.deltaTime;
        }

        // --- THIS IS THE SPECIAL ATTACK DECISION LOGIC ---
        // HIGHEST PRIORITY CHECK: Should we do the special attack?
        // 1. Is the cooldown ready?
        // 2. Are we NOT already doing something important (like a combo or counter)?
        if (specialAttackCooldownTimer <= 0 && !isActionLocked && !attack.IsAttacking())
        {
            // 3. Roll the dice to see if we should perform the attack.
            float roll = Random.Range(0f, 1f);
            if (roll <= specialAttackChance)
            {
                // 4. SUCCESS! Start the special attack sequence and STOP all other logic.
                StartCoroutine(SpecialAttackSequence());
                return; // CRITICAL: Exit the Update loop immediately.
            }
            else
            {
                // 5. FAILED THE ROLL. Reset the cooldown for another attempt later.
                // This prevents the AI from spamming the check every frame.
                ResetSpecialAttackCooldown();
            }
        }
        // --- END OF FIX ---

       
    }
    public void TriggerCounterAttack()
    {
        // --- CRUCIAL DEBUG #1: Has the command been received? ---
        Debug.Log("<color=magenta>--- KnightAI: TriggerCounterAttack() RECEIVED! ---</color>");

        // Failsafe: If a counter is already running, we can't start another one.
        if (isCounterSequenceRunning)
        {
            Debug.LogWarning("<color=orange>KnightAI: Received counter command, but a counter is already in progress. Ignoring.</color>");
            return;
        }

        // Start the sequence.
        StartCoroutine(CounterAttackSequence());
    }

    private IEnumerator CounterAttackSequence()
    {
        // 1. LOCK THE STATE. The counter is now the absolute priority.
        isCounterSequenceRunning = true;
        Debug.Log("<color=red>KnightAI: LOCKING STATE. Starting Counter-Attack Sequence.</color>");

        // 2. COMMAND the attack script to become unbreakable and start the counter.
        if (attack != null)
        {
            attack.StartCounterAttack();
        }
        else
        {
            Debug.LogError("KnightAI: Cannot start counter, KnightAttack script is missing!", this);
            isCounterSequenceRunning = false;
            yield break;
        }

        // 3. Wait for the counter-attack to finish.
        // --- CRUCIAL DEBUG #2: Are we waiting for the correct duration? ---
        float waitTime = attack.GetCounterAttackDuration() + 0.2f;
        Debug.Log($"<color=cyan>KnightAI: Waiting for {waitTime} seconds for counter to finish...</color>");
        yield return new WaitForSeconds(waitTime);

        // 4. COMMAND the health script to reset its block counter.
        if (health != null)
        {
            health.ResetBlockCounter();
        }

        // 5. UNLOCK THE STATE.
        isCounterSequenceRunning = false;
        Debug.Log("<color=green>KnightAI: UNLOCKING STATE. Counter-Attack Finished.</color>");
    }
    public void OnPlayerAttackTelegraphed(Transform player)
    {
        // --- THE HYPER ARMOR LOGIC ---
        // If the Follow brain is already locked in an attack, DO NOTHING.
        if (attack != null && attack.IsAttacking())
        {
            Debug.Log("<color=red>AI is ATTACKING. Ignoring player attack telegraph.</color>");
            return;
        }

        // --- If we are not busy, COMMAND the health script to block. ---
        if (health != null)
        {
            Debug.Log("<color=green>AI BRAIN: Received telegraph. Commanding a block.</color>");
            health.PerformBlock(player);
        }
    }
    private IEnumerator SpecialAttackSequence()
    {
        Debug.Log("<color=yellow>!!! SPECIAL ATTACK TRIGGERED !!!</color>");

        // 1. LOCK THE BRAIN & BECOME INVINCIBLE.
        isActionLocked = true;
        isPerformingSpecialAttack = true; // A specific flag for this state.
        if (health != null) health.isUnbreakable = true;

        // 2. COMMAND the attack script to play the animation.
        if (attack != null)
        {
            attack.StartSpecialAttack(); // We will create this new method.
        }

        // 3. Wait for the attack to finish.
        // You can get this duration from the attack script or hardcode it.
        yield return new WaitForSeconds(2.0f); // Adjust to your special attack animation length.

        // 4. UNLOCK THE BRAIN & BECOME VULNERABLE AGAIN.
        if (health != null) health.isUnbreakable = false;
        isPerformingSpecialAttack = false;
        isActionLocked = false;

        // 5. RESET THE COOLDOWN for the next special attack.
        ResetSpecialAttackCooldown();

        Debug.Log("<color=green>Special Attack Sequence Finished.</color>");
    }

    /// <summary>
    /// Resets the special attack cooldown to a new random value.
    /// </summary>
    private void ResetSpecialAttackCooldown()
    {
        specialAttackCooldownTimer = Random.Range(minSpecialAttackCooldown, maxSpecialAttackCooldown);
        Debug.Log($"Special Attack cooldown reset. Next attempt in {specialAttackCooldownTimer} seconds.");
    }
}