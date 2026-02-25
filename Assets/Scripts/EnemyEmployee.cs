using UnityEngine;

public class EnemyEmployee : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Wander Behavior")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderChangeInterval = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask obstacleLayer; // Set to "Map" layer in Inspector
    [Tooltip("If true, enemies need clear line of sight to detect the player")]
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float avoidanceRayLength = 1.5f;
    [SerializeField] private float avoidanceStrength = 2f;
    [SerializeField] private int avoidanceRayCount = 3; // rays per side (center + left + right)
    [SerializeField] private float avoidanceSpreadAngle = 45f; // degrees from center

    [Header("Home Point")]
    [SerializeField] private bool useStartAsHome = true;
    [SerializeField] private Vector2 homePoint;

    private Rigidbody2D rb;
    private Collider2D col;
    private Collider2D playerCol;
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
    [SerializeField] private float returnHomeCooldown = 3f;
    private float canChaseAgainTime;

    private enum State { Wandering, Chasing, KnockedBack, Stunned, ReturningHome }
    private State currentState = State.Wandering;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color stunnedColor = new Color(0.7f, 0.7f, 0.3f);

    [Header("Dialogue")]
    [SerializeField] private string[] possibleDialogues = new string[]
    {
        "Hey boss! Got a minute?",
        "I need to talk to you about my performance review!",
        "Have you seen my TPS reports?",
        "Can we schedule a 1-on-1?",
        "I've been here since 6 AM waiting for you!"
    };

    private int lastDialogueIndex = -1;

    // Stuck detection for wander/return home
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_THRESHOLD = 0.1f; // If moved less than this in STUCK_TIME, pick new target
    private const float STUCK_TIME = 1f;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (useStartAsHome)
        {
            homePoint = transform.position;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCol = playerObj.GetComponent<Collider2D>();
        }

        PickNewWanderTarget();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        canChaseAgainTime = 0f;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (player == null) return;

        // Freeze all AI during non-playing states
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Knockback → Stunned transition
        if (isKnockedBack && Time.time >= knockbackEndTime)
        {
            isKnockedBack = false;
            EnterStunnedState();
        }

        if (isKnockedBack) return;

        // Stun → Wander transition
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
            EnterWanderState();
        }

        if (isStunned) return;

        // Stuck detection (for wander and return home)
        UpdateStuckDetection();

        // PRIORITY: ReturningHome can't be interrupted
        if (currentState == State.ReturningHome)
        {
            ReturnHome();
            return;
        }

        // Detection + state transitions
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool canChase = Time.time >= canChaseAgainTime;
        bool inRange = distanceToPlayer <= detectionRadius;
        bool hasLOS = !requireLineOfSight || HasLineOfSight();

        if (inRange && canChase && hasLOS)
        {
            if (currentState != State.Chasing)
            {
                EnterChaseState();
            }
        }
        else
        {
            // Lost sight of player while chasing → go back to wandering
            if (currentState == State.Chasing)
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

    // ---- Line of Sight ----

    /// <summary>
    /// Raycast from enemy to player. Returns true if no obstacle (Map layer) blocks the path.
    /// </summary>
    private bool HasLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;
        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleLayer);

        // If raycast hit nothing on the obstacle layer, we have clear LOS
        return hit.collider == null;
    }

    // ---- Obstacle Avoidance ----

    /// <summary>
    /// Given a desired movement direction, cast feeler rays and steer around obstacles.
    /// Returns the adjusted direction.
    /// </summary>
    private Vector2 AvoidObstacles(Vector2 desiredDirection, float speed)
    {
        if (obstacleLayer == 0) return desiredDirection; // No obstacle layer set

        Vector2 origin = (Vector2)transform.position;
        Vector2 avoidance = Vector2.zero;
        float rayLength = avoidanceRayLength * (speed / moveSpeed); // Scale with speed

        // Cast rays in a fan: center, then spread left and right
        for (int i = 0; i < avoidanceRayCount; i++)
        {
            float fraction = (avoidanceRayCount <= 1) ? 0f : (float)i / (avoidanceRayCount - 1);
            // Map from [0, 1] to [-spreadAngle, +spreadAngle]
            float angle = Mathf.Lerp(-avoidanceSpreadAngle, avoidanceSpreadAngle, fraction);

            Vector2 rayDir = RotateVector(desiredDirection.normalized, angle);
            RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, rayLength, obstacleLayer);

            if (hit.collider != null)
            {
                // Weight: closer hits push harder
                float weight = 1f - (hit.distance / rayLength);
                // Push perpendicular to the ray (away from the hit surface)
                avoidance += hit.normal * weight * avoidanceStrength;
            }
        }

        Vector2 result = (desiredDirection + avoidance).normalized;
        return result;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ---- Stuck Detection ----

    private void UpdateStuckDetection()
    {
        if (currentState == State.Wandering || currentState == State.ReturningHome)
        {
            float distMoved = Vector2.Distance(transform.position, lastPosition);
            if (distMoved < STUCK_THRESHOLD * Time.deltaTime * 60f) // Scale threshold by framerate
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= STUCK_TIME)
                {
                    OnStuck();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private void OnStuck()
    {
        if (currentState == State.Wandering)
        {
            // Pick a new wander target — the current one is probably behind a wall
            PickNewWanderTarget();
        }
        else if (currentState == State.ReturningHome)
        {
            // Can't reach home — just start wandering from here
            homePoint = transform.position;
            EnterWanderState();
        }
    }

    // ---- State Transitions ----

    void EnterWanderState()
    {
        currentState = State.Wandering;
        SetIgnoreCharacters(false);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        PickNewWanderTarget();
    }

    void EnterChaseState()
    {
        currentState = State.Chasing;
        SetIgnoreCharacters(false);
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
        SetIgnoreCharacters(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = stunnedColor;
        }
    }

    // ---- Movement States ----

    void Wander()
    {
        if (Time.time >= nextWanderTime)
        {
            PickNewWanderTarget();
        }

        Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
        direction = AvoidObstacles(direction, moveSpeed);
        rb.linearVelocity = direction * moveSpeed;

        if (Vector2.Distance(transform.position, wanderTarget) < 0.5f)
        {
            PickNewWanderTarget();
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        direction = AvoidObstacles(direction, chaseSpeed);
        rb.linearVelocity = direction * chaseSpeed;
    }

    void PickNewWanderTarget()
    {
        // Try a few times to find a target that isn't inside a wall
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            Vector2 candidate = homePoint + randomOffset;

            // Check if there's a clear path to the candidate
            Vector2 origin = (Vector2)transform.position;
            Vector2 dir = candidate - origin;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dir.magnitude, obstacleLayer);

            if (hit.collider == null)
            {
                wanderTarget = candidate;
                nextWanderTime = Time.time + wanderChangeInterval;
                return;
            }
        }

        // Fallback: just use a nearby point if all raycasts failed
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * 1f;
        nextWanderTime = Time.time + wanderChangeInterval;
    }

    void ReturnHome()
    {
        Vector2 direction = (homePoint - (Vector2)transform.position).normalized;
        direction = AvoidObstacles(direction, moveSpeed);
        rb.linearVelocity = direction * moveSpeed;

        if (Vector2.Distance(transform.position, homePoint) < 0.5f)
        {
            canChaseAgainTime = Time.time + returnHomeCooldown;
            EnterWanderState();
        }
    }

    // ---- Combat ----

    public void ApplyKnockback(Vector2 direction)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);

        isKnockedBack = true;
        knockbackEndTime = Time.time + knockbackDuration;
        currentState = State.KnockedBack;
        SetIgnoreCharacters(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = stunnedColor;
        }
    }

    // ---- Dialogue ----

    public void OnDialogueEnd()
    {
        canChaseAgainTime = Time.time + returnHomeCooldown;
        currentState = State.ReturningHome;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    private string GetRandomDialogue()
    {
        if (possibleDialogues.Length == 1)
        {
            return possibleDialogues[0];
        }

        int newIndex;

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
        if (currentState != State.Chasing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            DialogueManager dialogue = FindFirstObjectByType<DialogueManager>();
            if (dialogue != null)
            {
                string randomDialogue = GetRandomDialogue();
                dialogue.ShowDialogue(randomDialogue, this);
            }

            rb.linearVelocity = Vector2.zero;
            currentState = State.Wandering;
            ReturnHome();
        }
    }

    // ---- Collision Ignore ----

    private void SetIgnoreCharacters(bool ignore)
    {
        if (col == null) return;

        if (playerCol != null)
        {
            Physics2D.IgnoreCollision(col, playerCol, ignore);
        }

        foreach (var other in FindObjectsByType<EnemyEmployee>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            Collider2D otherCol = other.GetComponent<Collider2D>();
            if (otherCol != null)
            {
                Physics2D.IgnoreCollision(col, otherCol, ignore);
            }
        }
    }

    // ---- Gizmos ----

    void OnDrawGizmosSelected()
    {
        Vector3 homePos = useStartAsHome ? transform.position : (Vector3)homePoint;

        // Wander radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(homePos, wanderRadius);

        // Detection radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Line of sight to player (green = clear, red = blocked)
        if (Application.isPlaying && player != null)
        {
            bool los = HasLineOfSight();
            Gizmos.color = los ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
