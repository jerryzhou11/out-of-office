using UnityEngine;

/// <summary>
/// Coffee pickup — gives the player a permanent speed boost for the rest of the day.
/// Destroys itself on pickup.
/// </summary>
public class CoffeePickup : MonoBehaviour
{
    [SerializeField] private float speedBoost = 1.5f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ApplySpeedBoost(speedBoost);
        }

        Destroy(gameObject);
    }
}
