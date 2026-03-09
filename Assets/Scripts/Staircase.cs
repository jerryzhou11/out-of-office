using UnityEngine;
using UnityEngine.SceneManagement;

public class Staircase : MonoBehaviour
{
    [Header("Visual (Optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color staircaseColor = new Color(0.4f, 0.8f, 0.4f); // Green

    [Header("Override (Boss Arena)")]
    [Tooltip("If set, loads this scene instead of progressing to the next floor. Use for the final staircase leading to the boss.")]
    [SerializeField] private string overrideSceneName = "";

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.color = staircaseColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        // Override: load a specific scene (e.g., boss arena) instead of normal progression
        if (!string.IsNullOrEmpty(overrideSceneName))
        {
            SceneManager.LoadScene(overrideSceneName);
            return;
        }

        // Last floor: show win screen. Otherwise: go straight to next floor.
        if (GameManager.Instance.IsLastFloor())
        {
            GameManager.Instance.SetState(GameManager.GameState.Won);
        }
        else
        {
            GameManager.Instance.NextFloor();
        }
    }

    void OnDrawGizmos()
    {
        // Draw a green marker in the editor so you can see it
        Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        Gizmos.DrawCube(transform.position, Vector3.one);
    }
}