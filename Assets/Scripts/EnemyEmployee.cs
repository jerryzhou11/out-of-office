using UnityEngine;

public class EnemyEmployee : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 3f; // How far from home point
    [SerializeField] private float wanderChangeInterval = 2f; // How often to pick new wander point

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;

    [Header("Home Point")]
    [SerializeField] private bool useStartAsHome = true;
    [SerializeField] private Vector2 homePoint;

    private Rigidbody2D rb;
    private Transform player;
    private Vector2 wanderTarget;
    private float nextWanderTime;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;

    private float knockbackEndTime;
    private bool isKnockedBack = false;

    [Header("Stun (after knockback)")]
    [SerializeField] private float stunDuration = 1f;

    private float stunEndTime;
    private bool isStunned = false;

    [Header("Post-Dialogue Cooldown")]
    [SerializeField] private float returnHomeCooldown = 3f; // How long to ignore player after returning home
    private float canChaseAgainTime;

    private enum State { Wandering, Chasing, KnockedBack, Stunned, ReturningHome }
    private State currentState = State.Wandering;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color stunnedColor = new Color(0.7f, 0.7f, 0.3f); // Yellowish daze

    [Header("Dialogue")]
    [SerializeField] private string[] possibleDialogues = new string[]
    {
        "Hey boss! Got a minute?",
        "I need to talk to you about my performance review!",
        "Have you seen my TPS reports?",
        "Can we schedule a 1-on-1?",
        "I've been here since 6 AM waiting for you!"
    };

    private int lastDialogueIndex = -1; // Track last dialogue to prevent repeats


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Set home point to starting position
        if (useStartAsHome)
        {
            homePoint = transform.position;
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Pick first wander target
        PickNewWanderTarget();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        // Allow chasing immediately at start
        canChaseAgainTime = 0f;
    }

    void Update()
    {
        if (player == null) return;

        // Freeze all AI during dialogue (and other non-playing states except Playing)
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Check if knockback has ended → transition to stunned
        if (isKnockedBack && Time.time >= knockbackEndTime)
        {
            isKnockedBack = false;
            EnterStunnedState();
        }

        // Don't do normal AI during knockback
        if (isKnockedBack)
        {
            return;
        }

        // Check if stun has ended → return to wander
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            EnterWanderState();
        }

        // Don't do normal AI during stun
        if (isStunned)
        {
            return;
        }

        // PRIORITY: Handle ReturningHome state first - don't let anything interrupt it
        if (currentState == State.ReturningHome)
        {
            ReturnHome();
            return; // Exit early - don't do any other state checks
        }

        // Check distance to player (only when not returning home)
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // State transitions
        // Only chase if cooldown has expired
        bool canChase = Time.time >= canChaseAgainTime;

        if (distanceToPlayer <= detectionRadius && canChase)
        {
            if (currentState != State.Chasing)
            {
                EnterChaseState();
            }
        }
        else
        {
            if (currentState != State.Wandering)
            {
                EnterWanderState();
            }
        }

        // Execute current state
        switch (currentState)
        {
            case State.Wandering:
                Wander();
                break;
            case State.Chasing:
                ChasePlayer();
                break;
        }
    }

    void EnterWanderState()
    {
        currentState = State.Wandering;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        PickNewWanderTarget();
    }

    void EnterChaseState()
    {
        currentState = State.Chasing;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = chaseColor;
        }
    }

    void EnterStunnedState()
    {
        currentState = State.Stunned;
        isStunned = true;
        stunEndTime = Time.time + stunDuration;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = stunnedColor;
        }
    }

    void Wander()
    {
        // Pick new wander target periodically
        if (Time.time >= nextWanderTime)
        {
            PickNewWanderTarget();
        }

        // Move toward wander target
        Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // If close enough to target, pick new one
        if (Vector2.Distance(transform.position, wanderTarget) < 0.5f)
        {
            PickNewWanderTarget();
        }
    }

    void ChasePlayer()
    {
        // Move toward player
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
    }

    void PickNewWanderTarget()
    {
        // Pick random point within wander radius of home
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = homePoint + randomOffset;

        nextWanderTime = Time.time + wanderChangeInterval;
    }

    void ReturnHome()
    {
        // Move toward home point
        Vector2 direction = (homePoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // If close enough to home, start wandering again
        if (Vector2.Distance(transform.position, homePoint) < 0.5f)
        {
            // Set cooldown before allowing chase again
            canChaseAgainTime = Time.time + returnHomeCooldown;
            EnterWanderState();
        }
    }

    // Called by player attack
    public void ApplyKnockback(Vector2 direction)
    {
        // Apply knockback force
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);

        // Set knockback state
        isKnockedBack = true;
        knockbackEndTime = Time.time + knockbackDuration;
        currentState = State.KnockedBack;

        // Immediately show stunned color — stays through knockback + stun
        if (spriteRenderer != null)
        {
            spriteRenderer.color = stunnedColor;
        }
    }

    // Called by DialogueManager when dialogue closes
    public void OnDialogueEnd()
    {
        // Immediately set cooldown to prevent chasing right away
        canChaseAgainTime = Time.time + returnHomeCooldown;
        currentState = State.ReturningHome;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    // Get a random dialogue that's different from the last one
    private string GetRandomDialogue()
    {
        // If only one dialogue, just return it
        if (possibleDialogues.Length == 1)
        {
            return possibleDialogues[0];
        }

        int newIndex;

        // Keep picking until we get a different one
        do
        {
            newIndex = Random.Range(0, possibleDialogues.Length);
        }
        while (newIndex == lastDialogueIndex);

        lastDialogueIndex = newIndex;
        return possibleDialogues[newIndex];
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only trigger dialogue when aggroed (chasing) — not while wandering, stunned, etc.
        if (currentState != State.Chasing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            DialogueManager dialogue = FindFirstObjectByType<DialogueManager>();
            if (dialogue != null)
            {
                // Get random dialogue (won't repeat last one)
                string randomDialogue = GetRandomDialogue();
                dialogue.ShowDialogue(randomDialogue, this);
            }

            rb.linearVelocity = Vector2.zero;
            currentState = State.Wandering; // Pause AI during dialogue
            ReturnHome();
        }
    }

    // Visualize detection radius in editor
    void OnDrawGizmosSelected()
    {
        Vector3 homePos = useStartAsHome ? transform.position : (Vector3)homePoint;

        // Wander radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(homePos, wanderRadius);

        // Detection radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
