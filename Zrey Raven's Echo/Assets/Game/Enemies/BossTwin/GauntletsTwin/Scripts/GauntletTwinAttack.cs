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

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private static readonly int NormalComboHash = Animator.StringToHash("NormalCombo");

    public bool isFacingRight = true;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private bool isDamageWindowOpen = false;
    private ImpactData currentImpactData;

    private Coroutine lungeCoroutine;
    private bool isFlipLocked = false;
    private bool isTrailActive = false;
    private Coroutine trailCoroutine;


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

        // Disable collision between player and enemy layers from the start
        Physics2D.IgnoreLayerCollision(playerLayerValue, enemyLayerValue, true);

        if (animator == null)
            animator = GetComponent<Animator>();

       
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

        // If guard-broken mid-attack, force reset
        if (  isAttacking)
        {
            ForceResetAttackState();
        }

        // Always face the player (unless flip-locked)
        FacePlayer();

        // Tick cooldown
        cooldownTimer -= Time.deltaTime;

        // ── Try normal combo ──
        if (!isAttacking && cooldownTimer <= 0f && IsPlayerInComboRange())
        {
            StartNormalCombo();
        }

        // ── Damage window polling ──
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

    // ─────────────────────────────────────────────
    //  FORCE RESET
    // ─────────────────────────────────────────────
    public void ForceResetAttackState()
    {
        isAttacking = false;
        isDamageWindowOpen = false;
        StopTrail();
        if (lungeCoroutine != null) { StopCoroutine(lungeCoroutine); lungeCoroutine = null; }

        if (player != null)
        {
            isFacingRight = player.position.x > transform.position.x;
            SetFacing(isFacingRight);
        }

        Debug.LogWarning("[GauntletTwin] ForceResetAttackState called — all flags cleared.");
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
    }
}