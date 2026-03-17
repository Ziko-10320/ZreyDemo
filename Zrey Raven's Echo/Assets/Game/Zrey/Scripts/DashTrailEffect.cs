using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DashTrailEffect : MonoBehaviour
{
    [Tooltip("Total lifetime before the object is destroyed.")]
    public float lifetime = 1f;

    [Tooltip("How long before destroy to stop emitting and fade out.")]
    public float fadeOutTime = 0.5f;

    private ParticleSystem[] particles;
    private VisualEffect[] vfxList;
    private Color[] originalColors;

    void Start()
    {
        particles = GetComponentsInChildren<ParticleSystem>();
        vfxList = GetComponentsInChildren<VisualEffect>();

        // Cache original particle colors
        originalColors = new Color[particles.Length];
        for (int i = 0; i < particles.Length; i++)
            originalColors[i] = particles[i].main.startColor.color;

        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        // Wait until fade out should begin
        float displayTime = lifetime - fadeOutTime;
        if (displayTime > 0f)
            yield return new WaitForSeconds(displayTime);

        // Stop emitting
        foreach (ParticleSystem ps in particles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = false;
        }

        foreach (VisualEffect vfx in vfxList)
        {
            if (vfx == null) continue;
            vfx.SendEvent("OnStop");
            vfx.Stop();
        }

        // Fade out over fadeOutTime
        float elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;
                var main = particles[i].main;
                Color c = originalColors[i];
                c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                main.startColor = c;
            }

            foreach (VisualEffect vfx in vfxList)
            {
                if (vfx == null) continue;
                if (vfx.HasFloat("Alpha"))
                    vfx.SetFloat("Alpha", Mathf.Lerp(1f, 0f, t));
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}