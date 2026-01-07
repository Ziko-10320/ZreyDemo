using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class ZreyMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float runSpeed = 5f;

    [Header("Jumping Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Flipping Logic")]
    [SerializeField] private Vector3 rightFacingRotation = new Vector3(0, 90, 0);
    [SerializeField] private Vector3 leftFacingRotation = new Vector3(0, -90, 0);
    [SerializeField] private Vector3 rightFacingScale = new Vector3(1, 1, 1);
    [SerializeField] private Vector3 leftFacingScale = new Vector3(1, -1, 1);
    [SerializeField] private GameObject[] objectsToFlip;

    // --- THIS IS THE KEY TO YOUR SETUP ---
    [Header("Manual Dash Root Motion")]
    [Tooltip("The child object that is animated to create the dash movement.")]
    [SerializeField] private Transform dashMover;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    // --- State Variables ---
    private bool isFacingRight = true;
    private bool isGrounded;
    private float jumpBufferCounter;
    private bool isDashing = false; // Our master state switch

    // --- Manual Root Motion State ---
    private Vector3 previousMoverPosition;

    // --- Animation Hashes ---
    private readonly int isRunningHash = Animator.StringToHash("isRunning");
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int jumpTriggerHash = Animator.StringToHash("jump");
    private readonly int isFallingHash = Animator.StringToHash("isFalling");
    private readonly int dashTriggerHash = Animator.StringToHash("dash");
    [Header("Physics Dash Settings")]
    [Tooltip("The overall distance the player will dash.")]
    [SerializeField] private float dashDistance = 5f; 

    [Tooltip("The total duration of the dash in seconds.")]
    [SerializeField] private float dashDuration = 0.5f;

    [Tooltip("The speed curve of the dash. X-axis is time (0 to 1), Y-axis is speed multiplier (0 to 1).")]
    [SerializeField]private AnimationCurve dashSpeedCurve;


    private float dashTimer;
    private float dashDirection;


    [Header("Air Dash (Teleport) Settings")]
    [Tooltip("The horizontal distance the player teleports forward.")]
    [SerializeField] private float airDashDistance = 4f;

    [Tooltip("The vertical distance the player teleports upward.")]
    [SerializeField] private float upwardDashDistance = 3f;

    [Tooltip("The prefab of the particle effect to spawn on dash.")]
    [SerializeField] private GameObject vanishEffect;
    [SerializeField] private GameObject reappearEffect;

    [Tooltip("The child transform where the effect should spawn from.")]
    [SerializeField] private Transform effectSpawnPoint;

    [Tooltip("The maximum number of air dashes the player has.")]
    [SerializeField] private int maxAirDashes = 2;
    private float originalGravityScale;
    // --- Private State Variables ---
    // We need to track how many dashes are left.
    private int airDashesRemaining;
    [SerializeField] private float teleportVanishDuration = 0.2f;
    [SerializeField] private float postDashHopForce = 2f;
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();
        airDashesRemaining = maxAirDashes;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Jump.performed += HandleJump;
        inputActions.Player.Dash.performed += HandleDash;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Jump.performed -= HandleJump;
        inputActions.Player.Dash.performed -= HandleDash;
    }

    void Update()
    {
        // Normal input gathering is always safe because it doesn't move the character.
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Ground Check
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded != wasGrounded) { animator.SetBool(isGroundedHash, isGrounded); }

        // Jump Buffer
        if (jumpBufferCounter > 0) { jumpBufferCounter -= Time.deltaTime; }
        if (!wasGrounded && isGrounded && jumpBufferCounter > 0) { PerformJump(); }

        // Normal Animations (only if not dashing)
        if (!isDashing)
        {
            HandleMovementAnimation();
            Flip();
        }
        HandleAirborneAnimation();
        if (!wasGrounded && isGrounded)
        {
            // ...reset their air dashes.
            airDashesRemaining = maxAirDashes;
            Debug.Log("Dashes Reset to: " + airDashesRemaining);
        }
    }


    void FixedUpdate()
    {
        if (isDashing)
        {
            // --- STATE: DASHING (Physics) ---
            // Increment the timer.
            dashTimer += Time.fixedDeltaTime;

            // 1. Calculate how far through the dash we are (a value from 0 to 1).
            float progress = dashTimer / dashDuration;

            // 2. Get the speed multiplier from our curve based on the progress.
            float speedMultiplier = dashSpeedCurve.Evaluate(progress);

            // 3. Calculate the final dash speed for this frame.
            // The average speed is distance / time. We multiply by the curve's value.
            float currentDashSpeed = (dashDistance / dashDuration) * speedMultiplier;

            // 4. Apply the velocity directly to the Rigidbody.
            rb.linearVelocity = new Vector2(currentDashSpeed * dashDirection, 0);
        }
        else
        {
            // --- STATE: NORMAL (Physics) ---
            // This is your normal, reliable run/idle movement.
            rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
        }
    }
    // --- PUBLIC METHODS FOR ANIMATION EVENTS ---
    /// </summary>
    public void EnableDash()
    {
        isDashing = true;
        dashTimer = 0f; // Reset the dash timer

        // Store the direction the player is facing when the dash starts.
        dashDirection = isFacingRight ? 1f : -1f;
        
    }

    /// <summary>
    /// Ends the physics-based dash. Called by an animation event.
    /// </summary>
    public void DisableDash()
    {
        isDashing = false;
       
    }
    // --- INPUT HANDLERS AND HELPERS (Mostly unchanged) ---
    private void HandleJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            PerformJump();
        }
        // If we are in the air, we can only buffer a jump if we are NOT dashing.
        // This prevents buffering a jump during an air dash.
        else if (!isDashing)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void HandleDash(InputAction.CallbackContext context)
    {
        // --- DECIDE WHICH DASH TO USE ---

        if (isGrounded)
        {
            // --- GROUND DASH ---
            // If on the ground, perform the physics-based dash.
            if (!isDashing)
            {
                animator.SetTrigger(dashTriggerHash);
            }
        }
        else
        {
            // --- AIR DASH ---
            // If in the air, perform the teleport dash, if any are left.
            if (airDashesRemaining > 0)
            {
                PerformAirDash();
            }
        }
    }
    private void PerformAirDash()
    {
        // --- 1. Spend a dash charge ---
        airDashesRemaining--;
        StartCoroutine(AirDashTeleportSequence());
        Debug.Log("Air Dashed! Remaining: " + airDashesRemaining);
        
    }
    private IEnumerator AirDashTeleportSequence()
    {
        // --- 1. VANISH PHASE ---
        Debug.Log("Vanish!");
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        // Spawn the "start" effect.
        if (vanishEffect != null && effectSpawnPoint != null)
        {
            Instantiate(vanishEffect, effectSpawnPoint.position, effectSpawnPoint.rotation);
        }

        // Make the player invisible. You can do this by disabling the Sprite Renderer or Mesh Renderer.
        // Let's assume you have a reference to it. If not, you'll need to add one.
        // For now, let's just disable the whole GameObject's visual components.
        // A better way is to have a dedicated "Visuals" child object to disable.
        // For simplicity, let's just disable the renderer.
        var playerRenderer = GetComponentInChildren<Renderer>(); // This finds the first renderer (Sprite or Mesh)
        if (playerRenderer != null) playerRenderer.enabled = false;

        // Temporarily stop physics interactions during the "in-between" state.
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;


        // --- 2. WAIT PHASE ---
        // This is the magic of coroutines. It pauses the function here for the specified duration.
        yield return new WaitForSeconds(teleportVanishDuration);


        // --- 3. REAPPEAR PHASE ---
        Debug.Log("Reappear!");

        // Determine the teleport destination.
        Vector2 dashDirectionVector;
        float verticalInput = moveInput.y;
        if (verticalInput > 0.5f)
        {
            dashDirectionVector = Vector2.up * upwardDashDistance;
        }
        else
        {
            float direction = isFacingRight ? 1f : -1f;
            dashDirectionVector = new Vector2(airDashDistance * direction, 0);
        }

        // Teleport to the new position.
        Vector2 newPosition = rb.position + dashDirectionVector;
        transform.position = newPosition; // Use transform.position since rb is kinematic.
        rb.isKinematic = false; // Turn physics back on!

        // Make the player visible again.
        if (playerRenderer != null) playerRenderer.enabled = true;

        if (reappearEffect != null && effectSpawnPoint != null)
        {
            Instantiate(reappearEffect, effectSpawnPoint.position, effectSpawnPoint.rotation);
        }
        rb.gravityScale = originalGravityScale;
        animator.SetTrigger(jumpTriggerHash);
        rb.linearVelocity = new Vector2(0, postDashHopForce);
    }
    private void PerformJump()
    {
        if (isDashing)
        {
            DisableDash(); // Call the same function our animation event does!
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        animator.SetTrigger(jumpTriggerHash);
        jumpBufferCounter = 0f;
    }
    private void HandleMovementAnimation()
    {
        animator.SetBool(isRunningHash, moveInput.x != 0);
    }

    private void HandleAirborneAnimation()
    {
        // This logic no longer needs to touch the jump parameter, making it cleaner.
        if (isGrounded)
        {
            animator.SetBool(isFallingHash, false);
        }
        else
        {
            // If we are in the air and moving down, we are falling.
            if (rb.linearVelocity.y < 0)
            {
                animator.SetBool(isFallingHash, true);
            }
        }
    }
  
    private void Flip()
    {
        if (moveInput.x < 0 && isFacingRight)
        {
            transform.localRotation = Quaternion.Euler(leftFacingRotation);
            transform.localScale = leftFacingScale;
            isFacingRight = false;
            FlipChildObjects(-1f);
        }
        else if (moveInput.x > 0 && !isFacingRight)
        {
            transform.localRotation = Quaternion.Euler(rightFacingRotation);
            transform.localScale = rightFacingScale;
            isFacingRight = true;
            FlipChildObjects(1f);
        }
    }

    private void FlipChildObjects(float newXScale)
    {
        if (objectsToFlip == null || objectsToFlip.Length == 0) return;
        foreach (GameObject obj in objectsToFlip)
        {
            obj.transform.localScale = new Vector3(newXScale, obj.transform.localScale.y, obj.transform.localScale.z);
        }
    }
   
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
