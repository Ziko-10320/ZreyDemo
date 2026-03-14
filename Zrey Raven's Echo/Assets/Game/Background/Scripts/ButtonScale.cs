using UnityEngine;
using UnityEngine.EventSystems; // Required for listening to all UI pointer events

// This script now handles both hover and press scaling for a UI element.
public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scaling Settings")]
    [Tooltip("The target scale when the mouse hovers over the button (e.g., 1.1 for 10% bigger).")]
    [SerializeField] private float hoverScale = 1.1f; // --- NEW ---

    [Tooltip("The target scale when the button is pressed down (e.g., 0.9 for 10% smaller).")]
    [SerializeField] private float pressedScale = 0.9f;

    [Tooltip("How fast the button scales down and back up.")]
    [SerializeField] private float scaleSpeed = 15f;
    [Header("Sound Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.75f;

    private AudioSource audioSource;
    // --- Internal Variables ---
    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool isPointerOver = false; // --- NEW: A flag to track if the mouse is currently over the button.

    private void Awake()
    {
        initialScale = transform.localScale;
        targetScale = initialScale;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // Force fully 2D
        audioSource.dopplerLevel = 0f;
    }

    private void Update()
    {
        // The Lerp function remains the same, smoothly moving towards the target scale.
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    // --- This function is automatically called when the mouse is PRESSED DOWN ---
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = initialScale * pressedScale;
        if (clickSound != null) audioSource.PlayOneShot(clickSound, soundVolume);
    }

    // --- This function is automatically called when the mouse is RELEASED ---
    public void OnPointerUp(PointerEventData eventData)
    {
        // When released, we need to decide what scale to return to.
        // If the pointer is still over the button, return to the hover scale.
        if (isPointerOver)
        {
            targetScale = initialScale * hoverScale;
        }
        // Otherwise, return to the normal scale.
        else
        {
            targetScale = initialScale;
        }
    }

    // --- NEW: This function is automatically called when the mouse ENTERS the button's area ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        targetScale = initialScale * hoverScale;
        if (hoverSound != null) audioSource.PlayOneShot(hoverSound, soundVolume);
    }

    // --- NEW: This function is automatically called when the mouse EXITS the button's area ---
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        // Set the target back to the normal initial scale.
        targetScale = initialScale;
    }
}
