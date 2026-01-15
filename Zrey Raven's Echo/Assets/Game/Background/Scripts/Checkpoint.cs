using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // This "enum" creates a dropdown menu in the Inspector for us to choose the type.
    public enum CheckpointType { Mini, Major }

    [Header("Checkpoint Settings")]
    [Tooltip("Is this a Mini checkpoint (for taking damage) or a Major one (for dying)?")]
    public CheckpointType type;
    [Tooltip("The SpriteRenderer of the checkpoint visual (e.g., the flag).")]
    public SpriteRenderer checkpointVisual;
    [Tooltip("The color the checkpoint will turn when it's inactive.")]
    public Color inactiveColor = Color.gray;
    [Tooltip("The color the checkpoint will turn when it becomes the active one.")]
    public Color activeColor = Color.yellow;
    private bool isCurrentlyActive = false;
    private CheckpointManager checkpointManager;

    void Start()
    {
        // Find the manager when the checkpoint is created.
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. If the player enters AND this checkpoint is NOT already the active one...
        if (other.CompareTag("Player") && !isCurrentlyActive)
        {
            Debug.Log($"Player touched checkpoint '{gameObject.name}'.");

            // 2. Tell the CheckpointManager to update its records.
            // We pass a reference to THIS VERY SCRIPT.
            checkpointManager.UpdateCheckpoint(transform.position, type, this);

            // 3. Mark this checkpoint as the active one and change its color.
            ActivateCheckpoint();
        }
    }

    // --- AND ADD THESE TWO NEW PUBLIC FUNCTIONS ---

    // This function will be called by the CheckpointManager to activate this checkpoint.
    public void ActivateCheckpoint()
    {
        isCurrentlyActive = true;
        if (checkpointVisual != null)
        {
            checkpointVisual.color = activeColor;
        }
    }

    // This function will be called by the CheckpointManager to deactivate this checkpoint.
    public void DeactivateCheckpoint()
    {
        isCurrentlyActive = false;
        if (checkpointVisual != null)
        {
            checkpointVisual.color = inactiveColor;
        }
    }
}
