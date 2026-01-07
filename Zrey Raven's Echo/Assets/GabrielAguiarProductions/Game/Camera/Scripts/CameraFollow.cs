using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player or object for the camera to follow. Assign your player here.")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("How smoothly the camera follows the target. Lower values are slower and smoother, higher values are faster and snappier.")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothSpeed = 0.125f;

    [Tooltip("The offset from the target. For a 2D game, you'll want to keep Z at -10 or another negative value to see the scene.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Axis Locking")]
    [Tooltip("Check this box to prevent the camera from following the player on the Y-axis.")]
    [SerializeField] private bool lockYAxis = false;

    // LateUpdate is called once per frame, after all Update functions have been called.
    // This is the best place for camera logic, as it ensures the target has already
    // finished its movement for the frame.
    void LateUpdate()
    {
        // --- 1. CHECK FOR TARGET ---
        // If no target is assigned, do nothing. This prevents errors.
        if (target == null)
        {
            Debug.LogWarning("CameraFollow script has no target assigned!");
            return;
        }

        // --- 2. CALCULATE DESIRED POSITION ---
        // This is the position where we want the camera to be.
        Vector3 desiredPosition = target.position + offset;

        // --- 3. APPLY Y-AXIS LOCK (IF ENABLED) ---
        // This is the core of your requested feature.
        if (lockYAxis)
        {
            // If the Y-axis is locked, we override the desired Y position.
            // We force it to be the camera's current Y position, so it never moves vertically.
            desiredPosition.y = transform.position.y;
        }

        // --- 4. SMOOTHLY MOVE THE CAMERA ---
        // Instead of instantly teleporting the camera (transform.position = desiredPosition),
        // we use Vector3.Lerp (Linear Interpolation) to move it smoothly over time.
        // It moves a fraction of the distance (defined by smoothSpeed) each frame.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // --- 5. APPLY THE FINAL POSITION ---
        transform.position = smoothedPosition;
    }

    // This is a public function so other scripts (like a GameManager) could change the target at runtime if needed.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
