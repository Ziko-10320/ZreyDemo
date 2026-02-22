using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Tooltip("How much this layer moves relative to the camera. 0 = doesn't move. 1 = moves with the camera.")]
    [Range(-1f, 1f)]
    public float parallaxMultiplier;

    private Transform cameraTransform;
    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    void Start()
    {
        // Find the main camera
        cameraTransform = Camera.main.transform;

        // Store the starting position of THIS parallax layer
        startPosition = transform.position;

        // Store the starting position of the CAMERA
        cameraStartPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        // 1. Calculate how far the camera has moved from its own starting point (on the X-axis only)
        float distanceMovedByCamera = cameraTransform.position.x - cameraStartPosition.x;

        // 2. Calculate how much this layer should move based on that distance and the multiplier
        float parallaxDistance = distanceMovedByCamera * parallaxMultiplier;

        // 3. The new position is simply the layer's original starting position plus the calculated parallax distance
        Vector3 newPosition = new Vector3(startPosition.x + parallaxDistance, startPosition.y, startPosition.z);

        // 4. Set the position. This ensures the Y and Z positions never change from their starting values.
        transform.position = newPosition;
    }
}
