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
    [HideInInspector] public bool isHanging = false;
    [HideInInspector] public float overrideMoveTimer = 0f;
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
    public bool isFacingRight = true;
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
    [SerializeField] private AnimationCurve dashSpeedCurve;


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

    [Header("Wall Mechanics Settings")]
    [Tooltip("The child object used to detect walls.")]
    [SerializeField] private Transform wallCheck;

    [Tooltip("The radius of the wall check sphere.")]
    [SerializeField] private float wallCheckRadius = 0.2f;

    [Tooltip("The layer that should be considered a wall.")]
    [SerializeField] private LayerMask wallLayer; // This can be the same as your Ground layer or a new one."

    [Tooltip("The downward speed of the player when sliding on a wall.")]
    [SerializeField] private float minWallSlideSpeed = 2f;
    [SerializeField] private float maxWallSlideSpeed = 2f;
    [SerializeField] private float wallSlideAccelerationTime = 2f;

    [Tooltip("The time in seconds the player sticks to the wall before sliding.")]
    [SerializeField] private float wallStickTime = 0.5f;

    [Tooltip("The force of the wall jump, applied diagonally.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(8f, 16f);
    [SerializeField] private float wallJumpInputLockTime = 0.3f;


    // --- Private State Variables ---
    private bool isTouchingWall;
    private bool isWallSliding;
    private float wallStickCounter;
    [HideInInspector] public bool justWallJumped = false;
    // --- New Animation Hashes ---
    private readonly int touchWallTriggerHash = Animator.StringToHash("touchWall");
    private readonly int isWallSlidingBoolHash = Animator.StringToHash("isWallSliding");
    private readonly int wallJumpTriggerHash = Animator.StringToHash("wallJump");
    [SerializeField] private float wallJumpMomentum = 6f;
    private Coroutine wallJumpCoroutine;
    [HideInInspector] public bool wallJumpInputLocked = false;
    public bool justGrappleJumped = false;
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();
        airDashesRemaining = maxAirDashes;
        originalGravityScale = rb.gravityScale;
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
        if (overrideMoveTimer > 0)
        {
            overrideMoveTimer -= Time.deltaTime;
        }
        HandleWallMechanics();
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // We only freeze input if we are actively sliding on a wall.
        if (isHanging || isWallSliding)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            // Only read input if the player is in a normal, controllable state.
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        }

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
            if (moveInput.x != 0 && !wallJumpInputLocked)
            {
                if (moveInput.x < 0 && isFacingRight) { Flip(); }
                else if (moveInput.x > 0 && !isFacingRight) { Flip(); }
            }

        }
        HandleAirborneAnimation();
        if (!wasGrounded && isGrounded)
        {
            // ...reset their air dashes.
            airDashesRemaining = maxAirDashes;
            justWallJumped = false;
            wallJumpInputLocked = false;
            Debug.Log("Dashes Reset to: " + airDashesRemaining);
        }
    }


    void FixedUpdate()
    {
        if (overrideMoveTimer > 0)
        {
            // If the override timer is active, let the grapple momentum ride.
            return;
        }


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
        else if (justWallJumped)
        {
            if (wallJumpInputLocked)
            {
                return;
            }

            // If input is NOT locked, check if the player wants to take over.
            if (moveInput.x != 0)
            {
                // Player is taking control. Cut the momentum.
                rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
                justWallJumped = false; // End the wall jump state.
            }
        }
        else
        {
            if (!justGrappleJumped)
            {
                rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
            }
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
        // Check for wall slide condition directly here. This is more reliable.
        bool onWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer) && !isGrounded;

        if (onWall)
        {
            PerformWallJump();
            return;
        }

        // Your existing ground/air jump logic
        if (isGrounded)
        {
            PerformJump();
        }
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
        rb.bodyType = RigidbodyType2D.Kinematic;
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
        rb.bodyType = RigidbodyType2D.Dynamic; // Turn physics back on!

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
    private void PerformWallJump()
    {
        Debug.Log("PERFORMING DYNAMIC WALL JUMP!");


        if (wallJumpCoroutine != null)
        {
            StopCoroutine(wallJumpCoroutine);
        }

        // --- THIS IS THE FIX ---
        // Before adding any new force, we must guarantee that all previous
        // velocity from the last jump is completely erased.
        rb.linearVelocity = Vector2.zero;
        // --- END OF FIX ---

        // Now, we apply the new force to a clean slate.
        float jumpDirectionX = isFacingRight ? -1f : 1f;
        rb.linearVelocity = new Vector2(wallJumpForce.x * jumpDirectionX, wallJumpForce.y);

        animator.SetTrigger(wallJumpTriggerHash);
        Flip();

        wallJumpCoroutine = StartCoroutine(WallJumpInputLock());
    }

    private IEnumerator WallJumpInputLock()
    {
        // 1. Start the wall jump state and lock input.
        justWallJumped = true;
        wallJumpInputLocked = true;
        isWallSliding = false;
        animator.SetBool(isWallSlidingBoolHash, false);

        // 2. Wait for the lock duration.
        // During this time, FixedUpdate sees wallJumpInputLocked is true and does nothing.
        yield return new WaitForSeconds(wallJumpInputLockTime);

        // 3. After the timer, unlock input.
        // The player can now move to cut the momentum.
        wallJumpInputLocked = false;
        Debug.Log("Wall jump air control is now available.");
    }
    private void HandleWallMechanics()
    {
        // Store the state from the previous frame. This is key for the animation trigger.
        bool wasTouchingWall = isTouchingWall;
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
        if (justWallJumped)
        {
            // --- THIS IS THE FIX ---
            // Check if we are now touching a wall again.
            if (Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer))
            {
                // If we hit a new wall, the post-jump state is over.
                // Instantly lower the shield so the rest of the function can run.
                justWallJumped = false;
                Debug.Log("Hit a new wall. Wall jump lock is OFF.");
            }
            else
            {
                // If we are still flying in the air, we don't want the rest of the
                // wall mechanics to run, so we exit here.
                return;
            }
        }
        // --- THE GUARANTEED ANIMATION TRIGGER ---
        // If we were NOT touching a wall last frame, but we ARE now, and we're in the air...
        if (!wasTouchingWall && isTouchingWall && !isGrounded)
        {
            // ...this is the exact frame of contact. Fire the trigger.
            animator.SetTrigger(touchWallTriggerHash);
            Debug.Log("TOUCH WALL TRIGGER FIRED!");
            wallStickCounter = 0f;
        }

        // Determine the wall slide state.
        if (isTouchingWall && !isGrounded && !justWallJumped)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }

        // --- THE GRAVITY STICK FIX ---
        // This logic now runs based on the state we just determined.
        if (isWallSliding)
        {
            wallStickCounter += Time.deltaTime;

            if (wallStickCounter < wallStickTime)
            {
                // STICK PHASE: This part is correct. Gravity is 0, velocity is 0.
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                // --- THE ACCELERATION FIX ---
                // SLIDE PHASE: Restore gravity and accelerate downward.
                rb.gravityScale = originalGravityScale;
                animator.SetBool(isWallSlidingBoolHash, true);

                // Calculate how far into the acceleration we are.
                float timeSinceStickEnd = wallStickCounter - wallStickTime;
                float accelerationProgress = Mathf.Clamp01(timeSinceStickEnd / wallSlideAccelerationTime);

                // Use Lerp to smoothly transition from min to max speed.
                float currentSlideSpeed = Mathf.Lerp(minWallSlideSpeed, maxWallSlideSpeed, accelerationProgress);

                // Apply the new accelerating downward velocity.
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -currentSlideSpeed);
            }
        }
        else
        {
            // If we are not on a wall, always restore gravity and reset the counter.
            rb.gravityScale = originalGravityScale;
            wallStickCounter = 0;
            animator.SetBool(isWallSlidingBoolHash, false);
        }
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
        // Determine the flip direction based on the current facing direction.
        // This makes it usable by both player input and the wall jump.
        if (!isFacingRight) // If facing left, flip right
        {
            transform.localRotation = Quaternion.Euler(rightFacingRotation);
            transform.localScale = rightFacingScale;
            isFacingRight = true;
            FlipChildObjects(1f);
        }
        else // If facing right, flip left
        {
            transform.localRotation = Quaternion.Euler(leftFacingRotation);
            transform.localScale = leftFacingScale;
            isFacingRight = false;
            FlipChildObjects(-1f);
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
