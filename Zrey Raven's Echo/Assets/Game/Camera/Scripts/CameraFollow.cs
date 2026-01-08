// CameraFollow.cs (UPGRADED WITH LOOKAHEAD)
using UnityEngine;
using UnityEngine.InputSystem; // We need this for the new input system

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 baseOffset = new Vector3(0, 0, -10); // Renamed from "offset"

    [Header("Axis Locking")]
    [SerializeField] private bool lockYAxis = false;

    // --- NEW LOOKAHEAD SETTINGS ---
    [Header("Grapple Lookahead Settings")]
    [Tooltip("Assign the Player object here so the camera can check if it's grappling.")]
    [SerializeField] private PlayerGrapple playerGrapple;

    [Tooltip("The max distance the camera will pan when holding a direction.")]
    [SerializeField] private Vector2 lookaheadDistance = new Vector2(8f, 5f); // X and Y pan distance

    [Tooltip("How long you need to hold a key before the camera starts panning.")]
    [SerializeField] private float holdToPanTime = 0.5f;

    [Tooltip("How quickly the camera pans to the lookahead position.")]
    [SerializeField] private float panSpeed = 2f;

    // --- Private State Variables ---
    private Vector3 currentOffset; // This will be our dynamic offset
    private float holdTimer = 0f;
    private Vector2 panDirection = Vector2.zero;
    private InputSystem_Actions inputActions; // For reading W, A, S, D

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
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
        // At the start, our current offset is just the base offset
        currentOffset = baseOffset;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow script has no target assigned!");
            return;
        }

        // --- 1. HANDLE LOOKAHEAD LOGIC (Only if hanging) ---
        HandleGrappleLookahead();

        // --- 2. CALCULATE DESIRED POSITION ---
        // We now use our dynamic 'currentOffset' instead of the fixed baseOffset
        Vector3 desiredPosition = target.position + currentOffset;

        // --- 3. APPLY Y-AXIS LOCK (IF ENABLED) ---
        if (lockYAxis)
        {
            desiredPosition.y = transform.position.y;
        }

        // --- 4. SMOOTHLY MOVE THE CAMERA ---
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // --- 5. APPLY THE FINAL POSITION ---
        transform.position = smoothedPosition;
    }

    private void HandleGrappleLookahead()
    {
        // Check if the playerGrapple script exists and if the player is currently hanging
        if (playerGrapple != null && playerGrapple.IsHanging())
        {
            // Read the W, A, S, D input from the player
            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

            if (moveInput.magnitude > 0.1f) // Is the player holding a direction?
            {
                // If we just started holding a new direction, reset the timer
                if (moveInput != panDirection)
                {
                    holdTimer = 0f;
                    panDirection = moveInput;
                }

                holdTimer += Time.deltaTime;

                // If we've held the key long enough, start panning
                if (holdTimer >= holdToPanTime)
                {
                    // Calculate the target offset based on the direction and lookahead distance
                    Vector3 targetOffset = baseOffset + new Vector3(
                        panDirection.x * lookaheadDistance.x,
                        panDirection.y * lookaheadDistance.y,
                        0
                    );
                    // Smoothly move our current offset towards the target offset
                    currentOffset = Vector3.Lerp(currentOffset, targetOffset, panSpeed * Time.deltaTime);
                }
            }
            else // Player is not holding any direction
            {
                // Reset the timer and pan direction
                holdTimer = 0f;
                panDirection = Vector2.zero;
            }
        }
        else // If not hanging, or no grapple script assigned
        {
            // Always reset the timer and pan direction when not hanging
            holdTimer = 0f;
            panDirection = Vector2.zero;
        }

        // If we are not panning, always smoothly return the offset to its base position
        if (panDirection == Vector2.zero)
        {
            currentOffset = Vector3.Lerp(currentOffset, baseOffset, panSpeed * Time.deltaTime);
        }
    }

    // Public function to check the hanging state from PlayerGrapple
    public bool IsPlayerHanging()
    {
        if (playerGrapple != null)
        {
            return playerGrapple.IsHanging();
        }
        return false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
