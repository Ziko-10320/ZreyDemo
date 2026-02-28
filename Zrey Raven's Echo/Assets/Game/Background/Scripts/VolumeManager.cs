using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    // --- THIS IS THE SINGLETON PATTERN ---
    // 'public static VolumeManager instance;' creates a variable that can be accessed
    // from any other script in the game without needing a direct reference.
    public static VolumeManager instance;

    [Header("Master Volume")]
    [Range(0f, 1f)]
    [Tooltip("The master volume control for all sound effects.")]
    public float masterSfxVolume = 1.0f;

    // A list to keep track of all the hazard audio sources in the scene.
    private HazardController[] allHazards;

    // Awake is called before any Start() functions.
    void Awake()
    {
        // --- SINGLETON SETUP ---
        // If an instance of this manager already exists...
        if (instance != null)
        {
            // ...destroy this new one. We only ever want one.
            Destroy(gameObject);
            return;
        }
        // Otherwise, this is the one and only instance.
        instance = this;
        // Don't destroy this object when we load new scenes.
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // When the game starts, find all the hazards in the scene and update their volume.
        UpdateAllHazardVolumes();
    }

    // This is a special function that is called in the Editor whenever you change a value.
    // This allows us to hear the volume change in real-time without pressing play!
    private void OnValidate()
    {
        // We only want to run this if the game is actually playing.
        if (Application.isPlaying)
        {
            UpdateAllHazardVolumes();
        }
    }
    public void SetMasterSfxVolume(float newVolume)
    {
        // Set our master volume variable to the new value from the slider.
        masterSfxVolume = newVolume;

        // After changing the volume, we immediately tell all the hazards to update.
        UpdateAllHazardVolumes();
    }
    // This is the core function that does all the work.
    public void UpdateAllHazardVolumes()
    {
        // Find every single active HazardController script in the entire scene.
        allHazards = FindObjectsOfType<HazardController>();

        Debug.Log($"Found {allHazards.Length} hazards. Updating their volumes...");

        // Loop through each hazard we found.
        foreach (HazardController hazard in allHazards)
        {
            // Tell each hazard to update its volume based on our master slider.
            hazard.UpdateVolume();
        }
    }
}
