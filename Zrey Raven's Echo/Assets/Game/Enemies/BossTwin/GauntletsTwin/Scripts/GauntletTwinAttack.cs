using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class GauntletTwinAttack : MonoBehaviour
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

    [Header("Flip With Enemy")]
    [SerializeField] private GameObject[] flipWithEnemy;

    [Header("Attack Detection")]
    [Tooltip("Centre of the attack box in LOCAL space (relative to this transform).")]
    [SerializeField] private Vector2 attackBoxOffset = new Vector2(1.2f, 0f);
    [Tooltip("Size of the attack box.")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.5f, 1.2f);
    [Tooltip("Layer(s) that count as the player.")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;

    [Header("Normal Combo")]
    [SerializeField] private Vector2 comboRangeBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private Vector2 comboRangeBoxSize = new Vector2(3f, 2f);
    [SerializeField] private int normalComboDamage = 15;

    [Header("Combo Cooldown")]
    [SerializeField] private float comboCooldown = 2.5f;

    [Header("Camera Shake")]
    public ShakeData CameraShakeLight;
    public ShakeData CameraShakeMid;
    public ShakeData CameraShakeHeavy;

    [Header("Lunge")]
    [SerializeField] private float lungeSpeed = 8f;
    [SerializeField] private float lungeDuration = 0.15f;

    [Header("Layer Collision")]
    [Tooltip("Integer value of the Player layer.")]
    [SerializeField] private int playerLayerValue = 6;
    [Tooltip("Integer value of this Enemy's layer.")]
    [SerializeField] private int enemyLayerValue = 7;

    [Header("Trail Effect")]
    [SerializeField] private float trailDuration = 0.5f;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private float meshRefreshRate = 0.05f;
    [SerializeField] private float snapshotLifetime = 0.5f;
    [SerializeField] private SkinnedMeshRenderer characterMeshRenderer;
    [SerializeField] private Transform trailSpawnParent;

    [Header("Close Distance Dash")]
    [SerializeField] private Vector2 closeRangeBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private Vector2 closeRangeBoxSize = new Vector2(3f, 2f);
    [SerializeField] private float closeDashSpeed = 12f;
    [SerializeField] private float closeDashMaxDuration = 1.5f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Air Launch Attack")]
    [SerializeField] private float airLaunchMinYDistance = 2f;
    [SerializeField] private float airLaunchMaxYDistance = 8f;
    [SerializeField] private float airLaunchSpeedX = 12f;
    [SerializeField] private float airLaunchSpeedY = 10f;
    [SerializeField] private float airLaunchMaxDuration = 1.5f;
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

    [Header("Launch Grab Attack")]
    [SerializeField] private Vector2 launchGrabBoxOffset = new Vector2(0.8f, 0f);
    [SerializeField] private Vector2 launchGrabBoxSize = new Vector2(1.2f, 1.4f);
    [SerializeField] private float launchGrabSpeed = 14f;
    [SerializeField] private float launchGrabMaxDuration = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float launchGrabChance = 0.6f;
    [SerializeField] private float launchGrabCooldown = 5f;
    [SerializeField] private float launchGrabMinRange = 5f;
    [SerializeField] private int grabDamage = 35;

    [Header("Grab Player Offset")]
    [Tooltip("Where the player is snapped to relative to the GauntletTwin on grab.")]
    [SerializeField] private Vector3 grabSnapOffset = new Vector3(1.2f, 0f, 0f);

    [Header("Grab Wall Check")]
    [SerializeField] private float grabWallCheckDistance = 0.6f;
    [SerializeField] private LayerMask grabWallLayer;

    [Header("Cinematic Zoom")]
    [SerializeField] private Transform cinematicFocusPoint;
    [SerializeField] private float cinematicZoomSize = 3f;
    [SerializeField] private float cinematicZoomSpeed = 3f;

    [Header("Backstep / Corner Retreat")]
    [SerializeField] private Transform cornerLeft;
    [SerializeField] private Transform cornerRight;
    [SerializeField] private float cornerWallCheckDistance = 2f;
    [SerializeField] private LayerMask cornerWallLayer;
    [Range(0f, 1f)]
    [SerializeField] private float backstepChance = 0.3f;
    [SerializeField] private float backstepCooldown = 5f;
    [SerializeField] private float backstepSpeed = 8f;
    [SerializeField] private float distanceNoBackstep = 5f;

    [Header("Smash Punch Attack")]
    [SerializeField] private Vector2 smashBoxOffset = new Vector2(1.5f, 0f);
    [SerializeField] private Vector2 smashBoxSize = new Vector2(2f, 1.5f);
    [SerializeField] private int smashDamagePerSecond = 20;
    [SerializeField] private float smashMinRange = 4f;
    [SerializeField] private float smashMaxRange = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float smashChance = 0.4f;
    [SerializeField] private float smashCooldown = 4f;
    [SerializeField] private ImpactData smashImpactData;

    [Header("Twin Boss Manager")]
    [SerializeField] private TwinBossManager twinBossManager;

    [Header("Phase Thresholds")]
    [Tooltip("Health % to unlock smash punch (0-1). e.g. 0.6 = 60%")]
    [SerializeField] private float smashUnlockThreshold = 0.6f;
    [Tooltip("Health % to unlock launch grab (0-1). e.g. 0.4 = 40%")]
    [SerializeField] private float launchGrabUnlockThreshold = 0.4f;

    [SerializeField] private float danceCooldown = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float danceChance = 0.3f;
    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────


    public bool isFacingRight = true;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private bool isDamageWindowOpen = false;
    private ImpactData currentImpactData;
    private GauntletTwinHealth health;

    private Coroutine lungeCoroutine;
    private bool isFlipLocked = false;
    private bool isTrailActive = false;
    private Coroutine trailCoroutine;

    // Close Dash
    private bool isCloseDashing = false;
    private Coroutine closeDashCoroutine;

    // Air Launch
    private bool isAirLaunching = false;
    private float airLaunchCooldownTimer = 0f;
    private Coroutine airLaunchCoroutine;
    private ImpactData currentAirKickImpactData;
    private bool isAirKickDamageWindowOpen = false;

    // Fall / Landing
    private bool isFalling = false;
    private bool wasGrounded = true;

  

    // Animation hashes
    private static readonly int NormalComboHash = Animator.StringToHash("NormalCombo");
    private static readonly int CloseDistanceLaunchHash = Animator.StringToHash("CloseDistanceLaunch");
    private static readonly int StartComboKickHash = Animator.StringToHash("StartComboKick");
    private static readonly int AnticipationDashHash = Animator.StringToHash("AnticipationDash");
    private static readonly int LaunchWallHash = Animator.StringToHash("LaunchWall");
    private static readonly int LaunchAirHash = Animator.StringToHash("LaunchAir");
    private static readonly int AirKickLaunchHash = Animator.StringToHash("AirKickLaunch");
    private static readonly int FallingHash = Animator.StringToHash("Falling");
    private static readonly int LandingHash = Animator.StringToHash("Landing");
    private static readonly int AnticipationLaunchGrabHash = Animator.StringToHash("AnticipationLaunch");
    private static readonly int LaunchGrabHash = Animator.StringToHash("Launch");
    private static readonly int GrabClawHash = Animator.StringToHash("GrabClaw");
    private static readonly int SpecialGrabImpactHash = Animator.StringToHash("SpecialGrabImpact");
    private static readonly int BackStepHash = Animator.StringToHash("BackStep");
    private static readonly int BackLandHash = Animator.StringToHash("BackLand");
    private static readonly int SmashPunchHash = Animator.StringToHash("SmashPunch");

    // Launch Grab
    private bool isLaunchGrabbing = false;
    private float launchGrabCooldownTimer = 0f;
    private Coroutine launchGrabCoroutine;
    private bool isGrabWindowOpen = false;
    private bool grabConnected = false;
    public bool isReleasingFromCounter = false;

    private Camera mainCam;
    private float defaultCamSize;
    private Vector3 defaultCamPos;
    private Coroutine zoomCoroutine;
    private bool isCinematicActive = false;
    private Vector3 cinematicCamVelocity = Vector3.zero;


    private float backstepCooldownTimer = 0f;
    private bool isBackstepping = false;
    private Coroutine backstepCoroutine;

    // Smash Punch
    private bool isSmashAttacking = false;
    private bool isSmashDamageActive = false;
    private float smashCooldownTimer = 0f;
    private Coroutine smashDamageCoroutine;
    private float smashDamageAccumulator = 0f;

    private bool isHeldByManager = false;

    public bool isBeingCountered = false;

    private static readonly int SyncComboHash = Animator.StringToHash("SyncCombo");
    private static readonly int DanceHash = Animator.StringToHash("Dance");
    private bool isDancing = false;
    private float heldTimer = 0f;
    private float danceCooldownTimer = 0f;
   
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
        health = GetComponent<GauntletTwinHealth>();
        // Disable collision between player and enemy layers from the start
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);

        if (animator == null)
            animator = GetComponent<Animator>();
        mainCam = Camera.main;
        defaultCamSize = mainCam.orthographicSize;
        defaultCamPos = mainCam.transform.position;

    }

    private void Start()
    {
        // Face the player immediately on spawn
        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (health != null && health.isGuardBroken && isAttacking)
        {
            ForceResetAttackState();
        }
        FacePlayer();
        if (isCinematicActive && mainCam != null && cinematicFocusPoint != null)
        {
            Vector3 target = cinematicFocusPoint.position;
            target.z = mainCam.transform.position.z;
            mainCam.transform.position = Vector3.SmoothDamp(
                mainCam.transform.position,
                target,
                ref cinematicCamVelocity,
                0.35f);
        }

        cooldownTimer -= Time.deltaTime;
        airLaunchCooldownTimer -= Time.deltaTime;
        launchGrabCooldownTimer -= Time.deltaTime;
        backstepCooldownTimer -= Time.deltaTime;
        smashCooldownTimer -= Time.deltaTime;
        danceCooldownTimer -= Time.deltaTime;

        // ── Ground / fall / landing state machine ──
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
        if (!isAttacking && !isAirLaunching
    && (health == null || !health.isGuardBroken && !isBeingCountered)
    && (health == null || !health.isBeingCountered)
    && !isBeingCountered)
        {
            float yDist = Mathf.Abs(player.position.y - transform.position.y);
            bool airLaunchReady = yDist >= airLaunchMinYDistance &&
                                  yDist <= airLaunchMaxYDistance &&
                                  airLaunchCooldownTimer <= 0f;

            if (airLaunchReady && Random.value <= airLaunchChance)
            {
                bool permitted = twinBossManager == null || twinBossManager.RequestAirLaunch(false);
                if (permitted) StartAirLaunch();
            }
        }

        // ── Attack decision ──
        if (!isAttacking && !isAirLaunching
    && !isHeldByManager                               // ← the gate
    && (health == null || !health.isGuardBroken)
    && (health == null || !health.isBeingCountered))
        {
            float yDist = Mathf.Abs(player.position.y - transform.position.y);
            bool airLaunchReady = yDist >= airLaunchMinYDistance &&
                                  yDist <= airLaunchMaxYDistance &&
                                  airLaunchCooldownTimer <= 0f;

            bool comboReady = cooldownTimer <= 0f && IsPlayerInCloseRange();
            bool closeDashReady = cooldownTimer <= 0f && IsPlayerInComboRange() && !IsPlayerInCloseRange() && IsGrounded();
            bool launchGrabReady = !isLaunchGrabbing &&
              launchGrabCooldownTimer <= 0f &&
              GetHealthPercent() <= launchGrabUnlockThreshold &&
              IsGrounded() &&
              IsPlayerInLaunchGrabRange();
            bool backstepReady = backstepCooldownTimer <= 0f &&
                     !isBackstepping &&
                     IsGrounded() &&
                     IsCornerBehindEnemy();
            bool smashReady = smashCooldownTimer <= 0f &&
        !isSmashAttacking &&
        GetHealthPercent() <= smashUnlockThreshold &&
        IsGrounded() &&
        IsPlayerInSmashRange();

           
             if (launchGrabReady && Random.value <= launchGrabChance)
            {
                StartLaunchGrab();
            }
            else if (backstepReady && Random.value <= backstepChance)
            {
                StartBackstep();
            }
            else if (smashReady && Random.value <= smashChance)
            {
                StartSmashPunch();
            }
            else if (comboReady)
            {
                StartNormalCombo();
            }
            else if (closeDashReady)
            {
                StartCloseDistanceDash();
            }
        }
        if (isHeldByManager && !isAttacking && !isAirLaunching
    && !isLaunchGrabbing && !isBackstepping && !isSmashAttacking
    && !isBeingCountered
    && (health == null || !health.isGuardBroken)
    && (health == null || !health.isBeingCountered)
    && IsGrounded() && !isDancing && danceCooldownTimer <= 0f)
        {
            if (Random.value <= danceChance)
            {
                isDancing = true;
                danceCooldownTimer = danceCooldown;
                animator.SetTrigger(DanceHash);
            }
            else
            {
                // failed the chance roll — set a small retry delay so it doesn't spam roll every frame
                danceCooldownTimer = 2f;
            }
        }
        // ── Normal combo damage window ──
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

        // ── Air kick damage window ──
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
    }

    // ─────────────────────────────────────────────
    //  FACING
    // ─────────────────────────────────────────────
    public void FacePlayer()
    {
        if (isFlipLocked) return;
        if (player == null) return;

        isFacingRight = player.position.x > transform.position.x;
        SetFacing(isFacingRight);
    }

    /// <summary>Force a facing direction even when the flip is locked, then restore the lock state.</summary>
    public void SetFacingDirect(bool facingRight)
    {
        bool wasLocked = isFlipLocked;
        isFlipLocked = false;
        SetFacing(facingRight);
        isFlipLocked = wasLocked;
    }

    private void SetFacing(bool facingRight)
    {
        isFacingRight = facingRight;
        transform.localEulerAngles = facingRight ? rightFacingRotation : leftFacingRotation;
        transform.localScale = facingRight ? rightFacingScale : leftFacingScale;

        foreach (GameObject go in flipWithEnemy)
        {
            if (go == null) continue;

            ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null)
            {
                var main = ps.main;
                main.startRotation = facingRight ? -90f * Mathf.Deg2Rad : 90f * Mathf.Deg2Rad;
            }
            else
            {
                Vector3 euler = go.transform.localEulerAngles;
                euler.y = facingRight ? 0f : 180f;
                go.transform.localEulerAngles = euler;
            }
        }
    }

    public bool IsFacingRight() => isFacingRight;

    public void LockFlip() => isFlipLocked = true;
    public void UnlockFlip() => isFlipLocked = false;

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
    private bool IsPlayerInCloseRange()
    {
        Vector2 boxCenter = new Vector2(
            transform.position.x + (closeRangeBoxOffset.x * (isFacingRight ? 1f : -1f)),
            transform.position.y + closeRangeBoxOffset.y
        );
        return Physics2D.OverlapBox(boxCenter, closeRangeBoxSize, 0f, playerLayer) != null;
    }
    private bool IsPlayerInLaunchGrabRange()
    {
        float xDist = Mathf.Abs(player.position.x - transform.position.x);
        return xDist >= launchGrabMinRange;
    }
    private bool IsPlayerInSmashRange()
    {
        float xDist = Mathf.Abs(player.position.x - transform.position.x);
        return xDist >= smashMinRange && xDist <= smashMaxRange;
    }
    private bool IsGrounded()
    {
        if (groundCheckPoint == null) return true;
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer) != null;
    }
    // ─────────────────────────────────────────────
    //  NORMAL COMBO
    // ─────────────────────────────────────────────
    private void StartNormalCombo()
    {
        isAttacking = true;
        cooldownTimer = comboCooldown;
        animator.SetTrigger(NormalComboHash);
    }

    // ── Animation Events ──

    /// <summary>Drag an ImpactData asset into the Animation Event slot.</summary>
    public void SetImpactType(ImpactData impactData)
    {
        currentImpactData = impactData;
    }

    /// <summary>Call this in the Animation Event to OPEN the damage window.</summary>
    public void StartDamage()
    {
        isDamageWindowOpen = true;
    }

    /// <summary>Call this in the Animation Event to CLOSE the damage window.</summary>
    public void StopDamage()
    {
        isDamageWindowOpen = false;
    }

    /// <summary>Place this at the END of the NormalCombo animation clip.</summary>
    public void EVENT_ComboFinished()
    {
        isAttacking = false;
        isDamageWindowOpen = false;

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    // ─────────────────────────────────────────────
    //  CLOSE DISTANCE DASH
    // ─────────────────────────────────────────────
    private void StartCloseDistanceDash()
    {
        if (isAirLaunching) return;
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
            Debug.LogWarning("[GauntletTwin] CloseDash WATCHDOG triggered — force resetting.");
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
            Vector2 wallCheckOrigin = new Vector2(
                transform.position.x + (wallCheckDistance * direction),
                transform.position.y
            );
            if (Physics2D.OverlapPoint(wallCheckOrigin, wallLayer))
            {
                closeDashCoroutine = null;
                StopCloseDashAndPlayWall();
                yield break;
            }

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

        closeDashCoroutine = null;
        StopCloseDashAndPlayWall();
    }

    private void StopCloseDashAndPlayWall()
    {
        isCloseDashing = false;
        isAttacking = false;
        animator.SetTrigger(GrabClawHash);
    }

    // Animation Event — place at END of StartComboKick clip
    public void EVENT_CloseDistanceKickFinished()
    {
        isCloseDashing = false;
        isAttacking = false;
        StartNormalCombo();
    }

    // Animation Event — place at END of LaunchWall clip (recovery)
    public void EVENT_WallRecoveryFinished()
    {
        isCloseDashing = false;
        isAttacking = false;

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    // ─────────────────────────────────────────────
    //  AIR LAUNCH
    // ─────────────────────────────────────────────
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
            Debug.LogWarning("[GauntletTwin] AirLaunch WATCHDOG triggered — force resetting.");
            ForceResetAttackState();
        }
    }

    // Animation Event — place at the frame movement should begin inside LaunchAir clip
    public void EVENT_BeginAirLaunch()
    {
        if (airLaunchCoroutine != null) StopCoroutine(airLaunchCoroutine);
        airLaunchCoroutine = StartCoroutine(AirLaunchCoroutine());
    }

    private IEnumerator AirLaunchCoroutine()
    {
        float timer = 0f;
        float dirX = isFacingRight ? 1f : -1f;
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
        if (airLaunchCoroutine != null) { StopCoroutine(airLaunchCoroutine); airLaunchCoroutine = null; }
        animator.SetTrigger(AirKickLaunchHash);
    }

    // Animation Event — drag ImpactData in the event slot
    public void SetAirKickImpactType(ImpactData impactData)
    {
        currentAirKickImpactData = impactData;
    }

    public void StartAirKickDamage() => isAirKickDamageWindowOpen = true;
    public void StopAirKickDamage() => isAirKickDamageWindowOpen = false;

    // Animation Event — place at END of AirKickLaunch clip
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

    // ─────────────────────────────────────────────
    //  LAUNCH GRAB
    // ─────────────────────────────────────────────
    private void StartLaunchGrab()
    {
        // ADD temporarily to diagnose
        Debug.Log($"[LaunchGrab] isLaunchGrabbing={isLaunchGrabbing} isAttacking={isAttacking} isBeingCountered={isBeingCountered}");

        isAttacking = true;
        isLaunchGrabbing = true;
        launchGrabCooldownTimer = launchGrabCooldown;
        animator.SetTrigger(AnticipationLaunchGrabHash);
        StartCoroutine(LaunchGrabWatchdog());
    }
    private IEnumerator LaunchGrabWatchdog()
    {
        yield return new WaitForSeconds(launchGrabMaxDuration + 3f);
        if (isLaunchGrabbing)
        {
            Debug.LogWarning("[GauntletTwin] LaunchGrab WATCHDOG triggered — force resetting.");
            ForceResetAttackState();
        }
    }

    // Animation Event — place at END of AnticipationLaunch clip
    public void EVENT_BeginLaunchGrab()
    {
        animator.SetTrigger(LaunchGrabHash);
        if (launchGrabCoroutine != null) StopCoroutine(launchGrabCoroutine);
        launchGrabCoroutine = StartCoroutine(LaunchGrabCoroutine());
    }

    private IEnumerator LaunchGrabCoroutine()
    {
        float timer = 0f;
        float direction = isFacingRight ? 1f : -1f;

        while (timer < launchGrabMaxDuration)
        {
            // ── Wall check ──
            Vector2 wallCheckOrigin = new Vector2(
                transform.position.x + (grabWallCheckDistance * direction),
                transform.position.y
            );
            if (Physics2D.OverlapPoint(wallCheckOrigin, grabWallLayer))
            {
                // Hit a wall — play GrabClaw with no target, then recover
                StopLaunchGrabCoroutine();
                animator.SetTrigger(GrabClawHash);
                yield break;
            }

            // ── Player detection box ──
            Vector2 grabBoxWorld = new Vector2(
                transform.position.x + (launchGrabBoxOffset.x * direction),
                transform.position.y + launchGrabBoxOffset.y
            );
            Collider2D hit = Physics2D.OverlapBox(grabBoxWorld, launchGrabBoxSize, 0f, playerLayer);
            if (hit != null)
            {
                StopLaunchGrabCoroutine();
                TryGrabPlayer(hit);
                yield break;
            }

            // ── Move ──
            transform.position += new Vector3(launchGrabSpeed * direction * Time.deltaTime, 0f, 0f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Timed out with no contact — play GrabClaw as a whiff and recover
        StopLaunchGrabCoroutine();
        animator.SetTrigger(GrabClawHash);
    }

    private void StopLaunchGrabCoroutine()
    {
        if (launchGrabCoroutine != null)
        {
            StopCoroutine(launchGrabCoroutine);
            launchGrabCoroutine = null;
        }
    }

    private void TryGrabPlayer(Collider2D playerCollider)
    {
        float direction = isFacingRight ? 1f : -1f;
        Vector3 snapPos = transform.position + new Vector3(
            grabSnapOffset.x * direction,
            grabSnapOffset.y,
            grabSnapOffset.z
        );
        playerCollider.transform.position = snapPos;

        // ── Facing fix ──
        // Enemy faces toward the player side — already correct from isFacingRight
        // Player must face the OPPOSITE direction (toward the enemy)
        ZreyMovements playerMovements = playerCollider.GetComponent<ZreyMovements>();
        if (playerMovements != null)
        {
            // Player faces enemy: if enemy faces right, player is to the right → player faces LEFT
            playerMovements.ForceFaceDirection(!isFacingRight);
        }

        PlayerHealth ph = playerCollider.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.GetGrabbedByGauntlet(snapPos, transform);

        grabConnected = true;
        animator.SetTrigger(GrabClawHash);
    }

    // ── Called by Animation Event to OPEN the grab damage window (inside GrabClaw clip) ──
    public void StartGrab()
    {
        if (!grabConnected) return; // whiff — no player snapped

        isGrabWindowOpen = true;

        // Fire both animations immediately — don't wait for StopGrab
        grabConnected = false;
        isGrabWindowOpen = false;

        animator.SetTrigger(SpecialGrabImpactHash);

        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.PlayGetGrabGroundAnimation();
        }
    }
    // ── Called by Animation Event to CLOSE the grab window and deal impact ──
    public void StopGrab()
    {
        isGrabWindowOpen = false;
        grabConnected = false;
    }


    // ── Animation Event — place at the damage frame of SpecialGrabImpact clip ──
    public void DealGrabDamage()
    {
        if (player == null) return;
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeGrabDamage(grabDamage);
    }
    // ── Animation Event — place at END of SpecialGrabImpact clip ──
    public void EVENT_LaunchGrabFinished()
    {
        isLaunchGrabbing = false;
        isAttacking = false;
        isGrabWindowOpen = false;
        grabConnected = false;

        // Release the player
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsGrabbed) ph.ReleaseFromGrab();
        }

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    // ── Animation Event — place at END of GrabClaw clip when it's a WHIFF (no grab connected) ──
    public void EVENT_GrabClawWhiffFinished()
    {
        isLaunchGrabbing = false;
        isAttacking = false;
        isGrabWindowOpen = false;
        grabConnected = false;

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    // ─────────────────────────────────────────────
    //  BACKSTEP
    // ─────────────────────────────────────────────
    private bool IsCornerBehindEnemy()
    {
        Transform behindCorner = GetCornerBehindEnemy();
        if (behindCorner == null) return false;

        float distToCorner = Mathf.Abs(transform.position.x - behindCorner.position.x);

        // Already AT or PAST the corner — don't backstep
        if (distToCorner < distanceNoBackstep) return false;

        // Wall already pinning us from behind — don't backstep
        float behindDir = isFacingRight ? -1f : 1f;
        Vector2 origin = new Vector2(
            transform.position.x + (cornerWallCheckDistance * behindDir),
            transform.position.y
        );
        return !Physics2D.OverlapPoint(origin, cornerWallLayer);
    }

    private Transform GetCornerBehindEnemy()
    {
        if (cornerLeft == null || cornerRight == null) return null;
        // Behind = opposite of facing
        return isFacingRight ? cornerLeft : cornerRight;
    }

    private void StartBackstep()
    {
        Transform target = GetCornerBehindEnemy();
        if (target == null) return;

        isAttacking = true;
        isBackstepping = true;
        backstepCooldownTimer = backstepCooldown;
        LockFlip();
        animator.SetTrigger(BackStepHash);

        if (backstepCoroutine != null) StopCoroutine(backstepCoroutine);
        backstepCoroutine = StartCoroutine(BackstepCoroutine(target));
    }

    private IEnumerator BackstepCoroutine(Transform targetCorner)
    {
        // Snapshot the target X once — don't track the transform live
        float targetX = targetCorner.position.x;
        float direction = isFacingRight ? -1f : 1f; // move BEHIND enemy (opposite of facing)

        while (true)
        {
            float remaining = Mathf.Abs(transform.position.x - targetX);

            // Stop if close enough
            if (remaining < 0.05f)
            {
                transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                break;
            }

            // Stop if we've overshot (moved past the corner)
            float currentDiff = (targetX - transform.position.x) * direction;
            if (currentDiff < 0f)
            {
                transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                break;
            }

            float step = backstepSpeed * Time.deltaTime;

            // Clamp the step so we never overshoot
            step = Mathf.Min(step, remaining);

            transform.position += new Vector3(step * direction, 0f, 0f);
            yield return null;
        }

        backstepCoroutine = null;
        animator.SetTrigger(BackLandHash);
    }

    // Animation Event — place at END of BackLand clip
    public void EVENT_BackstepFinished()
    {
        isBackstepping = false;
        isAttacking = false;
        UnlockFlip();

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }

    // ─────────────────────────────────────────────
    //  SMASH PUNCH
    // ─────────────────────────────────────────────
    private void StartSmashPunch()
    {
        isAttacking = true;
        isSmashAttacking = true;
        smashCooldownTimer = smashCooldown;
        animator.SetTrigger(SmashPunchHash);
    }

    // Animation Event — place at the frame the smash hits the ground
    public void StartSmashDamage()
    {
        isSmashDamageActive = true;
        if (smashDamageCoroutine != null) StopCoroutine(smashDamageCoroutine);
        smashDamageCoroutine = StartCoroutine(SmashDamageTickCoroutine());
    }

    // Animation Event — place at the frame the smash effect ends
    public void StopSmashDamage()
    {
        isSmashDamageActive = false;
        if (smashDamageCoroutine != null)
        {
            StopCoroutine(smashDamageCoroutine);
            smashDamageCoroutine = null;
        }
    }

    private IEnumerator SmashDamageTickCoroutine()
    {
        smashDamageAccumulator = 0f;
        while (isSmashDamageActive)
        {
            smashDamageAccumulator += smashDamagePerSecond * Time.deltaTime;

            if (smashDamageAccumulator >= 1f)
            {
                int tickDamage = Mathf.FloorToInt(smashDamageAccumulator);
                smashDamageAccumulator -= tickDamage;

                Vector2 worldOffset = new Vector2(
                    transform.position.x + (smashBoxOffset.x * (isFacingRight ? 1f : -1f)),
                    transform.position.y + smashBoxOffset.y
                );
                Collider2D hit = Physics2D.OverlapBox(worldOffset, smashBoxSize, 0f, playerLayer);
                if (hit != null)
                {
                    PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null && smashImpactData != null)
                        ph.TakeUnblockableButParryableDamage(tickDamage, transform, smashImpactData);
                }
            }
            yield return null;
        }
        smashDamageCoroutine = null;
    }

    // Animation Event — place at END of SmashPunch clip
    public void EVENT_SmashPunchFinished()
    {
        isSmashAttacking = false;
        isAttacking = false;
        isSmashDamageActive = false;

        if (smashDamageCoroutine != null)
        {
            StopCoroutine(smashDamageCoroutine);
            smashDamageCoroutine = null;
        }

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    // ─────────────────────────────────────────────
    //  CAMERA SHAKE  (public — usable from Animation Events)
    // ─────────────────────────────────────────────
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
    // ─────────────────────────────────────────────
    //  CINEMATIC ZOOM  (public — usable from Animation Events)
    // ─────────────────────────────────────────────
    public void StartCinematicZoom()
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        isCinematicActive = true;
        LockFlip();
        zoomCoroutine = StartCoroutine(ZoomSizeCoroutine(cinematicZoomSize));
    }

    public void EndCinematicZoom()
    {
        cinematicCamVelocity = Vector3.zero;
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        isCinematicActive = false;
        UnlockFlip();
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
    // ─────────────────────────────────────────────
    //  LUNGE  (public — usable from Animation Events)
    // ─────────────────────────────────────────────
    /// <summary>Lunge forward (in the direction the enemy is currently facing).</summary>
    public void PerformTransformLunge()
    {
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(TransformLungeCoroutine(1f));
    }

    /// <summary>Lunge backward (opposite of the facing direction).</summary>
    public void PerformBackwardTransformLunge()
    {
        if (lungeCoroutine != null) StopCoroutine(lungeCoroutine);
        lungeCoroutine = StartCoroutine(TransformLungeCoroutine(-1f));
    }

    private IEnumerator TransformLungeCoroutine(float directionMultiplier)
    {
        float timer = 0f;
        Vector3 direction = (isFacingRight ? Vector3.right : Vector3.left) * directionMultiplier;

        while (timer < lungeDuration)
        {
            transform.position += direction * (lungeSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        lungeCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  LAYER COLLISION TOGGLE  (public)
    // ─────────────────────────────────────────────
    public void EnableCollision()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, false);
    }

    public void DisableCollision()
    {
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);
    }

    // ─────────────────────────────────────────────
    //  STATE QUERIES  (public)
    // ─────────────────────────────────────────────
    public bool IsAttacking() => isAttacking;
    public bool IsLaunching()
    {
        return isLaunchGrabbing;
    }
    public bool IsAirLaunching()
    {
        return isAirLaunching;
    }
    // ─────────────────────────────────────────────
    //  FORCE RESET
    // ─────────────────────────────────────────────
    public void ForceResetAttackState()
    {
        if (health != null && health.isGuardBroken)
        {
        isAttacking = false;
        isDamageWindowOpen = false;
        isCloseDashing = false;
        isAirLaunching = false;
        isAirKickDamageWindowOpen = false;
        isFalling = false;
        isLaunchGrabbing = false;
        isGrabWindowOpen = false;
        grabConnected = false;
        isBackstepping = false;
        isSmashAttacking = false;
        isSmashDamageActive = false;
        grabConnected = false;
        isDancing = false;

            UnlockFlip();

            StopTrail();
        if (lungeCoroutine != null) { StopCoroutine(lungeCoroutine); lungeCoroutine = null; }
        if (closeDashCoroutine != null) { StopCoroutine(closeDashCoroutine); closeDashCoroutine = null; }
        if (airLaunchCoroutine != null) { StopCoroutine(airLaunchCoroutine); airLaunchCoroutine = null; }
        if (launchGrabCoroutine != null) { StopCoroutine(launchGrabCoroutine); launchGrabCoroutine = null; }
        if (backstepCoroutine != null) { StopCoroutine(backstepCoroutine); backstepCoroutine = null; }
        if (smashDamageCoroutine != null) { StopCoroutine(smashDamageCoroutine); smashDamageCoroutine = null; }
            return; // EXIT — don't touch the animator
        }
        // Also release the player if we hard-reset mid-grab:
        if (player != null && !isReleasingFromCounter)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsGrabbed) ph.ReleaseFromGrab();
        }
        isReleasingFromCounter = false;
        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }   
            animator.ResetTrigger(DanceHash);
        animator.CrossFade("Idle", 0.0f, 0);
        if (twinBossManager != null)
            twinBossManager.NotifyAttackEnded(false, forced: true);
      
        Debug.LogWarning("[GauntletTwin] ForceResetAttackState called — all flags cleared.");
    }
    private IEnumerator ResetTriggerNextFrame(int triggerHash)
    {
        yield return null;
        animator.ResetTrigger(triggerHash);
    }
    // ─────────────────────────────────────────────
    //  TRAIL EFFECT  (public — usable from Animation Events)
    // ─────────────────────────────────────────────
    public void StartTrail()
    {
        if (!isTrailActive)
        {
            if (trailCoroutine != null) StopCoroutine(trailCoroutine);
            trailCoroutine = StartCoroutine(ActivateTrailRoutine());
        }
    }

    public void StopTrail()
    {
        if (trailCoroutine != null)
        {
            StopCoroutine(trailCoroutine);
            trailCoroutine = null;
        }
        isTrailActive = false;
    }

    private IEnumerator ActivateTrailRoutine()
    {
        isTrailActive = true;
        float timer = 0f;
        while (timer < trailDuration)
        {
            CreateTrailSnapshot();
            yield return new WaitForSeconds(meshRefreshRate);
            timer += meshRefreshRate;
        }
        isTrailActive = false;
        trailCoroutine = null;
    }

    private void CreateTrailSnapshot()
    {
        if (characterMeshRenderer == null) return;

        GameObject snapshotObject = new GameObject("Trail_Snapshot");
        snapshotObject.transform.SetPositionAndRotation(
            characterMeshRenderer.transform.position,
            characterMeshRenderer.transform.rotation
        );
        if (trailSpawnParent != null) snapshotObject.transform.SetParent(trailSpawnParent);

        MeshFilter meshFilter = snapshotObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = snapshotObject.AddComponent<MeshRenderer>();

        Mesh snapshotMesh = new Mesh();
        characterMeshRenderer.BakeMesh(snapshotMesh);
        meshFilter.mesh = snapshotMesh;

        Material snapshotMaterial = new Material(trailMaterial);
        // Match the render face to the current facing direction
        snapshotMaterial.SetInt("_Cull", isFacingRight
            ? (int)UnityEngine.Rendering.CullMode.Back
            : (int)UnityEngine.Rendering.CullMode.Front);

        meshRenderer.material = snapshotMaterial;

        StartCoroutine(FadeSnapshotRoutine(snapshotMaterial));
        Destroy(snapshotObject, snapshotLifetime);
    }

    private IEnumerator FadeSnapshotRoutine(Material materialToFade)
    {
        float timeElapsed = 0f;
        int alphaPropertyID = Shader.PropertyToID("_Alpha");

        while (timeElapsed < snapshotLifetime)
        {
            timeElapsed += Time.deltaTime;
            materialToFade.SetFloat(alphaPropertyID, Mathf.Lerp(1f, 0f, timeElapsed / snapshotLifetime));
            yield return null;
        }
    }

    public void HoldAttack(bool hold)
    {
        isHeldByManager = hold;
        if (!hold)
        {
            isDancing = false;
            animator.ResetTrigger(DanceHash);
        }
    }
    public void EVENT_DanceFinished()
    {
        isDancing = false;
    }
    public void TryEvasiveRetreat()
    {
        if (isAttacking || isBackstepping || isAirLaunching || isLaunchGrabbing) return;
        if (!IsGrounded()) return;
        if (backstepCooldownTimer <= 0f && IsCornerBehindEnemy())
            StartBackstep();
    }
    private float GetHealthPercent()
    {
        if (health == null) return 1f;
        return (float)health.GetCurrentHealth() / health.GetMaxHealth();
    }
    public void PlaySyncCombo()
    {
        isAttacking = true;
        animator.SetTrigger(SyncComboHash);
    }

    // Animation Event — place at END of SyncCombo clip  
    public void EVENT_SyncComboFinished()
    {
        isAttacking = false;
        // Only one twin needs to notify the manager — boot handles it
        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }
    }
    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        bool fr = Application.isPlaying ? isFacingRight : true;
        float dirSign = fr ? 1f : -1f;
        Vector3 pos = transform.position;

        // ── Normal Combo Damage Box (red) ──
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

        // ── Combo Range Box (yellow) ──
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
        // Close Range Box (white)
        Vector3 closeBoxCenter = new Vector3(
            pos.x + (closeRangeBoxOffset.x * dirSign),
            pos.y + closeRangeBoxOffset.y, pos.z
        );
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        Gizmos.DrawCube(closeBoxCenter, new Vector3(closeRangeBoxSize.x, closeRangeBoxSize.y, 0.05f));
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(closeBoxCenter, new Vector3(closeRangeBoxSize.x, closeRangeBoxSize.y, 0.05f));

        // Air Kick Box (purple)
        Vector3 airKickCenter = new Vector3(
            pos.x + (airKickBoxOffset.x * dirSign),
            pos.y + airKickBoxOffset.y, pos.z
        );
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.2f);
        Gizmos.DrawCube(airKickCenter, new Vector3(airKickBoxSize.x, airKickBoxSize.y, 0.05f));
        Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
        Gizmos.DrawWireCube(airKickCenter, new Vector3(airKickBoxSize.x, airKickBoxSize.y, 0.05f));

        // Launch Grab Detection Box (red-orange)
        Vector3 grabBoxCenter = new Vector3(
            pos.x + (launchGrabBoxOffset.x * dirSign),
            pos.y + launchGrabBoxOffset.y, pos.z
        );
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.2f);
        Gizmos.DrawCube(grabBoxCenter, new Vector3(launchGrabBoxSize.x, launchGrabBoxSize.y, 0.05f));
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 1f);
        Gizmos.DrawWireCube(grabBoxCenter, new Vector3(launchGrabBoxSize.x, launchGrabBoxSize.y, 0.05f));
        // Corner targets
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

        // Corner wall check behind (pink)
        float backstepBehindDir = Application.isPlaying ? (isFacingRight ? -1f : 1f) : -1f;
        Vector3 cornerCheckOrigin = new Vector3(
            pos.x + (cornerWallCheckDistance * backstepBehindDir), pos.y, pos.z
        );
        Gizmos.color = new Color(1f, 0.4f, 0.8f, 1f);
        Gizmos.DrawLine(pos, cornerCheckOrigin);
        Gizmos.DrawWireSphere(cornerCheckOrigin, 0.12f);
        // Smash Punch Box (dark orange)
        Vector3 smashBoxCenter = new Vector3(
            pos.x + (smashBoxOffset.x * dirSign),
            pos.y + smashBoxOffset.y, pos.z
        );
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawCube(smashBoxCenter, new Vector3(smashBoxSize.x, smashBoxSize.y, 0.05f));
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(smashBoxCenter, new Vector3(smashBoxSize.x, smashBoxSize.y, 0.05f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 1f);
        UnityEditor.Handles.Label(
            smashBoxCenter + Vector3.up * (smashBoxSize.y * 0.5f + 0.15f),
            $"Smash Range  {smashMinRange}–{smashMaxRange}m  dps:{smashDamagePerSecond}"
        );
#endif
    }
    // Smash RUNTIME check (add inside existing OnDrawGizmosSelected or a new OnDrawGizmos)
    private void OnDrawGizmos()
    {
        if (!isSmashDamageActive) return;
        Vector2 worldOffset = new Vector2(
            (attackPoint != null ? attackPoint.position.x : transform.position.x)
                + (smashBoxOffset.x * (isFacingRight ? 1f : -1f)),
            (attackPoint != null ? attackPoint.position.y : transform.position.y)
                + smashBoxOffset.y
        );
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldOffset, smashBoxSize);
    }
}