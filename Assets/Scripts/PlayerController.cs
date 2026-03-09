using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Invincibility Frames")]
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float iFramePushRadius = 2f;
    [SerializeField] private float iFramePushForce = 8f;

    // Status effect IDs used by StatusEffectManager
    public const string EFFECT_ATTACK_DISABLED = "attack_disabled";
    public const string EFFECT_COFFEE_SPEED = "coffee_speed";

    private float speedBonus = 0f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float lastAttackTime;
    private float iFrameEndTime;
    private float attackDisableEndTime;
    private Animator animator;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    [SerializeField] private AttackSlash slashEffect;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Try to find slash effect if not assigned
        if (slashEffect == null)
        {
            slashEffect = GetComponent<AttackSlash>();
        }
    }

    void Update()
    {
        // Freeze if not in Playing state (covers Paused, Won, Lost, InDialogue)
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            movement = Vector2.zero;
            return;
        }

        // Simple polling for movement
        movement = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool isUp = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            bool isDown = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            bool isLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool isRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            bool isMoving = isUp || isDown || isLeft || isRight;

            if (isUp)
                movement.y += 1;
            if (isDown)
                movement.y -= 1;
            if (isLeft) 
                movement.x -= 1;
            if (isRight) 
                movement.x += 1;


            // Update animator when we do animations
            if (animator != null)
            {
                float speed = isMoving ? moveSpeed : 0f;
                animator.SetFloat("Speed", speed);
                animator.SetBool("IsUp", isUp);
                animator.SetBool("IsDown", isDown);
                animator.SetBool("IsLeft", isLeft);
                animator.SetBool("IsRight", isRight);
                animator.SetBool("IsMoving", movement.magnitude > 0);
            }
        }
        
        // Normalize diagonal movement
        movement = movement.normalized;
        
        // Simple polling for attack
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= lastAttackTime + attackCooldown && !IsAttackDisabled())
            {
                Attack();
            }
        }
        
        
    }
    
    void FixedUpdate()
    {
        // Move the player (base speed + any bonus from pickups)
        rb.MovePosition(rb.position + movement * (moveSpeed + speedBonus) * Time.fixedDeltaTime);
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        // Get mouse position in world space
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());

        // Calculate direction to mouse
        Vector2 attackDirection = (mousePos - (Vector2)transform.position).normalized;
        
        // Play slash effect
        if (slashEffect != null)
        {
            slashEffect.PlaySlash(attackDirection);
        }

        // Calculate attack position
        Vector2 attackPoint = (Vector2)transform.position + attackDirection * attackRange;

        // Check for employees in range and push them back
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, 0.5f);
        
        foreach (Collider2D hit in hits)
        {
            EnemyEmployee employee = hit.GetComponent<EnemyEmployee>();
            if (employee != null)
            {
                // Push the employee away from the player
                Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                employee.ApplyKnockback(knockbackDir);
            }

            // Boss fight: check for exposed critical points
            CriticalPoint critPoint = hit.GetComponent<CriticalPoint>();
            if (critPoint != null)
            {
                critPoint.OnMeleeHit();
            }
        }

        // Play attack animation when we make one
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    // ---- Speed Boost (Coffee) ----

    public void ApplySpeedBoost(float bonus)
    {
        speedBonus += bonus;

        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.AddEffect(EFFECT_COFFEE_SPEED, -1f); // permanent for the day
        }
    }

    // ---- Attack Disable ----

    public void DisableAttack(float duration)
    {
        attackDisableEndTime = Time.time + duration;

        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.AddEffect(EFFECT_ATTACK_DISABLED, duration);
        }
    }

    public bool IsAttackDisabled()
    {
        return Time.time < attackDisableEndTime;
    }

    // ---- Invincibility Frames ----

    /// <summary>
    /// Called by DialogueManager when dialogue closes.
    /// Pushes all nearby enemies away and grants brief dialogue immunity.
    /// </summary>
    public void ActivateIFrames()
    {
        iFrameEndTime = Time.time + iFrameDuration;

        // AoE push: knock back all enemies in radius
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, iFramePushRadius);
        foreach (Collider2D col in nearby)
        {
            EnemyEmployee enemy = col.GetComponent<EnemyEmployee>();
            if (enemy != null)
            {
                Vector2 pushDir = (col.transform.position - transform.position).normalized;
                enemy.ApplyKnockback(pushDir, iFramePushForce);
            }
        }
    }

    public bool HasIFrames()
    {
        return Time.time < iFrameEndTime;
    }
}