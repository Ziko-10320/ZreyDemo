using UnityEditor;
using UnityEngine;

public class HazardController : MonoBehaviour
{
    // This enum lets us choose the hazard's behavior in the Inspector.
    public enum HazardType { SimpleSpike, ProximityTrap, SwingingAxe, PathFollower }
    public HazardType type = HazardType.SimpleSpike;
    public bool isBrainOnly = false;

    // --- DAMAGE ---
    public int damageAmount = 1;
    public string targetTag = "Player";

    // --- PROXIMITY TRAP SETTINGS ---
    // We need a public variable to control the visibility of this group
    [HideInInspector] public bool showProximitySettings;
    public Animator trapAnimator;
    public string animationTriggerName = "ActivateTrap";
    public bool triggerOnce = true;

    // --- SWINGING AXE SETTINGS ---
    // And another one for this group
    [HideInInspector] public bool showSwingingAxeSettings;
    public float leftAngle = -45.0f;
    public float rightAngle = 45.0f;
    public float swingSpeed = 2.0f;
    [Tooltip("If checked, the axe will slow down at the ends of its swing for a more natural momentum.")]
    public bool useSmoothMomentum = false;

    [Tooltip("If checked, the axe will ignore angles and spin forever. Swing Speed controls the spin rate.")]
    public bool fullSpin = false;
    [Tooltip("The direction of the spin (1 for clockwise, -1 for counter-clockwise).")]
    public int spinDirection = 1;
    // --- PRIVATE VARIABLES (No change here) ---
    private bool isDamageActive = false;
    private float swingTimer = 0.0f;
    [Header("Path Follower Settings")]
    [Tooltip("The list of empty GameObjects that define the path.")]
    public Transform[] waypoints;
    [Tooltip("How fast the hazard moves between waypoints.")]
    public float pathSpeed = 3.0f;
    [Tooltip("If checked, the path will loop back to the start. If unchecked, it will go back and forth.")]
    public bool loopPath = false;
    [Tooltip("How fast the saw blade sprite rotates (visual only). Set to 0 for no rotation.")]
    public float visualRotationSpeed = 360.0f;
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    // --- THIS IS THE MAGIC FUNCTION ---
    // OnValidate is called in the editor whenever a value is changed.
    private void OnValidate()
    {
        // Check the value of our dropdown 'type'
        switch (type)
        {
            case HazardType.ProximityTrap:
                showProximitySettings = true;
                showSwingingAxeSettings = false;
                break;
            case HazardType.SwingingAxe:
                showProximitySettings = false;
                showSwingingAxeSettings = true;
                break;
            case HazardType.SimpleSpike:
            default:
                showProximitySettings = false;
                showSwingingAxeSettings = false;
                break;
        }
    }

    void Start()
    {
        if (type == HazardType.SimpleSpike || type == HazardType.SwingingAxe || type == HazardType.PathFollower)
        {
            isDamageActive = true;
        }
        // If it's a simple spike OR a swinging axe, it's always dangerous.
        if (type == HazardType.SimpleSpike || type == HazardType.SwingingAxe)
        {
            isDamageActive = true;
        }
        else // This covers ProximityTrap
        {
            isDamageActive = false;
        }
    }
    void Update()
    {
        if (type == HazardType.PathFollower)
        {
            transform.Rotate(Vector3.forward * visualRotationSpeed * Time.deltaTime);
            // 1. Safety check: Make sure there are waypoints to follow.
            if (waypoints.Length == 0)
            {
                return; // Do nothing if the path is empty.
            }

            // 2. Get the position of the current target waypoint.
            Transform targetWaypoint = waypoints[currentWaypointIndex];

            // 3. Move this object towards the target waypoint.
            // Vector3.MoveTowards is a perfect tool for this. It moves from a current point to a target at a specific speed.
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, pathSpeed * Time.deltaTime);

            // 4. Check if we have reached the waypoint.
            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
            {
                // If we've arrived, we need to figure out the next waypoint.

                if (loopPath)
                {
                    // --- A: LOOPING LOGIC ---
                    // Just move to the next index, and wrap around to 0 if we reach the end.
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }
                else
                {
                    // --- B: BACK-AND-FORTH LOGIC ---
                    if (movingForward)
                    {
                        if (currentWaypointIndex >= waypoints.Length - 1)
                        {
                            // We've reached the end, so reverse direction.
                            movingForward = false;
                            currentWaypointIndex--;
                        }
                        else
                        {
                            currentWaypointIndex++;
                        }
                    }
                    else // if (!movingForward)
                    {
                        if (currentWaypointIndex <= 0)
                        {
                            // We've reached the start, so go forward again.
                            movingForward = true;
                            currentWaypointIndex++;
                        }
                        else
                        {
                            currentWaypointIndex--;
                        }
                    }
                }
            }
        }

        // We only run this logic if the type is SwingingAxe.
        if (type == HazardType.SwingingAxe)
        {
            // --- A: FULL SPIN LOGIC ---
            if (fullSpin)
            {
                // If fullSpin is checked, we ignore everything else and just rotate.
                // We use transform.Rotate to continuously add rotation each frame.
                // Vector3.forward is the Z-axis in 2D.
                transform.Rotate(Vector3.forward * swingSpeed * spinDirection * Time.deltaTime);
            }
            // --- B: SWINGING LOGIC ---
            else
            {
                // This is the logic for the back-and-forth swing.
                swingTimer += Time.deltaTime * swingSpeed;
                float lerpFactor = 0f; // This will be our 0-1 value.

                // --- THIS IS THE MOMENTUM CHOICE ---
                if (useSmoothMomentum)
                {
                    // FOR SMOOTH MOMENTUM: We use Mathf.Sin.
                    // The sine wave naturally creates a smooth ease-in and ease-out curve.
                    // We add 1 and multiply by 0.5 to map the -1 to 1 range of Sine into a 0 to 1 range.
                    lerpFactor = (Mathf.Sin(swingTimer) + 1.0f) * 0.5f;
                }
                else
                {
                    // FOR ROBOTIC SWING: We use the original Mathf.PingPong.
                    // This creates a linear, constant-speed movement.
                    lerpFactor = Mathf.PingPong(swingTimer, 1.0f);
                }

                // The rest of the logic is the same. We use our calculated 'lerpFactor'
                // to find the target angle between our left and right limits.
                float targetAngle = Mathf.Lerp(leftAngle, rightAngle, lerpFactor);
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
        }
    }
    // --- PROXIMITY TRIGGER LOGIC ---
    // This function only runs if this object has a trigger collider.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- THIS IS THE FIX ---
        // If this is a "Brain Only" component, we check for proximity, but then we STOP.
        // We do not proceed to the damage logic.
        if (isBrainOnly)
        {
            // This is the same proximity logic as before.
            if (type == HazardType.ProximityTrap && !isDamageActive && other.CompareTag(targetTag))
            {
                if (triggerOnce)
                {
                    isDamageActive = true; // Still set this to prevent re-triggering.
                }
                if (trapAnimator != null)
                {
                    trapAnimator.SetTrigger(animationTriggerName);
                }
            }
            // After checking for proximity, we immediately exit the function.
            return;
        }
        // --- END OF FIX ---


        // --- DAMAGE LOGIC ---
        // This code will now ONLY run if 'isBrainOnly' is false.
        if (isDamageActive && other.CompareTag(targetTag))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }

    // --- ANIMATION EVENT FUNCTIONS ---
    // These functions are called by the animation itself.

    // This function will be called by an Animation Event to make the spears dangerous.
    public void ActivateDamage()
    {
        // This is a special command that tells this script to run the "EnableDamageOnChildren" function.
        // We use BroadcastMessage to ensure all children receive the command.
        BroadcastMessage("EnableDamage", SendMessageOptions.DontRequireReceiver);
    }

    // This function will be called by an Animation Event to make the spears safe again.
    public void DeactivateDamage()
    {
        BroadcastMessage("DisableDamage", SendMessageOptions.DontRequireReceiver);
    }

    // --- HELPER FUNCTIONS FOR CHILDREN ---
    // These functions are designed to be called by the BroadcastMessage.

    public void EnableDamage()
    {
        isDamageActive = true;
    }

    public void DisableDamage()
    {
        isDamageActive = false;
    }
}
#if UNITY_EDITOR

[CustomEditor(typeof(HazardController))]
public class HazardControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // Get a reference to the script
        HazardController hazard = (HazardController)target;

        // Draw the core fields
        EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
        hazard.type = (HazardController.HazardType)EditorGUILayout.EnumPopup("Hazard Type", hazard.type);
        hazard.damageAmount = EditorGUILayout.IntField("Damage Amount", hazard.damageAmount);
        hazard.targetTag = EditorGUILayout.TextField("Target Tag", hazard.targetTag);
        hazard.isBrainOnly = EditorGUILayout.Toggle("Is Brain Only", hazard.isBrainOnly);

        EditorGUILayout.Space();

        // Use a switch to show the correct fields
        switch (hazard.type)
        {
            case HazardController.HazardType.PathFollower:
                EditorGUILayout.LabelField("Path Follower Settings", EditorStyles.boldLabel);
                hazard.pathSpeed = EditorGUILayout.FloatField("Path Speed", hazard.pathSpeed);
                hazard.loopPath = EditorGUILayout.Toggle("Loop Path", hazard.loopPath);
                hazard.visualRotationSpeed = EditorGUILayout.FloatField("Visual Rotation Speed", hazard.visualRotationSpeed);
                // This is a special field for drawing the array of waypoints.
                // It's a bit more complex but very powerful.
                SerializedProperty waypointsProperty = serializedObject.FindProperty("waypoints");
                EditorGUILayout.PropertyField(waypointsProperty, true);
                break;
            case HazardController.HazardType.ProximityTrap:
                EditorGUILayout.LabelField("Proximity Trap Settings", EditorStyles.boldLabel);
                hazard.trapAnimator = (Animator)EditorGUILayout.ObjectField("Trap Animator", hazard.trapAnimator, typeof(Animator), true);
                hazard.animationTriggerName = EditorGUILayout.TextField("Animation Trigger Name", hazard.animationTriggerName);
                hazard.triggerOnce = EditorGUILayout.Toggle("Trigger Once", hazard.triggerOnce);
                break;

            case HazardController.HazardType.SwingingAxe:
                EditorGUILayout.LabelField("Swinging Axe Settings", EditorStyles.boldLabel);

                // --- 1. Draw the Full Spin checkbox FIRST ---
                hazard.fullSpin = EditorGUILayout.Toggle("Full Spin", hazard.fullSpin);

                // --- 2. If Full Spin is checked, show only the spin settings ---
                if (hazard.fullSpin)
                {
                    hazard.swingSpeed = EditorGUILayout.FloatField("Spin Speed", hazard.swingSpeed);
                    hazard.spinDirection = EditorGUILayout.IntField("Spin Direction (1 or -1)", hazard.spinDirection);
                }
                // --- 3. If Full Spin is NOT checked, show the normal swing settings ---
                else
                {
                    hazard.leftAngle = EditorGUILayout.FloatField("Left Angle", hazard.leftAngle);
                    hazard.rightAngle = EditorGUILayout.FloatField("Right Angle", hazard.rightAngle);
                    hazard.swingSpeed = EditorGUILayout.FloatField("Swing Speed", hazard.swingSpeed);
                    // And now we draw the new momentum checkbox
                    hazard.useSmoothMomentum = EditorGUILayout.Toggle("Use Smooth Momentum", hazard.useSmoothMomentum);
                }
                break;

        }

        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(hazard);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif