using UnityEngine;
public class VolumeManager : MonoBehaviour
{
    public static VolumeManager instance;

    [Header("Master Volume")]
    [Range(0f, 1f)]
    [Tooltip("The master volume control for all sound effects.")]
    public float masterSfxVolume = 1.0f;

    private HazardController[] allHazards;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateAllVolumes();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) UpdateAllVolumes();
    }

    public void SetMasterSfxVolume(float newVolume)
    {
        masterSfxVolume = newVolume;
        UpdateAllVolumes();
    }

    public void UpdateAllVolumes()
    {
        // --- Hazards (existing) ---
        allHazards = FindObjectsOfType<HazardController>();
        foreach (HazardController hazard in allHazards)
            hazard.UpdateVolume();
        ZreyAttacks attacks = FindObjectOfType<ZreyAttacks>();
        if (attacks != null) attacks.UpdateVolume(masterSfxVolume);
        // --- Player movement sounds (new) ---
        ZreyMovements player = FindObjectOfType<ZreyMovements>();
        if (player != null)
            player.UpdateVolume(masterSfxVolume);
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null) audioManager.UpdateVolume(masterSfxVolume);
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null) playerHealth.UpdateVolume(masterSfxVolume);
        foreach (KnightHealth knight in FindObjectsOfType<KnightHealth>())
            knight.UpdateVolume(masterSfxVolume);
        foreach (KnightAttack knight in FindObjectsOfType<KnightAttack>())
            knight.UpdateVolume(masterSfxVolume);
        foreach (KnightHealth k in FindObjectsOfType<KnightHealth>()) k.UpdateVolume(masterSfxVolume);
        foreach (SpearHealth s in FindObjectsOfType<SpearHealth>()) s.UpdateVolume(masterSfxVolume);
        foreach (SpearAttack s in FindObjectsOfType<SpearAttack>()) s.UpdateVolume(masterSfxVolume);
    }
}