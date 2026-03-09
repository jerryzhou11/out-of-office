using UnityEngine;
using System.Collections;

/// <summary>
/// Central boss AI for A.R.I.A. — the rogue AI that replaced the CEO's secretary.
/// Manages 3 phases of escalating bullet-hell patterns and periodic critical point exposure.
/// Stationary boss at center of arena.
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int phase2Threshold = 66; // HP% to enter phase 2
    [SerializeField] private int phase3Threshold = 33; // HP% to enter phase 3

    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private BossHealthBar healthBar;
    [SerializeField] private CriticalPoint[] criticalPoints; // 4 terminals around the arena
    [SerializeField] private Transform bulletSpawnPoint; // Where bullets originate (boss center)
    [SerializeField] private SpriteRenderer bossSprite;

    [Header("Boss Name")]
    [SerializeField] private string bossName = "A.R.I.A.";

    [Header("Bullet Speeds (per phase)")]
    [SerializeField] private float phase1BulletSpeed = 4f;
    [SerializeField] private float phase2BulletSpeed = 6f;
    [SerializeField] private float phase3BulletSpeed = 8f;

    [Header("Phase 1 — Spread + Radial")]
    [SerializeField] private float p1AttackInterval = 2f;
    [SerializeField] private int p1SpreadCount = 5;
    [SerializeField] private float p1SpreadAngle = 30f;
    [SerializeField] private int p1RadialCount = 12;

    [Header("Phase 2 — Add Spiral")]
    [SerializeField] private float p2AttackInterval = 1.5f;
    [SerializeField] private int p2SpreadCount = 7;
    [SerializeField] private float p2SpreadAngle = 40f;
    [SerializeField] private int p2RadialCount = 18;
    [SerializeField] private int p2SpiralArms = 3;
    [SerializeField] private int p2SpiralBulletsPerArm = 8;

    [Header("Phase 3 — Add Cross")]
    [SerializeField] private float p3AttackInterval = 1.2f;
    [SerializeField] private int p3SpreadCount = 8;
    [SerializeField] private float p3SpreadAngle = 45f;
    [SerializeField] private int p3RadialCount = 24;
    [SerializeField] private int p3SpiralArms = 4;
    [SerializeField] private int p3SpiralBulletsPerArm = 10;
    [SerializeField] private float p3CrossRotateSpeed = 30f; // degrees per second

    [Header("Phase Transition")]
    [SerializeField] private float phaseTransitionPause = 2f;
    [SerializeField] private string phase2Dialogue = "Impressive. But I'm only getting started.";
    [SerializeField] private string phase3Dialogue = "ENOUGH. Maximum efficiency protocols engaged!";
    [SerializeField] private string deathDialogue = "No... this can't... I was... optimal...";

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color invulnerableColor = new Color(0.5f, 0.5f, 1f, 0.5f);

    // Runtime state
    private int currentHealth;
    private int currentPhase = 1;
    private bool isFighting = false;
    private bool isTransitioning = false;
    private Transform player;

    // Active coroutines (so we can stop them on phase change)
    private Coroutine attackLoopCoroutine;
    private Coroutine crossPatternCoroutine;

    void Start()
    {
        if (bulletSpawnPoint == null)
            bulletSpawnPoint = transform;

        if (bossSprite == null)
            bossSprite = GetComponent<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    /// <summary>
    /// Called by BossArena after the intro dialogue completes.
    /// </summary>
    public void StartFight()
    {
        currentHealth = maxHealth;
        currentPhase = 1;
        isFighting = true;
        isTransitioning = false;

        // Freeze the game clock and hide its UI — boss fight has no time limit
        if (GameClock.Instance != null)
        {
            GameClock.Instance.FreezeAndHide();
        }

        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth, bossName);
            healthBar.Show();
        }

        StartPhaseLoops();
    }

    /// <summary>
    /// Called by CriticalPoint when the player melee-hits an exposed terminal.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isFighting || isTransitioning) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Check for phase transitions
        float healthPercent = (float)currentHealth / maxHealth * 100f;

        if (currentPhase == 1 && healthPercent <= phase2Threshold)
        {
            StartCoroutine(TransitionToPhase(2));
        }
        else if (currentPhase == 2 && healthPercent <= phase3Threshold)
        {
            StartCoroutine(TransitionToPhase(3));
        }
    }

    // ---- Phase Management ----

    private void StartPhaseLoops()
    {
        StopPhaseLoops();
        attackLoopCoroutine = StartCoroutine(AttackLoop());
    }

    private void StopPhaseLoops()
    {
        if (attackLoopCoroutine != null)
        {
            StopCoroutine(attackLoopCoroutine);
            attackLoopCoroutine = null;
        }
        if (crossPatternCoroutine != null)
        {
            StopCoroutine(crossPatternCoroutine);
            crossPatternCoroutine = null;
        }
    }

    private IEnumerator TransitionToPhase(int newPhase)
    {
        isTransitioning = true;

        StopPhaseLoops();
        ClearAllBullets();

        // Brief invulnerability visual
        if (bossSprite != null)
            bossSprite.color = invulnerableColor;

        // Show phase transition dialogue
        string dialogue = newPhase == 2 ? phase2Dialogue : phase3Dialogue;
        DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(dialogue, null, skipQTE: true, confirmText: "...");

            // Wait for dialogue to close
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive());
        }

        yield return new WaitForSeconds(phaseTransitionPause);

        // Restore visual
        if (bossSprite != null)
            bossSprite.color = normalColor;

        currentPhase = newPhase;
        isTransitioning = false;

        StartPhaseLoops();
    }

    private void Die()
    {
        isFighting = false;
        StopPhaseLoops();
        ClearAllBullets();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Brief flash
        if (bossSprite != null)
            bossSprite.color = invulnerableColor;

        yield return new WaitForSeconds(0.5f);

        // Death dialogue
        DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogue(deathDialogue, null, skipQTE: true, confirmText: "Goodbye, A.R.I.A.");

            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive());
        }

        yield return new WaitForSeconds(0.5f);

        // Win!
        if (healthBar != null)
            healthBar.Hide();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Won);
    }

    // ---- Attack Loop ----

    private IEnumerator AttackLoop()
    {
        // Initial delay before first attack
        yield return new WaitForSeconds(1f);

        int patternIndex = 0;

        while (isFighting && !isTransitioning)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            // Don't attack during dialogue
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.InDialogue)
            {
                yield return null;
                continue;
            }

            float interval;
            float bulletSpeed;

            switch (currentPhase)
            {
                case 1:
                    interval = p1AttackInterval;
                    bulletSpeed = phase1BulletSpeed;
                    // Alternate between spread and radial
                    if (patternIndex % 2 == 0)
                        yield return StartCoroutine(SpreadBurst(p1SpreadCount, p1SpreadAngle, bulletSpeed));
                    else
                        yield return StartCoroutine(RadialBurst(p1RadialCount, bulletSpeed));
                    break;

                case 2:
                    interval = p2AttackInterval;
                    bulletSpeed = phase2BulletSpeed;
                    // Cycle: spread, radial, spiral
                    int p2Pattern = patternIndex % 3;
                    if (p2Pattern == 0)
                        yield return StartCoroutine(SpreadBurst(p2SpreadCount, p2SpreadAngle, bulletSpeed));
                    else if (p2Pattern == 1)
                        yield return StartCoroutine(RadialBurst(p2RadialCount, bulletSpeed));
                    else
                        yield return StartCoroutine(SpiralPattern(p2SpiralArms, p2SpiralBulletsPerArm, bulletSpeed));
                    break;

                case 3:
                default:
                    interval = p3AttackInterval;
                    bulletSpeed = phase3BulletSpeed;
                    // Cycle: spread, radial, spiral, combo(spread+radial)
                    int p3Pattern = patternIndex % 4;
                    if (p3Pattern == 0)
                        yield return StartCoroutine(SpreadBurst(p3SpreadCount, p3SpreadAngle, bulletSpeed));
                    else if (p3Pattern == 1)
                        yield return StartCoroutine(RadialBurst(p3RadialCount, bulletSpeed));
                    else if (p3Pattern == 2)
                        yield return StartCoroutine(SpiralPattern(p3SpiralArms, p3SpiralBulletsPerArm, bulletSpeed));
                    else
                    {
                        // Combo: fire spread + start cross simultaneously
                        yield return StartCoroutine(SpreadBurst(p3SpreadCount, p3SpreadAngle, bulletSpeed));
                        if (crossPatternCoroutine == null)
                            crossPatternCoroutine = StartCoroutine(CrossPattern(p3CrossRotateSpeed, bulletSpeed));
                    }
                    break;
            }

            patternIndex++;
            yield return new WaitForSeconds(interval);
        }
    }

    // ---- Attack Patterns ----

    /// <summary>
    /// Fan of bullets aimed at the player's current position.
    /// </summary>
    private IEnumerator SpreadBurst(int count, float spreadAngle, float speed)
    {
        if (player == null) yield break;

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)bulletSpawnPoint.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float fraction = count <= 1 ? 0f : (float)i / (count - 1);
            float angle = baseAngle + Mathf.Lerp(-spreadAngle, spreadAngle, fraction);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            SpawnBullet(dir, speed);
        }

        yield return null;
    }

    /// <summary>
    /// Ring of bullets in 360° with evenly-spaced gaps.
    /// Rotated randomly each time so gaps aren't always in the same place.
    /// </summary>
    private IEnumerator RadialBurst(int count, float speed)
    {
        float randomOffset = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = randomOffset + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            SpawnBullet(dir, speed);
        }

        yield return null;
    }

    /// <summary>
    /// Rotating spiral arms that emit bullets over time, creating sweeping arcs.
    /// </summary>
    private IEnumerator SpiralPattern(int arms, int bulletsPerArm, float speed)
    {
        float rotationSpeed = 120f; // degrees per second
        float timeBetweenBullets = 0.1f;

        for (int b = 0; b < bulletsPerArm; b++)
        {
            if (!isFighting || isTransitioning) yield break;

            float baseAngle = Time.time * rotationSpeed;

            for (int a = 0; a < arms; a++)
            {
                float angle = baseAngle + (360f / arms) * a;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                SpawnBullet(dir, speed);
            }

            yield return new WaitForSeconds(timeBetweenBullets);
        }
    }

    /// <summary>
    /// 4 streams forming a + shape that slowly rotates. Continuous until stopped.
    /// Runs as a separate coroutine for a fixed duration.
    /// </summary>
    private IEnumerator CrossPattern(float rotateSpeed, float speed)
    {
        float duration = 4f;
        float timeBetweenBullets = 0.15f;
        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < duration && isFighting && !isTransitioning)
        {
            currentAngle += rotateSpeed * Time.deltaTime;

            for (int i = 0; i < 4; i++)
            {
                float angle = currentAngle + 90f * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                SpawnBullet(dir, speed);
            }

            elapsed += timeBetweenBullets;
            yield return new WaitForSeconds(timeBetweenBullets);
        }

        crossPatternCoroutine = null;
    }

    // ---- Bullet Spawning ----

    private void SpawnBullet(Vector2 direction, float speed)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        BossBullet bb = bullet.GetComponent<BossBullet>();
        if (bb != null)
        {
            bb.Initialize(direction, speed);
        }

        // Rotate bullet sprite to face travel direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---- Cleanup ----

    private void ClearAllBullets()
    {
        BossBullet[] bullets = FindObjectsByType<BossBullet>(FindObjectsSortMode.None);
        foreach (BossBullet bullet in bullets)
        {
            if (bullet != null)
                Destroy(bullet.gameObject);
        }
    }

    /// <summary>
    /// Force-stop the fight (e.g., if clock runs out).
    /// </summary>
    public void ForceStop()
    {
        isFighting = false;
        StopPhaseLoops();
        ClearAllBullets();
    }

    // ---- Gizmos ----

    void OnDrawGizmosSelected()
    {
        // Show critical point connections
        if (criticalPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (CriticalPoint cp in criticalPoints)
            {
                if (cp != null)
                {
                    Gizmos.DrawLine(transform.position, cp.transform.position);
                    Gizmos.DrawWireSphere(cp.transform.position, 0.5f);
                }
            }
        }
    }
}
