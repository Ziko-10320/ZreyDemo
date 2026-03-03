using UnityEngine;
using UnityEngine.EventSystems; // Required for listening to UI events like clicks

// This script makes a UI element scale down on press and back up on release.
// It implements IPointerDownHandler and IPointerUpHandler to detect clicks.
public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scaling Settings")]
    [Tooltip("The target scale when the button is pressed down (e.g., 0.9).")]
    [SerializeField] private float pressedScale = 0.9f;

    [Tooltip("How fast the button scales down and back up.")]
    [SerializeField] private float scaleSpeed = 15f;

    // --- Internal Variables ---
    private Vector3 initialScale; // Stores the button's original scale
    private Vector3 targetScale;  // The scale we are currently moving towards

    // Awake is called when the script instance is being loaded.
    private void Awake()
    {
        // Store the button's starting scale so we can always return to it.
        initialScale = transform.localScale;
        // At the start, the target scale is the initial scale.
        targetScale = initialScale;
    }

    // Update is called once per frame.
    private void Update()
    {
        // Smoothly move the button's current scale towards the target scale every frame.
        // Vector3.Lerp is perfect for creating smooth transitions.
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    // This function is automatically called by Unity when the mouse is pressed down ON this UI element.
    public void OnPointerDown(PointerEventData eventData)
    {
        // When pressed, set the target scale to the smaller "pressed" scale.
        targetScale = initialScale * pressedScale;
    }

    // This function is automatically called by Unity when the mouse is released FROM this UI element.
    public void OnPointerUp(PointerEventData eventData)
    {
        // When released, set the target scale back to the original size.
        targetScale = initialScale;
    }
}
