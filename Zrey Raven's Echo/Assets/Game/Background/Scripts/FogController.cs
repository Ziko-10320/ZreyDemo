using UnityEngine;

public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = Color.white;
    public Vector2 fogSpeed = new Vector2(1.0f, 0.5f); // Now handles X and Y
    public float fogSize = 0.5f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        spriteRenderer.GetPropertyBlock(propBlock);

        // Send the Color, the Vector2 for speed, and the Float for size
        propBlock.SetColor("_FogColor", fogColor);
        propBlock.SetVector("_FogSpeed", fogSpeed); // Unity treats Vector2 as Vector4 in shaders
        propBlock.SetFloat("_FogSize", fogSize);

        spriteRenderer.SetPropertyBlock(propBlock);
    }
}