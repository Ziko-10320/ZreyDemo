using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag the Player GameObject here from your scene.")]
    public GameObject player;
    private Checkpoint lastMiniCheckpointScript;
    private Checkpoint lastMajorCheckpointScript;

    // These will store the positions of the last activated checkpoints.
    private Vector3 lastMiniCheckpointPos;
    private Vector3 lastMajorCheckpointPos;

    void Start()
    {
        // Safety check
        if (player == null)
        {
            Debug.LogError("Player has not been assigned in the CheckpointManager Inspector!");
            return;
        }

        // At the start of the game, the player's starting position is considered
        // the first "major" checkpoint.
        lastMajorCheckpointPos = player.transform.position;
        // The mini checkpoint can also be the same at the start.
        lastMiniCheckpointPos = player.transform.position;

        Debug.Log("CheckpointManager initialized. Starting position saved as initial checkpoint.");
    }

    // This function will be called by the Checkpoint objects themselves.
    public void UpdateCheckpoint(Vector3 newPosition, Checkpoint.CheckpointType type, Checkpoint checkpointScript)
    {
        if (type == Checkpoint.CheckpointType.Mini)
        {
            // If there was a PREVIOUS mini checkpoint, tell it to become inactive.
            if (lastMiniCheckpointScript != null)
            {
                lastMiniCheckpointScript.DeactivateCheckpoint();
            }

            // Now, store the new position AND the new script reference.
            lastMiniCheckpointPos = newPosition;
            lastMiniCheckpointScript = checkpointScript;
            Debug.Log($"New MINI checkpoint set at: {newPosition}");
        }
        else if (type == Checkpoint.CheckpointType.Major)
        {
            // Same logic for the major checkpoint.
            if (lastMajorCheckpointScript != null)
            {
                lastMajorCheckpointScript.DeactivateCheckpoint();
            }
            lastMajorCheckpointPos = newPosition;
            lastMajorCheckpointScript = checkpointScript;

            // When you hit a major checkpoint, it should ALSO update your mini checkpoint.
            lastMiniCheckpointPos = newPosition;
            if (lastMiniCheckpointScript != null)
            {
                lastMiniCheckpointScript.DeactivateCheckpoint();
            }
            lastMiniCheckpointScript = checkpointScript; // The major checkpoint is now also the active mini one.
            Debug.Log($"New MAJOR checkpoint set at: {newPosition}. Mini checkpoint also updated.");
        }
    }

    // This is called by PlayerHealth when the player is hurt.
    public void RespawnAtMiniCheckpoint()
    {
        player.transform.position = lastMiniCheckpointPos;
        Debug.Log($"Player respawned at MINI checkpoint: {lastMiniCheckpointPos}");
    }

    // This is called by PlayerHealth when the player dies.
    public void RespawnAtMajorCheckpoint()
    {
        // 1. Move the player to the major checkpoint position (this is the same).
        player.transform.position = lastMajorCheckpointPos;
        Debug.Log($"Player respawned at MAJOR checkpoint: {lastMajorCheckpointPos}");

        // 2. --- THIS IS THE FIX ---
        // ALSO update the mini checkpoint position to be the same as the major one.
        lastMiniCheckpointPos = lastMajorCheckpointPos;
        Debug.Log("Mini checkpoint has been RESET to the major checkpoint's location.");
    }
}
