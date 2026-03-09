using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton; // Fallback if no QTE manager assigned

    [Header("QTE")]
    [SerializeField] private QTEManager qteManager;

    [Header("Settings")]
    [SerializeField] private bool pauseGameDuringDialogue = true;

    private bool isDialogueActive = false;
    private EnemyEmployee currentEnemy; // Track which enemy we're talking to

    void Start()
    {
        // Hide dialogue at start
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Setup continue button as fallback (only used if no QTE manager)
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(CloseDialogue);
        }

        // Find QTEManager at runtime if not assigned (survives scene reloads)
        if (qteManager == null)
        {
            qteManager = FindFirstObjectByType<QTEManager>();
        }
    }

    public void ShowDialogue(string text, EnemyEmployee enemy = null, bool skipQTE = false, string confirmText = null)
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("DialogueManager: No dialogue panel assigned!");
            return;
        }

        // Save which enemy triggered this dialogue
        currentEnemy = enemy;

        // Show panel
        dialoguePanel.SetActive(true);

        // Set text
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }

        // Enter dialogue state — clock keeps ticking, player freezes
        if (pauseGameDuringDialogue && GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.InDialogue);
        }

        isDialogueActive = true;

        // Start QTE if manager is assigned and not skipped, otherwise show confirm button
        if (!skipQTE && qteManager != null)
        {
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            qteManager.StartQTE(OnQTEComplete);
        }
        else
        {
            // No QTE — hide QTE UI elements and show the confirm button
            if (qteManager != null) qteManager.HideUI();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);

                // Update button label if custom text provided
                if (confirmText != null)
                {
                    TextMeshProUGUI btnText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = confirmText;
                }
            }
        }
    }

    /// <summary>
    /// Called by QTEManager when the player completes the QTE successfully.
    /// </summary>
    private void OnQTEComplete()
    {
        CloseDialogue();
    }

    public void CloseDialogue()
    {
        // Cancel any active QTE cleanly
        if (qteManager != null)
        {
            qteManager.CancelQTE();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Tell the enemy to return home
        if (currentEnemy != null)
        {
            currentEnemy.OnDialogueEnd();
            currentEnemy = null;
        }

        // Resume playing
        if (pauseGameDuringDialogue && GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }

        // Grant player i-frames — pushes nearby enemies away and prevents immediate re-dialogue
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null) pc.ActivateIFrames();
        }

        isDialogueActive = false;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
