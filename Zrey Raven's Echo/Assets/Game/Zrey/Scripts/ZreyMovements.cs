using UnityEngine;
using UnityEngine.InputSystem; // IMPORTANT: We need to add this line!

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class ZreyMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float runSpeed = 5f;

    [Header("Flipping Logic")]
    [SerializeField] private Vector3 rightFacingRotation = new Vector3(0, 90, 0);
    [SerializeField] private Vector3 leftFacingRotation = new Vector3(0, -90, 0);
    [SerializeField] private Vector3 rightFacingScale = new Vector3(1, 1, 1);
    [SerializeField] private Vector3 leftFacingScale = new Vector3(1, -1, 1);
    [SerializeField] private GameObject[] objectsToFlip;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    // --- New Input System Variables ---
    private InputSystem_Actions playerControls; // A variable to hold our generated controls class
    private Vector2 moveInput; // A Vector2 to store the input data (x, y)

    // --- Private State Variables ---
    private bool isFacingRight = true;

    // --- Animation Hashes ---
    private readonly int isRunningHash = Animator.StringToHash("isRunning");

    // Awake is called first
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        // --- Initialize the New Input System ---
        playerControls = new InputSystem_Actions();
    }

    // OnEnable is called when the object becomes enabled and active
    private void OnEnable()
    {
        playerControls.Enable();
    }

    // OnDisable is called when the object becomes disabled or inactive
    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        // --- 1. GATHER INPUT (New Way) ---
        // Read the value from the "Move" action we created.
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        // --- 2. HANDLE ANIMATION ---
        // We check the X component of the input vector.
        if (moveInput.x != 0)
        {
            animator.SetBool(isRunningHash, true);
        }
        else
        {
            animator.SetBool(isRunningHash, false);
        }

        // --- 3. HANDLE FLIPPING ---
        Flip();
    }

    // FixedUpdate is for physics
    void FixedUpdate()
    {
        // --- 4. APPLY MOVEMENT (New Way) ---
        // We use the X component of our moveInput vector.
        rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        // The logic is the same, but we use moveInput.x instead of horizontalInput.
        if (moveInput.x < 0 && isFacingRight)
        {
            transform.localRotation = Quaternion.Euler(leftFacingRotation);
            transform.localScale = leftFacingScale;
            isFacingRight = false;
            FlipChildObjects(-1f);
        }
        else if (moveInput.x > 0 && !isFacingRight)
        {
            transform.localRotation = Quaternion.Euler(rightFacingRotation);
            transform.localScale = rightFacingScale;
            isFacingRight = true;
            FlipChildObjects(1f);
        }
    }
    private void FlipChildObjects(float newXScale)
    {
        // If the array is empty or not assigned, do nothing.
        if (objectsToFlip == null || objectsToFlip.Length == 0)
        {
            return;
        }

        // Loop through every GameObject in the array.
        foreach (GameObject obj in objectsToFlip)
        {
            // Get the current scale of the object.
            Vector3 currentScale = obj.transform.localScale;

            // Set its new scale, only changing the X value.
            obj.transform.localScale = new Vector3(newXScale, currentScale.y, currentScale.z);
        }
    }
}
