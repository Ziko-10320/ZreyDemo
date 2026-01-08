// PlayerGrapple.cs (FINAL, UPGRADED FOR UNITY 6)
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // For the new Input System

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ZreyMovements))]
[RequireComponent(typeof(DistanceJoint2D))]
public class PlayerGrapple : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask grappleableLayer;
    [SerializeField] private GameObject targetIndicator;

    private Rigidbody2D rb;
    private ZreyMovements playerController;
    private DistanceJoint2D distanceJoint;
    private InputSystem_Actions inputActions; // For new input
    [Header("Grapple Lookahead Settings")]
    [Tooltip("Assign the Player object here so the camera can check if it's grappling.")]
    [SerializeField] private PlayerGrapple playerGrapple;
    [Header("Grapple Mechanics")]
    [SerializeField] private float maxGrappleRange = 20f;
    [SerializeField] private float grappleWindUpTime = 0.2f;
    [SerializeField] private float grappleZipSpeed = 25f;
    [SerializeField] private float momentumBoostForce = 45f;
    [SerializeField] private float momentumOverrideDuration = 0.6f;
    [SerializeField] private float jumpOffForce = 15f;

    private GrapplePoint targetPoint;
    private Coroutine grappleCoroutine;
    private bool isCurrentlyHanging = false;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<ZreyMovements>();
        distanceJoint = GetComponent<DistanceJoint2D>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;
        if (targetIndicator != null) targetIndicator.SetActive(false);
    }

    void Update()
    {
        if (isCurrentlyHanging || grappleCoroutine != null)
        {
            if (targetIndicator != null) targetIndicator.SetActive(false);

            if (isCurrentlyHanging)
            {
                if (inputActions.Player.Jump.WasPressedThisFrame()) JumpOffHang();
                else if (inputActions.Player.Grapple.WasPressedThisFrame()) StopHanging();
                if (lineRenderer.enabled) lineRenderer.SetPosition(0, transform.position);
            }
            return;
        }

        FindClosestGrapplePoint();

        if (inputActions.Player.Grapple.WasPressedThisFrame())
        {
            if (targetPoint != null)
            {
                grappleCoroutine = StartCoroutine(GrappleToPoint());
            }
        }
    }

    private void FindClosestGrapplePoint()
    {
        // ... (This logic is unchanged)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, maxGrappleRange, grappleableLayer);
        GrapplePoint bestTarget = null;
        float closestDist = float.MaxValue;
        foreach (var col in colliders)
        {
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = col.GetComponent<GrapplePoint>();
            }
        }
        targetPoint = bestTarget;
        if (targetIndicator != null)
        {
            if (targetPoint != null)
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = targetPoint.transform.position;
            }
            else
            {
                targetIndicator.SetActive(false);
            }
        }
    }

    private IEnumerator GrappleToPoint()
    {
        // --- FIX: Use bodyType instead of isKinematic ---
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero; // Use linearVelocity

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, targetPoint.transform.position);

        yield return new WaitForSeconds(grappleWindUpTime);

        bool bailedForMomentum = false;
        Vector3 targetHangPos = targetPoint.GetHangPosition();
        Vector2 correctMomentumDirection = (targetHangPos - transform.position).normalized;
        float zipDuration = Vector3.Distance(transform.position, targetHangPos) / grappleZipSpeed;
        float timer = 0f;

        while (timer < zipDuration)
        {
            if (inputActions.Player.Jump.WasPressedThisFrame())
            {
                bailedForMomentum = true;
                break;
            }
            transform.position = Vector3.MoveTowards(transform.position, targetHangPos, grappleZipSpeed * Time.deltaTime);
            lineRenderer.SetPosition(0, transform.position);
            timer += Time.deltaTime;
            yield return null;
        }

        // --- FIX: Use bodyType ---
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (bailedForMomentum)
        {
            Vector2 launchDirection = (correctMomentumDirection + Vector2.up * 0.4f).normalized;
            rb.linearVelocity = launchDirection * momentumBoostForce; // Use linearVelocity
            playerController.overrideMoveTimer = momentumOverrideDuration;
            lineRenderer.enabled = false;
        }
        else
        {
            transform.position = targetHangPos;
            rb.linearVelocity = Vector2.zero; // Use linearVelocity
            distanceJoint.connectedAnchor = targetPoint.transform.position;
            distanceJoint.distance = Vector2.Distance(transform.position, targetPoint.transform.position);
            distanceJoint.enabled = true;
            playerController.isHanging = true;
            isCurrentlyHanging = true;
        }

        grappleCoroutine = null;
    }

    private void StopHanging()
    {
        isCurrentlyHanging = false;
        playerController.isHanging = false;
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;
    }

    private void JumpOffHang()
    {
        StopHanging();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpOffForce); // Use linearVelocity
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxGrappleRange);
    }
    public bool IsHanging()
    {
        return isCurrentlyHanging;
    }
}
