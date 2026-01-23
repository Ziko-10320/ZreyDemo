// PASTE THIS ENTIRE SCRIPT OVER YOUR OLD ZreyTrail.cs

using System.Collections;
using UnityEngine;

public class ZreyTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("How long the trail effect should last when activated (in seconds).")]
    [SerializeField] private float trailDuration = 0.5f;
    [Tooltip("The material to apply to the trail snapshots. This should be a semi-transparent or emissive material with an '_Alpha' property.")]
    [SerializeField] private Material trailMaterial;

    [Header("Snapshot Settings")]
    [Tooltip("How often a new snapshot of the character is created (in seconds).")]
    [SerializeField] private float meshRefreshRate = 0.05f;
    [Tooltip("How long each individual snapshot lasts before fading out and being destroyed.")]
    [SerializeField] private float snapshotLifetime = 0.5f;

    [Header("Required Components")]
    [Tooltip("The Skinned Mesh Renderer of the character you want to create a trail for.")]
    [SerializeField] private SkinnedMeshRenderer characterMeshRenderer;
    [Tooltip("An empty GameObject that follows the player. The trail snapshots will be parented to this.")]
    [SerializeField] private Transform trailSpawnParent;

    [Header("Dependencies")]
    [Tooltip("A reference to the player's movement script to check facing direction.")]
    [SerializeField] private ZreyMovements playerMovements;

    // --- Private State ---
    private bool isTrailActive = false;

    void Awake()
    {
        // Failsafe to get the movement script if not assigned.
        if (playerMovements == null) playerMovements = GetComponentInParent<ZreyMovements>();
       
    }

    /// <summary>
    /// Public method called by other scripts (like ZreyMovements) to start the trail.
    /// </summary>
    public void StartTrail()
    {
        if (!isTrailActive)
        {
            StartCoroutine(ActivateTrailRoutine());
        }
    }

    /// <summary>
    /// The main coroutine that controls the duration of the trail effect.
    /// </summary>
    private IEnumerator ActivateTrailRoutine()
    {
        isTrailActive = true;
        float timer = 0f;
        while (timer < trailDuration)
        {
            CreateSnapshot();
            yield return new WaitForSeconds(meshRefreshRate);
            timer += meshRefreshRate;
        }
        isTrailActive = false;
    }

    /// <summary>
    /// Creates a single snapshot and starts its fade-out coroutine.
    /// </summary>
    private void CreateSnapshot()
    {
        GameObject snapshotObject = new GameObject("Trail_Snapshot");
        snapshotObject.transform.SetPositionAndRotation(characterMeshRenderer.transform.position, characterMeshRenderer.transform.rotation);
        if (trailSpawnParent != null) snapshotObject.transform.SetParent(trailSpawnParent);

        MeshFilter meshFilter = snapshotObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = snapshotObject.AddComponent<MeshRenderer>();

        Mesh snapshotMesh = new Mesh();
        characterMeshRenderer.BakeMesh(snapshotMesh);
        meshFilter.mesh = snapshotMesh;

        // --- THIS IS THE RENDER FACE & FADE FIX (ALL-IN-ONE) ---

        // 1. CLONE THE MATERIAL to create a unique instance for this snapshot.
        Material snapshotMaterial = new Material(trailMaterial);

        // 2. CHECK FACING DIRECTION and set the Render Face on the new material.
        if (playerMovements != null && playerMovements.IsFacingRight())
        {
            snapshotMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back); // Render front
        }
        else
        {
            snapshotMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front); // Render back
        }

        // 3. Assign the new, unique material to the snapshot.
        meshRenderer.material = snapshotMaterial;

        // 4. START THE FADE COROUTINE on this script, passing it the material to fade.
        StartCoroutine(FadeSnapshotRoutine(snapshotMaterial));

        // --- END OF FIX ---

        // 5. Schedule the snapshot to be destroyed after its lifetime.
        Destroy(snapshotObject, snapshotLifetime);
    }

    /// <summary>
    /// A coroutine that fades a single material's alpha from 1 to 0 over the snapshot's lifetime.
    /// </summary>
    private IEnumerator FadeSnapshotRoutine(Material materialToFade)
    {
        float timeElapsed = 0f;
        // Get the shader property ID once for performance.
        int alphaPropertyID = Shader.PropertyToID("_Alpha");

        while (timeElapsed < snapshotLifetime)
        {
            timeElapsed += Time.deltaTime;
            // Calculate the new alpha value from 1 down to 0.
            float newAlpha = Mathf.Lerp(1f, 0f, timeElapsed / snapshotLifetime);
            // Set the property on the material instance.
            materialToFade.SetFloat(alphaPropertyID, newAlpha);
            yield return null; // Wait for the next frame.
        }
    }
}
