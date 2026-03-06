using UnityEngine;

public class IndividualBlur : MonoBehaviour
{
    public float blurAmount = 2.0f;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // Get the current block, modify it, and shoot it back to the renderer
        spriteRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat("_BlurAmount", blurAmount); // "_BlurAmount" must match your shader variable name
        spriteRenderer.SetPropertyBlock(propBlock);
    }
}