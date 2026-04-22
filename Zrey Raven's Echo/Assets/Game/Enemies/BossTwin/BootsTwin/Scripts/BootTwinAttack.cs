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
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private Transform attackPoint;
   
    private ImpactData currentImpactData;
    [SerializeField] private int normalComboDamage = 15;

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
    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private static readonly int NormalComboHash = Animator.StringToHash("NormalCombo");

    public bool isFacingRight = true;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private bool isDamageWindowOpen = false;
    private Coroutine lungeCoroutine;
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

        // ❌ REMOVE the SetFacing call from here
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

        FacePlayer();

        cooldownTimer -= Time.deltaTime;

        if (!isAttacking && cooldownTimer <= 0f && IsPlayerInAttackRange())
        {
            StartNormalCombo();
        }
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
    private void FacePlayer()
    {
        // ✅ Don't flip mid-attack
        if (isAttacking) return;

        bool playerIsToRight = player.position.x > transform.position.x;

        if (playerIsToRight && !isFacingRight)
            SetFacing(true);
        else if (!playerIsToRight && isFacingRight)
            SetFacing(false);
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
    private bool IsPlayerInAttackRange()
    {
        return Vector2.Distance(transform.position, player.position) <= attackRange;
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
        // ── Attack Range Circle ──
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // yellow transparent fill
        DrawWireCircle2D(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        DrawWireCircle2D(transform.position, attackRange);

        // ── Damage Box ──
        bool fr = Application.isPlaying ? isFacingRight : true;

        Vector2 worldOffset = new Vector2(
            attackPoint.position.x + (attackBoxOffset.x * (fr ? 1f : -1f)),
            attackPoint.position.y + attackBoxOffset.y
        );

        // Solid fill
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
        Gizmos.DrawCube(new Vector3(worldOffset.x, worldOffset.y, transform.position.z),
                        new Vector3(attackBoxSize.x, attackBoxSize.y, 0.05f));

        // Outline
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(worldOffset.x, worldOffset.y, transform.position.z),
                            new Vector3(attackBoxSize.x, attackBoxSize.y, 0.05f));

        // ── Labels (Scene view only) ──
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (attackRange + 0.2f),
            $"Attack Range: {attackRange}m"
        );

        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(
            new Vector3(worldOffset.x, worldOffset.y + attackBoxSize.y * 0.5f + 0.15f, 0f),
            $"Damage Box  {attackBoxSize.x}x{attackBoxSize.y}  dmg:{normalComboDamage}"
        );
#endif
    }

    // Helper — draws a 2D circle approximation with Gizmos.DrawLine
    private static void DrawWireCircle2D(Vector3 center, float radius, int segments = 36)
    {
        float angleStep = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}