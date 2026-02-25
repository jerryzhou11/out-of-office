using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class QTEManager : MonoBehaviour
{
    public enum QTEType { KeyPress, ClickTarget }

    [Header("UI References")]
    [SerializeField] private GameObject qtePanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI keyDisplayText;
    [SerializeField] private Image timerBarFill;
    [SerializeField] private Button clickTarget;
    [SerializeField] private RectTransform clickTargetRect;
    [SerializeField] private RectTransform qtePanelRect; // For positioning click target within bounds

    [Header("Timing")]
    [SerializeField] private float qteTimeLimit = 3f;

    [Header("Key Press Settings")]
    [SerializeField] private Key[] keyPool = new Key[]
    {
        Key.F, Key.G, Key.H, Key.J, Key.K, Key.L,
        Key.Q, Key.R, Key.T, Key.U, Key.I, Key.O, Key.P,
        Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M
    };

    [Header("Prompts - Key Press")]
    [SerializeField] private string[] keyPressPrompts = new string[]
    {
        "Press [{0}] to approve the budget",
        "Press [{0}] to sign the memo",
        "Press [{0}] to authorize the expense report",
        "Press [{0}] to confirm the meeting",
        "Press [{0}] to acknowledge receipt",
        "Press [{0}] to endorse the proposal",
        "Press [{0}] to initial the document",
        "Press [{0}] to stamp APPROVED"
    };

    [Header("Prompts - Click Target")]
    [SerializeField] private string[] clickTargetPrompts = new string[]
    {
        "Sign here \u2193",
        "Stamp this \u2193",
        "Initial here \u2193",
        "Click to approve \u2193",
        "Rubber-stamp this \u2193",
        "Put your John Hancock here \u2193"
    };

    [Header("Shake Feedback")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 10f;

    // Runtime state
    private QTEType currentType;
    private Key currentKey;
    private float timeRemaining;
    private bool qteActive = false;
    private Coroutine shakeCoroutine;

    // Callback to DialogueManager
    private System.Action onComplete;

    void Start()
    {
        if (qtePanel != null)
        {
            qtePanel.SetActive(false);
        }

        if (clickTarget != null)
        {
            clickTarget.onClick.AddListener(OnClickTargetHit);
        }
    }

    void Update()
    {
        if (!qteActive) return;

        // Timer countdown (uses unscaled time since timeScale is 1 during InDialogue,
        // but this is future-proof)
        timeRemaining -= Time.deltaTime;

        // Update timer bar
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = Mathf.Clamp01(timeRemaining / qteTimeLimit);
        }

        // Timer expired — reset with new prompt
        if (timeRemaining <= 0f)
        {
            ShakeAndReset();
            return;
        }

        // Handle key press QTE input
        if (currentType == QTEType.KeyPress)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Check if the correct key was pressed
            if (keyboard[currentKey].wasPressedThisFrame)
            {
                CompleteQTE();
                return;
            }

            // Check if ANY other key was pressed (wrong key)
            if (keyboard.anyKey.wasPressedThisFrame && !keyboard[currentKey].wasPressedThisFrame)
            {
                ShakeAndReset();
            }
        }

        // ClickTarget input is handled by the button's onClick listener
    }

    /// <summary>
    /// Called by DialogueManager to start a QTE. onCompleteCallback fires when the player succeeds.
    /// </summary>
    public void StartQTE(System.Action onCompleteCallback)
    {
        onComplete = onCompleteCallback;

        // Randomly pick QTE type
        currentType = (QTEType)Random.Range(0, 2);

        SetupQTE();
    }

    private void SetupQTE()
    {
        qteActive = true;
        timeRemaining = qteTimeLimit;

        if (qtePanel != null)
        {
            qtePanel.SetActive(true);
        }

        switch (currentType)
        {
            case QTEType.KeyPress:
                SetupKeyPress();
                break;
            case QTEType.ClickTarget:
                SetupClickTarget();
                break;
        }

        // Reset timer bar
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 1f;
        }
    }

    private void SetupKeyPress()
    {
        // Pick random key
        currentKey = keyPool[Random.Range(0, keyPool.Length)];
        string keyName = currentKey.ToString();

        // Pick random prompt
        string prompt = keyPressPrompts[Random.Range(0, keyPressPrompts.Length)];

        if (promptText != null)
        {
            promptText.text = string.Format(prompt, keyName);
        }

        if (keyDisplayText != null)
        {
            keyDisplayText.text = keyName;
            keyDisplayText.gameObject.SetActive(true);
        }

        // Hide click target for key press QTE
        if (clickTarget != null)
        {
            clickTarget.gameObject.SetActive(false);
        }
    }

    private void SetupClickTarget()
    {
        // Pick random prompt
        string prompt = clickTargetPrompts[Random.Range(0, clickTargetPrompts.Length)];

        if (promptText != null)
        {
            promptText.text = prompt;
        }

        // Hide key display for click QTE
        if (keyDisplayText != null)
        {
            keyDisplayText.gameObject.SetActive(false);
        }

        // Show and position click target randomly below the prompt text
        if (clickTarget != null && clickTargetRect != null && qtePanelRect != null)
        {
            clickTarget.gameObject.SetActive(true);

            Vector2 panelSize = qtePanelRect.rect.size;
            Vector2 targetSize = clickTargetRect.rect.size;
            float margin = 10f; // px padding from edges

            // Full horizontal range within the panel (with margin)
            float halfWidth = (panelSize.x * 0.5f) - targetSize.x * 0.5f - margin;

            // Vertical range: only below the prompt text
            // promptText is anchored relative to the QTE panel — get its bottom edge
            float textBottomY = 0f;
            if (promptText != null)
            {
                RectTransform textRect = promptText.GetComponent<RectTransform>();
                // Bottom edge of text in panel-local space
                textBottomY = textRect.anchoredPosition.y - textRect.rect.height * (1f - textRect.pivot.y);
            }

            // Panel bottom edge (from center) = -panelSize.y / 2
            float panelBottom = -panelSize.y * 0.5f + targetSize.y * 0.5f + margin;
            // Top of spawn area = just below the text
            float spawnTop = textBottomY - targetSize.y * 0.5f - margin;

            // Clamp in case the text fills most of the panel
            if (spawnTop < panelBottom) spawnTop = panelBottom;

            Vector2 randomPos = new Vector2(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(panelBottom, spawnTop)
            );

            clickTargetRect.anchoredPosition = randomPos;
        }
    }

    private void OnClickTargetHit()
    {
        if (!qteActive) return;
        if (currentType != QTEType.ClickTarget) return;

        CompleteQTE();
    }

    private void CompleteQTE()
    {
        qteActive = false;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (qtePanel != null)
        {
            qtePanel.SetActive(false);
        }

        onComplete?.Invoke();
        onComplete = null;
    }

    /// <summary>
    /// Force-cancel the QTE without invoking the callback (e.g. when day ends during dialogue).
    /// </summary>
    public void CancelQTE()
    {
        qteActive = false;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (qtePanel != null)
        {
            qtePanel.SetActive(false);
        }

        onComplete = null;
    }

    /// <summary>
    /// Wrong input or timeout: shake the panel and reset with a new random prompt.
    /// </summary>
    private void ShakeAndReset()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeAndResetCoroutine());
    }

    private IEnumerator ShakeAndResetCoroutine()
    {
        // Briefly disable QTE input during shake
        qteActive = false;

        // Shake the panel
        if (qtePanelRect != null)
        {
            Vector2 originalPos = qtePanelRect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
                qtePanelRect.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

                elapsed += Time.deltaTime;
                yield return null;
            }

            qtePanelRect.anchoredPosition = originalPos;
        }

        shakeCoroutine = null;

        // Re-randomize: could switch type or just pick a new prompt
        currentType = (QTEType)Random.Range(0, 2);
        SetupQTE();
    }
}
