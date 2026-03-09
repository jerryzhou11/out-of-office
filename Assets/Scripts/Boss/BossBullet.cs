using UnityEngine;

/// <summary>
/// Simple projectile fired by the boss. Moves in a straight line at a set speed.
/// Damages the player on contact and self-destructs on walls or after a timeout.
/// Place on Enemy layer (8) so bullets don't collide with the boss or each other.
/// Requires: Rigidbody2D (Kinematic), CircleCollider2D (IsTrigger), SpriteRenderer.
/// </summary>
public class BossBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called by BossController after instantiation to set direction and speed.
    /// </summary>
    public void Initialize(Vector2 direction, float speed)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hit player
        if (other.CompareTag("Player"))
        {
            if (PlayerDamageReceiver.Instance != null)
            {
                PlayerDamageReceiver.Instance.TakeHit();
            }
            Destroy(gameObject);
            return;
        }

        // Hit wall (Map layer = 3)
        if (other.gameObject.layer == 3)
        {
            Destroy(gameObject);
        }
    }
}
