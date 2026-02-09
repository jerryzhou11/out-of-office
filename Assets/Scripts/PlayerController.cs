using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private float lastAttackTime;
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
        // Don't allow input during dialogue
        DialogueManager dialogue = FindObjectOfType<DialogueManager>();
        if (dialogue != null && dialogue.IsDialogueActive())
        {
            movement = Vector2.zero;
            return;
        }
        
        // Simple polling for movement
        movement = Vector2.zero;
        
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) 
                movement.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) 
                movement.y -= 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) 
                movement.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) 
                movement.x += 1;
        }
        
        // Normalize diagonal movement
        movement = movement.normalized;
        
        // Simple polling for attack
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        
        // Update animator when we do animations
        if (animator != null)
        {
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
            animator.SetBool("IsMoving", movement.magnitude > 0);
        }
    }
    
    void FixedUpdate()
    {
        // Move the player
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
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
        }

        // Play attack animation when we make one
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}