using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("How long to wait after hit before destroying the GameObject (lets trail finish).")]
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private GameObject destructionEffect;

    private ImpactData impactData;
    private Vector2 direction;
    private float speed;
    private bool hasHit = false;
    private float rotationSpeed;
    private Transform enemyTransform;

    // Cached components
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private ParticleSystem dustTrail;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // DustTrail is a child object
        Transform trailChild = transform.Find("DustTrail");
        if (trailChild != null)
            dustTrail = trailChild.GetComponent<ParticleSystem>();
    }

    public void Init(Vector2 targetPosition, float rockSpeed, int rockDamage, ImpactData impact,
                     LayerMask pLayer, float aimOffsetY, float spinSpeed, Transform enemy)
    {
        damage = rockDamage;
        speed = rockSpeed;
        impactData = impact;
        playerLayer = pLayer;
        rotationSpeed = spinSpeed;
        enemyTransform = enemy;

        Vector2 adjustedTarget = new Vector2(targetPosition.x, targetPosition.y + aimOffsetY);
        direction = (adjustedTarget - (Vector2)transform.position).normalized;

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (hasHit) return;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null && impactData != null)
        {
            hasHit = true;
            Transform knockbackSource = enemyTransform != null ? enemyTransform : transform;
            ph.TakeDamage(damage, knockbackSource, impactData);
            StartCoroutine(HitCleanup());
        }
    }

    private System.Collections.IEnumerator HitCleanup()
    {
        if (destructionEffect != null)
            Instantiate(destructionEffect, transform.position, Quaternion.identity);

        // Hide the rock visually and stop collisions immediately
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        // Stop the trail from emitting new particles but let existing ones finish
        if (dustTrail != null)
        {
            var emission = dustTrail.emission;
            emission.enabled = false;
        }

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}