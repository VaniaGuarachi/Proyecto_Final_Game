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
        Evolving,
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

    [Header("Evolucion tipo Pokemon")]
    [SerializeField] private bool autoEvolve = true;
    [SerializeField] private float evolutionDelay = 30f;
    [SerializeField] private float evolutionDuration = 2.6f;
    [SerializeField] private string evolvedResourcePath = "Slimes/Acid/Evolved";
    [SerializeField] private float evolutionFrameRate = 16f;
    [SerializeField] private float evolvedScaleMultiplier = 1f;
    [SerializeField] private string evolutionRequirementMaterial = "Minerales toxicos";
    [SerializeField] private string evolutionRequirementEnvironment = "Cavernas quimicas";
    [SerializeField] private string evolvedSlimeName = "Slime de Gas+";
    [SerializeField] private string evolvedSlimeType = "Acido evolucionado";
    [SerializeField] private string evolvedProducedResource = "Acido mineral disolvente";

    [Header("Multiplicacion")]
    [SerializeField] private bool autoMultiply = true;
    [SerializeField] private float multiplicationDelay = 30f;

    [Header("Habilidad corrosiva")]
    [SerializeField] private float corrosiveCloudRadius = 0.75f;
    [SerializeField] private float evolvedCorrosiveCloudRadius = 1.15f;
    [SerializeField] private float corrosiveCloudLifetime = 2.8f;
    [SerializeField] private float corrosiveCloudTickInterval = 0.75f;
    [SerializeField] private int gasBurstParticles = 16;

    private readonly Dictionary<AcidSlimeState, Sprite[]> framesByState = new Dictionary<AcidSlimeState, Sprite[]>();
    private readonly Dictionary<AcidSlimeState, Sprite[]> evolvedFramesByState = new Dictionary<AcidSlimeState, Sprite[]>();
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform targetPlayer;
    private Vector3 baseScale;
    private Vector3 originalBaseScale;
    private AcidSlimeState state = AcidSlimeState.Idle;
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
        if (isEvolving || state == AcidSlimeState.Attack || state == AcidSlimeState.Die)
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
        if (capturer == null || isEvolving || state == AcidSlimeState.Die)
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
        if (!isIdling) squashIntensity = 0.8f;
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
        if (isEvolving || attackCooldownTimer > 0f || state == AcidSlimeState.Die)
        {
            return;
        }

        ChangeState(AcidSlimeState.Attack);
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
        projectileRenderer.sprite = isEvolved && evolvedProjectileSprite != null ? evolvedProjectileSprite : projectileSprite;
        projectileRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.2f;
        projectileCollider.isTrigger = true;

        AcidSlimeProjectile projectileBehaviour = projectile.AddComponent<AcidSlimeProjectile>();
        projectileBehaviour.Launch(attackDirection, projectileSpeed, projectileLifetime);

        SpawnCorrosiveCloud((Vector2)projectile.transform.position - spawnOffset * 0.35f);
        professionalFx?.Burst(isEvolved ? gasBurstParticles + 10 : gasBurstParticles);
    }

    private void SpawnCorrosiveCloud(Vector2 position)
    {
        GameObject cloud = new GameObject(isEvolved ? "AcidSlime_EvolvedCorrosiveCloud" : "AcidSlime_CorrosiveCloud");
        cloud.transform.position = position;

        CircleCollider2D cloudCollider = cloud.AddComponent<CircleCollider2D>();
        cloudCollider.radius = isEvolved ? evolvedCorrosiveCloudRadius : corrosiveCloudRadius;
        cloudCollider.isTrigger = true;

        SpriteRenderer cloudRenderer = cloud.AddComponent<SpriteRenderer>();
        cloudRenderer.sprite = BuildCloudSprite();
        cloudRenderer.color = isEvolved
            ? new Color(0.7f, 0.18f, 1f, 0.42f)
            : new Color(0.63f, 0.18f, 0.95f, 0.34f);
        cloudRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        cloud.transform.localScale = Vector3.one * cloudCollider.radius;

        SlimeHazardZone hazard = cloud.AddComponent<SlimeHazardZone>();
        hazard.Configure(corrosiveCloudLifetime, corrosiveCloudTickInterval, true, "MineralDuro", isEvolved ? 2 : 1);
    }

    private Sprite BuildCloudSprite()
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center) / (size * 0.5f);
                float swirl = Mathf.Sin((point.x + point.y) * 0.18f) * 0.08f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance + swirl), 2.1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 96f);
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
        if (isEvolving && nextState != AcidSlimeState.Evolving)
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
            evolvedFramesByState.TryGetValue(AcidSlimeState.Evolving, out Sprite[] evolutionFrames) &&
            evolutionFrames.Length > 0)
        {
            return evolutionFrames;
        }

        Dictionary<AcidSlimeState, Sprite[]> source = isEvolved ? evolvedFramesByState : framesByState;
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
            AcidSlimeState.Bounce => bounceFrameRate,
            AcidSlimeState.Evolving => evolutionFrameRate,
            AcidSlimeState.Attack => attackFrameRate,
            AcidSlimeState.Hurt => hurtFrameRate,
            AcidSlimeState.Die => dieFrameRate,
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
        if (state == AcidSlimeState.Bounce || state == AcidSlimeState.Attack)
        {
            targetScale = new Vector3(baseScale.x * (1f - pulse * 0.15f * squashIntensity), baseScale.y * (1f + pulse * 0.25f * squashIntensity), baseScale.z);
        }
        else if (state == AcidSlimeState.Hurt)
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
        AddFrames(loadedSprites, AcidSlimeState.Idle, "AcidSlime_idle");
        AddFrames(loadedSprites, AcidSlimeState.Bounce, "AcidSlime_bounce");
        AddFrames(loadedSprites, AcidSlimeState.Evolving, "AcidSlime_evolve");
        AddFrames(loadedSprites, AcidSlimeState.Attack, "AcidSlime_attack");
        AddFrames(loadedSprites, AcidSlimeState.Hurt, "AcidSlime_hurt");
        AddFrames(loadedSprites, AcidSlimeState.Die, "AcidSlime_die");
        projectileSprite = loadedSprites.FirstOrDefault(sprite => sprite.name == "AcidSlime_Projectile");

        Sprite[] evolvedSprites = Resources.LoadAll<Sprite>(evolvedResourcePath);
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Idle, "AcidSlimeEvolved_idle");
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Bounce, "AcidSlimeEvolved_bounce");
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Evolving, "AcidSlime_evolve");
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Attack, "AcidSlimeEvolved_attack");
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Hurt, "AcidSlimeEvolved_hurt");
        AddFrames(evolvedFramesByState, evolvedSprites, AcidSlimeState.Die, "AcidSlimeEvolved_die");
        evolvedProjectileSprite = evolvedSprites.FirstOrDefault(sprite => sprite.name == "AcidSlimeEvolved_Projectile");
    }

    private void AddFrames(Sprite[] loadedSprites, AcidSlimeState frameState, string prefix)
    {
        AddFrames(framesByState, loadedSprites, frameState, prefix);
    }

    private void AddFrames(Dictionary<AcidSlimeState, Sprite[]> target, Sprite[] loadedSprites, AcidSlimeState frameState, string prefix)
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
        if (!autoMultiply || isEvolving || state == AcidSlimeState.Die) return;

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
        AcidSlimeAI cloneAI = clone.GetComponent<AcidSlimeAI>();
        if (cloneAI != null)
        {
            cloneAI.lifeTimer = 0f;
            cloneAI.multiplicationTimer = 0f;
            Rigidbody2D cloneRb = clone.GetComponent<Rigidbody2D>();
            cloneRb.linearVelocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), jumpForce);
        }

        professionalFx?.Burst(40);
        squashIntensity = 2.0f;
        ChangeState(AcidSlimeState.Bounce);
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
        ChangeState(AcidSlimeState.Evolving);
        ShowEvolutionGlow(0f);
    }

    private void TickEvolution()
    {
        evolutionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(evolutionTimer / Mathf.Max(0.1f, evolutionDuration));
        float pulse = Mathf.Sin(progress * Mathf.PI);

        spriteRenderer.color = Color.Lerp(Color.white, new Color(0.82f, 0.48f, 1f, 1f), pulse);
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
        ApplyEvolutionData();
        professionalFx?.SetEvolved(true);
        production?.SetEvolved(true);
        HideEvolutionGlow();
        ChangeState(AcidSlimeState.Idle);
        PickNextDecision();
    }

    private bool HasEvolvedFrames()
    {
        return evolvedFramesByState.TryGetValue(AcidSlimeState.Idle, out Sprite[] idleFrames) && idleFrames.Length > 0 &&
               evolvedFramesByState.TryGetValue(AcidSlimeState.Evolving, out Sprite[] evolveFrames) && evolveFrames.Length > 0;
    }

    private void ApplyEvolutionData()
    {
        SlimeController slimeController = GetComponent<SlimeController>();
        if (slimeController == null)
        {
            return;
        }

        slimeController.SetEvolutionData(evolvedSlimeName, evolvedSlimeType, evolvedProducedResource);
        Debug.Log($"{evolvedSlimeName} evoluciono por {evolutionRequirementMaterial} en {evolutionRequirementEnvironment}. Produce {evolvedProducedResource}.");
    }

    private void CreateEvolutionGlow()
    {
        GameObject glowObject = new GameObject("AcidSlime_EvolutionGlow");
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
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.4f);
                texture.SetPixel(x, y, new Color(0.72f, 0.22f, 1f, alpha * 0.68f));
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
        evolutionGlowRenderer.color = new Color(0.75f, 0.24f, 1f, Mathf.Clamp01(strength));
        float scale = Mathf.Lerp(0.9f, 2.45f, strength);
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
        Gizmos.color = new Color(0.65f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
        Gizmos.color = new Color(0.9f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, captureThreatRadius);
        Gizmos.color = new Color(0.65f, 0.2f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, isEvolved ? evolvedCorrosiveCloudRadius : corrosiveCloudRadius);
    }
}
