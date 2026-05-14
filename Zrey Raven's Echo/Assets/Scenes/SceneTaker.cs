using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTaker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string sceneName;
    [SerializeField] private float fadeDuration = 1f;

    [Header("References")]
    [SerializeField] private Image fadeImage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        fadeImage.gameObject.SetActive(true);
        float t = 0f;
        Color c = Color.black;
        c.a = 0f;
        fadeImage.color = c;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}