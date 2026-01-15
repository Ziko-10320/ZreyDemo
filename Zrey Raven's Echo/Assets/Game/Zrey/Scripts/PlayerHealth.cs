using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The maximum health the player can have.")]
    public int maxHealth = 3;

    // This is the player's current health.
    private int currentHealth;

    // We need a reference to the CheckpointManager to tell it when to respawn us.
    private CheckpointManager checkpointManager;

    void Start()
    {
        // When the game starts, the player is at full health.
        currentHealth = maxHealth;

        // Find the CheckpointManager in the scene.
        // This assumes you only have one manager object.
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
        if (checkpointManager == null)
        {
            Debug.LogError("FATAL ERROR: No CheckpointManager found in the scene!");
        }

        Debug.Log($"Player has started with {currentHealth}/{maxHealth} health.");
    }

    // This is the public function that other scripts (like DamageSource) will call.
    public void TakeDamage(int damageAmount)
    {
        // Subtract the damage from our current health.
        currentHealth -= damageAmount;
        Debug.Log($"Player took {damageAmount} damage. Health is now {currentHealth}.");

        // --- THIS IS THE CORE LOGIC ---

        // Check if the player is dead.
        if (currentHealth <= 0)
        {
            // Player is dead. Call the Major Respawn function.
            Die();
        }
        else
        {
            // Player is only hurt, not dead. Call the Mini Respawn function.
            Hurt();
        }
    }

    private void Hurt()
    {
        Debug.Log("Player is HURT. Respawning at MINI checkpoint.");
        // Tell the CheckpointManager to respawn the player at the last mini checkpoint.
        checkpointManager.RespawnAtMiniCheckpoint();
    }

    private void Die()
    {
        Debug.Log("Player has DIED. Respawning at MAJOR checkpoint.");
        // Tell the CheckpointManager to respawn the player at the last major checkpoint.
        checkpointManager.RespawnAtMajorCheckpoint();

        // After a major respawn, we should also restore the player's health.
        currentHealth = maxHealth;
        Debug.Log("Player health has been restored to full.");
    }
}
