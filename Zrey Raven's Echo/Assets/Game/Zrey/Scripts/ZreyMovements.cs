using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class ZreyMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float runSpeed = 5f;
    [Header("Dash UI")]
    [SerializeField] private CanvasGroup dashUICanvasGroup;
    [SerializeField] private UnityEngine.UI.Image dashCharge1;
    [SerializeField] private UnityEngine.UI.Image dashCharge2;
    [SerializeField] private float dashUIFadeOutDelay = 1.5f;
    [SerializeField] private Transform dashUIFollow; // assign the canvas transform
    [SerializeField] private Vector3 dashUIOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float dashUIFadeDuration = 0.25f;
    [SerializeField] private Color dashChargedColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private Color dashDepletedColor = new Color(1f, 0.85f, 0f, 0f);
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
    public bool hasGrappleMomentum = false;
    // --- THIS IS THE KEY TO YOUR SETUP ---
    [Header("Manual Dash Root Motion")]
    [Tooltip("The child object that is animated to create the dash movement.")]
    [SerializeField] private Transform dashMover;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator rootZreyAnimator;
    [SerializeField] private LayerMask dashCollisionLayer;
    public static InputSystem_Actions inputActions;
    private Vector2 moveInput;

    // --- State Variables ---
    public bool isFacingRight = true;
    private bool isGrounded;
    private float jumpBufferCounter;
    private bool isDashing = false; // Our master state switch

    // --- Manual Root Motion State ---
    private Vector3 previousMoverPosition;
    private Coroutine dashUIFadeOutCoroutine;
    private int lastKnownDashes = -1;
    // --- Animation Hashes ---
    private readonly int isRunningHash = Animator.StringToHash("isRunning");
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int jumpTriggerHash = Animator.StringToHash("jump");
    private readonly int isFallingHash = Animator.StringToHash("isFalling");
    private readonly int dashTriggerHash = Animator.StringToHash("dash");
    private readonly int rootDashTriggerHash = Animator.StringToHash("rootDash");
    private readonly int rootDashLeftTriggerHash = Animator.StringToHash("rootDashLeft");
    [Header("Physics Dash Settings")]
    [Tooltip("The overall distance the player will dash.")]
    [SerializeField] private float dashDistance = 5f;

    [Tooltip("The total duration of the dash in seconds.")]
    [SerializeField] private float dashDuration = 0.3f;

    [Tooltip("The speed curve of the dash. X-axis is time (0 to 1), Y-axis is speed multiplier (0 to 1).")]
    [SerializeField] private AnimationCurve dashSpeedCurve;

    [Tooltip("The speed of the ground backward dash in combat mode.")]
    [SerializeField] private float groundBackwardDashSpeed = 14f;

    [Tooltip("The duration of the ground backward dash.")]
    [SerializeField] private float groundBackwardDashDuration = 0.25f;

    private float dashTimer;
    private float dashDirection;
    private Coroutine groundDashCoroutine = null;


    [Header("Air Dash Settings")]
    [Tooltip("The speed of the forward air dash.")]
    [SerializeField] private float forwardAirDashSpeed = 15f; 
    [Tooltip("The duration of the forward air dash.")]
    [SerializeField] private float forwardAirDashDuration = 0.3f; 
    [Tooltip("The speed of the upward air dash.")]
    [SerializeField]  private float upwardAirDashSpeed = 12f; 
    [Tooltip("The duration of the upward air dash.")]
    [SerializeField] private float upwardAirDashDuration = 0.25f; 

    // --- THIS IS THE FIX ---
    [Tooltip("The specific layer the player can phase through during an air dash.")]
    [SerializeField] private LayerMask phaseThroughLayer;

    private readonly int forwardAirDashTriggerHash = Animator.StringToHash("ForwardAirDash");
    private readonly int upwardAirDashTriggerHash = Animator.StringToHash("UpwardAirDash");


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
    [HideInInspector] public bool isLungeActive = false;
    [HideInInspector] public Vector2 lungeVelocity;
    public bool CanMove { get; set; } = true;

    [SerializeField] private ZreyTrail playerTrail;
    private ZreyAttacks playerAttacks;
    private PlayerHealth playerHealth;
    [SerializeField] private PlayerGrapple playerGrapple;
    [HideInInspector] public bool canFlip = true;
    private Coroutine flipLockWatchdogCoroutine;
    public bool justPressedDash { get; private set; }
    private bool isInRootMotionState = false;
    [Tooltip("The particle effect to spawn during a dash animation.")]
    [SerializeField] private GameObject dashParticlePrefab; 
    [Tooltip("The point where the dash particles should spawn.")]
    [SerializeField] private Transform dashParticleSpawnPoint;
    private Coroutine airDashCoroutine = null;
    [Header("Combat Mode")]
    [Tooltip("The radius of the circle where the player will detect enemies to enter combat mode.")]
    [SerializeField] private float combatDetectionRange = 10f;
    [SerializeField] private Vector2 combatDetectionBoxSize = new Vector2(10f, 3f);
    [Tooltip("The layer the enemies are on.")]
    [SerializeField] private LayerMask enemyLayer; // You may need to re-assign this in the Inspector"
    [Tooltip("The player's movement speed when locked on in combat.")]
    [SerializeField] private float combatRunSpeed = 4f;

    private bool isInCombatMode = false;
    private Transform lockedOnTarget = null;
    public Transform LockedOnTarget => lockedOnTarget;
    public bool IsInCombatMode => isInCombatMode;
    public bool IsBackwardDashing { get; private set; } = false;
    private bool isAttackLocked = false;

    // --- ADD THESE NEW ANIMATION HASHES ---
    private readonly int combatModeBoolHash = Animator.StringToHash("isInCombatMode");
    private readonly int isMovingForwardHash = Animator.StringToHash("isMovingForward");
    private readonly int isMovingBackwardHash = Animator.StringToHash("isMovingBackward");
    private readonly int isAttackingBoolHash = Animator.StringToHash("isAttacking");
    private readonly int isChangingDirectionBoolHash = Animator.StringToHash("isChangingDirection");
    private readonly int rootDashBackwardTriggerHash = Animator.StringToHash("rootDashBackward");
    private readonly int rootDashBackwardLeftTriggerHash = Animator.StringToHash("rootDashBackwardLeft");
    private readonly int dashBackTriggerHash = Animator.StringToHash("dashback");
    private readonly int exitCombatTriggerHash = Animator.StringToHash("ExitCombat");
    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource combatRunSource;
    [Range(0f, 1f)][SerializeField] private float jumpSoundVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float landSoundVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float groundDashSoundVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float airDashSoundVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float footstepVolume = 1f;
    [SerializeField] private AudioClip wallJumpClip;
    [SerializeField] private AudioClip combatRunForwardClip;
    [SerializeField] private AudioClip combatRunBackwardClip;
    [Range(0f, 1f)][SerializeField] private float wallJumpSoundVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float combatRunSoundVolume = 1f;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip groundDashClip;
    [SerializeField] private AudioClip airDashClip;
    [SerializeField] private AudioSource footstepSource; // Separate looping source
    [SerializeField] private AudioClip footstepClip;

    private bool isAutoRunning = false;
    private float autoRunDirection = 0f;

    public bool IsAutoRunning => isAutoRunning;

    private bool justStoppedAutoRun = false;

    void Awake()
    {
        if (combatRunSource == null)
        {
            combatRunSource = gameObject.AddComponent<AudioSource>();
            combatRunSource.playOnAwake = false;
            combatRunSource.loop = true;
            combatRunSource.spatialBlend = 0f;
        }
        // --- THIS IS THE KING'S DECREE ---
        // 1. If the one true input system does not exist yet, create it.
        //    This happens in Awake(), so it is GUARANTEED to run before any script's OnEnable().
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
            // 2. ENABLE THE ENTIRE "PLAYER" ACTION MAP IMMEDIATELY.
            //    This is the master power switch. It is now ON.
            inputActions.Player.Enable();
        }
        // --- END OF THE KING'S DECREE ---
        if (inputActions == null || !inputActions.Player.enabled)
        {
            // If it's null, create it.
            if (inputActions == null)
            {
                inputActions = new InputSystem_Actions();
            }

            // ALWAYS enable it if we enter this block.
            inputActions.Player.Enable();
            Debug.Log("<color=lime>ZREYMOVEMENTS HAS GUARANTEED THAT INPUTS ARE ENABLED!</color>");
        }
        // The rest of your Awake function is perfect.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        airDashesRemaining = maxAirDashes;
        originalGravityScale = rb.gravityScale;
        if (playerTrail == null) playerTrail = GetComponent<ZreyTrail>();
        if (playerAttacks == null) playerAttacks = GetComponent<ZreyAttacks>();
        playerHealth = GetComponent<PlayerHealth>();
        if (playerGrapple == null) playerGrapple = GetComponentInParent<PlayerGrapple>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D sound
        }
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = true;
            footstepSource.spatialBlend = 0f; // 2D sound
        }
        if (dashUICanvasGroup != null) dashUICanvasGroup.alpha = 0f;
    }
    public static void NukeInputSystem()
    {
        // If the input system exists...
        if (inputActions != null)
        {
            // ...completely dispose of it and set it back to null.
            // This is the only way to guarantee a fresh start next time.
            inputActions.Dispose();
            inputActions = null;
            Debug.Log("<color=red>NUKE DEPLOYED: Input System has been destroyed and set to null.</color>");
        }
    }
    private void OnEnable()
    {
        // This script's ONLY job in OnEnable is to subscribe its own functions
        // to the system that was already turned on in Awake().
        inputActions.Player.Jump.performed += HandleJump;
        inputActions.Player.Dash.performed += HandleDash;
    }

    // Your OnDisable should also be updated to match
    private void OnDisable()
    {
        // Only unsubscribe from the events this script is responsible for.
        // Do not disable the whole inputActions object here.
        if (inputActions != null)
        {
            inputActions.Player.Jump.performed -= HandleJump;
            inputActions.Player.Dash.performed -= HandleDash;
        }
    }

    void Update()
    {
        Vector2 rawCompositeInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (dashUIFollow != null) dashUIFollow.position = transform.position + dashUIOffset;
        // 2. Check the individual key states.
        bool isLeftPressed = inputActions.Player.Move.ReadValue<Vector2>().x < 0;
        bool isRightPressed = inputActions.Player.Move.ReadValue<Vector2>().x > 0;

        // 3. The Latching Logic.
        // If the combined input is NOT zero, it means only one key is pressed. Use it.
        if (rawCompositeInput.x != 0)
        {
            moveInput = rawCompositeInput;
        }
        // If the combined input IS zero, it could mean two things:
        // A) No keys are pressed.
        // B) Both keys are pressed.
        // We only want to stop if NO keys are pressed.
        else if (!isLeftPressed && !isRightPressed)
        {
            moveInput = Vector2.zero;
        }
        if (!isAutoRunning && ((playerAttacks != null && playerAttacks.IsInCinematicState) || isHanging || isWallSliding))
        {
            moveInput = Vector2.zero;
        }

        // Run the brains.
        HandleCombatAndAnimation();
        HandleWallMechanics();
        justPressedDash = false;
        if (overrideMoveTimer > 0)
        {
            overrideMoveTimer -= Time.deltaTime;
        }



        // We only freeze input if we are actively sliding on a wall.
        if (isAutoRunning)
        {
            // Override everything — force run direction every frame
            moveInput = new Vector2(autoRunDirection, 0f);
            animator.SetBool(isRunningHash, true);
        }
        else if (isHanging || isWallSliding)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        }
        if (isAutoRunning)
        {
            moveInput = new Vector2(autoRunDirection, 0f);
            CanMove = false; // blocks FixedUpdate movement override
        }

        // Ground Check
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded != wasGrounded) { animator.SetBool(isGroundedHash, isGrounded); }
        if (hasGrappleMomentum && isGrounded)
        {
            // First, reset the state flag.
            hasGrappleMomentum = false;

            // Now, check for player input AT THIS MOMENT.
            float horizontalInput = inputActions.Player.Move.ReadValue<Vector2>().x;

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                // --- The player WANTS to run. ---
                // Immediately switch to the normal run speed. No stop, no freeze.
                rb.linearVelocity = new Vector2(horizontalInput * runSpeed, rb.linearVelocity.y);
                Debug.Log("<color=green>Grapple Momentum -> Seamlessly Transitioned to Run!</color>");
            }
            else
            {
                // --- The player is NOT holding a direction. ---
                // Bring them to a crisp, clean stop.
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                Debug.Log("<color=orange>Grapple Momentum Cancelled on Landing (Player Idle).</color>");
            }
        }
        // Jump Buffer
        if (jumpBufferCounter > 0) { jumpBufferCounter -= Time.deltaTime; }
        if (!wasGrounded && isGrounded && jumpBufferCounter > 0) { PerformJump(); }

        // Normal Animations (only if not dashing)
        if (!isDashing)
        {
            HandleMovementAnimation();
        }


        if (!isInCombatMode && isGrounded && moveInput.x != 0 && canFlip && !wallJumpInputLocked && (playerAttacks == null || !playerAttacks.isDashAttacking))
        {
            if (moveInput.x < 0 && isFacingRight) { Flip(); }
            else if (moveInput.x > 0 && !isFacingRight) { Flip(); }
        }
        HandleAirborneAnimation();
        if (!wasGrounded && isGrounded)
        {
            if (landClip != null) sfxSource.PlayOneShot(landClip, landSoundVolume); // ADD THIS
            if (playerAttacks != null)
            {
                playerAttacks.OnPlayerLanded(); // We will create this new public method.
            }
            if (playerAttacks != null && playerAttacks.IsDownSlamming())
            {
                // If yes, tell it to end the slam and create the impact.
                playerAttacks.EndDownSlam();
            }
            // ...reset their air dashes.
            airDashesRemaining = maxAirDashes;
            DashUI_OnDashesRestored();
            justWallJumped = false;
            wallJumpInputLocked = false;
            Debug.Log("Dashes Reset to: " + airDashesRemaining);
        }
    }

    public void EVENT_LockAllMovement()
    {
        CanMove = false;
        canFlip = false;
        isDashing = false;
        isInRootMotionState = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true; // Keep physics on so gravity works
        }

        Debug.LogError("--- EVENT_LockAllMovement: MOVEMENT FULLY LOCKED ---");
    }
    private void DashUI_OnDashUsed()
    {
        if (dashUICanvasGroup == null) return;

        // Cancel any pending fade out — we spent a dash so UI must stay
        if (dashUIFadeOutCoroutine != null)
        {
            StopCoroutine(dashUIFadeOutCoroutine);
            dashUIFadeOutCoroutine = null;
        }

        // Show the UI immediately
        StartCoroutine(DashUI_Fade(1f));

        // Update charge icons
        if (dashCharge1 != null) dashCharge1.color = airDashesRemaining >= 1 ? dashChargedColor : dashDepletedColor;
        if (dashCharge2 != null) dashCharge2.color = airDashesRemaining >= 2 ? dashChargedColor : dashDepletedColor;
    }

    private void DashUI_OnDashesRestored()
    {
        if (dashUICanvasGroup == null) return;
        if (dashCharge1 != null) dashCharge1.color = dashChargedColor;
        if (dashCharge2 != null) dashCharge2.color = dashChargedColor;

        // Only fade out if we're actually back to full
        if (airDashesRemaining >= maxAirDashes)
        {
            if (dashUIFadeOutCoroutine != null) StopCoroutine(dashUIFadeOutCoroutine);
            dashUIFadeOutCoroutine = StartCoroutine(DashUI_FadeOutAfterDelay());
        }
    }

    private IEnumerator DashUI_FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(dashUIFadeOutDelay);
        yield return StartCoroutine(DashUI_Fade(0f));
        dashUIFadeOutCoroutine = null;
    }

    private IEnumerator DashUI_Fade(float targetAlpha)
    {
        float startAlpha = dashUICanvasGroup.alpha;
        float t = 0f;
        while (t < dashUIFadeDuration)
        {
            t += Time.deltaTime;
            dashUICanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / dashUIFadeDuration);
            yield return null;
        }
        dashUICanvasGroup.alpha = targetAlpha;
    }
    // Call this from Animation Event to fully restore movement
    public void EVENT_UnlockAllMovement()
    {
        CanMove = true;
        canFlip = true;

        Debug.Log("<color=green>--- EVENT_UnlockAllMovement: MOVEMENT RESTORED ---</color>");
    }
    void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.isBeingKnockedBack) { return; }
        if (isInRootMotionState) { return; }

        // MUST check attackLocked BEFORE CanMove because PerformAttack sets both
        if (isAttackLocked)
        {
            // If a dash started during an attack, the dash wins — clear the lock
            if (isDashing || isInRootMotionState)
            {
                isAttackLocked = false;
                CanMove = true;
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
        }
        if (isAutoRunning)
        {
            rb.linearVelocity = new Vector2(autoRunDirection * runSpeed, rb.linearVelocity.y);
            return;
        }
        if (!CanMove) { return; }
        if (isAutoRunning)
        {
            rb.linearVelocity = new Vector2(autoRunDirection * runSpeed, rb.linearVelocity.y);
            return;
        }
        if (overrideMoveTimer > 0) { return; }
        if (isDashing) { return; }
        Vector2 currentMoveInput = ZreyMovements.inputActions.Player.Move.ReadValue<Vector2>();

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
        if (isInCombatMode && isGrounded && lockedOnTarget != null && !wallJumpInputLocked)
        {
            if (playerAttacks == null || !playerAttacks.isDashAttacking)
                ForceFaceDirection(lockedOnTarget.position.x > transform.position.x);

            rb.linearVelocity = new Vector2(moveInput.x * combatRunSpeed, rb.linearVelocity.y);
        }
        // PRIORITY #2: If not locked on (either not in combat OR in the air), do normal movement.
        else
        {
            if (!wallJumpInputLocked && (playerAttacks == null || !playerAttacks.isDashAttacking))
            {
                if (moveInput.x < 0 && isFacingRight) { Flip(); }
                else if (moveInput.x > 0 && !isFacingRight) { Flip(); }
            }

            // Apply normal movement physics (this also handles wall jumps correctly).
            if (justWallJumped)
            {
                // While input is locked, preserve the wall jump arc completely
                if (wallJumpInputLocked) return;

                // Lock expired — check if player is actively steering
                if (moveInput.x != 0)
                {
                    // Player wants to steer — hand off to normal movement and end wall jump state
                    rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
                    justWallJumped = false;
                }
                // No input held: do NOT touch rb.linearVelocity at all
                // Physics drag will decelerate naturally, no hard stop
            }
            else if (!justGrappleJumped)
            {
                // Only runs when justWallJumped is false (normal movement or after player steered)
                rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
            }
        }
       
    }
    // --- PUBLIC METHODS FOR ANIMATION EVENTS ---
    /// </summary>
    public void EnableDash()
    {
        isDashing = true;
    }

    /// <summary>
    /// Called by an animation event at the END of the dash.
    /// </summary>
    public void DisableDash()
    {
        isDashing = false;
    }
    public void CancelGroundDash()
    {
        if (groundDashCoroutine != null)
        {
            StopCoroutine(groundDashCoroutine);
            groundDashCoroutine = null;
        }
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravityScale;
        Debug.Log("<color=orange>Ground dash cancelled by dash attack.</color>");
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        if (isAutoRunning) return;
        if (playerAttacks != null && playerAttacks.IsAttacking() && isGrounded)
        {
            bool consumed = playerAttacks.TryAttackJump();
            if (consumed) return; // Attack jump fired — don't do a normal jump
        }
        if (playerHealth != null && playerHealth.IsGrabbed)
        {
            Debug.LogWarning("Jump Input Ignored: Player is GRABBED.");
            return;
        }
        if (playerAttacks != null && playerAttacks.IsInCinematicState)
        {
            Debug.Log("Jump Input Ignored: In Cinematic State.");
            return;
        }
        if (playerHealth != null && playerHealth.IsShieldBroken()) { Debug.LogWarning("Dash ignored: Shield broken."); return; }

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
        if (isAutoRunning) return;
        if (playerHealth != null && playerHealth.IsGrabbed)
        {
            Debug.LogWarning("Dash Input Ignored: Player is GRABBED.");
            return;
        }
        if (playerAttacks != null && playerAttacks.IsInCinematicState)
        {
            Debug.Log("Dash Input Ignored: In Cinematic State.");
            return;
        }
        if (isDashing)
        {
            Debug.Log("<color=orange>Dash Input Ignored: Already Dashing.</color>");
            return;
        }
        if (playerGrapple != null && playerGrapple.IsGrappling())
        {
            Debug.Log("<color=orange>Dash Input Ignored: Currently Grappling.</color>");
            return;
        }
        if (playerHealth != null && playerHealth.IsBlocking())
        {
            Debug.Log("<color=orange>Dash Input Ignored: Currently Blocking.</color>");
            return;
        }
        // SHIELD 2: Are we currently attacking?
        if (playerAttacks != null && playerAttacks.IsAttacking())
        {
            Debug.Log("<color=orange>Dash Input Ignored: Currently Attacking.</color>");
            return;
        }
        if (playerHealth != null && playerHealth.IsShieldBroken()) { Debug.LogWarning("Dash ignored: Shield broken."); return; }

        // SHIELD 3: Are we on a wall or hanging?
        if (isWallSliding || isHanging)
        {
            Debug.Log("<color=orange>Dash Input Ignored: On a Wall or Hanging.</color>");
            return;
        }

        // SHIELD 4: Are we stunned from taking damage?
        if (playerHealth != null && playerHealth.isStunned)
        {
            Debug.Log("<color=orange>Dash Input Ignored: Currently Stunned.</color>");
            return;
        }
        justPressedDash = true;
        // --- DECIDE WHICH DASH TO USE ---

        if (isInRootMotionState)
        {
            Debug.Log("<color=orange>Dash Input Ignored: A root motion action is already in progress.</color>");
            return;
        }
        animator.SetBool(isMovingForwardHash, false);
        animator.SetBool(isMovingBackwardHash, false);
        if (isGrounded)
        {
            // --- THIS IS THE GUARANTEED FIX ---
            // THE NEW BRAIN FOR THE GROUND DASH

            // 1. Determine the player's intent based on input.
            bool wantsToMoveBackward = (moveInput.x < -0.1f && isFacingRight) || (moveInput.x > 0.1f && !isFacingRight);

            // 2. We are in combat mode AND the player wants to move backward.
            if (isInCombatMode && wantsToMoveBackward)
            {
                Debug.Log("<color=orange>--- Performing GROUND BACKWARD Dash (Physics) ---</color>");
                IsBackwardDashing = true;
                animator.SetTrigger(dashBackTriggerHash);
                animator.SetBool(isMovingBackwardHash, false);
                if (groundDashClip != null) sfxSource.PlayOneShot(groundDashClip, groundDashSoundVolume);

                // Direction is OPPOSITE to facing
                float backwardDir = isFacingRight ? -1f : 1f;
                if (groundDashCoroutine != null) StopCoroutine(groundDashCoroutine);
                groundDashCoroutine = StartCoroutine(GroundDashRoutine(backwardDir, groundBackwardDashSpeed, groundBackwardDashDuration));
            }
            else
            {
                Debug.Log("<color=green>--- Performing GROUND FORWARD Dash (Physics) ---</color>");
                IsBackwardDashing = false;
                animator.SetTrigger(dashTriggerHash);
                if (groundDashClip != null) sfxSource.PlayOneShot(groundDashClip, groundDashSoundVolume);

                // Direction is the same as facing
                float forwardDir = isFacingRight ? 1f : -1f;
                float forwardSpeed = dashDistance / dashDuration;
                if (groundDashCoroutine != null) StopCoroutine(groundDashCoroutine);
                groundDashCoroutine = StartCoroutine(GroundDashRoutine(forwardDir, forwardSpeed, dashDuration));
            }
        }
        else
        {
          
            if (airDashesRemaining > 0)
            {
                PerformAirDash();
            }
        }
    }
    private IEnumerator GroundDashRoutine(float direction, float speed, float duration)
    {
        isDashing = true;
        CanMove = false;

        if (playerTrail != null) playerTrail.StartTrail();

        // Freeze vertical velocity so we don't fall mid-dash
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        float timer = 0f;
        while (timer < duration)
        {
            // Wall collision check — stop the dash early if we hit something
            if (Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer))
            {
                Debug.Log("<color=orange>Ground dash stopped by wall.</color>");
                break;
            }

            rb.linearVelocity = new Vector2(direction * speed, 0f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Cleanup
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravityScale;
        isDashing = false;
        CanMove = true;
        groundDashCoroutine = null;

        if (playerAttacks != null && !playerAttacks.isDashAttacking)
            playerAttacks.EVENT_OnDashComplete();
        Debug.Log("<color=lime>Ground dash complete.</color>");
    }
    private IEnumerator SynchronizeToRootMotion(float duration)
    {
        // Wait for one frame to ensure the animator is in the correct state.
        yield return null;

        // --- SETUP ---
        isInRootMotionState = true;
        CanMove = false;

        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        // 1. DISABLE THE RIGIDBODY.
        //    We are not just making it kinematic. We are turning it off.
        //    This tells the physics engine: "This object does not exist for you right now."
        //    This completely and utterly severs any possible interference.
        rb.simulated = false;

        // 2. CALCULATE THE ORIGINAL OFFSET.
        //    This is the vector from the parent (RootZrey) to the child (Zrey).
        //    We will maintain this exact offset for the entire duration.
        Vector3 positionOffset = transform.position - rootZreyAnimator.transform.position;
        // --- END OF FIX ---

        float timer = 0f;

        // --- EXECUTION ---
        while (timer < duration)
        {
            // We wait for the rendering frame to end, to ensure the animator has updated the transform.
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;

            // --- THE POSITION-SYNC ---
            // 3. Get the parent's new animated position.
            Vector3 parentPosition = rootZreyAnimator.transform.position;

            // 4. Set our position directly.
            //    Our new position is simply the parent's new position plus the original offset.
            //    This is a perfect, 1:1 synchronization. No deltas. No velocity. No chaos.
            transform.position = parentPosition + positionOffset;
            // --- END OF POSITION-SYNC ---
        }

        isInRootMotionState = false;
        isAttackLocked = false;
        CanMove = true;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero; // Clean slate after root motion

        if (playerAttacks != null && playerAttacks.IsAttacking())
        {
            Debug.LogWarning("Root motion ended while attack was active — force ending attack.");
            playerAttacks.EndAttack();
        }
    }
    public bool IsInRootMotionState()
{
    return isInRootMotionState;
}
    public void InitiateRootMotion(int triggerHash, float duration)
    {
        if (isInRootMotionState) return;
        StartCoroutine(StartRootMotionThenSync(triggerHash, duration));
    }

    // RENAME this coroutine.
    private IEnumerator StartRootMotionThenSync(int triggerHash, float duration)
    {
        rootZreyAnimator.SetTrigger(triggerHash);
        yield return null;
        // Call the new, correct coroutine.
        yield return StartCoroutine(SynchronizeToRootMotion(duration));
    }
    public bool IsDashing()
    {
        // The 'isDashing' variable already controls the dash state in your script.
        // We just need to expose its value to other scripts.
        return isDashing;
    }
    private void PerformAirDash()
    {
        // --- THIS IS THE GUARANTEED FIX ---
        // If an air dash is already running, KILL IT first.
        if (airDashCoroutine != null)
        {
            Debug.LogWarning("--- INTERRUPTING previous air dash! ---");
            StopCoroutine(airDashCoroutine);

          
            int playerLayer = this.gameObject.layer;
            int phaseLayer = (int)Mathf.Log(phaseThroughLayer.value, 2);
            Physics2D.IgnoreLayerCollision(playerLayer, phaseLayer, false);
        }
        // --- END OF FIX ---

        // Spend a dash charge
        airDashesRemaining--;
        DashUI_OnDashUsed();
        // Start the NEW coroutine and store its reference.
        airDashCoroutine = StartCoroutine(PhasingAirDashSequence());
        Debug.Log("Air Dashed! Remaining: " + airDashesRemaining);
    }
    private IEnumerator PhasingAirDashSequence()
    {
        // --- 1. SETUP PHASE ---
        isDashing = true; // Use the existing master dash flag
        if (airDashClip != null) sfxSource.PlayOneShot(airDashClip, airDashSoundVolume);
        float dashDuration;
        Vector2 dashVelocity;
        if (playerTrail != null)
        {
            playerTrail.StartTrail();
        }
        // Determine direction and set parameters
        float verticalInput = moveInput.y;
        if (verticalInput > 0.5f)
        {
            // UPWARD DASH
            Debug.Log("<color=cyan>--- Performing UPWARD Air Dash ---</color>");
            animator.SetTrigger(upwardAirDashTriggerHash);
            dashVelocity = Vector2.up * upwardAirDashSpeed;
            dashDuration = upwardAirDashDuration;
        }
        else
        {
            // FORWARD DASH
            Debug.Log("<color=cyan>--- Performing FORWARD Air Dash ---</color>");
            animator.SetTrigger(forwardAirDashTriggerHash);
            float direction = isFacingRight ? 1f : -1f;
            dashVelocity = new Vector2(forwardAirDashSpeed * direction, 0);
            dashDuration = forwardAirDashDuration;
        }

  

        // THIS IS THE MAGIC: Turn OFF collisions with the phasing layer.
        int playerLayer = this.gameObject.layer;
        int phaseLayer = (int)Mathf.Log(phaseThroughLayer.value, 2);
        Physics2D.IgnoreLayerCollision(playerLayer, phaseLayer, true);
        Debug.LogWarning($"PHASING ON: Ignoring collisions between layer {playerLayer} and {phaseLayer}.");

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.linearVelocity = dashVelocity; // Constantly apply the dash velocity
            timer += Time.deltaTime;
            yield return null;
        }

        // --- 3. CLEANUP PHASE ---
     
        rb.linearVelocity = Vector2.zero; // Stop instantly after the dash
        isDashing = false; // We are no longer dashing

        // THIS IS THE MAGIC: Turn collisions back ON.
        Physics2D.IgnoreLayerCollision(playerLayer, phaseLayer, false);
        Debug.LogWarning("PHASING OFF: Collisions restored.");
        airDashCoroutine = null;
    }
    public void SetGravityScaleToZero()
    {
        if (rb == null) return;
        Debug.Log("<color=cyan>--- GRAVITY SCALE: 0 (Set by Animation Event) ---</color>");
        rb.gravityScale = 0f;
    }
    private void StartFootsteps()
    {
        if (footstepSource == null || footstepClip == null) return;
        if (!footstepSource.isPlaying)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.volume = footstepVolume;
            footstepSource.Play();
        }
    }

    private void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
    }
    /// <summary>
    /// Called by an Animation Event to restore the player's original gravity scale.
    /// </summary>
    public void RestoreOriginalGravity()
    {
        if (rb == null) return;
        Debug.Log("<color=green>--- GRAVITY SCALE: Restored (Set by Animation Event) ---</color>");
        rb.gravityScale = originalGravityScale;
    }
    public void EVENT_SpawnDashParticles()
    {
        // Failsafe check is still correct.
        if (dashParticlePrefab == null || dashParticleSpawnPoint == null)
        {
            Debug.LogWarning("Cannot spawn dash particles: Prefab or Spawn Point is not assigned in the Inspector!");
            return;
        }

        Instantiate(dashParticlePrefab, dashParticleSpawnPoint.position, dashParticlePrefab.transform.rotation);
        
    }
    private void PerformJump()
    {
        if (isDashing) { DisableDash(); }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        animator.SetTrigger(jumpTriggerHash);
        jumpBufferCounter = 0f;

        // SOUNDS
        StopFootsteps();
        if (jumpClip != null) sfxSource.PlayOneShot(jumpClip, jumpSoundVolume);
    }
    private void PerformWallJump()
    {
        if (playerAttacks != null && playerAttacks.IsInCinematicState) { return; }

        Debug.Log("PERFORMING DYNAMIC WALL JUMP!");

        if (wallJumpCoroutine != null) StopCoroutine(wallJumpCoroutine);

        // Kill wall state FIRST — must happen before anything touches rb.linearVelocity
        isWallSliding = false;
        wallStickCounter = 0f;
        animator.SetBool(isWallSlidingBoolHash, false);
        animator.SetBool(isFallingHash, false);
        // Restore gravity immediately — HandleWallMechanics may have zeroed it
        rb.gravityScale = originalGravityScale;

        // Set lock flags immediately so FixedUpdate respects them this frame
        wallJumpInputLocked = true;
        justWallJumped = true;

        // Clean velocity slate
        rb.linearVelocity = Vector2.zero;

        // Apply jump force
        float jumpDirectionX = isFacingRight ? -1f : 1f;
        rb.linearVelocity = new Vector2(wallJumpForce.x * jumpDirectionX, wallJumpForce.y);

        Debug.Log($"Wall jump velocity applied: {rb.linearVelocity}");

        animator.SetTrigger(wallJumpTriggerHash);
        if (wallJumpClip != null) sfxSource.PlayOneShot(wallJumpClip, wallJumpSoundVolume);
        Flip();

        wallJumpCoroutine = StartCoroutine(WallJumpInputLock());
    }

    private IEnumerator WallJumpInputLock()
    {
        yield return new WaitForSeconds(wallJumpInputLockTime);
        wallJumpInputLocked = false;
        Debug.Log("Wall jump air control is now available.");

      
    }
    private void HandleWallMechanics()
    {
        if (isDashing) return;
        if (playerAttacks != null && playerAttacks.IsInCinematicState) return;

        // ADD: always check wall contact regardless of input lock
        bool currentlyTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);

        // REPLACE the old wallJumpInputLocked return with this:
        // Don't skip wall detection even when input is locked —
        // only skip the JUMP INPUT, not the wall CONTACT detection
        if (wallJumpInputLocked)
        {
            // Still update wall touch so we don't miss re-contact
            isTouchingWall = currentlyTouchingWall;

            // If player returned to ANY wall while input-locked, clear justWallJumped
            // so wall stick can engage immediately on landing back
            if (currentlyTouchingWall && !isGrounded)
            {
                justWallJumped = false;
                wallJumpInputLocked = false; // wall contact overrides the lock
                wallStickCounter = 0f;
                animator.SetTrigger(touchWallTriggerHash);
            }
            // Don't return here — fall through to wall slide logic below
            // so the animator and gravity are correctly set even while locked
            if (wallJumpInputLocked) return; // only return if lock wasn't cleared above
        }

        bool wasTouchingWall = isTouchingWall;
        isTouchingWall = currentlyTouchingWall; // use the value we already computed

        // REPLACE the old justWallJumped block:
        if (justWallJumped)
        {
            if (currentlyTouchingWall && !isGrounded)
            {
                // Hit a wall — clear post-jump state immediately
                justWallJumped = false;
                wallStickCounter = 0f;
                animator.SetBool(isWallSlidingBoolHash, false); // ADD: reset slide bool cleanly
                animator.SetTrigger(touchWallTriggerHash);
                Debug.Log("Returned to wall. Wall jump lock OFF.");
            }
            else
            {
                return;
            }
        }

        // Touch trigger — only on fresh contact
        if (!wasTouchingWall && isTouchingWall && !isGrounded)
        {
            animator.SetTrigger(touchWallTriggerHash);
            wallStickCounter = 0f;
            animator.SetBool(isWallSlidingBoolHash, false); // ADD: ensure clean state on contact
            Debug.Log("TOUCH WALL TRIGGER FIRED!");
        }

        // Wall slide state
        if (isTouchingWall && !isGrounded && !justWallJumped)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }

        if (isWallSliding)
        {
            wallStickCounter += Time.deltaTime;

            if (wallStickCounter < wallStickTime)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.gravityScale = originalGravityScale;
                animator.SetBool(isWallSlidingBoolHash, true);

                float timeSinceStickEnd = wallStickCounter - wallStickTime;
                float accelerationProgress = Mathf.Clamp01(timeSinceStickEnd / wallSlideAccelerationTime);
                float currentSlideSpeed = Mathf.Lerp(minWallSlideSpeed, maxWallSlideSpeed, accelerationProgress);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -currentSlideSpeed);
            }
        }
        else
        {
            rb.gravityScale = originalGravityScale;
            wallStickCounter = 0;
            animator.SetBool(isWallSlidingBoolHash, false);
        }
    }
    private void HandleMovementAnimation()
    {
        if (isAutoRunning)
        {
            animator.SetBool(isRunningHash, true);
            StartFootsteps();
            return;
        }

        bool shouldRun = moveInput.x != 0 && !isInCombatMode;
        animator.SetBool(isRunningHash, shouldRun);

        if (shouldRun && isGrounded && !isDashing)
            StartFootsteps();
        else
            StopFootsteps();
    }
    private void HandleCombatAndAnimation()
    {
        if (isAutoRunning)
        {
            animator.SetBool(isRunningHash, true);
            animator.SetBool(isMovingForwardHash, false);
            animator.SetBool(isMovingBackwardHash, false);
            animator.SetBool(isFallingHash, false);
            return; // skip everything else — nothing should override run during auto-run
        }
        if (!isGrounded && combatRunSource.isPlaying)
            combatRunSource.Stop();
        if (isAttackLocked)
        {
            // If we are attacking, we must ensure all movement booleans are OFF.
            // This prevents the animator from getting confused.
            animator.SetBool(isRunningHash, false);
            animator.SetBool(isMovingForwardHash, false);
            animator.SetBool(isMovingBackwardHash, false);
            return;
        }
        animator.SetBool(isChangingDirectionBoolHash, false);
        if (!isInCombatMode) { animator.SetBool(isMovingForwardHash, false); animator.SetBool(isMovingBackwardHash, false); }
        // --- Combat Detection ---
        Collider2D[] enemiesInRange = Physics2D.OverlapBoxAll(transform.position, combatDetectionBoxSize, 0f, enemyLayer);

        if (enemiesInRange.Length > 0)
        {
            // --- ENTER/UPDATE COMBAT MODE ---
            int validEnemyCount = 0;
            foreach (Collider2D enemyCollider in enemiesInRange)
            {
                KnightHealth kh = enemyCollider.GetComponentInParent<KnightHealth>();
                if (kh != null && kh.isFinishable) continue;
                SpearHealth sh = enemyCollider.GetComponentInParent<SpearHealth>();
                if (sh != null && sh.isFinishable) continue;
                ReaperHealth rh = enemyCollider.GetComponentInParent<ReaperHealth>();
                if (rh != null && rh.isFinishable) continue;
                validEnemyCount++;
            }

            if (validEnemyCount > 0)
            {
                // --- ENTER/UPDATE COMBAT MODE ---
                if (!isInCombatMode)
                {
                    isInCombatMode = true;
                    animator.SetBool(combatModeBoolHash, true);
                }

                // Find and lock on to the closest NON-finishable enemy
                Transform closestEnemy = null;
                float minDistance = float.MaxValue;
                foreach (Collider2D enemyCollider in enemiesInRange)
                {
                    KnightHealth kh = enemyCollider.GetComponentInParent<KnightHealth>();
                    if (kh != null && kh.isFinishable) continue;
                    SpearHealth sh = enemyCollider.GetComponentInParent<SpearHealth>();
                    if (sh != null && sh.isFinishable) continue;
                    ReaperHealth rh = enemyCollider.GetComponentInParent<ReaperHealth>();
                    if (rh != null && rh.isFinishable) continue;

                    float distance = Vector2.Distance(transform.position, enemyCollider.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestEnemy = enemyCollider.transform;
                    }
                }
                lockedOnTarget = closestEnemy;
            }

            // --- THIS IS THE FINAL, GUARANTEED ANIMATION FIX ---
            bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
            bool wasMovingForward = animator.GetBool(isMovingForwardHash);
            bool wasMovingBackward = animator.GetBool(isMovingBackwardHash);
            if (isMoving)
            {
                bool isMovingTowardsEnemy = (Mathf.Sign(moveInput.x) == Mathf.Sign(lockedOnTarget.position.x - transform.position.x));

                // --- THE VETO LOGIC ---
                // Did we just switch from backward to forward?
                if (isMovingTowardsEnemy && wasMovingBackward)
                {
                    animator.SetBool(isChangingDirectionBoolHash, true);
                }
                // Did we just switch from forward to backward?
                else if (!isMovingTowardsEnemy && wasMovingForward)
                {
                    animator.SetBool(isChangingDirectionBoolHash, true);
                }
                // --- END VETO LOGIC ---

                animator.SetBool(isMovingForwardHash, isMovingTowardsEnemy);
                animator.SetBool(isMovingBackwardHash, !isMovingTowardsEnemy);
                AudioClip desiredClip = isMovingTowardsEnemy ? combatRunForwardClip : combatRunBackwardClip;
                if (desiredClip != null && isGrounded)
                {
                    if (combatRunSource.clip != desiredClip || !combatRunSource.isPlaying)
                    {
                        combatRunSource.Stop();
                        combatRunSource.clip = desiredClip;
                        combatRunSource.volume = combatRunSoundVolume;
                        combatRunSource.Play();
                    }
                }
            }
            else
            {
                combatRunSource.Stop();
                // If not moving, both are false.
                animator.SetBool(isMovingForwardHash, false);
                animator.SetBool(isMovingBackwardHash, false);
            }
            if (!isAutoRunning)
                animator.SetBool(isRunningHash, false);
        }
        else
        {
            // --- EXIT COMBAT / NORMAL MOVEMENT LOGIC ---
            if (isInCombatMode)
            {
                isInCombatMode = false;
                lockedOnTarget = null;
                animator.SetBool(isMovingForwardHash, false);
                animator.SetBool(isMovingBackwardHash, false);
                animator.SetBool(combatModeBoolHash, false);
                animator.SetTrigger(exitCombatTriggerHash);
                combatRunSource.Stop();
            }

            // Always force-clear these regardless, every frame, when no enemies exist
            animator.SetBool(isMovingForwardHash, false);
            animator.SetBool(isMovingBackwardHash, false);

            if (!isGrounded) return;
            // Handle normal running animation ONLY when not in combat.
            animator.SetBool(isRunningHash, Mathf.Abs(moveInput.x) > 0.1f && isGrounded);
        }
        
        // Handle airborne animation universally.
        animator.SetBool(isFallingHash, !isGrounded && rb.linearVelocity.y < 0);
    }
    public void ForceExitCombatMode()
    {
        if (!isInCombatMode) return;

        isInCombatMode = false;
        lockedOnTarget = null;
        animator.SetBool(combatModeBoolHash, false);
        animator.SetBool(isMovingForwardHash, false);
        animator.SetBool(isMovingBackwardHash, false);
        animator.SetTrigger(exitCombatTriggerHash);
        combatRunSource.Stop();
        Debug.Log("<color=lime>Combat mode force-exited after finisher.</color>");
    }
    // --- STEP 5: THE PUBLIC METHOD FOR THE ATTACK SCRIPT ---
    // Your ZreyAttacks script will call this.
    public void SetAttacking(bool attacking)
    {
        isAttackLocked = attacking;
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
    public void ForceFaceDirection(bool shouldFaceRight)
    {
        // 1. Check if a flip is actually needed.
        //    - If we need to face right BUT we are currently facing left...
        //    - OR if we need to face left BUT we are currently facing right...
        if (shouldFaceRight != isFacingRight)
        {
            // 2. If a flip is needed, call the existing Flip() method.
            Flip();
        }
        // If no flip is needed, this method does nothing, which is efficient.
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
    public bool IsFacingRight()
    {
        return isFacingRight;
    }
    private void FlipChildObjects(float newXScale)
    {
        if (objectsToFlip == null || objectsToFlip.Length == 0) return;
        foreach (GameObject obj in objectsToFlip)
        {
            obj.transform.localScale = new Vector3(newXScale, obj.transform.localScale.y, obj.transform.localScale.z);
        }
    }
    public void LockFlip()
    {
        canFlip = false;
        Debug.Log("<color=red>FLIP LOCKED</color>");
        CanMove = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        // --- THIS IS THE FIX ---
        // 1. If a previous watchdog is somehow still running, stop it.
        if (flipLockWatchdogCoroutine != null)
        {
            StopCoroutine(flipLockWatchdogCoroutine);
        }
        // 2. Start the NEW watchdog timer.
        flipLockWatchdogCoroutine = StartCoroutine(FlipLockWatchdogRoutine());
        // --- END OF FIX ---
    }
    public void UnlockFlip()
    {
        // --- THIS IS THE FIX ---
        // If the animation finished cleanly, we don't need the watchdog anymore.
        // Stop it so it doesn't run unnecessarily.
        if (flipLockWatchdogCoroutine != null)
        {
            StopCoroutine(flipLockWatchdogCoroutine);
            flipLockWatchdogCoroutine = null;
        }
        // --- END OF FIX ---

        canFlip = true;
        CanMove = true;
        Debug.Log("<color=green>FLIP UNLOCKED (Cleanly by Animation Event)</color>");
    }
    private IEnumerator FlipLockWatchdogRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        // Only force unlock if we're genuinely stuck in a normal state
        if (!canFlip
            && !isDashing
            && !isInRootMotionState
            && !isHanging
            && !isWallSliding
            && (playerAttacks == null || !playerAttacks.IsInCinematicState)
            && (playerHealth == null || !playerHealth.IsGrabbed))
        {
            Debug.LogWarning("<color=orange>FLIP LOCK TIMEOUT: Force unlocking.</color>");
            canFlip = true;
            CanMove = true;
        }

        flipLockWatchdogCoroutine = null;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
    public void ForceResetState()
    {
        // Unlock all state flags
        CanMove = true;
        canFlip = true;
        isDashing = false;
        isInRootMotionState = false;
        wallJumpInputLocked = false;
        justWallJumped = false;
        isHanging = false;
        isWallSliding = false;
        hasGrappleMomentum = false;

        // Reset Physics
        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = originalGravityScale;
            rb.linearVelocity = Vector2.zero; // CRITICAL: Start from a clean slate
        }


        // Stop any lingering coroutines in this script
        StopAllCoroutines();
    }
    public void UpdateVolume(float masterVolume)
    {
        jumpSoundVolume = masterVolume;
        landSoundVolume = masterVolume;
        groundDashSoundVolume = masterVolume;
        airDashSoundVolume = masterVolume;
        footstepVolume = masterVolume;
        wallJumpSoundVolume = masterVolume;      // ? add this
        combatRunSoundVolume = masterVolume;
        // Also update the footstep source live if it's currently playing
        if (footstepSource != null)
            footstepSource.volume = footstepVolume;
        if (combatRunSource != null)
            combatRunSource.volume = combatRunSoundVolume;
    }

    public void ForceAutoRun(bool facingRight)
    {
        isAutoRunning = true;
        autoRunDirection = facingRight ? 1f : -1f;
        ForceFaceDirection(facingRight);
        // ADD THESE — lock all player agency
        CanMove = false;
        canFlip = false;
    }

    public void StopAutoRun()
    {
        isAutoRunning = false;
        autoRunDirection = 0f;
        moveInput = Vector2.zero;
        CanMove = true;
        canFlip = true;
        justStoppedAutoRun = true; // ADD — blocks HandleCombatAndAnimation for one frame
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        animator.SetBool(isRunningHash, false);
        animator.SetBool(isMovingForwardHash, false);
        animator.SetBool(isMovingBackwardHash, false);
    }
    /// <summary>
    /// Forces the combat-walk animation on without needing real enemies nearby.
    /// Call this at the START of an auto-run cutscene.
    /// </summary>
    public void ForceEnterCombatRunAnimation(bool facingRight)
    {
        // Set the animator bools directly — bypasses the enemy-detection requirement
        animator.SetBool(combatModeBoolHash, true);
        animator.SetBool(isRunningHash, false);
        animator.SetBool(isMovingForwardHash, true);
        animator.SetBool(isMovingBackwardHash, false);
    }

    /// <summary>
    /// Clears the forced combat-walk animation.
    /// Call this when the auto-run stops.
    /// </summary>
    public void ForceExitCombatRunAnimation()
    {
        animator.SetBool(isMovingForwardHash, false);
        animator.SetBool(isMovingBackwardHash, false);
        
        animator.SetBool(isRunningHash, false);
        combatRunSource.Stop();
    }
    private void SetFacingInternal(bool facingRight)
    {
        if (facingRight != isFacingRight) Flip();
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, combatDetectionBoxSize);

        // Also draw a line to the locked-on target for debugging
        if (isInCombatMode && lockedOnTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, lockedOnTarget.position);
        }
    }
}
