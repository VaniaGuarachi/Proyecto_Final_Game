using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class AcidSlimeAI : MonoBehaviour
{
    private enum AcidSlimeState
    {
        Idle,
        Bounce,
        Attack,
        Hurt,
        Die
    }

    [Header("Movimiento autonomo")]
    [SerializeField] private float walkSpeed = 1.15f;
    [SerializeField] private float jumpForce = 4.6f;
    [SerializeField] private float minDecisionTime = 0.85f;
    [SerializeField] private float maxDecisionTime = 2.6f;
    [SerializeField] private float idleChance = 0.22f;

    [Header("Deteccion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 0.34f;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float edgeCheckDistance = 0.55f;

    [Header("Combate")]
    [SerializeField] private float attackDetectionRadius = 4.2f;
    [SerializeField] private float captureThreatRadius = 2.7f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackWindup = 0.26f;
    [SerializeField] private float attackDuration = 0.82f;
    [SerializeField] private float projectileSpeed = 6.8f;
    [SerializeField] private float projectileLifetime = 2.4f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.34f, 0.06f);

    [Header("Animacion por sprites")]
    [SerializeField] private string resourcePath = "Slimes/Acid";
    [SerializeField] private float idleFrameRate = 8f;
    [SerializeField] private float bounceFrameRate = 13f;
    [SerializeField] private float attackFrameRate = 15f;
    [SerializeField] private float hurtFrameRate = 12f;
    [SerializeField] private float dieFrameRate = 10f;

    private readonly Dictionary<AcidSlimeState, Sprite[]> framesByState = new Dictionary<AcidSlimeState, Sprite[]>();
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform targetPlayer;
    private Vector3 baseScale;
    private AcidSlimeState state = AcidSlimeState.Idle;
    private float direction = 1f;
    private float decisionTimer;
    private float attackCooldownTimer;
    private float attackTimer;
    private float animationTimer;
    private int frameIndex;
    private bool isIdling;
    private bool projectileFired;
    private Sprite projectileSprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        LoadFrames();
        PickNextDecision();
    }

    private void Update()
    {
        attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - Time.deltaTime);
        FindPlayerIfNeeded();

        if (state == AcidSlimeState.Attack)
        {
            TickAttack();
        }
        else
        {
            TickMovementDecision();
            TryReactToCaptureInput();
        }

        Animate();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (state == AcidSlimeState.Attack || state == AcidSlimeState.Die)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float horizontalSpeed = isIdling ? 0f : direction * walkSpeed;
        rb.linearVelocity = new Vector2(horizontalSpeed, rb.linearVelocity.y);

        if (!isIdling)
        {
            HandleObstacleNavigation();
        }
    }

    public bool ReactToCaptureAttempt(Transform capturer)
    {
        if (capturer == null || state == AcidSlimeState.Die)
        {
            return false;
        }

        targetPlayer = capturer;
        FaceTarget(capturer.position);
        StartAttack();
        return true;
    }

    private void TickMovementDecision()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            PickNextDecision();
        }
    }

    private void PickNextDecision()
    {
        decisionTimer = UnityEngine.Random.Range(minDecisionTime, maxDecisionTime);
        isIdling = UnityEngine.Random.value < idleChance;

        if (!isIdling && UnityEngine.Random.value < 0.58f)
        {
            direction *= -1f;
        }

        ChangeState(isIdling ? AcidSlimeState.Idle : AcidSlimeState.Bounce);
    }

    private void HandleObstacleNavigation()
    {
        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x + direction * bounds.extents.x, bounds.center.y);
        bool obstacleAhead = Physics2D.Raycast(origin, Vector2.right * direction, obstacleCheckDistance, obstacleLayer);

        Vector2 edgeOrigin = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.16f), bounds.min.y + 0.08f);
        bool groundAhead = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

        if (obstacleAhead && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            return;
        }

        if (!groundAhead)
        {
            direction *= -1f;
            PickNextDecision();
        }
    }

    private bool IsGrounded()
    {
        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
        Vector2 size = new Vector2(bounds.size.x * 0.72f, 0.08f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void TryReactToCaptureInput()
    {
        if (targetPlayer == null || attackCooldownTimer > 0f || !WasCaptureKeyPressed())
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, targetPlayer.position);
        if (distance <= captureThreatRadius)
        {
            FaceTarget(targetPlayer.position);
            StartAttack();
        }
    }

    private bool WasCaptureKeyPressed()
    {
        bool pressed = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(KeyCode.E);
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        pressed = pressed || (keyboard != null && keyboard.eKey.wasPressedThisFrame);
#endif

        return pressed;
    }

    private void StartAttack()
    {
        if (attackCooldownTimer > 0f || state == AcidSlimeState.Die)
        {
            return;
        }

        ChangeState(AcidSlimeState.Attack);
        attackTimer = 0f;
        projectileFired = false;
        attackCooldownTimer = attackCooldown;
    }

    private void TickAttack()
    {
        attackTimer += Time.deltaTime;

        if (!projectileFired && attackTimer >= attackWindup)
        {
            projectileFired = true;
            SpawnProjectile();
        }

        if (attackTimer >= attackDuration)
        {
            PickNextDecision();
        }
    }

    private void SpawnProjectile()
    {
        if (projectileSprite == null)
        {
            return;
        }

        Vector2 attackDirection = targetPlayer != null
            ? ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized
            : Vector2.right * direction;

        if (attackDirection.sqrMagnitude <= 0.001f)
        {
            attackDirection = Vector2.right * direction;
        }

        Vector2 spawnOffset = new Vector2(projectileSpawnOffset.x * direction, projectileSpawnOffset.y);
        GameObject projectile = new GameObject("AcidSlime_Projectile");
        projectile.transform.position = (Vector2)transform.position + spawnOffset;
        projectile.transform.localScale = new Vector3(Mathf.Sign(attackDirection.x), 1f, 1f);

        SpriteRenderer projectileRenderer = projectile.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = projectileSprite;
        projectileRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.2f;
        projectileCollider.isTrigger = true;

        AcidSlimeProjectile projectileBehaviour = projectile.AddComponent<AcidSlimeProjectile>();
        projectileBehaviour.Launch(attackDirection, projectileSpeed, projectileLifetime);
    }

    private void FindPlayerIfNeeded()
    {
        if (targetPlayer != null)
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackDetectionRadius, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            targetPlayer = hit.transform;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= attackDetectionRadius)
        {
            targetPlayer = player.transform;
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        direction = targetPosition.x >= transform.position.x ? 1f : -1f;
    }

    private void UpdateFacing()
    {
        spriteRenderer.flipX = direction < 0f;
    }

    private void ChangeState(AcidSlimeState nextState)
    {
        if (state == nextState)
        {
            return;
        }

        state = nextState;
        frameIndex = 0;
        animationTimer = 0f;
    }

    private void Animate()
    {
        Sprite[] frames = GetFramesForCurrentState();
        if (frames.Length == 0)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, GetFrameRate());
        if (animationTimer >= frameTime)
        {
            animationTimer -= frameTime;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
        ApplySquash(frames.Length);
    }

    private Sprite[] GetFramesForCurrentState()
    {
        return framesByState.TryGetValue(state, out Sprite[] frames) ? frames : Array.Empty<Sprite>();
    }

    private float GetFrameRate()
    {
        return state switch
        {
            AcidSlimeState.Bounce => bounceFrameRate,
            AcidSlimeState.Attack => attackFrameRate,
            AcidSlimeState.Hurt => hurtFrameRate,
            AcidSlimeState.Die => dieFrameRate,
            _ => idleFrameRate
        };
    }

    private void ApplySquash(int frameCount)
    {
        if (state != AcidSlimeState.Bounce || frameCount <= 1)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 12f);
            return;
        }

        float pulse = Mathf.Sin((frameIndex / (float)frameCount) * Mathf.PI);
        Vector3 targetScale = new Vector3(baseScale.x * (1f - pulse * 0.08f), baseScale.y * (1f + pulse * 0.1f), baseScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 14f);
    }

    private void LoadFrames()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        AddFrames(loadedSprites, AcidSlimeState.Idle, "AcidSlime_idle");
        AddFrames(loadedSprites, AcidSlimeState.Bounce, "AcidSlime_bounce");
        AddFrames(loadedSprites, AcidSlimeState.Attack, "AcidSlime_attack");
        AddFrames(loadedSprites, AcidSlimeState.Hurt, "AcidSlime_hurt");
        AddFrames(loadedSprites, AcidSlimeState.Die, "AcidSlime_die");
        projectileSprite = loadedSprites.FirstOrDefault(sprite => sprite.name == "AcidSlime_Projectile");
    }

    private void AddFrames(Sprite[] loadedSprites, AcidSlimeState frameState, string prefix)
    {
        framesByState[frameState] = loadedSprites
            .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.65f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
        Gizmos.color = new Color(0.9f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, captureThreatRadius);
    }
}
