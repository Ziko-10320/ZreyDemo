// GrapplePoint.cs
using UnityEngine;

public class GrapplePoint : MonoBehaviour
{
    [Tooltip("The exact spot the player will hang from. Create an empty child object and place it just below the ring.")]
    [SerializeField] private Transform hangPosition;

    // A public property to access the hang position from the player script
    public Vector3 GetHangPosition()
    {
        // If no specific hang position is set, just return a spot slightly below the grapple point itself.
        if (hangPosition == null)
        {
            return transform.position - new Vector3(0, 0.8f, 0);
        }
        return hangPosition.position;
    }

    // We can use OnDrawGizmos to visualize our radiuses!
    // This helps a lot during level design.
    void OnDrawGizmosSelected()
    {
        // This requires you to have access to the player's grapple settings.
        // For simplicity, we'll just draw the hang position for now.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetHangPosition(), 0.2f);
    }
}
