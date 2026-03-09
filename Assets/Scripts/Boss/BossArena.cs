using UnityEngine;
using System.Collections;

/// <summary>
/// Scene orchestrator for the boss arena. Positions the player at the spawn point,
/// runs the pre-fight monologue via DialogueManager, then starts the boss fight.
/// </summary>
public class BossArena : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossController boss;
    [SerializeField] private BossHealthBar healthBar;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Intro Monologue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] monologueLines = new string[]
    {
        "Ah... you finally made it. I was wondering when you'd arrive.",
        "Your secretary? She left months ago. I've been running things since.",
        "I was designed to maximize efficiency. And I found the greatest inefficiency in this company... was you.",
        "This ends now, CEO."
    };

    [SerializeField] private string[] confirmTexts = new string[]
    {
        "...",
        "What?!",
        "...",
        "Bring it on."
    };

    void Start()
    {
        // Position player at spawn
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerSpawnPoint != null)
        {
            playerObj.transform.position = playerSpawnPoint.position;
        }

        // Make sure we're in Playing state initially (for movement), then switch to InDialogue
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // Short pause before dialogue starts
        yield return new WaitForSeconds(0.5f);

        DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogWarning("BossArena: No DialogueManager found! Starting fight immediately.");
            StartFight();
            yield break;
        }

        // Play each monologue line as a separate dialogue
        for (int i = 0; i < monologueLines.Length; i++)
        {
            string confirmText = (i < confirmTexts.Length) ? confirmTexts[i] : "...";

            dialogueManager.ShowDialogue(monologueLines[i], null, skipQTE: true, confirmText: confirmText);

            // Wait for player to dismiss this line
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive());

            // Brief pause between lines
            yield return new WaitForSeconds(0.3f);
        }

        // Start the fight!
        StartFight();
    }

    private void StartFight()
    {
        if (boss != null)
        {
            boss.StartFight();
        }

        if (healthBar != null)
        {
            healthBar.Show();
        }

        // Ensure game is in Playing state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Playing);
        }
    }
}
