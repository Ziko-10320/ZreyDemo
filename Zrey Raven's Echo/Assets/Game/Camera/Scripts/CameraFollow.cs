// CameraFollow.cs (FINAL UPGRADE - DUAL INPUT PANNING)
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 baseOffset = new Vector3(0, 0, -10);

    [Header("Axis Locking")]
    [SerializeField] private bool lockYAxis = false;

    [Header("Lookahead Settings")]
    [Tooltip("Assign the Player object here so the camera can check if it's grappling.")]
    [SerializeField] private PlayerGrapple playerGrapple;
    [Tooltip("The max distance the camera will pan when holding a direction.")]
    [SerializeField] private Vector2 lookaheadDistance = new Vector2(8f, 5f);
    [Tooltip("How long you need to hold a movement key (WASD) while hanging before the camera pans.")]
    [SerializeField] private float holdToPanTime = 0.5f;
    [Tooltip("How quickly the camera pans to the lookahead position and returns to center.")]
    [SerializeField] private float panSpeed = 2f;

    // --- Private State Variables ---
    private Vector3 currentOffset;
    private float holdTimer = 0f;
    private Vector2 panDirection = Vector2.zero;
    private InputSystem_Actions inputActions;

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
        currentOffset = baseOffset;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow script has no target assigned!");
            return;
        }

        // --- 1. HANDLE ALL CAMERA PANNING LOGIC ---
        HandleCameraPan();

        // --- 2. CALCULATE DESIRED POSITION ---
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

    // --- THIS IS THE NEW, UNIFIED FUNCTION ---
    private void HandleCameraPan()
    {
        // Read both input actions every frame.
        Vector2 manualLookInput = inputActions.Player.Look.ReadValue<Vector2>();
        Vector2 movementInput = inputActions.Player.Move.ReadValue<Vector2>();

        Vector2 desiredPanDirection = Vector2.zero;

        // --- PRIORITY 1: MANUAL LOOK (ARROW KEYS) ---
        // If the player is using the arrow keys, this ALWAYS takes priority.
        if (manualLookInput.magnitude > 0.1f)
        {
            desiredPanDirection = manualLookInput;
            // We don't need a hold timer for manual look, it should be instant.
            holdTimer = holdToPanTime;
        }
        // --- PRIORITY 2: HANGING LOOKAHEAD (WASD) ---
        // Else, if the player is hanging and using movement keys...
        else if (playerGrapple != null && playerGrapple.IsHanging() && movementInput.magnitude > 0.1f)
        {
            // If we just started holding a new direction, reset the timer.
            if (movementInput != panDirection)
            {
                holdTimer = 0f;
            }
            holdTimer += Time.deltaTime;

            // Only set the pan direction if the hold time is met.
            if (holdTimer >= holdToPanTime)
            {
                desiredPanDirection = movementInput;
            }
            panDirection = movementInput; // Store the current input regardless of timer.
        }
        else
        {
            // If neither condition is met, reset everything.
            holdTimer = 0f;
            panDirection = Vector2.zero;
        }

        // --- APPLY THE PANNING ---
        // If we have a direction to pan in...
        if (desiredPanDirection.magnitude > 0.1f)
        {
            // Calculate the target offset.
            Vector3 targetOffset = baseOffset + new Vector3(
                desiredPanDirection.x * lookaheadDistance.x,
                desiredPanDirection.y * lookaheadDistance.y,
                0
            );
            // Smoothly move towards it.
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, panSpeed * Time.deltaTime);
        }
        else // Otherwise, return to center.
        {
            currentOffset = Vector3.Lerp(currentOffset, baseOffset, panSpeed * Time.deltaTime);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
