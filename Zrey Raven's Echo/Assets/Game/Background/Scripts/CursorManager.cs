using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [Header("Custom Cursor")]
    public Texture2D customCursorTexture;
    public Vector2 hotspot = Vector2.zero;
    [Tooltip("Rotate your cursor sprite. 0 = as-is, 90 = turned right, -90 = turned left, 180 = flipped.")]
    public float cursorRotation = 0f;
    [Tooltip("Scale your cursor. 1 = original size, 0.5 = half size, 2 = double size.")]
    public float cursorScale = 1f;
    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    private int _visibilityRequests = 0;
    private Texture2D _invisibleTexture;
    private Texture2D _rotatedCursorTexture;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _invisibleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        _invisibleTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
        _invisibleTexture.Apply();

        _rotatedCursorTexture = RotateAndScaleTexture(customCursorTexture, cursorRotation, cursorScale);

        ApplyCursorForScene(SceneManager.GetActiveScene().name);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _visibilityRequests = 0;
        ApplyCursorForScene(scene.name);
    }

    private void ApplyCursorForScene(string sceneName)
    {
        if (sceneName == mainMenuSceneName) ShowCustomCursor();
        else HideCursor();
    }

    void Update()
    {
        // Auto-detect any active overlay canvas in the scene
        bool anyOverlayCanvasActive = false;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay
                && canvas.gameObject.activeInHierarchy
                && canvas.CompareTag("CursorCanvas"))
            {
                anyOverlayCanvasActive = true;
                break;
            }
        }

        if (anyOverlayCanvasActive)
            ShowCustomCursor();
        else if (_visibilityRequests <= 0 && SceneManager.GetActiveScene().name != mainMenuSceneName)
            HideCursor();
    }

    public void RequestShowCursor()
    {
        _visibilityRequests++;
        ShowCustomCursor();
    }

    public void RequestHideCursor()
    {
        _visibilityRequests = Mathf.Max(0, _visibilityRequests - 1);
        if (_visibilityRequests <= 0) HideCursor();
    }

    public void ForceHide()
    {
        _visibilityRequests = 0;
        HideCursor();
    }

    private void ShowCustomCursor()
    {
        Cursor.SetCursor(_rotatedCursorTexture, hotspot, CursorMode.ForceSoftware);
        Cursor.visible = true;
    }

    private void HideCursor()
    {
        Cursor.SetCursor(_invisibleTexture, Vector2.zero, CursorMode.ForceSoftware);
        Cursor.visible = false;
    }

    private Texture2D RotateAndScaleTexture(Texture2D original, float angleDegrees, float scale)
    {
        if (original == null) return null;

        int srcW = original.width;
        int srcH = original.height;
        int dstW = Mathf.RoundToInt(srcW * scale);
        int dstH = Mathf.RoundToInt(srcH * scale);

        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Texture2D result = new Texture2D(dstW, dstH, TextureFormat.ARGB32, false, true);

        Color[] clear = new Color[dstW * dstH];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        result.SetPixels(clear);

        float cx = dstW / 2f;
        float cy = dstH / 2f;
        float srcCx = srcW / 2f;
        float srcCy = srcH / 2f;

        for (int x = 0; x < dstW; x++)
        {
            for (int y = 0; y < dstH; y++)
            {
                float dx = (x - cx) / scale;
                float dy = (y - cy) / scale;

                float srcX = cos * dx + sin * dy + srcCx;
                float srcY = -sin * dx + cos * dy + srcCy;

                if (srcX >= 0 && srcX < srcW && srcY >= 0 && srcY < srcH)
                    result.SetPixel(x, y, original.GetPixelBilinear(srcX / srcW, srcY / srcH));
                else
                    result.SetPixel(x, y, Color.clear);
            }
        }

        result.Apply();
        return result;
    }
}