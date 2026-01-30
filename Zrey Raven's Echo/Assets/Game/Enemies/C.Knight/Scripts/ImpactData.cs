using UnityEngine;

[CreateAssetMenu(fileName = "New ImpactData", menuName = "Combat/Knight Impact Data")]
public class ImpactData : ScriptableObject
{
    [Header("Player Reaction")]
    [Tooltip("The hit animation the player should play (e.g., 'back', 'down', 'finalback').")]
    public string hitReactionType = "back";

    [Header("Player Knockback")]
    [Tooltip("How far the player is knocked back by this specific hit.")]
    public float knockbackDistance = 4f;
    [Tooltip("How long the player knockback from this hit lasts.")]
    public float knockbackDuration = 0.2f;
    [Header("Directional Knockback (Optional)")]
    [Tooltip("The upward force to apply (for launchers). Overrides horizontal knockback if > 0.")]
    public float upwardForce = 0f;
    [Tooltip("The downward force to apply (for slams). Overrides horizontal knockback if > 0.")]
    public float downwardForce = 0f;
}