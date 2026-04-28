using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;
public class BootTwinAttack : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    [Header("3D Model Facing")]
    [SerializeField] private Vector3 rightFacingRotation = new Vector3(0, 90, 0);
    [SerializeField] private Vector3 leftFacingRotation = new Vector3(0, -90, 0);
    [SerializeField] private Vector3 rightFacingScale = new Vector3(1, 1, 1);
    [SerializeField] private Vector3 leftFacingScale = new Vector3(1, -1, 1);

    [Header("Attack Detection")]
    [Tooltip("Centre of the attack box in LOCAL space (relative to this transform).")]
    [SerializeField] private Vector2 attackBoxOffset = new Vector2(1.2f, 0f);
    [Tooltip("Size of the attack box.")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.5f, 1.2f);
    [Tooltip("Layer(s) that count as the player.")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Detection Range")]
    [Tooltip("How close the player must be before the boot twin starts attacking.")]
  
    [SerializeField] private Transform attackPoint;

    [Header("Normal Combo Range")]
    [SerializeField] private Vector2 comboRangeBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private Vector2 comboRangeBoxSize = new Vector2(3f, 2f);

    private ImpactData currentImpactData;
    [SerializeField] private int normalComboDamage = 15;

    [Header("Close Distance Dash")]
    [SerializeField] private Vector2 closeRangeBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private Vector2 closeRangeBoxSize = new Vector2(3f, 2f);
    [SerializeField] private float closeDashSpeed = 12f;
    [SerializeField] private float closeDashMaxDuration = 1.5f;

   
    [Header("Combo Cooldown")]
    [SerializeField] private float comboCooldown = 2.5f;

    public ShakeData CameraShakeLight;
    public ShakeData CameraShakeMid;
    public ShakeData CameraShakeHeavy;

    [SerializeField] private float lungeSpeed = 8f;
    [Tooltip("How long the lunge lasts (in seconds).")]
    [SerializeField] private float lungeDuration = 0.15f;

    [SerializeField] private int playerLayerValue = 6; // Example: Change this to your actual Player layer number"
    [Tooltip("The integer value of the Enemy's layer.")]
    [SerializeField] private int enemyLayerValue = 7;

    [Header("Launch Attack")]
  
    [Header("Launch Range Box")]
    [SerializeField] private Vector2 launchRangeBoxOffset = new Vector2(5f, 0f);
    [SerializeField] private Vector2 launchRangeBoxSize = new Vector2(6f, 2f);
    [Tooltip("Speed of the launch in the X axis.")]
    [SerializeField] private float launchSpeed = 14f;
    [Tooltip("Max time the launch travels before giving up.")]
    [SerializeField] private float launchMaxDuration = 1.2f;
    [Tooltip("0 to 1 chance the enemy performs launch when conditions are met.")]
    [Range(0f, 1f)]
    [SerializeField] private float launchChance = 0.6f;
    [Tooltip("Cooldown before launch can trigger again.")]
    [SerializeField] private float launchCooldown = 4f;

    [Header("Launch Kick Box")]
    [SerializeField] private Vector2 launchKickBoxOffset = new Vector2(0.8f, 0f);
    [SerializeField] private Vector2 launchKickBoxSize = new Vector2(1.2f, 1.4f);
    [SerializeField] private int launchKickDamage = 25;

    [Header("Launch Wall Check")]
    [Tooltip("How far ahead to check for a wall during launch.")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Cinematic Zoom")]
    [SerializeField] private Transform cinematicFocusPoint;
    [SerializeField] private float cinematicZoomSize = 3f;
    [SerializeField] private float cinematicZoomSpeed = 3f;

    [Header("Launch Air Attack")]
    [SerializeField] private float airLaunchMinYDistance = 2f;
    [SerializeField] private float airLaunchMaxYDistance = 8f;
    [SerializeField] private float airLaunchSpeedX = 12f;
    [SerializeField] private float airLaunchSpeedY = 10f;
    [SerializeField] private float airLaunchMaxDuration = 1.5f;
    [Tooltip("How far in FRONT of the player the enemy stops (X offset).")]
    [SerializeField] private float airLaunchStopOffsetX = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float airLaunchChance = 0.5f;
    [SerializeField] private float airLaunchCooldown = 5f;

    [Header("Air Kick Box")]
    [SerializeField] private Vector2 airKickBoxOffset = new Vector2(0.8f, 0f);
    [SerializeField] private Vector2 airKickBoxSize = new Vector2(1.2f, 1.4f);
    [SerializeField] private int airKickDamage = 30;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Attack (Mid-Combo)")]
    [SerializeField] private float jumpWallCheckDistance = 1.2f;
    [SerializeField] private LayerMask jumpWallLayer;
    [SerializeField] private bool checkJumpWallDuringCombo = false; // toggled by animation events
    [SerializeField] private float jumpAttackCooldown = 3f;

    [Header("Backflip / Corner Retreat")]
    [SerializeField] private Transform cornerLeft;
    [SerializeField] private Transform cornerRight;
    [SerializeField] private float cornerWallCheckDistance = 2f;
    [SerializeField] private LayerMask cornerWallLayer;
    [Range(0f, 1f)]
    [SerializeField] private float backflipChance = 0.3f;
    [SerializeField] private float backflipCooldown = 5f;
    [SerializeField] private float backflipArcHeight = 4f;
    [SerializeField] private float backflipDuration = 1.2f;
    [SerializeField] private float distanceNoBackFLip = 5f;

    [Header("Rock Range Attack")]
    [SerializeField] private Transform rockSpawnPoint;
    [SerializeField] private GameObject smallRockPrefab;
    [SerializeField] private GameObject midRockPrefab;
    [SerializeField] private GameObject bigRockPrefab;
    [SerializeField] private int smallRockDamage = 8;
    [SerializeField] private int midRockDamage = 15;
    [SerializeField] private int bigRockDamage = 25;
     
    [SerializeField] private ImpactData rockImpactData;
    [SerializeField] private float rockAttackMinRange = 5f;
    [Range(0f, 1f)]
    [SerializeField] private float rockAttackChance = 0.4f;
    [SerializeField] private float rockAttackCooldown = 4f;

    [Header("Rock Aim & Spin")]
    [SerializeField] private float smallRockAimOffsetY = 0.5f;
    [SerializeField] private float midRockAimOffsetY = 0.8f;
    [SerializeField] private float bigRockAimOffsetY = 1.2f;

    [SerializeField] private float smallRockSpeed = 14f;
    [SerializeField] private float midRockSpeed = 10f;
    [SerializeField] private float bigRockSpeed = 7f;

    [SerializeField] private float smallRockSpinSpeed = 720f;
    [SerializeField] private float midRockSpinSpeed = 480f;
    [SerializeField] private float bigRockSpinSpeed = 240f;
    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private static readonly int NormalComboHash = Animator.StringToHash("NormalComboV2");

    public bool isFacingRight = true;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private bool isDamageWindowOpen = false;
    private Coroutine lungeCoroutine;

    private bool isLaunching = false;
    private float launchCooldownTimer = 0f;
    private Coroutine launchCoroutine;
    private ImpactData currentLaunchImpactData;
    private bool isLaunchDamageWindowOpen = false;
    private Camera mainCam;
    private float defaultCamSize;
    private Vector3 defaultCamPos;
    private Coroutine zoomCoroutine;
    private bool isCinematicActive = false;
    private static readonly int AnticipationLaunchHash = Animator.StringToHash("AnticipationLaunch");
    private static readonly int LaunchHash = Animator.StringToHash("Launch");
    private static readonly int LaunchKickHash = Animator.StringToHash("LaunchKick");
    private static readonly int LaunchWallHash = Animator.StringToHash("LaunchWall");

    private bool isAirLaunching = false;
    private float airLaunchCooldownTimer = 0f;
    private Coroutine airLaunchCoroutine;
    private ImpactData currentAirKickImpactData;
    private bool isAirKickDamageWindowOpen = false;

    private bool isFalling = false;
    private bool wasGrounded = true;

    private static readonly int LaunchAirHash = Animator.StringToHash("LaunchAir");
    private static readonly int AirKickLaunchHash = Animator.StringToHash("AirKickLaunch");
    private static readonly int FallingHash = Animator.StringToHash("Falling");
    private static readonly int LandingHash = Animator.StringToHash("Landing");

    private static readonly int CloseDistanceLaunchHash = Animator.StringToHash("CloseDistanceLaunch");
    private static readonly int StartComboKickHash = Animator.StringToHash("StartComboKick");
    private static readonly int AnticipationDashHash = Animator.StringToHash("AnticipationDash");

    private bool isCloseDashing = false;
    private Coroutine closeDashCoroutine;

    private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
    private float comboResumeNormalizedTime = 0f; // stores the frame we left at
    private bool isJumpAttacking = false;
    private float jumpAttackCooldownTimer = 0f;
    private bool isFlipLocked = false;

    private float backflipCooldownTimer = 0f;
    private bool isBackflipping = false;
    private Coroutine backflipCoroutine;

    private static readonly int BackJumpHash = Animator.StringToHash("BackJump");
    private static readonly int BackFallHash = Animator.StringToHash("BackFall");
    private static readonly int BackLandHash = Animator.StringToHash("BackLand");

    private float rockAttackCooldownTimer = 0f;
    private bool isRockAttacking = false;
    

    private static readonly int ThrowKickRocksHash = Animator.StringToHash("ThrowKickRocks");

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
        if (animator == null)
            animator = GetComponent<Animator>();
        mainCam = Camera.main;
        defaultCamSize = mainCam.orthographicSize;
        defaultCamPos = mainCam.transform.position;
    }

    // ✅ ADD this whole method
    private void Start()
    {
        if (player != null)
        {
            bool playerIsToRight = player.position.x > transform.position.x;
            isFacingRight = playerIsToRight;
            SetFacing(isFacingRight);
        }
    }
    private void Update()
    {
        if (player == null) return;
        if (isCinematicActive && mainCam != null && cinematicFocusPoint != null)
        {
            Vector3 follow = cinematicFocusPoint.position;
            follow.z = mainCam.transform.position.z;
            mainCam.transform.position = Vector3.Lerp(
                mainCam.transform.position, follow,
                Time.deltaTime * cinematicZoomSpeed);
        }
        FacePlayer();


        cooldownTimer -= Time.deltaTime;
        launchCooldownTimer -= Time.deltaTime;
        airLaunchCooldownTimer -= Time.deltaTime;
        jumpAttackCooldownTimer -= Time.deltaTime;
        backflipCooldownTimer -= Time.deltaTime;
        rockAttackCooldownTimer -= Time.deltaTime;

        // Ground / fall / landing state machine
        bool grounded = IsGrounded();
        if (wasGrounded && !grounded && !isAirLaunching)
        {
            isFalling = true;
            animator.SetTrigger(FallingHash);
            StartCoroutine(ResetTriggerNextFrame(FallingHash));
        }
        if (!wasGrounded && grounded && isFalling)
        {
            isFalling = false;
            animator.SetTrigger(LandingHash);
            StartCoroutine(ResetTriggerNextFrame(LandingHash));
        }
        wasGrounded = grounded;

        if (!isAttacking && !isLaunching && !isAirLaunching)
        {
            bool playerInLaunchBox = IsPlayerInLaunchRangeBox();
            float yDist = Mathf.Abs(player.position.y - transform.position.y);

            bool airLaunchReady = yDist >= airLaunchMinYDistance &&
                                  yDist <= airLaunchMaxYDistance &&
                                  airLaunchCooldownTimer <= 0f;

            bool launchReady = playerInLaunchBox && launchCooldownTimer <= 0f;
            bool comboReady = cooldownTimer <= 0f && IsPlayerInCloseRange();
            bool closeDashReady = cooldownTimer <= 0f && IsPlayerInComboRange() && !IsPlayerInCloseRange() && IsGrounded();

            if (airLaunchReady && Random.value <= airLaunchChance)
            {
                StartAirLaunch();
            }
            else if (backflipCooldownTimer <= 0f && !isBackflipping && !isAttacking && !isAirLaunching && !isLaunching && IsCornerBehindEnemy() && Random.value <= backflipChance)
            {
                StartBackflip();
            }
            else if (rockAttackCooldownTimer <= 0f && !isRockAttacking && IsEnemyAtCorner() && IsPlayerFarEnoughForRocks() && Random.value <= rockAttackChance)
            {
                StartRockAttack();
            }
            else if (comboReady)
            {
                StartNormalCombo();
            }
            else if (launchReady && closeDashReady)
            {
                if (Random.value < 0.5f) StartAnticipationLaunch();
                else StartCloseDistanceDash();
            }
            else if (launchReady)
            {
                if (Random.value <= launchChance) StartAnticipationLaunch();
            }
            else if (closeDashReady)
            {
                StartCloseDistanceDash();
            }
        }
        // Normal combo damage window
        if (isDamageWindowOpen)
        {
            Vector2 worldOffset = new Vector2(
                transform.position.x + (attackBoxOffset.x * (isFacingRight ? 1f : -1f)),
                transform.position.y + attackBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(worldOffset, attackBoxSize, 0f, playerLayer);
            if (hit != null)
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null && currentImpactData != null)
                {
                    isDamageWindowOpen = false;
                    ph.TakeDamage(normalComboDamage, transform, currentImpactData);
                }
            }
        }

        // Launch kick damage window
        if (isLaunchDamageWindowOpen)
        {
            Vector2 kickOffset = new Vector2(
                transform.position.x + (launchKickBoxOffset.x * (isFacingRight ? 1f : -1f)),
                transform.position.y + launchKickBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(kickOffset, launchKickBoxSize, 0f, playerLayer);
            if (hit != null)
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null && currentLaunchImpactData != null)
                {
                    isLaunchDamageWindowOpen = false;
                    ph.TakeDamage(launchKickDamage, transform, currentLaunchImpactData);
                }
            }
        }
        if (isAirKickDamageWindowOpen)
        {
            Vector2 airKickWorld = new Vector2(
                transform.position.x + (airKickBoxOffset.x * (isFacingRight ? 1f : -1f)),
                transform.position.y + airKickBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(airKickWorld, airKickBoxSize, 0f, playerLayer);
            if (hit != null)
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null && currentAirKickImpactData != null)
                {
                    isAirKickDamageWindowOpen = false;
                    ph.TakeDamage(airKickDamage, transform, currentAirKickImpactData);
                }
            }
        }

        if (checkJumpWallDuringCombo && !isJumpAttacking && jumpAttackCooldownTimer <= 0f && IsJumpWallNearby())
        {
            TriggerJumpAttack();
        }
    }

    // ─────────────────────────────────────────────
    //  FACING
    // ─────────────────────────────────────────────
    private void FacePlayer()
    {
        if (isFlipLocked) return;

        bool playerIsToRight = player.position.x > transform.position.x;
        isFacingRight = playerIsToRight; // always update the flag

        SetFacing(isFacingRight); // always apply, every frame
    }

    private void SetFacing(bool facingRight)
    {
        isFacingRight = facingRight;
        // ✅ localEulerAngles instead of eulerAngles
        transform.localEulerAngles = facingRight ? rightFacingRotation : leftFacingRotation;
        transform.localScale = facingRight ? rightFacingScale : leftFacingScale;
    }

    // ─────────────────────────────────────────────
    //  RANGE CHECK
    // ─────────────────────────────────────────────
    private bool IsPlayerInComboRange()
    {
        Vector2 boxCenter = new Vector2(
            transform.position.x + (comboRangeBoxOffset.x * (isFacingRight ? 1f : -1f)),
            transform.position.y + comboRangeBoxOffset.y
        );
        return Physics2D.OverlapBox(boxCenter, comboRangeBoxSize, 0f, playerLayer) != null;
    }

    private bool IsPlayerInLaunchRangeBox()
    {
        // Launch triggers when player is FAR enough — outside the min distance on X axis
        float xDist = Mathf.Abs(player.position.x - transform.position.x);
        return xDist >= launchRangeBoxOffset.x;
    }
    private bool IsPlayerInCloseRange()
    {
        Vector2 boxCenter = new Vector2(
            transform.position.x + (closeRangeBoxOffset.x * (isFacingRight ? 1f : -1f)),
            transform.position.y + closeRangeBoxOffset.y
        );
        return Physics2D.OverlapBox(boxCenter, closeRangeBoxSize, 0f, playerLayer) != null;
    }
    private bool IsPlayerFarEnoughForRocks()
    {
        float xDist = Mathf.Abs(player.position.x - transform.position.x);
        return xDist >= rockAttackMinRange;
    }

    private bool IsEnemyAtCorner()
    {
        if (cornerLeft == null || cornerRight == null) return false;
        float distLeft = Mathf.Abs(transform.position.x - cornerLeft.position.x);
        float distRight = Mathf.Abs(transform.position.x - cornerRight.position.x);
        return distLeft < distanceNoBackFLip || distRight < distanceNoBackFLip;
    }
    private bool IsGrounded()
    {
        if (groundCheckPoint == null) return true;
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer) != null;
    }
    // ─────────────────────────────────────────────
    //  COMBO TRIGGER
    // ─────────────────────────────────────────────
    private void StartNormalCombo()
    {
        isAttacking = true;
        cooldownTimer = comboCooldown;
        animator.SetTrigger(NormalComboHash);
    }

    // ─────────────────────────────────────────────
    //  ANIMATION EVENT — call this on the damage frame
    //  in the NormalCombo animation clip
    // ─────────────────────────────────────────────
    // Field
  

    // Called by Animation Event — drag any ImpactData asset in the event slot
    public void SetImpactType(ImpactData impactData)
    {
        currentImpactData = impactData;
    }

    // Called by Animation Event to OPEN the damage window
    public void StartDamage()
    {
        isDamageWindowOpen = true;
    }

    // Called by Animation Event to CLOSE the damage window
    public void StopDamage()
    {
        isDamageWindowOpen = false;
    }
    // ─────────────────────────────────────────────
    //  ANIMATION EVENT — call this at the END of
    //  the NormalCombo animation clip
    // ─────────────────────────────────────────────
    public void EVENT_ComboFinished()
    {
        isAttacking = false;

        // Re-enforce correct facing immediately so the post-combo frame doesn't flicker
        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    // ─────────────────────────────────────────────
    //  LAUNCH ATTACK
    // ─────────────────────────────────────────────
    private void StartAnticipationLaunch()
    {
        isAttacking = true;   // lock normal combo
        isLaunching = true;
        launchCooldownTimer = launchCooldown;
        animator.SetTrigger(AnticipationLaunchHash);
        // Actual launch movement starts via Animation Event: EVENT_BeginLaunch()
    }

    // Called by Animation Event at the END of AnticipationLaunch clip
    public void EVENT_BeginLaunch()
    {
        animator.SetTrigger(LaunchHash);

        if (launchCoroutine != null) StopCoroutine(launchCoroutine);
        launchCoroutine = StartCoroutine(LaunchCoroutine());
    }

    private IEnumerator LaunchCoroutine()
    {
        float timer = 0f;
        float direction = isFacingRight ? 1f : -1f;

        while (timer < launchMaxDuration)
        {
            // ── Wall check ──
            Vector2 wallCheckOrigin = new Vector2(
                transform.position.x + (wallCheckDistance * direction),
                transform.position.y
            );
            bool hitWall = Physics2D.OverlapPoint(wallCheckOrigin, wallLayer);

            if (hitWall)
            {
                StopLaunchAndPlayWall();
                yield break;
            }

            // ── Kick box check ──
            Vector2 kickOffset = new Vector2(
                transform.position.x + (launchKickBoxOffset.x * direction),
                transform.position.y + launchKickBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(kickOffset, launchKickBoxSize, 0f, playerLayer);

            if (hit != null)
            {
                StopLaunchAndKick();
                yield break;
            }

            // ── Move ──
            transform.position += new Vector3(launchSpeed * direction * Time.deltaTime, 0f, 0f);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ran out of time with no contact — treat as wall hit to recover cleanly
        StopLaunchAndPlayWall();
    }

    private void StopLaunchAndKick()
    {
        if (launchCoroutine != null)
        {
            StopCoroutine(launchCoroutine);
            launchCoroutine = null;
        }
        animator.SetTrigger(LaunchKickHash);
        // Damage will open via Animation Event: StartLaunchDamage()
        // and close via: StopLaunchDamage()
        // Combo finished via: EVENT_LaunchFinished()
    }

    private void StopLaunchAndPlayWall()
    {
        if (launchCoroutine != null)
        {
            StopCoroutine(launchCoroutine);
            launchCoroutine = null;
        }
        animator.SetTrigger(LaunchWallHash);
        // Recovery finished via: EVENT_LaunchFinished()
    }

    // ─────────────────────────────────────────────
    //  LAUNCH ANIMATION EVENTS
    // ─────────────────────────────────────────────

    // Drag an ImpactData asset in the Animation Event slot — same pattern as SetImpactType
    public void SetLaunchImpactType(ImpactData impactData)
    {
        currentLaunchImpactData = impactData;
    }

    public void StartLaunchDamage()
    {
        isLaunchDamageWindowOpen = true;
    }

    public void StopLaunchDamage()
    {
        isLaunchDamageWindowOpen = false;
    }

    // Call this at the END of both LaunchKick and LaunchWall animation clips
    public void EVENT_LaunchFinished()
    {
        isLaunching = false;
        isAttacking = false;
        isLaunchDamageWindowOpen = false;

        // Re-enforce facing immediately
        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    private void StartAirLaunch()
    {
        isAttacking = true;
        isAirLaunching = true;
        airLaunchCooldownTimer = airLaunchCooldown;
        animator.SetTrigger(LaunchAirHash);
        StartCoroutine(AirLaunchWatchdog());
    }
    private IEnumerator AirLaunchWatchdog()
    {
        yield return new WaitForSeconds(airLaunchMaxDuration + 2f);
        if (isAirLaunching)
        {
            Debug.LogWarning("[BootTwin] AirLaunch WATCHDOG triggered — force resetting.");
            ForceResetAttackState();
        }
    }
    public void EVENT_BeginAirLaunch()
    {

        if (airLaunchCoroutine != null) StopCoroutine(airLaunchCoroutine);
        airLaunchCoroutine = StartCoroutine(AirLaunchCoroutine());
    }

    private IEnumerator AirLaunchCoroutine()
    {
        float timer = 0f;
        float dirX = isFacingRight ? 1f : -1f;

        // Snapshot once — player movement after this point is ignored
        float targetX = player.position.x - (airLaunchStopOffsetX * dirX);
        float targetY = player.position.y;
 
        while (timer < airLaunchMaxDuration)
        {
            Vector3 current = transform.position;
            float moveX = Mathf.MoveTowards(current.x, targetX, airLaunchSpeedX * Time.deltaTime);
            float moveY = Mathf.MoveTowards(current.y, targetY, airLaunchSpeedY * Time.deltaTime);
            transform.position = new Vector3(moveX, moveY, current.z);

            Vector2 airKickWorld = new Vector2(
                transform.position.x + (airKickBoxOffset.x * dirX),
                transform.position.y + airKickBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(airKickWorld, airKickBoxSize, 0f, playerLayer);
            if (hit != null) { StopAirLaunchAndKick(); yield break; }

            bool reachedX = Mathf.Abs(transform.position.x - targetX) < 0.05f;
            bool reachedY = Mathf.Abs(transform.position.y - targetY) < 0.05f;
            if (reachedX && reachedY) { StopAirLaunchAndKick(); yield break; }

            timer += Time.deltaTime;
            yield return null;
        }
        StopAirLaunchAndKick();
    }

    private void StopAirLaunchAndKick()
    {
        if (airLaunchCoroutine != null)
        {
            StopCoroutine(airLaunchCoroutine);
            airLaunchCoroutine = null;
        }
        animator.SetTrigger(AirKickLaunchHash);
    }

    // Animation Event — drag ImpactData in the event slot
    public void SetAirKickImpactType(ImpactData impactData)
    {
        currentAirKickImpactData = impactData;
    }

    public void StartAirKickDamage()
    {
        isAirKickDamageWindowOpen = true;
    }

    public void StopAirKickDamage()
    {
        isAirKickDamageWindowOpen = false;
    }

    // Call at END of AirKickLaunch clip
    public void EVENT_AirLaunchFinished()
    {
        isAirLaunching = false;
        isAttacking = false;
        isAirKickDamageWindowOpen = false;
        isFalling = true;
        animator.SetTrigger(FallingHash);

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
       
    }

    private void StartCloseDistanceDash()
    {
        if (isLaunching || isAirLaunching) return;
        if (!IsGrounded()) return;
        isAttacking = true;
        isCloseDashing = true;
        cooldownTimer = comboCooldown;
        animator.SetTrigger(AnticipationDashHash); 
        StartCoroutine(CloseDashWatchdog());
    }
    private IEnumerator CloseDashWatchdog()
    {
        yield return new WaitForSeconds(closeDashMaxDuration + 3f);
        if (isCloseDashing)
        {
            Debug.LogWarning("[BootTwin] CloseDash WATCHDOG triggered — force resetting.");
            ForceResetAttackState();
        }
    }

    // Animation Event — place at END of AnticipationDash clip
    public void EVENT_BeginCloseDash()
    {
        animator.SetTrigger(CloseDistanceLaunchHash);
        if (closeDashCoroutine != null) StopCoroutine(closeDashCoroutine);
        closeDashCoroutine = StartCoroutine(CloseDashCoroutine());
    }

    private IEnumerator CloseDashCoroutine()
    {
        float timer = 0f;
        float direction = isFacingRight ? 1f : -1f;

        while (timer < closeDashMaxDuration)
        {
            if (IsPlayerInCloseRange())
            {
                closeDashCoroutine = null;
                animator.SetTrigger(StartComboKickHash);
                yield break;
            }

            transform.position += new Vector3(closeDashSpeed * direction * Time.deltaTime, 0f, 0f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Timed out — recover cleanly
        isCloseDashing = false;
        isAttacking = false;
        closeDashCoroutine = null;
    }

    // Animation Event — place at the END of StartComboKick animation
    public void EVENT_CloseDistanceKickFinished()
    {
        isCloseDashing = false;
        isAttacking = false;
        // Immediately chain into normal combo
        StartNormalCombo();
    }
    private void ForceResetAttackState()
    {
        isAttacking = false;
        isLaunching = false;
        isAirLaunching = false;
        isCloseDashing = false;
        isDamageWindowOpen = false;
        isLaunchDamageWindowOpen = false;
        isAirKickDamageWindowOpen = false;
        isFalling = false;
        isBackflipping = false;
        isRockAttacking = false;

        if (backflipCoroutine != null) { StopCoroutine(backflipCoroutine); backflipCoroutine = null; }
        if (launchCoroutine != null) { StopCoroutine(launchCoroutine); launchCoroutine = null; }
        if (airLaunchCoroutine != null) { StopCoroutine(airLaunchCoroutine); airLaunchCoroutine = null; }
        if (closeDashCoroutine != null) { StopCoroutine(closeDashCoroutine); closeDashCoroutine = null; }

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }

        Debug.LogWarning("[BootTwin] ForceResetAttackState called — all flags cleared.");
    }
    private bool IsJumpWallNearby()
    {
        float dirSign = isFacingRight ? 1f : -1f;
        Vector2 origin = new Vector2(
            transform.position.x + (jumpWallCheckDistance * dirSign),
            transform.position.y
        );
        return Physics2D.OverlapPoint(origin, jumpWallLayer);
    }
    private void TriggerJumpAttack()
    {
        checkJumpWallDuringCombo = false;
        isJumpAttacking = true;
        jumpAttackCooldownTimer = jumpAttackCooldown;
        // Save the exact normalized time of the NormalCombo animation right now
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        comboResumeNormalizedTime = stateInfo.normalizedTime % 1f; // % 1f handles looping clips

        animator.SetTrigger(JumpAttackHash);
    }

    // Animation Event — place at the END of JumpAttack clip
    public void EVENT_JumpAttackFinished()
    {
        isJumpAttacking = false;
        checkJumpWallDuringCombo = false;

        // Resume NormalCombo at the exact frame we left
        animator.Play(NormalComboHash, 0, comboResumeNormalizedTime);
    }
    public void EVENT_EnableJumpWallCheck()
    {
        if (!isJumpAttacking)
            checkJumpWallDuringCombo = true;
    }

    public void EVENT_DisableJumpWallCheck()
    {
        checkJumpWallDuringCombo = false;
    }

    private bool IsCornerBehindEnemy()
    {
        Transform behindCorner = GetCornerBehindEnemy();
        if (behindCorner == null) return false;

        // If already AT the corner, don't backflip
        float distToCorner = Mathf.Abs(transform.position.x - behindCorner.position.x);
        if (distToCorner < distanceNoBackFLip) return false; // ← tune this threshold in inspector if needed

        // Check if there's a wall behind — if already pinned, don't backflip
        float behindDir = isFacingRight ? -1f : 1f;
        Vector2 origin = new Vector2(
            transform.position.x + (cornerWallCheckDistance * behindDir),
            transform.position.y
        );
        bool wallBehind = Physics2D.OverlapPoint(origin, cornerWallLayer);
        return !wallBehind;
    }

    private Transform GetCornerBehindEnemy()
    {
        // Pick the corner that is BEHIND the enemy (opposite of facing)
        if (cornerLeft == null || cornerRight == null) return null;

        float leftDist = transform.position.x - cornerLeft.position.x;
        float rightDist = cornerRight.position.x - transform.position.x;

        if (isFacingRight)
            // Facing right → behind is left
            return cornerLeft;
        else
            // Facing left → behind is right
            return cornerRight;
    }
    private void StartBackflip()
    {
        Transform target = GetCornerBehindEnemy();
        if (target == null) return;

        isAttacking = true;
        isBackflipping = true;
        backflipCooldownTimer = backflipCooldown;
        animator.SetTrigger(BackJumpHash);
        // Movement starts via animation event EVENT_BeginBackflipArc
    }

    // Animation Event — call this at the frame you want the arc movement to begin
    public void EVENT_BeginBackflipArc()
    {
        Transform target = GetCornerBehindEnemy();
        if (target == null) { ForceResetAttackState(); return; }

        if (backflipCoroutine != null) StopCoroutine(backflipCoroutine);
        backflipCoroutine = StartCoroutine(BackflipArcCoroutine(target.position));
    }

    private IEnumerator BackflipArcCoroutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float timer = 0f;
        bool peakReached = false;
        bool fallTriggered = false;

        while (timer < backflipDuration)
        {
            float t = timer / backflipDuration; // 0 → 1

            // Horizontal — lerp straight from start to target
            float x = Mathf.Lerp(startPos.x, targetPos.x, t);

            // Vertical — parabolic arc: peaks at t=0.5
            // y = startY + height * 4t(1-t)  → classic parabola peaking at t=0.5
            float y = Mathf.Lerp(startPos.y, targetPos.y, t) + backflipArcHeight * 4f * t * (1f - t);

            transform.position = new Vector3(x, y, transform.position.z);

            // Trigger BackFall once past the peak (t > 0.5)
            if (!fallTriggered && t >= 0.5f)
            {
                fallTriggered = true;
                animator.SetTrigger(BackFallHash);
                StartCoroutine(ResetTriggerNextFrame(BackFallHash));
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Snap to target cleanly
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        // Play land animation
        animator.SetTrigger(BackLandHash);
        StartCoroutine(ResetTriggerNextFrame(BackLandHash));

        backflipCoroutine = null;
    }

    // Animation Event — call at END of BackLand clip
    public void EVENT_BackflipFinished()
    {
        isBackflipping = false;
        isAttacking = false;

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    private void StartRockAttack()
    {
        isAttacking = true;
        isRockAttacking = true;
        rockAttackCooldownTimer = rockAttackCooldown;

         

        animator.SetTrigger(ThrowKickRocksHash);
    }

    // Animation Event — call at exact frame for small rock
    public void EVENT_SpawnSmallRock()
    {
        SpawnRock(smallRockPrefab, smallRockDamage, smallRockSpeed, smallRockAimOffsetY, smallRockSpinSpeed);
    }

    public void EVENT_SpawnMidRock()
    {
        SpawnRock(midRockPrefab, midRockDamage, midRockSpeed, midRockAimOffsetY, midRockSpinSpeed);
    }

    public void EVENT_SpawnBigRock()
    {
        SpawnRock(bigRockPrefab, bigRockDamage, bigRockSpeed, bigRockAimOffsetY, bigRockSpinSpeed);
    }

    private void SpawnRock(GameObject prefab, int damage, float speed, float aimOffsetY, float spinSpeed)
    {
        if (prefab == null || rockSpawnPoint == null) return;

        GameObject rock = Instantiate(prefab, rockSpawnPoint.position, Quaternion.identity);
        RockProjectile rp = rock.GetComponent<RockProjectile>();
        if (rp != null)
            rp.Init((Vector2)player.position, speed, damage, rockImpactData, playerLayer,
                    aimOffsetY, spinSpeed, transform);
    }
    // Animation Event — place at END of ThrowKickRocks clip
    public void EVENT_RockAttackFinished()
    {
        isRockAttacking = false;
        isAttacking = false;

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    public void LockFlip()
    {
        isFlipLocked = true;
    }

    public void UnlockFlip()
    {
        isFlipLocked = false;
    }
    private IEnumerator ResetTriggerNextFrame(int triggerHash)
    {
        yield return null;
        animator.ResetTrigger(triggerHash);
    }
    public void CameraShake()
    {
        CameraShakerHandler.Shake(CameraShakeLight);
    }
    public void CameraShakeMiid()
    {
        CameraShakerHandler.Shake(CameraShakeMid);
    }
    public void CameraShakeheavy()
    {
        CameraShakerHandler.Shake(CameraShakeHeavy);
    }
    public void PerformTransformLunge()
    {
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(TransformLungeCoroutine(1f));
    }
    public void StartCinematicZoom()
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        isCinematicActive = true;
        zoomCoroutine = StartCoroutine(ZoomSizeCoroutine(cinematicZoomSize));
    }

    public void EndCinematicZoom()
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        isCinematicActive = false;
        zoomCoroutine = StartCoroutine(ZoomSizeCoroutine(defaultCamSize));
    }

    private IEnumerator ZoomSizeCoroutine(float targetSize)
    {
        while (Mathf.Abs(mainCam.orthographicSize - targetSize) > 0.01f)
        {
            mainCam.orthographicSize = Mathf.Lerp(
                mainCam.orthographicSize, targetSize,
                Time.deltaTime * cinematicZoomSpeed);
            yield return null;
        }
        mainCam.orthographicSize = targetSize;

        // If zoom ended, snap cam back to default pos
        if (!isCinematicActive)
        {
            while (Vector3.Distance(mainCam.transform.position, defaultCamPos) > 0.01f)
            {
                Vector3 target = defaultCamPos;
                target.z = mainCam.transform.position.z;
                mainCam.transform.position = Vector3.Lerp(
                    mainCam.transform.position, target,
                    Time.deltaTime * cinematicZoomSpeed);
                yield return null;
            }
            mainCam.transform.position = defaultCamPos;
        }

        zoomCoroutine = null;
    }
    public void PerformBackwardTransformLunge()
    {
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(TransformLungeCoroutine(-1f));
    }
    public void EnableCollision()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
    }
    public void DisableCollision()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
    }
    private IEnumerator TransformLungeCoroutine(float directionMultiplier)
    {
        float timer = 0f;

        // ✅ REPLACE the IsFacingPlayer logic with this
        Vector3 direction = (isFacingRight ? Vector3.right : Vector3.left) * directionMultiplier;

        while (timer < lungeDuration)
        {
            float moveStep = lungeSpeed * Time.deltaTime;
            transform.position += direction * moveStep;
            timer += Time.deltaTime;
            yield return null;
        }

        lungeCoroutine = null;
    }
    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        bool fr = Application.isPlaying ? isFacingRight : true;
        float dirSign = fr ? 1f : -1f;
        Vector3 pos = transform.position;

        // ═══════════════════════════════════════════
        //  NORMAL COMBO DAMAGE BOX  (red)
        // ═══════════════════════════════════════════
        if (attackPoint != null)
        {
            Vector3 damageBoxCenter = new Vector3(
                attackPoint.position.x + (attackBoxOffset.x * dirSign),
                attackPoint.position.y + attackBoxOffset.y,
                pos.z
            );
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
            Gizmos.DrawCube(damageBoxCenter, new Vector3(attackBoxSize.x, attackBoxSize.y, 0.05f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(damageBoxCenter, new Vector3(attackBoxSize.x, attackBoxSize.y, 0.05f));
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(
                damageBoxCenter + Vector3.up * (attackBoxSize.y * 0.5f + 0.15f),
                $"Combo Damage  {attackBoxSize.x}x{attackBoxSize.y}  dmg:{normalComboDamage}"
            );
#endif
        }

        // ═══════════════════════════════════════════
        //  COMBO RANGE BOX  (yellow)
        // ═══════════════════════════════════════════
        Vector3 comboBoxCenter = new Vector3(
            pos.x + (comboRangeBoxOffset.x * dirSign),
            pos.y + comboRangeBoxOffset.y,
            pos.z
        );
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawCube(comboBoxCenter, new Vector3(comboRangeBoxSize.x, comboRangeBoxSize.y, 0.05f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(comboBoxCenter, new Vector3(comboRangeBoxSize.x, comboRangeBoxSize.y, 0.05f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            comboBoxCenter + Vector3.up * (comboRangeBoxSize.y * 0.5f + 0.15f),
            $"Combo Range  {comboRangeBoxSize.x}x{comboRangeBoxSize.y}"
        );
#endif

        // ═══════════════════════════════════════════
        //  LAUNCH MIN X DISTANCE LINE  (cyan)
        //  Shows the threshold — enemy launches when player is beyond this
        // ═══════════════════════════════════════════
        float launchThresholdX = launchRangeBoxOffset.x;
        Vector3 launchLineTop = new Vector3(pos.x + (launchThresholdX * dirSign), pos.y + 1.5f, pos.z);
        Vector3 launchLineBottom = new Vector3(pos.x + (launchThresholdX * dirSign), pos.y - 1.5f, pos.z);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(launchLineBottom, launchLineTop);
        // Small tick marks
        Gizmos.DrawLine(launchLineTop + Vector3.left * 0.15f, launchLineTop + Vector3.right * 0.15f);
        Gizmos.DrawLine(launchLineBottom + Vector3.left * 0.15f, launchLineBottom + Vector3.right * 0.15f);
        // Arrow pointing outward from enemy
        Vector3 arrowBase = new Vector3(pos.x + (launchThresholdX * dirSign), pos.y, pos.z);
        Gizmos.DrawLine(arrowBase, arrowBase + Vector3.right * dirSign * 0.4f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(
            launchLineTop + Vector3.up * 0.1f,
            $"Launch Min X: {launchThresholdX}m  chance:{launchChance * 100f:0}%"
        );
#endif

        // ═══════════════════════════════════════════
        //  LAUNCH KICK BOX  (orange)
        // ═══════════════════════════════════════════
        Vector3 launchKickCenter = new Vector3(
            pos.x + (launchKickBoxOffset.x * dirSign),
            pos.y + launchKickBoxOffset.y,
            pos.z
        );
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawCube(launchKickCenter, new Vector3(launchKickBoxSize.x, launchKickBoxSize.y, 0.05f));
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(launchKickCenter, new Vector3(launchKickBoxSize.x, launchKickBoxSize.y, 0.05f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 1f);
        UnityEditor.Handles.Label(
            launchKickCenter + Vector3.up * (launchKickBoxSize.y * 0.5f + 0.15f),
            $"Launch Kick  {launchKickBoxSize.x}x{launchKickBoxSize.y}  dmg:{launchKickDamage}"
        );
#endif

        // ═══════════════════════════════════════════
        //  WALL CHECK  (magenta)
        // ═══════════════════════════════════════════
        Vector3 wallOrigin = new Vector3(
            pos.x + (wallCheckDistance * dirSign),
            pos.y,
            pos.z
        );
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, wallOrigin);
        Gizmos.DrawWireSphere(wallOrigin, 0.15f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.magenta;
        UnityEditor.Handles.Label(wallOrigin + Vector3.up * 0.2f, $"Wall Check {wallCheckDistance}m");
#endif

        // ═══════════════════════════════════════════
        //  AIR LAUNCH Y MIN / MAX LINES  (green / lime)
        //  Horizontal lines showing Y distance thresholds above and below enemy
        // ═══════════════════════════════════════════
        float lineHalfWidth = 2f;

        // Min Y — above enemy
        Vector3 airYMinLeft = new Vector3(pos.x - lineHalfWidth, pos.y + airLaunchMinYDistance, pos.z);
        Vector3 airYMinRight = new Vector3(pos.x + lineHalfWidth, pos.y + airLaunchMinYDistance, pos.z);
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 1f);
        Gizmos.DrawLine(airYMinLeft, airYMinRight);

        // Max Y — above enemy
        Vector3 airYMaxLeft = new Vector3(pos.x - lineHalfWidth, pos.y + airLaunchMaxYDistance, pos.z);
        Vector3 airYMaxRight = new Vector3(pos.x + lineHalfWidth, pos.y + airLaunchMaxYDistance, pos.z);
        Gizmos.color = new Color(0f, 1f, 0f, 1f);
        Gizmos.DrawLine(airYMaxLeft, airYMaxRight);

        // Vertical connector between min and max
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawLine(
            new Vector3(pos.x, pos.y + airLaunchMinYDistance, pos.z),
            new Vector3(pos.x, pos.y + airLaunchMaxYDistance, pos.z)
        );

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0.4f, 1f, 0.4f, 1f);
        UnityEditor.Handles.Label(
            airYMinRight + Vector3.right * 0.1f,
            $"Air Min Y: {airLaunchMinYDistance}m"
        );
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(
            airYMaxRight + Vector3.right * 0.1f,
            $"Air Max Y: {airLaunchMaxYDistance}m  chance:{airLaunchChance * 100f:0}%"
        );
#endif

        // ═══════════════════════════════════════════
        //  AIR KICK BOX  (purple)
        // ═══════════════════════════════════════════
        Vector3 airKickCenter = new Vector3(
            pos.x + (airKickBoxOffset.x * dirSign),
            pos.y + airKickBoxOffset.y,
            pos.z
        );
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.2f);
        Gizmos.DrawCube(airKickCenter, new Vector3(airKickBoxSize.x, airKickBoxSize.y, 0.05f));
        Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
        Gizmos.DrawWireCube(airKickCenter, new Vector3(airKickBoxSize.x, airKickBoxSize.y, 0.05f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0.5f, 0f, 1f, 1f);
        UnityEditor.Handles.Label(
            airKickCenter + Vector3.up * (airKickBoxSize.y * 0.5f + 0.15f),
            $"Air Kick  {airKickBoxSize.x}x{airKickBoxSize.y}  dmg:{airKickDamage}"
        );
#endif

        // ═══════════════════════════════════════════
        //  GROUND CHECK  (green sphere)
        // ═══════════════════════════════════════════
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                groundCheckPoint.position + Vector3.right * 0.25f,
                $"Ground Check r:{groundCheckRadius}"
            );
#endif
        }

        // CLOSE RANGE BOX (white)
        Vector3 closeBoxCenter = new Vector3(
            pos.x + (closeRangeBoxOffset.x * dirSign),
            pos.y + closeRangeBoxOffset.y,
            pos.z
        );
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        Gizmos.DrawCube(closeBoxCenter, new Vector3(closeRangeBoxSize.x, closeRangeBoxSize.y, 0.05f));
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(closeBoxCenter, new Vector3(closeRangeBoxSize.x, closeRangeBoxSize.y, 0.05f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            closeBoxCenter + Vector3.up * (closeRangeBoxSize.y * 0.5f + 0.15f),
            $"Close Range  {closeRangeBoxSize.x}x{closeRangeBoxSize.y}"
        );
#endif

        // JUMP WALL CHECK (teal)
        float jumpDirSign = Application.isPlaying ? (isFacingRight ? 1f : -1f) : 1f;
        Vector3 jumpWallOrigin = new Vector3(
            pos.x + (jumpWallCheckDistance * jumpDirSign),
            pos.y, pos.z
        );
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, jumpWallOrigin);
        Gizmos.DrawWireSphere(jumpWallOrigin, 0.12f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(jumpWallOrigin + Vector3.up * 0.2f, $"Jump Wall Check {jumpWallCheckDistance}m");
#endif

        // CORNER WALL CHECK BEHIND (pink)
        float behindDir = Application.isPlaying ? (isFacingRight ? -1f : 1f) : -1f;
        Vector3 cornerCheckOrigin = new Vector3(
            pos.x + (cornerWallCheckDistance * behindDir), pos.y, pos.z
        );
        Gizmos.color = new Color(1f, 0.4f, 0.8f, 1f);
        Gizmos.DrawLine(pos, cornerCheckOrigin);
        Gizmos.DrawWireSphere(cornerCheckOrigin, 0.12f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.4f, 0.8f, 1f);
        UnityEditor.Handles.Label(cornerCheckOrigin + Vector3.up * 0.2f, $"Corner Check {cornerWallCheckDistance}m");
#endif

        // Corner transform targets
        if (cornerLeft != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cornerLeft.position, 0.2f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(cornerLeft.position + Vector3.up * 0.3f, "Corner L");
#endif
        }
        if (cornerRight != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cornerRight.position, 0.2f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(cornerRight.position + Vector3.up * 0.3f, "Corner R");
#endif
        }
        // ROCK ATTACK MIN RANGE LINE (orange-red)
        Vector3 rockRangeLeft = new Vector3(pos.x - rockAttackMinRange, pos.y, pos.z);
        Vector3 rockRangeRight = new Vector3(pos.x + rockAttackMinRange, pos.y, pos.z);
        Gizmos.color = new Color(1f, 0.3f, 0f, 1f);
        Gizmos.DrawLine(new Vector3(pos.x - rockAttackMinRange, pos.y - 1f, pos.z),
                        new Vector3(pos.x - rockAttackMinRange, pos.y + 1f, pos.z));
        Gizmos.DrawLine(new Vector3(pos.x + rockAttackMinRange, pos.y - 1f, pos.z),
                        new Vector3(pos.x + rockAttackMinRange, pos.y + 1f, pos.z));
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0f, 1f);
        UnityEditor.Handles.Label(
            new Vector3(pos.x + rockAttackMinRange, pos.y + 1.2f, pos.z),
            $"Rock Min Range: {rockAttackMinRange}m  chance:{rockAttackChance * 100f:0}%"
        );
#endif
    }


}