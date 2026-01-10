// ZreyAttacks.cs - A clean, focused combo attack system.
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Using the new Input System

[RequireComponent(typeof(Animator))]
public class ZreyAttacks : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    // We need a reference to the movement script to check its state.
    [SerializeField] private ZreyMovements playerMovement;

    [Header("Combo Settings")]
    [Tooltip("How long the player has to press the next attack button to continue the combo.")]
    [SerializeField] private float comboResetTime = 1f;

    // --- Private State Variables ---
    private int comboStep = 0;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private InputSystem_Actions inputActions;

    // --- Animation Hashes (for performance) ---
    private readonly int attack1TriggerHash = Animator.StringToHash("attack1");
    private readonly int attack2TriggerHash = Animator.StringToHash("attack2");
    private readonly int attack3TriggerHash = Animator.StringToHash("attack3");
    private readonly int attack4TriggerHash = Animator.StringToHash("attack4");
    private Rigidbody2D rb;
    [SerializeField] private float lungeSpeed = 8f;
    [Tooltip("How long the lunge lasts (in seconds).")]
    [SerializeField] private float lungeDuration = 0.15f;
    void Awake()
    {
        // Automatically get components if they aren't assigned.
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<ZreyMovements>();
        rb = GetComponent<Rigidbody2D>();
        // Set up the new Input System.
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // When the "Fire" action (Left Mouse Button) is performed, call our HandleAttack method.
        inputActions.Player.Attack.performed += HandleAttack;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Attack.performed -= HandleAttack;
    }

    void Update()
    {
        // This is the core combo timer. If too much time has passed since the last
        // attack, the combo resets back to the beginning.
        if (Time.time - lastAttackTime > comboResetTime && comboStep > 0)
        {
            ResetCombo();
        }
    }

    /// <summary>
    /// This method is called by the Input System when the player clicks the left mouse button.
    /// </summary>
    private void HandleAttack(InputAction.CallbackContext context)
    {
        // --- Attack Conditions ---
        // We can only attack if:
        // 1. We are not already in the middle of an attack.
        // 2. The player is on the ground (you can remove this if you want air combos).
        // 3. The player is not dashing or in another special state.
        if (isAttacking || !playerMovement.IsGrounded() )
        {
            return; // Exit if we can't attack right now.
        }

        // If we are here, it's a valid attack input.
        lastAttackTime = Time.time; // Record the time of this attack.
        comboStep++; // Move to the next step in the combo.

        // If the combo goes past the 4th attack, loop it back to the 1st.
        if (comboStep > 4)
        {
            comboStep = 1;
        }

        // Perform the attack corresponding to the current combo step.
        PerformAttack(comboStep);
    }

    /// <summary>
    /// Triggers the correct attack animation based on the combo step.
    /// </summary>
    private void PerformAttack(int step)
    {
        isAttacking = true; // Set our state to "attacking".
        // You might want to tell the movement script to lock movement here.
        // For example: playerMovement.SetAttacking(true);

        // Trigger the correct animation.
        if (step == 1)
        {
            animator.SetTrigger(attack1TriggerHash);
        }
        else if (step == 2)
        {
            animator.SetTrigger(attack2TriggerHash);
        }
        else if (step == 3)
        {
            animator.SetTrigger(attack3TriggerHash);
        }
        else if (step == 4)
        {
            animator.SetTrigger(attack4TriggerHash);
        }
        Debug.Log($"<color=green>ATTACK {step} TRIGGERED!</color>");
    }

    /// <summary>
    /// Resets the combo state. Called by the timer in Update().
    /// </summary>
    private void ResetCombo()
    {
        Debug.Log("<color=orange>Combo Reset.</color>");
        comboStep = 0;
    }

    // --- PUBLIC METHODS FOR ANIMATION EVENTS ---
    // These methods are called directly from your animation clips to control the flow of the combo.

    /// <summary>
    /// Call this from an Animation Event at the end of each attack animation.
    /// This tells the script that the attack is finished and it's ready for the next input.
    /// </summary>
    /// 
    public void PerformLunge()
    {
        if (playerMovement == null) return;
        StartCoroutine(LungeCoroutine());
    }

    private IEnumerator LungeCoroutine()
    {
        float timer = 0f;
        Vector2 direction = playerMovement.IsFacingRight() ? Vector2.right : Vector2.left;

        while (timer < lungeDuration)
        {
            // Calculate the movement for this frame.
            Vector2 moveStep = direction * lungeSpeed * Time.deltaTime;
            // Apply the movement using MovePosition.
            rb.MovePosition(rb.position + moveStep);

            timer += Time.deltaTime;
            yield return null;
        }
    }
    public void EndAttack()
    {
        isAttacking = false;
        // You might want to unlock movement here.
        // For example: playerMovement.SetAttacking(false);
        Debug.Log($"Attack {comboStep} animation finished. Ready for next input.");
    }
}
