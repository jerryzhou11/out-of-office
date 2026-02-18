using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton;
    
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
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(CloseDialogue);
        }
    }
    
    public void ShowDialogue(string text, EnemyEmployee enemy = null)
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
    }
    
    public void CloseDialogue()
    {
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

        isDialogueActive = false;
    }
    
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}