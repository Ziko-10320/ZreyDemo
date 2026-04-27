using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask playerLayer;

    private ImpactData impactData;
    private Vector2 direction;
    private float speed;
    private bool hasHit = false;

    // Rotation spin
    private float rotationSpeed;

    // Knockback source — enemy transform passed in so knockback always goes away from enemy
    private Transform enemyTransform;

    public void Init(Vector2 targetPosition, float rockSpeed, int rockDamage, ImpactData impact,
                     LayerMask pLayer, float aimOffsetY, float spinSpeed, Transform enemy)
    {
        damage = rockDamage;
        speed = rockSpeed;
        impactData = impact;
        playerLayer = pLayer;
        rotationSpeed = spinSpeed;
        enemyTransform = enemy;

        // Apply Y aim offset so rock arcs higher and reaches player
        Vector2 adjustedTarget = new Vector2(targetPosition.x, targetPosition.y + aimOffsetY);
        direction = (adjustedTarget - (Vector2)transform.position).normalized;

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (hasHit) return;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Continuous spin on Z axis
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

            // Use enemy transform for knockback direction — never the rock's position
            Transform knockbackSource = enemyTransform != null ? enemyTransform : transform;
            ph.TakeDamage(damage, knockbackSource, impactData);

            Destroy(gameObject);
        }
    }
}