using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(KnightAttack))]
[RequireComponent(typeof(KnightHealth))]
public class KnightAI : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator animator; 
    [SerializeField] private KnightFollow follow; 
    [SerializeField] private Transform playerTarget; 
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

    [Header("Special Attack Counter")]
    [Tooltip("An empty GameObject marking the center of the counter-check area.")]
    [SerializeField] private Transform counterCheckPoint; 
    [Tooltip("The size of the counter-check area (Width, Height).")]
    [SerializeField] private Vector2 counterCheckAreaSize = new Vector2(3f, 2f); 
    [Tooltip("The layer the player is on.")]
    [SerializeField] private LayerMask playerLayer; 
    [Tooltip("The horizontal distance to offset the knight when a counter is successful.")]
    [SerializeField] private float counterSuccessOffsetX = 1.5f; 
 
    // --- ADD THESE NEW PRIVATE STATE VARIABLES --
    private bool isCounterWindowOpen = false;
    private ZreyMovements playerMovements; // A reference to the player's movement script.

    // --- ADD THESE NEW ANIMATION HASHES ---
    private readonly int getCounteredTriggerHash = Animator.StringToHash("getCountered");
    private bool isPlayerInCounterBox = false;
    private ZreyAttacks playerAttacks;
    [Header("Counter Effects")]
    [Tooltip("The blood particle effect prefab to spawn when countered.")]
    [SerializeField] private GameObject counterBloodPrefab; 
    [Tooltip("The child GameObject marking where the blood should spawn.")]
    [SerializeField] private Transform counterBloodPoint;

    [SerializeField] public GameObject counterPromptUI;
    private void OnEnable()
    {
        // When this script is enabled, start listening for the counter press event.
        InputManager.OnCounterPressed += HandleCounterInput;
    }

    private void OnDisable()
    {
        // When this script is disabled, stop listening to prevent memory leaks.
        InputManager.OnCounterPressed -= HandleCounterInput;
    }
    private void HandleCounterInput()
    {
        // --- THIS IS THE FIX ---
        // This method is ONLY called when the counter button is pressed.

        // 1. BRUTAL DEBUG: Announce that we heard the event.
        Debug.LogWarning("!!! KnightAI HEARD OnCounterPressed EVENT !!!");

        // 2. Check the two conditions for a successful counter.
        //    A. Is the counter window open?
        //    B. Is the player inside the box?
        if (isCounterWindowOpen && isPlayerInCounterBox)
        {
            // 3. SUCCESS! The conditions are met. Execute the counter.
            Debug.LogError("--- SUCCESS! Conditions met. Calling ExecuteCounterSequence() NOW. ---");
            StartCoroutine(ExecuteCounterSequence());
        }
        else
        {
            Debug.LogWarning("KnightAI heard counter press, but conditions were not met." +
                             $" isCounterWindowOpen: {isCounterWindowOpen}, isPlayerInCounterBox: {isPlayerInCounterBox}");
        }
        // --- END OF FIX ---
    }
    void Awake()
    {
        animator = GetComponent<Animator>();
        follow = GetComponent<KnightFollow>();

        // 2. Add this block to find the player automatically.
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }
        if (playerTarget != null)
        {
            // --- THIS IS THE FIX ---
            playerAttacks = playerTarget.GetComponent<ZreyAttacks>();
            // --- END OF FIX ---
            playerMovements = playerTarget.GetComponent<ZreyMovements>();
        }
        attack = GetComponent<KnightAttack>();
        health = GetComponent<KnightHealth>();

        // This line will now work because playerTarget exists.
        if (playerTarget != null)
        {
            playerMovements = playerTarget.GetComponent<ZreyMovements>();
        }
    }

    void Update()
    {
        if (health != null && health.IsStunned())
        {
            // 2. If we ARE stunned, do NOTHING.
            //    Exit the Update loop immediately. No decisions will be made.
            //    The AI brain is effectively "paused".
            return;
        }
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
                isActionLocked = true;
                Debug.LogWarning("--- AI DECISION: Backstep into Special Attack ---");

                // 2. Command the KnightAttack script to perform the backstep.
                if (attack != null)
                {
                    attack.PerformBackstep();
                }

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
       
       
        // 2. COMMAND the attack script to play the animation.
        if (attack != null)
        {
            attack.StartSpecialAttack(); // We will create this new method.
        }
        if (counterPromptUI != null)
        {
            counterPromptUI.SetActive(false);
        }
        float specialAttackDuration = 2.0f; // The total duration of your special attack.
        float timer = 0f;
        while (timer < specialAttackDuration)
        {
            // Check if the player is inside the counter box.
            Collider2D playerCollider = Physics2D.OverlapBox(counterCheckPoint.position, counterCheckAreaSize, 0f, playerLayer);
            isPlayerInCounterBox = (playerCollider != null);
            if (counterPromptUI != null)
            {
                // ...set its active state to be the SAME as isPlayerInCounterBox.
                // If isPlayerInCounterBox is true, the UI is turned on.
                // If isPlayerInCounterBox is false, the UI is turned off.
                counterPromptUI.SetActive(isPlayerInCounterBox);
            }
            timer += Time.deltaTime;
            yield return null; // Changed back from WaitForEndOfFrame
        }
        if (counterPromptUI != null)
        {
            counterPromptUI.SetActive(false);
        }

        // 3. Wait for the attack to finish.
        // You can get this duration from the attack script or hardcode it.
        yield return new WaitForSeconds(2.0f); // Adjust to your special attack animation length.

        // 4. UNLOCK THE BRAIN & BECOME VULNERABLE AGAIN.
      
        isPerformingSpecialAttack = false;
        isActionLocked = false;
      
        isPlayerInCounterBox = false;
        if (attack != null) attack.StopSpecialDamage(); // Ensure DOT is off.
        if (health != null) health.BecomeVulnerable();
        // 5. RESET THE COOLDOWN for the next special attack.
        ResetSpecialAttackCooldown();

        Debug.Log("<color=green>Special Attack Sequence Finished.</color>");
    }
    private IEnumerator ExecuteCounterSequence()
    {
        // 1. CANCEL EVERYTHING.
        if (attack != null) attack.CancelAllAttacks();
        // The brain is already locked, but we need to stop the attack animations.
        isCounterWindowOpen = false;
       

        float directionToPlayer = Mathf.Sign(playerTarget.position.x - transform.position.x);
        // A. Get the player's X position and our OWN Y and Z positions.
        Vector3 newPosition = new Vector3(
            playerTarget.position.x + (counterSuccessOffsetX * -directionToPlayer), // New X behind the player
            transform.position.y,                                                  // Keep our current Y
            transform.position.z                                                   // Keep our current Z
        );
        transform.position = newPosition;

        // Make sure the knight is facing the player after the teleport.
        if (follow != null) follow.FacePlayer();

        animator.ResetTrigger(specialAttackTriggerHash);
        animator.SetTrigger(getCounteredTriggerHash);
        if (health != null)
        {
            // We need to get the duration from the health script itself.
            // This is a bit tricky, so we'll add a public getter for it.
            health.TriggerStun(health.GetCounterStunDuration()); // We will create this getter.
        }
        // 4. COMMAND THE PLAYER TO START THEIR COUNTER-ATTACK.
        if (playerAttacks != null)
        {
            // We call the method with NO parameters.
            playerAttacks.StartKnightCounter();
        }
        // 5. Wait for the sequence to end before unlocking the brain.
        yield return new WaitForSeconds(3.0f); // Adjust to your counter sequence length.

        // 6. RECOVER.
       
        isActionLocked = false;
        ResetSpecialAttackCooldown();
        Debug.Log("<color=green>Knight Counter Sequence Finished. AI Unlocked.</color>");
    }
    private void OnDrawGizmosSelected()
    {
        if (counterCheckPoint == null) return;

        // --- THIS IS THE FIX ---
        // 1. Check if the counter window is open AND if the player is inside.
        if (isCounterWindowOpen && isPlayerInCounterBox)
        {
            // If both are true, draw a GREEN box.
            Gizmos.color = Color.green;
        }
        else if (isCounterWindowOpen)
        {
            // If the window is open but the player is NOT inside, draw a CYAN box.
            Gizmos.color = Color.cyan;
        }
        else
        {
            // If the window is closed, don't draw the box at all.
            return;
        }

        // 2. Draw the box with the chosen color.
        Gizmos.DrawWireCube(counterCheckPoint.position, counterCheckAreaSize);
        // --- END OF FIX ---
    }
    public void SpawnCounterBloodEffect()
    {
        // --- THIS IS THE FIX ---
        // 1. BRUTAL DEBUG: Announce that the animation event worked.
        Debug.LogWarning("!!! ANIMATION EVENT: SpawnCounterBloodEffect() CALLED !!!");

        // 2. Check if the prefab and spawn point exist.
        if (counterBloodPrefab != null && counterBloodPoint != null)
        {
            // 3. Spawn the blood effect. We don't need to hold a reference to it.
            Instantiate(counterBloodPrefab, counterBloodPoint.position, counterBloodPoint.rotation);
        }
        else
        {
            Debug.LogError("Cannot spawn counter blood effect! Prefab or Spawn Point is not assigned in the Inspector!", this);
        }
        // --- END OF FIX ---
    }
    public void OpenCounterWindow()
    {
        isCounterWindowOpen = true;
        Debug.LogWarning("--- COUNTER WINDOW: OPEN ---");
    }

    /// <summary>
    /// Called by KnightAttack to close the counter window.
    /// </summary>
    public void CloseCounterWindow()
    {
        isCounterWindowOpen = false;
        Debug.Log("<color=grey>--- COUNTER WINDOW: CLOSED ---</color>");
    }
    private void ResetSpecialAttackCooldown()
    {
        specialAttackCooldownTimer = Random.Range(minSpecialAttackCooldown, maxSpecialAttackCooldown);
        Debug.Log($"Special Attack cooldown reset. Next attempt in {specialAttackCooldownTimer} seconds.");
    }
    public void TriggerSpecialAttackFromEvent()
    {
        // Failsafe: if we are not in the middle of a special attack action, do nothing.
        if (!isActionLocked) return;

        // Start the special attack sequence we already have.
        StartCoroutine(SpecialAttackSequence());
    }
}