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
public class ColdSlimeAI : MonoBehaviour
{
    private enum ColdSlimeState
    {
        Idle,
        Bounce,
        Evolving,
        Attack,
        Hurt,
        Die
    }

    [Header("Movimiento autonomo")]
    [SerializeField] private float walkSpeed = 1.05f;
    [SerializeField] private float jumpForce = 4.9f;
    [SerializeField] private float minDecisionTime = 0.9f;
    [SerializeField] private float maxDecisionTime = 2.8f;
    [SerializeField] private float idleChance = 0.24f;

    [Header("Deteccion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 0.34f;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float edgeCheckDistance = 0.55f;

    [Header("Combate")]
    [SerializeField] private float attackDetectionRadius = 4f;
    [SerializeField] private float captureThreatRadius = 2.65f;
    [SerializeField] private float attackCooldown = 1.25f;
    [SerializeField] private float attackWindup = 0.25f;
    [SerializeField] private float attackDuration = 0.78f;
    [SerializeField] private float projectileSpeed = 7.1f;
    [SerializeField] private float projectileLifetime = 2.3f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.33f, 0.06f);

    [Header("Animacion por sprites")]
    [SerializeField] private string resourcePath = "Slimes/Cold";
    [SerializeField] private float idleFrameRate = 8f;
    [SerializeField] private float bounceFrameRate = 13f;
    [SerializeField] private float attackFrameRate = 15f;
    [SerializeField] private float hurtFrameRate = 12f;
    [SerializeField] private float dieFrameRate = 10f;

    [Header("Evolucion")]
    [SerializeField] private bool autoEvolve = true;
    [SerializeField] private float evolutionDelay = 30f;
    [SerializeField] private float evolutionDuration = 2.6f;
    [SerializeField] private string evolvedResourcePath = "Slimes/Cold/Evolved";
    [SerializeField] private float evolutionFrameRate = 16f;
    [SerializeField] private float evolvedScaleMultiplier = 1f;
    [SerializeField] private string evolutionRequirementMaterial = "Cristales";
    [SerializeField] private string evolutionRequirementEnvironment = "Zona fria congelada";
    [SerializeField] private string evolvedSlimeName = "Cold Crystal Slime";
    [SerializeField] private string evolvedSlimeType = "Cristal helado";
    [SerializeField] private string evolvedProducedResource = "Cristales";

    [Header("Multiplicacion")]
    [SerializeField] private bool autoMultiply = true;
    [SerializeField] private float multiplicationDelay = 30f;

    [Header("Habilidad gelida")]
    [SerializeField] private float frostAuraRadius = 1.15f;
    [SerializeField] private float evolvedFrostAuraRadius = 1.85f;
    [SerializeField] private float frostAuraCooldown = 3.6f;
    [SerializeField] private int frostBurstParticles = 14;
    [SerializeField] private int frostAuraCristalesAmount = 1;

    [Header("Crystal Spike (evolucionado)")]
    [SerializeField] private float crystalSpikeCooldown = 8f;
    [SerializeField] private int crystalSpikeCount = 3;
    [SerializeField] private float crystalSpikeRange = 2.8f;
    [SerializeField] private float crystalSpikeLifetime = 4f;
    [SerializeField] private int crystalSpikeCristalesReward = 2;

    [Header("Animacion mejorada")]
    [SerializeField] private float idleBobSpeed = 1.8f;
    [SerializeField] private float idleBobAmount = 0.04f;
    [SerializeField] private float bounceSquashX = 0.13f;
    [SerializeField] private float bounceSquashY = 0.16f;

    private readonly Dictionary<ColdSlimeState, Sprite[]> framesByState = new Dictionary<ColdSlimeState, Sprite[]>();
    private readonly Dictionary<ColdSlimeState, Sprite[]> evolvedFramesByState = new Dictionary<ColdSlimeState, Sprite[]>();
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform targetPlayer;
    private Vector3 baseScale;
    private Vector3 originalBaseScale;
    private ColdSlimeState state = ColdSlimeState.Idle;
    private float direction = 1f;
    private float decisionTimer;
    private float attackCooldownTimer;
    private float attackTimer;
    private float animationTimer;
    private float frostAuraTimer;
    private float crystalSpikeTimer;
    private float idleBobTimer;
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
        professionalFx = GetComponent<SlimeProfessionalFX>();
        production = GetComponent<SlimeProduction>();
        baseScale = transform.localScale;
        originalBaseScale = baseScale;
        crystalSpikeTimer = crystalSpikeCooldown * 0.5f;

        // Configurar produccion: Frio base, Cristales al evolucionar
        production?.Configure("Frio", evolvedProducedResource, 1, 2, 10f, 6f);

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
        TickFrostAura();
        TickCrystalSpike();
        TickIdleBob();

        if (state == ColdSlimeState.Attack)
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
        if (isEvolving || state == ColdSlimeState.Attack || state == ColdSlimeState.Die)
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
        if (capturer == null || isEvolving || state == ColdSlimeState.Die)
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

        ChangeState(isIdling ? ColdSlimeState.Idle : ColdSlimeState.Bounce);
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

        if (Vector2.Distance(transform.position, targetPlayer.position) <= captureThreatRadius)
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
        if (isEvolving || attackCooldownTimer > 0f || state == ColdSlimeState.Die)
        {
            return;
        }

        ChangeState(ColdSlimeState.Attack);
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
        Sprite activeProjectileSprite = isEvolved && evolvedProjectileSprite != null ? evolvedProjectileSprite : projectileSprite;
        if (activeProjectileSprite == null)
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
        GameObject projectile = new GameObject("ColdSlime_Projectile");
        projectile.transform.position = (Vector2)transform.position + spawnOffset;
        projectile.transform.localScale = new Vector3(Mathf.Sign(attackDirection.x), 1f, 1f);

        SpriteRenderer projectileRenderer = projectile.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = activeProjectileSprite;
        projectileRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Rigidbody2D projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.2f;
        projectileCollider.isTrigger = true;

        ColdSlimeProjectile projectileBehaviour = projectile.AddComponent<ColdSlimeProjectile>();
        projectileBehaviour.Launch(attackDirection, projectileSpeed, projectileLifetime);
        professionalFx?.Burst(frostBurstParticles);
    }

    private void TickFrostAura()
    {
        frostAuraTimer -= Time.deltaTime;
        if (frostAuraTimer > 0f || state == ColdSlimeState.Die)
        {
            return;
        }

        frostAuraTimer = frostAuraCooldown;
        float activeRadius = isEvolved ? evolvedFrostAuraRadius : frostAuraRadius;
        Collider2D playerHit = Physics2D.OverlapCircle(transform.position, activeRadius, LayerMask.GetMask("Player"));
        if (playerHit != null)
        {
            playerHit.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
            professionalFx?.Burst(isEvolved ? frostBurstParticles * 2 : frostBurstParticles);
        }

        // Produce Cristales pasivamente al pulsar el aura
        if (isEvolved && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource("Cristales", frostAuraCristalesAmount);
        }
    }

    private void TickCrystalSpike()
    {
        if (!isEvolved || state == ColdSlimeState.Die || isEvolving)
        {
            return;
        }

        crystalSpikeTimer -= Time.deltaTime;
        if (crystalSpikeTimer > 0f || targetPlayer == null)
        {
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        if (distToPlayer > attackDetectionRadius)
        {
            return;
        }

        crystalSpikeTimer = crystalSpikeCooldown;
        SpawnCrystalSpikes();
    }

    private void SpawnCrystalSpikes()
    {
        professionalFx?.Burst(frostBurstParticles * 3);

        for (int i = 0; i < crystalSpikeCount; i++)
        {
            float offsetX = (i - crystalSpikeCount / 2f) * (crystalSpikeRange / crystalSpikeCount);
            Vector2 spikePos = (Vector2)transform.position + new Vector2(offsetX, -0.3f);

            GameObject spike = new GameObject("ColdSlime_CrystalSpike");
            spike.transform.position = spikePos;

            SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
            sr.sprite = BuildCrystalSpikeSprite();
            sr.color = new Color(0.55f, 0.9f, 1f, 0.92f);
            sr.sortingOrder = spriteRenderer.sortingOrder - 1;
            spike.transform.localScale = new Vector3(
                UnityEngine.Random.Range(0.18f, 0.28f),
                UnityEngine.Random.Range(0.32f, 0.52f), 1f);
            spike.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-12f, 12f));

            CircleCollider2D col = spike.AddComponent<CircleCollider2D>();
            col.radius = 0.18f;
            col.isTrigger = true;

            SlimeHazardZone hz = spike.AddComponent<SlimeHazardZone>();
            hz.Configure(crystalSpikeLifetime, 1.2f, true, "Cristales", crystalSpikeCristalesReward);

            // Animacion de aparicion: escala desde 0
            spike.AddComponent<CrystalSpikeRise>();
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource("Frio", 1);
        }
    }

    private Sprite BuildCrystalSpikeSprite()
    {
        const int w = 24;
        const int h = 56;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)h;
            float halfW = Mathf.Lerp(w * 0.48f, 0.5f, t);
            float cx = w * 0.5f;

            for (int x = 0; x < w; x++)
            {
                float dist = Mathf.Abs(x - cx);
                float alpha = dist < halfW ? Mathf.Lerp(1f, 0.3f, dist / halfW) : 0f;
                float bright = Mathf.Lerp(0.7f, 1f, t);
                tex.SetPixel(x, y, new Color(bright * 0.7f, bright * 0.95f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 96f);
    }

    private void TickIdleBob()
    {
        if (state != ColdSlimeState.Idle || isEvolving)
        {
            idleBobTimer = 0f;
            return;
        }

        idleBobTimer += Time.deltaTime;
        // Efecto de respiracion suave usando escala Y (seguro con Rigidbody2D)
        float breathe = Mathf.Sin(idleBobTimer * idleBobSpeed) * idleBobAmount;
        Vector3 breatheScale = new Vector3(baseScale.x, baseScale.y * (1f + breathe), baseScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, breatheScale, Time.deltaTime * 6f);
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

    private void ChangeState(ColdSlimeState nextState)
    {
        if (isEvolving && nextState != ColdSlimeState.Evolving)
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
            evolvedFramesByState.TryGetValue(ColdSlimeState.Evolving, out Sprite[] evolutionFrames) &&
            evolutionFrames.Length > 0)
        {
            return evolutionFrames;
        }

        Dictionary<ColdSlimeState, Sprite[]> source = isEvolved ? evolvedFramesByState : framesByState;
        if (source.TryGetValue(state, out Sprite[] frames) && frames.Length > 0)
        {
            return frames;
        }

        return framesByState.TryGetValue(state, out Sprite[] fallbackFrames) ? fallbackFrames : Array.Empty<Sprite>();
    }

    private float GetFrameRate()
    {
        return state switch
        {
            ColdSlimeState.Bounce => bounceFrameRate,
            ColdSlimeState.Evolving => evolutionFrameRate,
            ColdSlimeState.Attack => attackFrameRate,
            ColdSlimeState.Hurt => hurtFrameRate,
            ColdSlimeState.Die => dieFrameRate,
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

        float squashMult = isEvolved ? 1.6f : 1f;

        Vector3 targetScale;
        if (state == ColdSlimeState.Bounce || state == ColdSlimeState.Attack)
        {
            targetScale = new Vector3(baseScale.x * (1f - pulse * bounceSquashX * squashMult * squashIntensity), baseScale.y * (1f + pulse * bounceSquashY * squashMult * squashIntensity), baseScale.z);
        }
        else if (state == ColdSlimeState.Hurt)
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

        if (!isEvolving && state != ColdSlimeState.Idle)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 18f);
        }
    }

    private void LoadFrames()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        AddFrames(loadedSprites, ColdSlimeState.Idle, "ColdSlime_idle");
        AddFrames(loadedSprites, ColdSlimeState.Bounce, "ColdSlime_bounce");
        AddFrames(loadedSprites, ColdSlimeState.Evolving, "ColdSlime_evolve");
        AddFrames(loadedSprites, ColdSlimeState.Attack, "ColdSlime_attack");
        AddFrames(loadedSprites, ColdSlimeState.Hurt, "ColdSlime_hurt");
        AddFrames(loadedSprites, ColdSlimeState.Die, "ColdSlime_die");
        projectileSprite = loadedSprites.FirstOrDefault(sprite => sprite.name == "ColdSlime_Projectile");

        Sprite[] evolvedSprites = Resources.LoadAll<Sprite>(evolvedResourcePath);
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Idle, "ColdSlimeEvolved_idle");
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Bounce, "ColdSlimeEvolved_bounce");
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Evolving, "ColdSlime_evolve");
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Attack, "ColdSlimeEvolved_attack");
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Hurt, "ColdSlimeEvolved_hurt");
        AddFrames(evolvedFramesByState, evolvedSprites, ColdSlimeState.Die, "ColdSlimeEvolved_die");
        evolvedProjectileSprite = evolvedSprites.FirstOrDefault(sprite => sprite.name == "ColdSlimeEvolved_Projectile");
    }

    private void AddFrames(Sprite[] loadedSprites, ColdSlimeState frameState, string prefix)
    {
        AddFrames(framesByState, loadedSprites, frameState, prefix);
    }

    private void AddFrames(Dictionary<ColdSlimeState, Sprite[]> target, Sprite[] loadedSprites, ColdSlimeState frameState, string prefix)
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
        if (!autoMultiply || isEvolving || state == ColdSlimeState.Die) return;

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
        ColdSlimeAI cloneAI = clone.GetComponent<ColdSlimeAI>();
        if (cloneAI != null)
        {
            cloneAI.lifeTimer = 0f;
            cloneAI.multiplicationTimer = 0f;
            Rigidbody2D cloneRb = clone.GetComponent<Rigidbody2D>();
            cloneRb.linearVelocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), jumpForce);
        }

        professionalFx?.Burst(40);
        squashIntensity = 2.0f;
        ChangeState(ColdSlimeState.Bounce);
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
        ChangeState(ColdSlimeState.Evolving);
        ShowEvolutionGlow(0f);
    }

    private void TickEvolution()
    {
        evolutionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(evolutionTimer / Mathf.Max(0.1f, evolutionDuration));
        float pulse = Mathf.Sin(progress * Mathf.PI);

        spriteRenderer.color = Color.Lerp(Color.white, new Color(0.62f, 0.95f, 1f, 1f), pulse);
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
        ChangeState(ColdSlimeState.Idle);
        PickNextDecision();
    }

    private bool HasEvolvedFrames()
    {
        return evolvedFramesByState.TryGetValue(ColdSlimeState.Idle, out Sprite[] idleFrames) && idleFrames.Length > 0 &&
               evolvedFramesByState.TryGetValue(ColdSlimeState.Evolving, out Sprite[] evolveFrames) && evolveFrames.Length > 0;
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
        GameObject glowObject = new GameObject("ColdSlime_EvolutionGlow");
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
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                texture.SetPixel(x, y, new Color(0.28f, 0.86f, 1f, alpha * 0.72f));
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
        evolutionGlowRenderer.color = new Color(0.35f, 0.9f, 1f, Mathf.Clamp01(strength));
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
        Gizmos.color = new Color(0.25f, 0.75f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
        Gizmos.color = new Color(0.7f, 0.95f, 1f);
        Gizmos.DrawWireSphere(transform.position, captureThreatRadius);
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, isEvolved ? evolvedFrostAuraRadius : frostAuraRadius);
    }
}
