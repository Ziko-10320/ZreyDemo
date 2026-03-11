using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source & Mixer")]
    public AudioSource musicSource;
    public AudioMixer mainMixer;

    [Header("Mixer Snapshots")]
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot muffledSnapshot;

    [Header("Fade & Volume Settings")]
    [Tooltip("How long the music takes to fade in at the start of the scene.")]
    public float fadeInDuration = 2.0f;
    [Tooltip("How long the music takes to fade out when the game ends.")]
    public float fadeOutDuration = 1.5f;
    [Tooltip("Set the music volume (0.0 to 1.0). This is the target volume for fade-in.")]
    [Range(0f, 1f)]
    public float musicVolume = 0.8f;

    void Start()
    {
        musicSource.loop = true;
        musicSource.volume = 0; // Start silent before fading in
        StartCoroutine(FadeInMusic());
    }

    private IEnumerator FadeInMusic()
    {
        float timer = 0;
        musicSource.Play();
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, musicVolume, timer / fadeInDuration);
            yield return null;
        }
        musicSource.volume = musicVolume;
    }

    // --- PUBLIC FUNCTIONS FOR OTHER SCRIPTS TO CALL ---

    public void MuffleMusic()
    {
        muffledSnapshot.TransitionTo(0.5f);
    }

    public void UnmuffleMusic()
    {
        normalSnapshot.TransitionTo(0.5f);
    }

    public void StartFadeOut()
    {
        // Check if a fade-out is already happening to prevent errors
        if (!IsInvoking("FadeOutMusic"))
        {
            StartCoroutine(FadeOutMusic());
        }
    }
    public void UpdateVolume(float masterVolume)
    {
        musicVolume = masterVolume;
        // Only update live volume if music is actually playing (respects fade state)
        if (musicSource != null && musicSource.isPlaying)
            musicSource.volume = musicVolume;
    }

    private IEnumerator FadeOutMusic()
    {
        float timer = 0;
        float startVolume = musicSource.volume;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeOutDuration);
            yield return null;
        }
        musicSource.volume = 0;
        musicSource.Stop();
    }
}
