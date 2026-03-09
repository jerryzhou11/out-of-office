using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI health bar for the boss using a filled Image sprite.
/// Set your fill Image's Image Type to "Filled", Fill Method to "Horizontal",
/// and Fill Origin to "Left". The script controls fillAmount for smooth drain.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;             // Image Type = Filled, Horizontal, Left
    [SerializeField] private Image backgroundImage;       // Optional: static background bar sprite
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private GameObject barContainer;     // Parent object to show/hide

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private Color healthColor = new Color(0.8f, 0.1f, 0.1f);   // Red
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.3f, 0f);     // Orange at low HP

    private int maxHealth;
    private float currentFill = 1f;
    private float targetFill = 1f;

    void Start()
    {
        if (barContainer != null)
        {
            barContainer.SetActive(false);
        }
    }

    void Update()
    {
        // Smooth lerp toward target fill
        if (fillImage != null && !Mathf.Approximately(currentFill, targetFill))
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);

            // Snap when close enough
            if (Mathf.Abs(currentFill - targetFill) < 0.001f)
            {
                currentFill = targetFill;
            }

            fillImage.fillAmount = currentFill;

            // Color shifts toward orange at low health
            fillImage.color = Color.Lerp(lowHealthColor, healthColor, currentFill);
        }
    }

    /// <summary>
    /// Set up the health bar with the boss's max health and display name.
    /// </summary>
    public void Initialize(int max, string bossName)
    {
        maxHealth = max;
        currentFill = 1f;
        targetFill = 1f;

        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
            fillImage.color = healthColor;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }
    }

    /// <summary>
    /// Update the health bar to reflect the current health. Lerps smoothly.
    /// </summary>
    public void SetHealth(int current)
    {
        if (maxHealth <= 0) return;
        targetFill = Mathf.Clamp01((float)current / maxHealth);
    }

    public void Show()
    {
        if (barContainer != null)
        {
            barContainer.SetActive(true);
        }
    }

    public void Hide()
    {
        if (barContainer != null)
        {
            barContainer.SetActive(false);
        }
    }
}
