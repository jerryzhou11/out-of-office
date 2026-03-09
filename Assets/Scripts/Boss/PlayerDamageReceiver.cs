using UnityEngine;
using System.Collections;

/// <summary>
/// Tracks hits on the player during the boss fight.
/// Separate from PlayerController since the regular floors have no HP system.
/// Scene-level singleton (not DontDestroyOnLoad).
/// </summary>
public class PlayerDamageReceiver : MonoBehaviour
{
    public static PlayerDamageReceiver Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private int maxHits = 3; // 4th hit = death
    [SerializeField] private float iFrameDuration = 1.5f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private float flashInterval = 0.1f;

    // Status effect ID for damage indicators
    public const string EFFECT_BOSS_DAMAGE = "boss_damage";

    private int currentHits = 0;
    private float iFrameEndTime;
    private Coroutine flashCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Called by BossBullet when it hits the player.
    /// Respects i-frames. Accumulates hits and triggers game over on the final hit.
    /// </summary>
    public void TakeHit()
    {
        // Respect i-frames (both boss-fight i-frames and dialogue i-frames)
        if (Time.time < iFrameEndTime) return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.HasIFrames()) return;

        currentHits++;

        // Add a permanent damage icon to the status bar for each hit
        if (StatusEffectManager.Instance != null)
        {
            // Use unique IDs so multiple damage icons accumulate
            string effectId = EFFECT_BOSS_DAMAGE + "_" + currentHits;
            StatusEffectManager.Instance.AddEffect(effectId, -1f); // permanent
        }

        // Grant i-frames
        iFrameEndTime = Time.time + iFrameDuration;

        // Flash the player sprite during i-frames
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashSprite());

        // Check for death
        if (currentHits > maxHits)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Lost);
            }
        }
    }

    public bool IsInvulnerable()
    {
        return Time.time < iFrameEndTime;
    }

    public int GetCurrentHits()
    {
        return currentHits;
    }

    private IEnumerator FlashSprite()
    {
        if (playerSprite == null) yield break;

        float elapsed = 0f;
        Color original = playerSprite.color;

        while (elapsed < iFrameDuration)
        {
            // Toggle alpha between full and half
            Color c = playerSprite.color;
            c.a = c.a > 0.6f ? 0.3f : 1f;
            playerSprite.color = c;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // Restore original
        playerSprite.color = original;
        flashCoroutine = null;
    }
}
