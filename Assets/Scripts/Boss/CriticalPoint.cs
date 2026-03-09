using UnityEngine;

/// <summary>
/// Vulnerable terminal around the boss arena. Always active and hittable.
/// The player melee-slaps it to deal damage to the boss.
/// </summary>
public class CriticalPoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damagePerHit = 10;

    [Header("References")]
    [SerializeField] private BossController boss;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D hitCollider;

    [Header("Visuals")]
    [SerializeField] private Color activeColor = Color.green;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();

        // Always active
        if (hitCollider != null)
            hitCollider.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;
    }

    /// <summary>
    /// Called by PlayerController's Attack() when the melee overlap finds this collider.
    /// </summary>
    public void OnMeleeHit()
    {
        if (boss != null)
        {
            boss.TakeDamage(damagePerHit);
        }
    }
}
