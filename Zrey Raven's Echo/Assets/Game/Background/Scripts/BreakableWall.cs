using UnityEngine;

public class BreakableWall : MonoBehaviour
{
    [Header("Wall Settings")]
    [Tooltip("Only a down slam can break this wall.")]
    [SerializeField] private int wallHealth = 1;
    [Header("Sound")]
    [SerializeField] private AudioClip breakSound;
    [Range(0f, 1f)][SerializeField] private float breakSoundVolume = 1f;
    [Header("Effects")]
    [SerializeField] private GameObject breakEffectPrefab;

    public void TakeDownSlamDamage(int damage)
    {
        wallHealth -= damage;
        if (wallHealth <= 0)
        {
            BreakWall();
        }
    }

    private void BreakWall()
    {
        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        if (breakSound != null)
        {
            GameObject tempAudio = new GameObject("WallBreakSound");
            AudioSource src = tempAudio.AddComponent<AudioSource>();
            src.clip = breakSound;
            src.spatialBlend = 0f;
            src.volume = breakSoundVolume;
            src.Play();
            Destroy(tempAudio, breakSound.length);
        }
        Destroy(gameObject);
    }
}