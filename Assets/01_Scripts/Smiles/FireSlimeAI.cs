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
public class FireSlimeAI : MonoBehaviour
{
    private enum FireSlimeState
    {
        Idle,
        Bounce,
        Evolving,
        Attack,
        Hurt,
        Die
    }

    [Header("Movimiento autonomo")]
    [SerializeField] private float walkSpeed = 1.75f;
    [SerializeField] private float jumpForce = 6.5f;
    [SerializeField] private float minDecisionTime = 1.2f;
    [SerializeField] private float maxDecisionTime = 3.2f;
    [SerializeField] private float idleChance = 0.25f;

    [Header("Deteccion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 0.45f;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float edgeCheckDistance = 0.75f;

    [Header("Combate")]
    [SerializeField] private float attackDetectionRadius = 4f;
    [SerializeField] private float captureThreatRadius = 2.6f;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float attackWindup = 0.28f;
    [SerializeField] private float attackDuration = 0.75f;
    [SerializeField] private float projectileSpeed = 7.5f;
    [SerializeField] private float projectileLifetime = 2.2f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.45f, 0.12f);

    [Header("Animacion por sprites")]
    [SerializeField] private string resourcePath = "Slimes/Fire";
    [SerializeField] private float idleFrameRate = 5f;
    [SerializeField] private float bounceFrameRate = 9f;
    [SerializeField] private float attackFrameRate = 12f;
    [SerializeField] private float hurtFrameRate = 10f;
    [SerializeField] private float dieFrameRate = 8f;

    [Header("Evolucion tipo Pokemon")]
    [SerializeField] private bool autoEvolve = true;
    [SerializeField] private float evolutionDelay = 30f;
    [SerializeField] private float evolutionDuration = 2.6f;
    [SerializeField] private string evolvedResourcePath = "Slimes/Fire/Evolved";
    [SerializeField] private float evolutionFrameRate = 16f;
    [SerializeField] private float evolvedScaleMultiplier = 1f;

    [Header("Multiplicacion")]
    [SerializeField] private bool autoMultiply = true;
    [SerializeField] private float multiplicationDelay = 30f;

    [Header("Habilidad elemental")]
    [SerializeField] private float heatPulseRadius = 1.45f;
    [SerializeField] private float evolvedHeatPulseRadius = 2.15f;
    [SerializeField] private float heatPulseCooldown = 3.2f;
    [SerializeField] private int heatPulseParticles = 18;

    private readonly Dictionary<FireSlimeState, Sprite[]> framesByState = new Dictionary<FireSlimeState, Sprite[]>();
    private readonly Dictionary<FireSlimeState, Sprite[]> evolvedFramesByState = new Dictionary<FireSlimeState, Sprite[]>();
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform targetPlayer;
    private Vector3 baseScale;
    private Vector3 originalBaseScale;
    private FireSlimeState state = FireSlimeState.Idle;
    private float direction = 1f;
    private float decisionTimer;
    private float attackCooldownTimer;
    private float attackTimer;
    private float animationTimer;
    private float lifeTimer;
    private float evolutionTimer;
    private float multiplicationTimer;
    private float squashIntensity = 0f;
    private int frameIndex;
    private bool isIdling;
    private bool projectileFired;
    private bool isEvolved;
    private bool isEvolving;
    private Sprite projectileSprite;
    private Sprite evolvedProjectileSprite;
    private SpriteRenderer evolutionGlowRenderer;
    private Sprite evolutionGlowSprite;
    private SlimeProfessionalFX professionalFx;
    private SlimeProduction production;
    private float heatPulseTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        originalBaseScale = baseScale;
        professionalFx = GetComponent<SlimeProfessionalFX>();
        production = GetComponent<SlimeProduction>();
        LoadFrames();
        CreateEvolutionGlow();
        PickNextDecision();
    }

    private void Update()
    {
        TickAutoEvolution();
        TickMultiplication();

        if (isEvolving)
        {
            TickEvolution();
            Animate();
            UpdateFacing();
            return;
        }

        attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - Time.deltaTime);
        FindPlayerIfNeeded();
        TickHeatPulse();

        if (state == FireSlimeState.Attack)
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
        if (isEvolving || state == FireSlimeState.Attack || state == FireSlimeState.Die)
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
        if (capturer == null || isEvolving || state == FireSlimeState.Die)
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
        if (decisionTimer > 0f)
        {
            return;
        }

        PickNextDecision();
    }

    private void PickNextDecision()
    {
        decisionTimer = UnityEngine.Random.Range(minDecisionTime, maxDecisionTime);
        isIdling = UnityEngine.Random.value < idleChance;

        if (!isIdling && UnityEngine.Random.value < 0.55f)
        {
            direction *= -1f;
        }

        ChangeState(isIdling ? FireSlimeState.Idle : FireSlimeState.Bounce);
        if (!isIdling) squashIntensity = 0.8f;
    }

    private void HandleObstacleNavigation()
    {
        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x + direction * bounds.extents.x, bounds.center.y);
        bool obstacleAhead = Physics2D.Raycast(origin, Vector2.right * direction, obstacleCheckDistance, obstacleLayer);

        Vector2 edgeOrigin = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.18f), bounds.min.y + 0.08f);
        bool groundAhead = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

        if (obstacleAhead && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            squashIntensity = 1.2f;
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
        if (isEvolving || attackCooldownTimer > 0f || state == FireSlimeState.Die)
        {
            return;
        }

        ChangeState(FireSlimeState.Attack);
        squashIntensity = 1.0f;
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

        Vector2 fireDirection = targetPlayer != null
            ? ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized
            : Vector2.right * direction;

        if (fireDirection.sqrMagnitude <= 0.001f)
        {
            fireDirection = Vector2.right * direction;
        }

        Vector2 spawnOffset = new Vector2(projectileSpawnOffset.x * direction, projectileSpawnOffset.y);
        GameObject projectile = new GameObject("FireSlime_Projectile");
        projectile.transform.position = (Vector2)transform.position + spawnOffset;
        projectile.transform.localScale = new Vector3(Mathf.Sign(fireDirection.x), 1f, 1f);

        SpriteRenderer projectileRenderer = projectile.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = isEvolved && evolvedProjectileSprite != null ? evolvedProjectileSprite : projectileSprite;
        projectileRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.22f;
        projectileCollider.isTrigger = true;

        FireSlimeProjectile projectileBehaviour = projectile.AddComponent<FireSlimeProjectile>();
        projectileBehaviour.Launch(fireDirection, projectileSpeed, projectileLifetime);

        professionalFx?.Burst(isEvolved ? heatPulseParticles + 10 : heatPulseParticles);
    }

    private void TickHeatPulse()
    {
        heatPulseTimer -= Time.deltaTime;
        if (heatPulseTimer > 0f || state == FireSlimeState.Die)
        {
            return;
        }

        heatPulseTimer = heatPulseCooldown;
        float radius = isEvolved ? evolvedHeatPulseRadius : heatPulseRadius;
        Collider2D playerHit = Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Player"));
        if (playerHit != null)
        {
            playerHit.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
        }

        professionalFx?.Burst(isEvolved ? heatPulseParticles + 8 : heatPulseParticles);
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

    private void ChangeState(FireSlimeState nextState)
    {
        if (isEvolving && nextState != FireSlimeState.Evolving)
        {
            return;
        }

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
        ApplySuperSquash(frames.Length);
    }

    private Sprite[] GetFramesForCurrentState()
    {
        if (isEvolving &&
            evolvedFramesByState.TryGetValue(FireSlimeState.Evolving, out Sprite[] evolutionFrames) &&
            evolutionFrames.Length > 0)
        {
            return evolutionFrames;
        }

        Dictionary<FireSlimeState, Sprite[]> source = isEvolved ? evolvedFramesByState : framesByState;
        if (source.TryGetValue(state, out Sprite[] frames) && frames.Length > 0)
        {
            return frames;
        }

        return framesByState.TryGetValue(state, out frames) ? frames : Array.Empty<Sprite>();
    }

    private float GetFrameRate()
    {
        return state switch
        {
            FireSlimeState.Bounce => bounceFrameRate,
            FireSlimeState.Evolving => evolutionFrameRate,
            FireSlimeState.Attack => attackFrameRate,
            FireSlimeState.Hurt => hurtFrameRate,
            FireSlimeState.Die => dieFrameRate,
            _ => idleFrameRate
        };
    }

    private void ApplySuperSquash(int frameCount)
    {
        squashIntensity = Mathf.Lerp(squashIntensity, 0f, Time.deltaTime * 8f);
        
        float pulse = 0f;
        if (frameCount > 1) {
            pulse = Mathf.Sin((frameIndex / (float)frameCount) * Mathf.PI);
        }

        Vector3 targetScale;
        if (state == FireSlimeState.Bounce || state == FireSlimeState.Attack)
        {
            targetScale = new Vector3(baseScale.x * (1f - pulse * 0.15f * squashIntensity), baseScale.y * (1f + pulse * 0.25f * squashIntensity), baseScale.z);
        }
        else if (state == FireSlimeState.Hurt)
        {
            targetScale = new Vector3(baseScale.x * (1f + pulse * 0.2f), baseScale.y * (1f - pulse * 0.15f), baseScale.z);
        }
        else
        {
            targetScale = baseScale;
        }

        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            float stretch = Mathf.Clamp(Mathf.Abs(rb.linearVelocity.y) * 0.03f, 0f, 0.3f);
            targetScale = new Vector3(targetScale.x * (1f - stretch), targetScale.y * (1f + stretch), targetScale.z);
        }

        if (!isEvolving)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 18f);
        }
    }

    private void LoadFrames()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        AddFrames(loadedSprites, FireSlimeState.Idle, "FireSlime_idle");
        AddFrames(loadedSprites, FireSlimeState.Bounce, "FireSlime_bounce");
        AddFrames(loadedSprites, FireSlimeState.Evolving, "FireSlime_evolve");
        AddFrames(loadedSprites, FireSlimeState.Attack, "FireSlime_attack");
        AddFrames(loadedSprites, FireSlimeState.Hurt, "FireSlime_hurt");
        AddFrames(loadedSprites, FireSlimeState.Die, "FireSlime_die");
        projectileSprite = loadedSprites.FirstOrDefault(sprite => sprite.name == "FireSlime_Projectile");

        Sprite[] evolvedSprites = Resources.LoadAll<Sprite>(evolvedResourcePath);
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Idle, "FireSlimeEvolved_idle");
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Bounce, "FireSlimeEvolved_bounce");
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Evolving, "FireSlime_evolve");
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Attack, "FireSlimeEvolved_attack");
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Hurt, "FireSlimeEvolved_hurt");
        AddFrames(evolvedFramesByState, evolvedSprites, FireSlimeState.Die, "FireSlimeEvolved_die");
        evolvedProjectileSprite = evolvedSprites.FirstOrDefault(sprite => sprite.name == "FireSlimeEvolved_Projectile");
    }

    private void AddFrames(Sprite[] loadedSprites, FireSlimeState frameState, string prefix)
    {
        AddFrames(framesByState, loadedSprites, frameState, prefix);
    }

    private void AddFrames(Dictionary<FireSlimeState, Sprite[]> target, Sprite[] loadedSprites, FireSlimeState frameState, string prefix)
    {
        target[frameState] = loadedSprites
            .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private void TickAutoEvolution()
    {
        if (!autoEvolve || isEvolved || isEvolving)
        {
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= evolutionDelay)
        {
            StartEvolution();
        }
    }

    private void TickMultiplication()
    {
        if (SlimeReproductionSystem.ReproduccionEcologicaActiva) return;
        if (!autoMultiply || isEvolving || state == FireSlimeState.Die) return;

        multiplicationTimer += Time.deltaTime;
        if (multiplicationTimer >= multiplicationDelay)
        {
            Multiply();
        }
    }

    private void Multiply()
    {
        multiplicationTimer = 0f;
        
        GameObject clone = Instantiate(gameObject, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        FireSlimeAI cloneAI = clone.GetComponent<FireSlimeAI>();
        if (cloneAI != null)
        {
            cloneAI.lifeTimer = 0f;
            cloneAI.multiplicationTimer = 0f;
            Rigidbody2D cloneRb = clone.GetComponent<Rigidbody2D>();
            cloneRb.linearVelocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), jumpForce);
        }

        professionalFx?.Burst(40);
        squashIntensity = 2.0f;
        ChangeState(FireSlimeState.Bounce);
    }

    private void StartEvolution()
    {
        if (isEvolved || isEvolving || !HasEvolvedFrames())
        {
            return;
        }

        isEvolving = true;
        isIdling = true;
        projectileFired = false;
        evolutionTimer = 0f;
        attackTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        ChangeState(FireSlimeState.Evolving);
        ShowEvolutionGlow(0f);
    }

    private void TickEvolution()
    {
        evolutionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(evolutionTimer / Mathf.Max(0.1f, evolutionDuration));
        float pulse = Mathf.Sin(progress * Mathf.PI);

        spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.35f, 1f), pulse);
        transform.localScale = originalBaseScale * Mathf.Lerp(1f, evolvedScaleMultiplier * 1.08f, pulse);
        ShowEvolutionGlow(pulse);

        if (evolutionTimer >= evolutionDuration)
        {
            FinishEvolution();
        }
    }

    private void FinishEvolution()
    {
        isEvolved = true;
        isEvolving = false;
        spriteRenderer.color = Color.white;
        baseScale = originalBaseScale * evolvedScaleMultiplier;
        transform.localScale = baseScale;
        professionalFx?.SetEvolved(true);
        production?.SetEvolved(true);
        HideEvolutionGlow();
        ChangeState(FireSlimeState.Idle);
        PickNextDecision();
    }

    private bool HasEvolvedFrames()
    {
        return evolvedFramesByState.TryGetValue(FireSlimeState.Idle, out Sprite[] idleFrames) && idleFrames.Length > 0 &&
               evolvedFramesByState.TryGetValue(FireSlimeState.Evolving, out Sprite[] evolveFrames) && evolveFrames.Length > 0;
    }

    private void CreateEvolutionGlow()
    {
        GameObject glowObject = new GameObject("FireSlime_EvolutionGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0.04f, 0f);

        evolutionGlowRenderer = glowObject.AddComponent<SpriteRenderer>();
        evolutionGlowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        evolutionGlowSprite = BuildGlowSprite();
        evolutionGlowRenderer.sprite = evolutionGlowSprite;
        evolutionGlowRenderer.enabled = false;
    }

    private Sprite BuildGlowSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.6f);
                texture.SetPixel(x, y, new Color(1f, 0.46f, 0.03f, alpha * 0.68f));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 96f);
    }

    private void ShowEvolutionGlow(float strength)
    {
        if (evolutionGlowRenderer == null)
        {
            return;
        }

        evolutionGlowRenderer.enabled = strength > 0.01f;
        evolutionGlowRenderer.color = new Color(1f, 0.56f, 0.08f, Mathf.Clamp01(strength));
        float scale = Mathf.Lerp(0.9f, 2.5f, strength);
        evolutionGlowRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void HideEvolutionGlow()
    {
        if (evolutionGlowRenderer != null)
        {
            evolutionGlowRenderer.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, captureThreatRadius);
        Gizmos.color = new Color(1f, 0.25f, 0f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, isEvolved ? evolvedHeatPulseRadius : heatPulseRadius);
    }
}
