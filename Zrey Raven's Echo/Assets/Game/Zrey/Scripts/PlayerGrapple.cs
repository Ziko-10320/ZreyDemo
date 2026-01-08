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
    [Tooltip("The VERTICAL force applied when jumping off a hang.")]
    [SerializeField] private float jumpOffVerticalForce = 15f;

    // ADD this new variable
    [Tooltip("The HORIZONTAL force applied when jumping off a hang, based on player input.")]
    [SerializeField] private float jumpOffHorizontalForce = 10f;
    [SerializeField] private float jumpOffMomentum = 7f;

    private GrapplePoint targetPoint;
    private Coroutine grappleCoroutine;
    private bool isCurrentlyHanging = false;

    [Header("Chain Animation Settings")]
    [Tooltip("The transform where the chain shoots from.")]
    [SerializeField] private Transform chainStartPoint; 

    [Tooltip("How fast the chain extends towards the target.")]
    [SerializeField] private float chainExtendSpeed = 50f; 

    [Tooltip("The magnitude of the wave effect on the chain.")]
    [SerializeField] private float waveMagnitude = 0.5f; 

    [Tooltip("How fast the wave travels along the chain.")]
    [SerializeField] private float waveSpeed = 10f; 

    [Tooltip("How many segments the chain has. More segments = smoother wave.")]
    [SerializeField] private int chainSegments = 20;
   
    // --- ADD THIS NEW VARIABLE ---
    [Tooltip("The point on the player that will attach to the hang point (e.g., the player's hands).")]
    [SerializeField] private Transform playerHangPoint;
    private readonly int throwChainsTriggerHash = Animator.StringToHash("throwChains");
    private readonly int startGrappleTriggerHash = Animator.StringToHash("startGrapple");
    private readonly int startHangBoolHash = Animator.StringToHash("isHanging");
    [HideInInspector] public bool justGrappleJumped = false;
    [HideInInspector] public bool grappleJumpInputLocked = false;
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
    private void FixedUpdate()
    {
        // If we are in the post-grapple-jump state...
        if (justGrappleJumped)
        {
            // If input is currently locked, do absolutely nothing. Let the momentum ride.
            if (grappleJumpInputLocked)
            {
                return;
            }

            // If input is NOT locked, check if the player wants to take over.
            // We read the input directly here.
            float horizontalInput = inputActions.Player.Move.ReadValue<Vector2>().x;
            if (horizontalInput != 0)
            {
                // The player is taking control. End the special momentum state.
                justGrappleJumped = false;
                // We don't need to set velocity here, because the ZreyMovements script's
                // FixedUpdate will immediately take over on the next frame.
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
        // --- 1. START ANIMATION & ZIP ---
        float directionToTarget = targetPoint.transform.position.x - transform.position.x;
        // Tell the player controller to face that direction.
        playerController.FaceDirection(directionToTarget);
        // This part is already working correctly.
        playerController.GetComponent<Animator>().SetTrigger(throwChainsTriggerHash);
        lineRenderer.enabled = true;
        grappleCoroutine = StartCoroutine(AnimateGrappleChain(targetPoint.transform.position));

        float extendDuration = Vector3.Distance(chainStartPoint.position, targetPoint.transform.position) / chainExtendSpeed;
        yield return new WaitForSeconds(extendDuration);

        playerController.GetComponent<Animator>().SetTrigger(startGrappleTriggerHash);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;

        // --- 2. THE "PRIMED JUMP" FIX ---
        // We create a new local variable to track if the player wants to jump at the end.
        bool jumpIsPrimed = false;
        // --- END OF FIX ---

        Vector3 targetHangPos = targetPoint.GetHangPosition();
        float zipDuration = Vector3.Distance(transform.position, targetHangPos) / grappleZipSpeed;
        float timer = 0f;

        // This is the main "zip" loop.
        while (timer < zipDuration)
        {
            // --- THIS IS THE CORE LOGIC CHANGE ---
            // If the player presses jump while zipping...
            if (inputActions.Player.Jump.WasPressedThisFrame())
            {
                // ...we set our flag to true. We DO NOT break the loop.
                jumpIsPrimed = true;
                Debug.Log("Grapple Jump Primed!");
            }
            // --- END OF CHANGE ---

            // The player continues to move towards the hang position, regardless of the jump press.
            transform.position = Vector3.MoveTowards(transform.position, targetHangPos, grappleZipSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // --- 3. END OF GRAPPLE ACTION ---
        // The player has reached the hang point. Now we decide what to do.

        // --- THIS IS THE FINAL DECISION ---
        // If the jump was primed during the zip...
        if (jumpIsPrimed)
        {
            // ...perform the jump-off immediately.
            Debug.Log("Executing Primed Grapple Jump!");
            JumpOffHang(); // This is your existing, working jump-off method.
        }
        else
        {
            // If the jump was NOT primed, perform the normal hang.
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            isCurrentlyHanging = true;
            playerController.GetComponent<Animator>().SetBool(startHangBoolHash, true);
        }
        // --- END OF DECISION ---
    
}

    private void StopHanging()
    {
        if (grappleCoroutine != null)
        {
            StopCoroutine(grappleCoroutine);
            grappleCoroutine = null;
        }
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        // --- THIS IS THE FIX ---
        // If we were hanging, our Rigidbody was Kinematic. We must restore it to Dynamic.
        rb.bodyType = RigidbodyType2D.Dynamic;
        // --- END OF FIX ---
        playerController.GetComponent<Animator>().SetBool(startHangBoolHash, false);
        isCurrentlyHanging = false;
        // playerController.isHanging = false; // For your other script
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;
        justGrappleJumped = false;
    }


    private void JumpOffHang()
    {
        // Get the direction the player is facing.
        float horizontalDirection = playerController.isFacingRight ? 1f : -1f;

        // Stop hanging (disables constraints, etc.).
        StopHanging();

        // Apply the guaranteed force.
        rb.velocity = Vector2.zero;
        Vector2 force = new Vector2(horizontalDirection * jumpOffHorizontalForce, jumpOffVerticalForce);
        rb.AddForce(force, ForceMode2D.Impulse);

        // --- THIS IS THE FINAL FIX ---
        // We tell the ZreyMovements script to raise its "momentum shields".
        // We are hijacking the working wall jump system.
        playerController.justWallJumped = true;
        playerController.wallJumpInputLocked = true;

        // Start a coroutine to lower the input lock shield after a delay.
        StartCoroutine(GrappleJumpInputLockRoutine());
        // --- END OF FIX ---
    }
    private IEnumerator GrappleJumpInputLockRoutine()
    {
        // Wait for the lock duration.
        yield return new WaitForSeconds(0.2f); // Use a value similar to your wall jump lock time.

        // After the timer, tell ZreyMovements to unlock the input.
        // Its FixedUpdate will then allow the player to take over momentum.
        playerController.wallJumpInputLocked = false;
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

    private IEnumerator AnimateGrappleChain(Vector3 targetPosition)
    {
        lineRenderer.positionCount = chainSegments;
        float distance = Vector3.Distance(chainStartPoint.position, targetPosition);
        float duration = distance / chainExtendSpeed;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration); // How far along the extension we are (0 to 1)

            // Calculate the current end point of the extending chain
            Vector3 currentEndPoint = Vector3.Lerp(chainStartPoint.position, targetPosition, progress);

            for (int i = 0; i < chainSegments; i++)
            {
                float segmentProgress = (float)i / (chainSegments - 1);
                Vector3 point = Vector3.Lerp(chainStartPoint.position, currentEndPoint, segmentProgress);

                // --- THE WAVE/WOBBLE EFFECT ---
                // Calculate the perpendicular direction for the wave
                Vector3 direction = (currentEndPoint - chainStartPoint.position).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

                // Add a sine wave offset
                float waveOffset = Mathf.Sin((segmentProgress * distance) + (Time.time * waveSpeed)) * waveMagnitude;

                // Apply the offset, but make it weaker at the start and end of the chain
                float falloff = Mathf.Sin(segmentProgress * Mathf.PI); // This is 0 at the start/end, 1 in the middle
                point += perpendicular * waveOffset * falloff;

                lineRenderer.SetPosition(i, point);
            }

            yield return null;
        }

        // After extending, keep the chain live and wavy
        while (lineRenderer.enabled)
        {
            for (int i = 0; i < chainSegments; i++)
            {
                float segmentProgress = (float)i / (chainSegments - 1);
                Vector3 point = Vector3.Lerp(chainStartPoint.position, targetPosition, segmentProgress);

                Vector3 direction = (targetPosition - chainStartPoint.position).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
                float waveOffset = Mathf.Sin((segmentProgress * distance) + (Time.time * waveSpeed)) * waveMagnitude;
                float falloff = Mathf.Sin(segmentProgress * Mathf.PI);
                point += perpendicular * waveOffset * falloff;

                lineRenderer.SetPosition(i, point);
            }
            yield return null;
        }
    }
}
