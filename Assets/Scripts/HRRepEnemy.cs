using UnityEngine;

/// <summary>
/// HR Rep enemy — subclass of EnemyEmployee.
/// Same AI (wander, chase, LOS, avoidance) but on collision:
///   1. Opens HR-themed dialogue
///   2. Temporarily disables the player's attack
///   3. Ignores the player for the rest of the level (one interaction per unit)
/// </summary>
public class HRRepEnemy : EnemyEmployee
{
    [Header("HR Rep Settings")]
    [SerializeField] private float attackDisableDuration = 5f;

    [Header("HR Dialogue")]
    [SerializeField] private string[] hrDialogues = new string[]
    {
        "You need to stop slapping your employees around!",
        "I'm getting too many complaints about your... management style.",
        "HR has received multiple reports of workplace violence!",
        "This is your formal warning about physical conduct!"
    };

    [SerializeField] private string debuffTooltip = "<i>Your attack has been temporarily disabled!</i>";
    [SerializeField] private string confirmButtonText = "Fine, I understand.";

    private bool hasInteracted = false;
    private int lastHRDialogueIndex = -1;

    protected override void HandlePlayerCollision(Collision2D collision)
    {
        // Already used our one interaction — ignore the player
        if (hasInteracted) return;

        // Don't trigger if player has i-frames
        PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc != null && pc.HasIFrames()) return;

        hasInteracted = true;

        // Open dialogue with HR-specific text + debuff tooltip
        DialogueManager dialogue = FindFirstObjectByType<DialogueManager>();
        if (dialogue != null)
        {
            string hrText = GetRandomHRDialogue() + "\n\n" + debuffTooltip;
            dialogue.ShowDialogue(hrText, this, skipQTE: true, confirmText: confirmButtonText);
        }

        rb.linearVelocity = Vector2.zero;
    }

    public override void OnDialogueEnd()
    {
        base.OnDialogueEnd(); // ReturningHome state + normal cooldown

        // Disable the player's attack
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.DisableAttack(attackDisableDuration);
            }
        }

        // Never chase this player again
        canChaseAgainTime = float.MaxValue;
    }

    private string GetRandomHRDialogue()
    {
        if (hrDialogues.Length == 0) return "HR would like a word with you.";
        if (hrDialogues.Length == 1) return hrDialogues[0];

        int newIndex;
        do
        {
            newIndex = Random.Range(0, hrDialogues.Length);
        }
        while (newIndex == lastHRDialogueIndex);

        lastHRDialogueIndex = newIndex;
        return hrDialogues[newIndex];
    }
}
