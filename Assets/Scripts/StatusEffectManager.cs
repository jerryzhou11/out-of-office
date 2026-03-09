using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages status effect icons in the UI. Effects can be temporary (flashing)
/// or permanent (steady). Icons auto-layout via a HorizontalLayoutGroup on the container.
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    public static StatusEffectManager Instance { get; private set; }

    [System.Serializable]
    public class StatusEffectDef
    {
        public string id;      // e.g. "attack_disabled", "coffee_speed"
        public Sprite icon;
    }

    [Header("Definitions")]
    [Tooltip("Configure all possible status effects and their icons here")]
    [SerializeField] private StatusEffectDef[] effectDefinitions;

    [Header("UI References")]
    [SerializeField] private Transform iconContainer; // HorizontalLayoutGroup panel
    [SerializeField] private GameObject iconPrefab;   // Simple Image prefab (48x48)

    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 3f;   // Pulse speed for temporary effects

    private class ActiveEffect
    {
        public string id;
        public float expireTime; // -1 = permanent
        public GameObject uiObject;
        public Image iconImage;
    }

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    void Awake()
    {
        // Scene-level singleton (not DontDestroyOnLoad)
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

    void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];

            // Auto-expire temporary effects
            if (effect.expireTime >= 0f && Time.time >= effect.expireTime)
            {
                RemoveEffectAt(i);
                continue;
            }

            // Flash temporary effects, keep permanent ones steady
            if (effect.iconImage != null)
            {
                if (effect.expireTime >= 0f)
                {
                    // Temporary: pulse alpha between 0.3 and 1.0
                    float alpha = Mathf.PingPong(Time.time * flashSpeed, 1f) * 0.7f + 0.3f;
                    Color c = effect.iconImage.color;
                    c.a = alpha;
                    effect.iconImage.color = c;
                }
                else
                {
                    // Permanent: full alpha
                    Color c = effect.iconImage.color;
                    c.a = 1f;
                    effect.iconImage.color = c;
                }
            }
        }
    }

    /// <summary>
    /// Add a status effect icon. duration > 0 = temporary (flashing), duration = -1 = permanent.
    /// If the effect already exists, refreshes its duration.
    /// </summary>
    public void AddEffect(string id, float duration = -1f)
    {
        // If already active, refresh duration
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].id == id)
            {
                activeEffects[i].expireTime = duration > 0f ? Time.time + duration : -1f;
                return;
            }
        }

        // Find the definition for this effect
        StatusEffectDef def = null;
        foreach (var d in effectDefinitions)
        {
            if (d.id == id)
            {
                def = d;
                break;
            }
        }

        if (def == null)
        {
            Debug.LogWarning($"StatusEffectManager: No definition found for effect '{id}'");
            return;
        }

        if (iconPrefab == null || iconContainer == null)
        {
            Debug.LogWarning("StatusEffectManager: Missing iconPrefab or iconContainer");
            return;
        }

        // Instantiate icon
        GameObject iconObj = Instantiate(iconPrefab, iconContainer);
        Image img = iconObj.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = def.icon;
        }
        iconObj.SetActive(true);

        ActiveEffect effect = new ActiveEffect
        {
            id = id,
            expireTime = duration > 0f ? Time.time + duration : -1f,
            uiObject = iconObj,
            iconImage = img
        };

        activeEffects.Add(effect);
    }

    /// <summary>
    /// Remove a status effect by ID. The icon is destroyed and remaining icons shift automatically.
    /// </summary>
    public void RemoveEffect(string id)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].id == id)
            {
                RemoveEffectAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Check if a status effect is currently active.
    /// </summary>
    public bool HasEffect(string id)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.id == id) return true;
        }
        return false;
    }

    private void RemoveEffectAt(int index)
    {
        if (activeEffects[index].uiObject != null)
        {
            Destroy(activeEffects[index].uiObject);
        }
        activeEffects.RemoveAt(index);
    }
}
