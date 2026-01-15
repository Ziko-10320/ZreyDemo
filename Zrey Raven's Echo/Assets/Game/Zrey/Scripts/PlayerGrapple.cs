// PlayerGrapple.cs (FINAL CORRECTED VERSION)
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private InputSystem_Actions inputActions;
    private Animator animator;

    [Header("Grapple Mechanics")]
    [SerializeField] private float maxGrappleRange = 20f;
    [SerializeField] private float grappleWindUpTime = 0.2f;
    [SerializeField] private float grappleZipSpeed = 25f;
    [SerializeField] private AnimationCurve zipToHangCurve;
    [SerializeField] private float commitDistance = 2.0f;
    [SerializeField] private float momentumBoostForce = 45f;
    [SerializeField] private float momentumOverrideDuration = 0.6f;
    [SerializeField] private float jumpOffForce = 15f;

    [Header("Chain Animation Settings")]
    [SerializeField] private Transform chainStartPoint;
    [SerializeField] private float chainExtendSpeed = 50f;
    [SerializeField] private float waveMagnitude = 0.5f;
    [SerializeField] private float waveSpeed = 10f;
    [SerializeField] private int chainSegments = 20;

    private GrapplePoint targetPoint;
    private Coroutine grappleCoroutine;
    private Coroutine chainAnimationCoroutine;
    private bool isCurrentlyHanging = false;

    private readonly int throwChainsTriggerHash = Animator.StringToHash("throwChains");
    private readonly int startGrappleTriggerHash = Animator.StringToHash("startGrapple");
    private readonly int isHangingBoolHash = Animator.StringToHash("isHanging");

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<ZreyMovements>();
        distanceJoint = GetComponent<DistanceJoint2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable() { inputActions.Enable(); }
    private void OnDisable() { inputActions.Disable(); }

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
            }
            return;
        }
        FindClosestGrapplePoint();
        if (inputActions.Player.Grapple.WasPressedThisFrame() && targetPoint != null)
        {
            grappleCoroutine = StartCoroutine(GrappleToPoint());
        }
    }

    private void FindClosestGrapplePoint()
    {
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
            else { targetIndicator.SetActive(false); }
        }
    }

    private IEnumerator GrappleToPoint()
    {
        // 1. Wind-up Phase: Go kinematic to freeze the player.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(grappleWindUpTime);

        // 2. Throw Phase
        animator.SetTrigger(throwChainsTriggerHash);
        lineRenderer.enabled = true;
        if (chainAnimationCoroutine != null) StopCoroutine(chainAnimationCoroutine);
        chainAnimationCoroutine = StartCoroutine(AnimateGrappleChain(targetPoint.transform.position));
        float extendDuration = Vector3.Distance(chainStartPoint.position, targetPoint.transform.position) / chainExtendSpeed;
        yield return new WaitForSeconds(extendDuration);

        // 3. Zip Phase
        animator.SetTrigger(startGrappleTriggerHash);

        bool bailedForMomentum = false;
        Vector3 startPos = transform.position;
        Vector3 targetHangPos = targetPoint.GetHangPosition();
        Vector2 correctMomentumDirection = (targetHangPos - startPos).normalized;
        float totalZipDistance = Vector3.Distance(startPos, targetHangPos);

        while (Vector3.Distance(transform.position, targetHangPos) > 0.01f)
        {
            if (Vector3.Distance(transform.position, targetHangPos) > commitDistance)
            {
                if (inputActions.Player.Jump.WasPressedThisFrame())
                {
                    bailedForMomentum = true;
                    break;
                }
            }
            float distanceCovered = Vector3.Distance(startPos, transform.position);
            float progress = Mathf.Clamp01(distanceCovered / totalZipDistance);
            float speedMultiplier = zipToHangCurve.Evaluate(progress);
            float currentSpeed = grappleZipSpeed * speedMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, targetHangPos, currentSpeed * Time.deltaTime);
            lineRenderer.SetPosition(0, transform.position);
            yield return null;
        }

        // 4. The Outcome
        if (bailedForMomentum)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Turn physics back on
            Vector2 launchDirection = (correctMomentumDirection + Vector2.up * 0.4f).normalized;
            rb.linearVelocity = launchDirection * momentumBoostForce;
            playerController.overrideMoveTimer = momentumOverrideDuration;
            playerController.hasGrappleMomentum = true;
            StopChainAnimation();
        }
        else
        {
            // --- THE FIX: THE CORRECT ORDER OF OPERATIONS FOR HANGING ---
            // 1. Set the final position.
            transform.position = targetHangPos;
            // 2. Turn physics back ON. The player is now a dynamic body at the hang position.
            rb.bodyType = RigidbodyType2D.Dynamic;
            // 3. Kill any remaining velocity from the zip.
            rb.linearVelocity = Vector2.zero;
            // 4. NOW that the player is a physics object, enable the joint to grab them.
            distanceJoint.connectedAnchor = targetPoint.transform.position;
            distanceJoint.distance = Vector2.Distance(transform.position, targetPoint.transform.position);
            distanceJoint.enabled = true;
            // 5. Finally, set the state for the other scripts.
            playerController.isHanging = true;
            isCurrentlyHanging = true;
            animator.SetBool(isHangingBoolHash, true);
            // --- END OF FIX ---
        }

        grappleCoroutine = null;
    }

    private void StopHanging()
    {
        isCurrentlyHanging = false;
        playerController.isHanging = false;
        distanceJoint.enabled = false;
        animator.SetBool(isHangingBoolHash, false);
        StopChainAnimation();
    }

    private void JumpOffHang()
    {
        StopHanging();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpOffForce);
    }

    private void StopChainAnimation()
    {
        if (chainAnimationCoroutine != null)
        {
            StopCoroutine(chainAnimationCoroutine);
            chainAnimationCoroutine = null;
        }
        lineRenderer.enabled = false;
    }

    private IEnumerator AnimateGrappleChain(Vector3 targetPosition)
    {
        lineRenderer.positionCount = chainSegments;
        float distance = Vector3.Distance(chainStartPoint.position, targetPosition);
        float duration = distance / chainExtendSpeed;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            Vector3 currentEndPoint = Vector3.Lerp(chainStartPoint.position, targetPosition, progress);
            UpdateChainPoints(currentEndPoint, targetPosition, distance);
            yield return null;
        }
        while (lineRenderer.enabled)
        {
            UpdateChainPoints(targetPosition, targetPosition, distance);
            yield return null;
        }
    }

    private void UpdateChainPoints(Vector3 currentEndPoint, Vector3 finalTarget, float totalDistance)
    {
        for (int i = 0; i < chainSegments; i++)
        {
            float segmentProgress = (float)i / (chainSegments - 1);
            Vector3 point = Vector3.Lerp(chainStartPoint.position, currentEndPoint, segmentProgress);
            Vector3 direction = (finalTarget - chainStartPoint.position).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
            float waveOffset = Mathf.Sin((segmentProgress * totalDistance) + (Time.time * waveSpeed)) * waveMagnitude;
            float falloff = Mathf.Sin(segmentProgress * Mathf.PI);
            point += perpendicular * waveOffset * falloff;
            lineRenderer.SetPosition(i, point);
        }
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
