using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
public class ReaperAI : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ReaperFollow follow;
    [SerializeField] private Transform playerTarget;
    // --- COMPONENT REFERENCES ---
    private ReaperAttack attack;
    private  ReaperHealth health;
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
    private bool tutorialSpecialAttackUnlocked = false;
    private bool hasRecoveredFromFirstGuardBreak = false;

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
    [Header("Finisher UI")]
    [Tooltip("The UI prompt to show when the enemy can be finished.")]
    [SerializeField] private GameObject finisherPromptUI;
    [Tooltip("The range within which the player can perform a finisher.")]
    [SerializeField] private float finisherRange = 2.5f;

    [Header("Special Attack Settings")]
    [Tooltip("The maximum distance from the player at which the AI can decide to use the special attack.")]
    [SerializeField] private float specialAttackRange = 7f; // Add this new variable"

    [SerializeField] private GameObject counterNotifyUI;
    private Animator counterNotifyAnimator;
    private readonly int earlyNotifyHash = Animator.StringToHash("EarlyNotify");
    private readonly int readyInputHash = Animator.StringToHash("ReadyInput");
    private readonly int fadeOutHash = Animator.StringToHash("FadeOut");
    private readonly int idleHash = Animator.StringToHash("Idle");
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
        Debug.LogWarning("!!! SpearAI HEARD OnCounterPressed EVENT !!!");

        if (isCounterWindowOpen && isPlayerInCounterBox)
        {
            Debug.LogError("--- SPEAR COUNTER SUCCESS! ---");
            StartCoroutine(ExecuteCounterSequence());
        }
        else
        {
            Debug.LogWarning($"SpearAI counter failed. Window: {isCounterWindowOpen}, InBox: {isPlayerInCounterBox}");
        }
    }
    void Awake()
    {
        animator = GetComponent<Animator>();
        follow = GetComponent<ReaperFollow>();
        if (counterNotifyUI != null)
        {
            counterNotifyAnimator = counterNotifyUI.GetComponent<Animator>();
            if (counterNotifyAnimator == null)
                Debug.LogError("CounterNotifyUI is assigned but has NO Animator component on it!", counterNotifyUI);
            else
                Debug.Log($"<color=lime>CounterNotifyAnimator found: {counterNotifyAnimator.name}</color>");
        }
        else
        {
            Debug.LogError("counterNotifyUI is NULL — not assigned in Inspector on " + gameObject.name);
        }
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
        attack = GetComponent<ReaperAttack>();
        health = GetComponent<ReaperHealth>();

        // This line will now work because playerTarget exists.
        if (playerTarget != null)
        {
            playerMovements = playerTarget.GetComponent<ZreyMovements>();
        }
    }

    void Update()
    {
        if (ZreyAttacks.PlayerInCinematic) return;
        if (health != null && !health.IsGrounded())
        {
            // If we are NOT on the ground, do NOTHING.
            // Exit the Update loop immediately. The brain is paused while airborne.
            return;
        }
        if (health != null && health.isFinishable)
        {
            // Check the distance to the player.
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

            // If the player is in range, show the prompt. Otherwise, hide it.
            if (finisherPromptUI != null)
            {
                finisherPromptUI.SetActive(distanceToPlayer <= finisherRange);
            }
            return; // If we are finishable, do nothing else.
        }
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
        bool tutorialAllowsSpecialAttack = true;
        if (TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode)
        {
            if (!tutorialSpecialAttackUnlocked)
            {
                tutorialAllowsSpecialAttack = false;
            }
        }

        if (tutorialAllowsSpecialAttack
            && specialAttackCooldownTimer <= 0
            && !isActionLocked
            && !attack.IsAttacking()
            && Vector2.Distance(transform.position, playerTarget.position) <= specialAttackRange)
        {
            if (Random.Range(0f, 1f) <= specialAttackChance)
            {
                Debug.LogWarning($"--- AI DECISION: Player is in range. Attempting SPECIAL ATTACK ---");
                isActionLocked = true;
                StartCoroutine(SpecialAttackSequence());
            }
            else
            {
                ResetSpecialAttackCooldown();
            }
        }

        // Counter window box check
        if (isCounterWindowOpen)
        {
            Collider2D playerCollider = Physics2D.OverlapBox(
                counterCheckPoint.position, counterCheckAreaSize, 0f, playerLayer);
            isPlayerInCounterBox = (playerCollider != null);
            if (counterPromptUI != null) counterPromptUI.SetActive(isPlayerInCounterBox);
        }
        else
        {
            isPlayerInCounterBox = false;
           
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
    private IEnumerator SpecialAttackSequence()
    {
        Debug.Log("<color=yellow>!!! SPECIAL ATTACK TRIGGERED !!!</color>");

        isPerformingSpecialAttack = true;
        if (TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode
      && !TutorialManager.Instance.HasPlayerLearnedParry)
        {
            ZreyMovements zm = FindObjectOfType<ZreyMovements>();
            if (zm != null) zm.CanMove = true;
            if (zm != null) zm.IsDashing();
        }
        if (attack != null) attack.StartSpecialAttack();
        NotifyEarlyCounter();

        float specialAttackDuration = attack != null
            ? attack.GetComboDuration()
            : 2.0f;

        Debug.Log($"<color=cyan>Special attack duration: {specialAttackDuration}s</color>");

        

        bool tutorialSlowTimeTriggered = false;
        float timer = 0f;

        while (timer < specialAttackDuration)
        {
            if (!isActionLocked)
            {
                Debug.Log("<color=cyan>Special attack interrupted by counter.</color>");
                yield break;
            }

            // Only trigger slow time once, after window is open AND player is inside
           

            timer += Time.deltaTime;
            yield return null;
        }

        CloseCounterWindow();

        yield return new WaitForSeconds(1.0f);

        if (attack != null) attack.StopSpecialDamage();
        if (health != null) health.BecomeVulnerable();

        isPerformingSpecialAttack = false;
        isActionLocked = false;

        ResetSpecialAttackCooldown();
        Debug.Log("<color=green>Reaper Special Attack Sequence Finished.</color>");
    }
    private IEnumerator ExecuteCounterSequence()
    {
        if (playerAttacks != null && playerAttacks.IsInCinematicState)
        {
            Debug.LogWarning("Reaper counter ignored: player already in cinematic state.");
            yield break;
        }

        // Hide counter prompt immediately
     

        // Stop the special attack
        if (attack != null) attack.CancelAllAttacks();
        isCounterWindowOpen = false;
        isActionLocked = false; // Unlock so SpecialAttackSequence can detect the interrupt

        // Snap Reaper to correct offset from player
        float directionToPlayer = Mathf.Sign(playerTarget.position.x - transform.position.x);
        transform.position = new Vector3(
            playerTarget.position.x + (counterSuccessOffsetX * -directionToPlayer),
            transform.position.y,
            transform.position.z
        );

        if (follow != null) follow.FacePlayer();

        // Play getCountered animation on the Reaper
        animator.ResetTrigger(specialAttackTriggerHash);
        animator.SetTrigger(getCounteredTriggerHash);

        // Stun the Reaper
        if (health != null)
            health.TriggerStun(health.GetCounterStunDuration());

        // Trigger player counter — pass Reaper's transform as the target
        // so it's excluded from the idle-force and plays its own getCountered anim
        if (playerAttacks != null)
            playerAttacks.StartReaperCounter(transform);

        yield return new WaitForSeconds(3.0f);

        ResetSpecialAttackCooldown();
        Debug.Log("<color=green>Reaper Counter Sequence Finished.</color>");
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);

    }
    public void SpawnCounterBloodEffect()
    {
        Debug.LogWarning("!!! ANIMATION EVENT: SpawnCounterBloodEffect() CALLED !!!");

        if (counterBloodPrefab != null && counterBloodPoint != null)
        {
            // --- THIS IS THE FINAL, GUARANTEED FIX ---
            // We get the PREFAB's own rotation. This respects the rotation you set in the prefab file.
            Quaternion prefabRotation = counterBloodPrefab.transform.rotation;

            // We Instantiate the prefab at our DEDICATED spawn point, but we use the PREFAB's rotation.
            Instantiate(counterBloodPrefab, counterBloodPoint.position, prefabRotation);
            // --- END OF FIX ---
        }
        else
        {
            Debug.LogError("Cannot spawn counter blood effect! Prefab or Spawn Point is not assigned!", this);
        }
    }
    public void NotifyEarlyCounter()
    {
        if (counterNotifyUI == null)
        {
            Debug.LogError("NotifyEarlyCounter: counterNotifyUI is NULL on " + gameObject.name);
            return;
        }
        if (counterNotifyAnimator == null)
        {
            Debug.LogError("NotifyEarlyCounter: counterNotifyAnimator is NULL on " + gameObject.name);
            return;
        }
        if (counterNotifyAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("NotifyEarlyCounter: No AnimatorController on counterNotifyUI!");
            return;
        }

        Debug.Log($"<color=magenta>NotifyEarlyCounter CALLED — playing EarlyNotify directly</color>");
        counterNotifyAnimator.Play("EarlyNotify", 0, 0f);
    }




    public void OpenCounterWindow()
    {
        isCounterWindowOpen = true;
        if (TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode)
            TutorialManager.Instance.TriggerCounterSlowTime();
        if (counterNotifyUI != null && counterNotifyAnimator != null)
        {
            // "ReadyInput" must exactly match your animation state name
            counterNotifyAnimator.Play("CounterPopUp", 0, 0f);
        }

        Debug.LogWarning("--- COUNTER WINDOW: OPEN ---");
    }

    public void CloseCounterWindow()
    {
        isCounterWindowOpen = false;

        if (counterNotifyUI != null && counterNotifyAnimator != null)
        {
            // "FadeOut" must exactly match your animation state name
            counterNotifyAnimator.Play("FadeOut", 0, 0f);
            StartCoroutine(ResetNotifyToIdle());
        }

        Debug.Log("<color=grey>--- COUNTER WINDOW: CLOSED ---</color>");
    }
    private IEnumerator ResetNotifyToIdle()
    {
        // Wait for FadeOut to finish before going Idle
        yield return new WaitForSeconds(0.5f);
        if (counterNotifyAnimator != null)
        {
            counterNotifyAnimator.SetTrigger(idleHash);
        }
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
    public void OnFirstGuardBreakRecovered()
    {
        if (hasRecoveredFromFirstGuardBreak) return;
        hasRecoveredFromFirstGuardBreak = true;

        // Only unlock special attack if all tutorial canvases have been shown
        if (TutorialManager.Instance != null && TutorialManager.Instance.InTutorialMode)
        {
            if (TutorialManager.Instance.HasShownAllCanvases)
            {
                tutorialSpecialAttackUnlocked = true;
                Debug.Log("<color=lime>Tutorial: Special attack unlocked after first guard break recovery.</color>");
            }
            else
            {
                // Wait until all canvases shown then unlock
                StartCoroutine(WaitForCanvasThenUnlock());
            }
        }
        else
        {
            tutorialSpecialAttackUnlocked = true;
        }
    }

    private IEnumerator WaitForCanvasThenUnlock()
    {
        while (TutorialManager.Instance != null
               && !TutorialManager.Instance.HasShownAllCanvases)
        {
            yield return new WaitForSeconds(0.5f);
        }
        tutorialSpecialAttackUnlocked = true;
        Debug.Log("<color=lime>Tutorial: All canvases shown — special attack unlocked.</color>");
    }
}