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

    [Tooltip("The Euler angle rotation for the blood effect when the Knight is facing RIGHT.")]
    [SerializeField] private Vector3 bloodRotationRight = new Vector3(0, 0, 45); 
    [Tooltip("The Euler angle rotation for the blood effect when the Knight is facing LEFT.")]
    [SerializeField] private Vector3 bloodRotationLeft = new Vector3(0, 0, 135);

    [Header("Finisher UI")]
    [Tooltip("The UI prompt to show when the enemy can be finished.")]
    [SerializeField] public GameObject finisherPromptUI; 
    [Tooltip("The range within which the player can perform a finisher.")]
    [SerializeField] private float finisherRange = 2.5f;

    [Header("Dismemberment Settings")]
    [Tooltip("The specific GameObject for the hand that will be detached.")]
    [SerializeField] private GameObject detachableHand; 
    [Tooltip("The horizontal force applied to the detached hand.")]
    [SerializeField]  private float handLaunchForceX = 3f; 
    [Tooltip("The vertical (upward) force applied to the detached hand.")]
    [SerializeField] private float handLaunchForceY = 7f;
    [SerializeField] private float handLaunchTorque = 2f;
    [Header("Special Attack Settings")]
    [Tooltip("The maximum distance from the player at which the knight can decide to use the special grab.")]
    [SerializeField] private float specialAttackRange = 4f; 
    private void OnEnable()
    {
        // --- THIS IS THE FIX ---
        // We now listen to the event from ZreyAttacks.
        ZreyAttacks.OnPlayerCounterAttempt += HandleCounterInput;
        // --- END OF FIX ---
    }

    private void OnDisable()
    {
        // --- THIS IS THE FIX ---
        ZreyAttacks.OnPlayerCounterAttempt -= HandleCounterInput;
        // --- END OF FIX ---
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
        if (health != null && health.isFinishable)
        {
            // Check the distance to the player.
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

            // If the player is in range, show the prompt. Otherwise, hide it.
            if (finisherPromptUI != null)
            {
                finisherPromptUI.SetActive(distanceToPlayer <= finisherRange);
            }
            return; // If we are finishable, the brain does nothing else.
        }
        if (health != null && health.IsStunned())
        {
            return;
        }
        if (specialAttackCooldownTimer > 0)
        {
            specialAttackCooldownTimer -= Time.deltaTime;
        }
        if (isCounterWindowOpen)
        {
            Collider2D playerCollider = Physics2D.OverlapBox(counterCheckPoint.position, counterCheckAreaSize, 0f, playerLayer);
            isPlayerInCounterBox = (playerCollider != null);
        }
        else
        {
            isPlayerInCounterBox = false;
        }
        // --- THIS IS THE NEW, SIMPLIFIED GRAB LOGIC ---
        // 1. Check if we can even attempt a grab.
        //    - Is the cooldown ready?
        //    - Is the knight NOT already doing something (attacking, getting hit)?
        if (specialAttackCooldownTimer <= 0 &&
       !attack.IsAttacking() &&
       Vector2.Distance(transform.position, playerTarget.position) <= specialAttackRange) // <-- THE NEW CONDITION
        {
            // 2. Roll the dice (this is the same as before).
            if (Random.Range(0f, 1f) <= specialAttackChance)
            {
                // 3. SUCCESS! Command the attack script to start the grab.
                Debug.LogWarning($"--- AI DECISION: Player is in range. Attempting GRAB SPECIAL ATTACK ---");
                if (attack != null)
                {
                    attack.StartGrabAttack();
                }

                // 4. Reset the cooldown immediately.
                ResetSpecialAttackCooldown();
            }
            else
            {
                // 5. FAILED THE ROLL. Reset cooldown so we don't check again next frame.
                ResetSpecialAttackCooldown();
            }
        }


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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);
    }
    public void SpawnCounterBloodEffect()
    {
        Debug.LogWarning("!!! ANIMATION EVENT: SpawnCounterBloodEffect() CALLED !!!");

        if (counterBloodPrefab != null && counterBloodPoint != null)
        {
            // --- THIS IS THE NEW DIRECTIONAL LOGIC ---

            // 1. Determine which rotation to use based on the Knight's facing direction.
            Quaternion desiredRotation;
            if (follow != null && follow.IsFacingRight())
            {
                // If facing RIGHT, use the right-facing rotation.
                desiredRotation = Quaternion.Euler(bloodRotationRight);
                Debug.Log("Spawning blood with RIGHT rotation.");
            }
            else
            {
                // If facing LEFT (or if follow script is missing), use the left-facing rotation.
                desiredRotation = Quaternion.Euler(bloodRotationLeft);
                Debug.Log("Spawning blood with LEFT rotation.");
            }

            // 2. Instantiate the prefab at the spawn point with the chosen rotation.
            Instantiate(counterBloodPrefab, counterBloodPoint.position, desiredRotation);

            // --- END OF NEW LOGIC ---
        }
        else
        {
            Debug.LogError("Cannot spawn counter blood effect! Prefab or Spawn Point is not assigned!", this);
        }
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
    public bool OnPlayerCounterAttempt(ZreyAttacks player)
    {
        // 1. The Knight hears the player's broadcast.
        Debug.LogWarning("--- KnightAI heard player's counter broadcast. Checking conditions... ---");

        // 2. The Knight checks its OWN internal state.
        if (isCounterWindowOpen && isPlayerInCounterBox)
        {
            // 3. SUCCESS! The conditions are met. The Knight takes command.
            Debug.LogError("--- GRAB COUNTER SUCCESS! Knight is in command! ---");
            StartCoroutine(ExecuteGrabCounterSequence(player));
            return true;
        }
        return false;
    }


    // --- ADD THIS NEW COROUTINE ---
    private IEnumerator ExecuteGrabCounterSequence(ZreyAttacks player)
    {
        // --- 1. COMMAND SELF ---
        // Cancel the grab attack and play the "get countered" animation.
        if (attack != null)
        {
            attack.StopAllMovement();
            attack.CloseGrabWindow();
           
        }
        animator.SetTrigger(getCounteredTriggerHash); // Use the existing hash

        // --- 2. COMMAND PLAYER ---
        // Get the stun duration from the player's attack script.
        float counterDuration = player.grabCounterStunDuration;
        player.ExecuteVagabondCounter(counterDuration);

        // --- 3. STUN SELF ---
        if (health != null)
        {
            health.TriggerStun(counterDuration);
        }

        // This coroutine's job is done. It has given the commands.
        yield return null;
    }
    public void EVENT_DetachAndLaunchHand()
    {
        // Failsafe: If no hand is assigned, do nothing.
        if (detachableHand == null)
        {
            Debug.LogError("DetachAndLaunchHand failed: Detachable Hand is not assigned in the Inspector!", this);
            return;
        }

        Debug.LogError($"--- KNIGHT ANIMATION EVENT: DETACHING AND LAUNCHING HAND: {detachableHand.name} ---");

        // 1. Unparent the hand.
        detachableHand.transform.SetParent(null);

        // 2. Get the Rigidbody2D.
        Rigidbody2D handRb = detachableHand.GetComponent<Rigidbody2D>();
        if (handRb != null)
        {
            // 3. Turn on physics.
            handRb.isKinematic = false;
            handRb.gravityScale = 4f;

            // 4. Determine launch direction (away from the player).
            float direction = (transform.position.x > playerTarget.position.x) ? 1f : -1f;

            // 5. Apply forces.
            Vector2 launchForce = new Vector2(handLaunchForceX * direction, handLaunchForceY);
            handRb.AddForce(launchForce, ForceMode2D.Impulse);
            handRb.AddTorque(handLaunchTorque, ForceMode2D.Impulse);
            Destroy(detachableHand, 4.0f);
        }
        else
        {
            Debug.LogError("Dismemberment failed: The assigned detachableHand does not have a Rigidbody2D component!", detachableHand);
        }
    }
   
}