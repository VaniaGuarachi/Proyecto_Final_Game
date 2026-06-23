using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Cleaner Slime AI — movimiento estilo "hongo de Mario Bros":
/// siempre en movimiento, salta obstaculos automaticamente, se voltea en bordes,
/// cambia de direccion aleatoriamente, y ataca con burbujas de jabon si el jugador intenta capturarlo.
///
/// EVOLUCION — Purificador (despues de 30 s):
///   Requisitos : Basura organica + agua sucia + humedad alta
///   Produce     : Purifica agua, limpia telas y elimina bacterias
///   Recurso     : Agua Purificada
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CleanerSlimeAI : MonoBehaviour
{
    private enum State { Idle, Bounce, Evolving, Attack, Hurt, Die }

    // ── Movimiento ──────────────────────────────────────────────────────────
    [Header("Movimiento (Mario Mushroom Style)")]
    [SerializeField] private float walkSpeed = 1.55f;
    [SerializeField] private float runSpeed = 2.4f;       // velocidad cuando huye
    [SerializeField] private float jumpForce = 5.8f;
    [SerializeField] private float minDecisionTime = 0.6f;
    [SerializeField] private float maxDecisionTime = 2.0f;
    [SerializeField] private float idleChance = 0.08f;    // casi nunca se detiene
    [SerializeField] private float directionChangeChance = 0.45f;
    [SerializeField] private float panicSpeedMultiplier = 1.55f; // huye mas rapido si el jugador esta cerca

    // ── Deteccion ───────────────────────────────────────────────────────────
    [Header("Deteccion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 0.4f;
    [SerializeField] private float groundCheckDistance = 0.14f;
    [SerializeField] private float edgeCheckDistance = 0.6f;
    [SerializeField] private float playerDetectionRadius = 5f;
    [SerializeField] private float panicRadius = 2.8f;   // distancia a la que entra en panico

    // ── Combate ─────────────────────────────────────────────────────────────
    [Header("Combate")]
    [SerializeField] private float attackDetectionRadius = 3.8f;
    [SerializeField] private float captureThreatRadius = 2.2f;
    [SerializeField] private float attackCooldown = 1.6f;
    [SerializeField] private float attackWindup = 0.3f;
    [SerializeField] private float attackDuration = 0.85f;
    [SerializeField] private float projectileSpeed = 6.2f;
    [SerializeField] private float projectileLifetime = 2.0f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.4f, 0.08f);

    // ── Animacion ───────────────────────────────────────────────────────────
    [Header("Animacion")]
    [SerializeField] private string resourcePath = "Slimes/Cleaner";
    [SerializeField] private float idleFrameRate = 7f;
    [SerializeField] private float bounceFrameRate = 14f;
    [SerializeField] private float attackFrameRate = 16f;
    [SerializeField] private float hurtFrameRate = 12f;
    [SerializeField] private float dieFrameRate = 9f;

    // ── Evolucion ───────────────────────────────────────────────────────────
    [Header("Evolucion — Purificador")]
    [SerializeField] private bool autoEvolve = true;
    [SerializeField] private float evolutionDelay = 30f;
    [SerializeField] private float evolutionDuration = 2.8f;   // 8 frames a 16 fps
    [SerializeField] private string evolvedResourcePath = "Slimes/Cleaner/Evolved";
    [SerializeField] private float evolutionFrameRate = 16f;
    [SerializeField] private float evolvedScaleMultiplier = 1.1f; // Purificador es mas grande
    [SerializeField] private string evolvedSlimeName = "Purificador";
    [SerializeField] private string evolvedSlimeType = "Slime Purificador";
    [SerializeField] private string evolvedProducedResource = "Agua Purificada";
    // [Tooltip("Requisitos de evolucion mostrados en UI")]
    // [SerializeField] private string evolutionRequirements = "Basura organica + Agua sucia + Humedad alta";
    // [Tooltip("Descripcion de habilidades del Purificador")]
    // [SerializeField] private string purificadorAbilities = "Purifica agua, limpia telas y elimina bacterias";

    [Header("Multiplicacion")]
    [SerializeField] private bool autoMultiply = true;
    [SerializeField] private float multiplicationDelay = 30f;

    // ── Habilidad de purificacion ───────────────────────────────────────────
    [Header("Aura de Purificacion")]
    [SerializeField] private float bubbleAuraRadius = 1.0f;
    [SerializeField] private float evolvedBubbleAuraRadius = 2.0f;  // Purificador: mayor alcance
    [SerializeField] private float bubbleAuraCooldown = 4f;
    [SerializeField] private int bubbleBurstCount = 16;              // Mas particulas al evolucionar

    // ── Squash / Stretch ────────────────────────────────────────────────────
    [Header("Squash & Stretch")]
    [SerializeField] private float squashX = 0.14f;
    [SerializeField] private float squashY = 0.18f;
    [SerializeField] private float squashSpeed = 20f;

    // ── Privado ─────────────────────────────────────────────────────────────
    private readonly Dictionary<State, Sprite[]> frames = new Dictionary<State, Sprite[]>();
    private readonly Dictionary<State, Sprite[]> evolvedFrames = new Dictionary<State, Sprite[]>();
    private Rigidbody2D rb;
    private Collider2D bodyCol;
    private SpriteRenderer sr;
    private Transform targetPlayer;
    private Vector3 baseScale;
    private Vector3 originalBaseScale;
    private State state = State.Idle;
    private float direction = 1f;
    private float decisionTimer;
    private float attackCooldownTimer;
    private float attackTimer;
    private float animTimer;
    private float bubbleAuraTimer;
    private float lifeTimer;
    private float evolutionTimer;
    private float multiplicationTimer;
    private float panicTimer;
    private int frameIndex;
    private float squashIntensity = 0f;
    private bool isIdling;
    private bool projectileFired;
    private bool isEvolved;
    private bool isEvolving;
    private bool isPanicking;
    private Sprite projectileSprite;
    private Sprite evolvedProjectileSprite;
    private SpriteRenderer glowRenderer;
    private SlimeProfessionalFX professionalFx;
    private SlimeProduction production;

    // ── Awake / Update / FixedUpdate ─────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        professionalFx = GetComponent<SlimeProfessionalFX>();
        production = GetComponent<SlimeProduction>();
        baseScale = transform.localScale;
        originalBaseScale = baseScale;

        production?.Configure("Biomasa", evolvedProducedResource, 1, 2, 9f, 5f);

        LoadAllFrames();
        CreateGlow();
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
        FindPlayer();
        TickBubbleAura();
        TickPanic();

        if (state == State.Attack)
            TickAttack();
        else
        {
            TickDecision();
            TryReactToCapture();
        }

        Animate();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isEvolving || state == State.Attack || state == State.Die)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float speed = isIdling ? 0f : (isPanicking ? runSpeed * panicSpeedMultiplier : walkSpeed) * direction;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        if (!isIdling)
            HandleNavigation();
    }

    // ── Movimiento / Navegacion ───────────────────────────────────────────────

    private void TickDecision()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer > 0f) return;
        PickNextDecision();
    }

    private void PickNextDecision()
    {
        decisionTimer = UnityEngine.Random.Range(minDecisionTime, maxDecisionTime);
        isIdling = !isPanicking && UnityEngine.Random.value < idleChance;

        // Mario-style: cambia de direccion con probabilidad
        if (!isIdling && UnityEngine.Random.value < directionChangeChance && !isPanicking)
            direction *= -1f;

        ChangeState(isIdling ? State.Idle : State.Bounce);
        if (!isIdling) squashIntensity = 0.8f;
    }

    private void HandleNavigation()
    {
        Bounds b = bodyCol.bounds;

        // Detecta obstaculo adelante → salta inmediatamente (estilo hongo Mario)
        Vector2 obstacleOrigin = new Vector2(b.center.x + direction * b.extents.x, b.center.y);
        bool obstacleAhead = Physics2D.Raycast(obstacleOrigin, Vector2.right * direction, obstacleCheckDistance, obstacleLayer);

        // Detecta borde → gira
        Vector2 edgeOrigin = new Vector2(b.center.x + direction * (b.extents.x + 0.16f), b.min.y + 0.06f);
        bool groundAhead = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

        if (obstacleAhead && IsGrounded())
        {
            // Salta el obstaculo sin detenerse
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
        Bounds b = bodyCol.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.02f);
        Vector2 size = new Vector2(b.size.x * 0.7f, 0.08f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void TickPanic()
    {
        if (targetPlayer == null)
        {
            isPanicking = false;
            panicTimer = 0f;
            return;
        }

        float dist = Vector2.Distance(transform.position, targetPlayer.position);
        if (dist <= panicRadius)
        {
            isPanicking = true;
            panicTimer = 1.5f;
            // Huye en direccion opuesta al jugador
            direction = transform.position.x < targetPlayer.position.x ? -1f : 1f;
        }
        else if (panicTimer > 0f)
        {
            panicTimer -= Time.deltaTime;
            if (panicTimer <= 0f) isPanicking = false;
        }
    }

    // ── Combate ───────────────────────────────────────────────────────────────

    public bool ReactToCaptureAttempt(Transform capturer)
    {
        if (capturer == null || isEvolving || state == State.Die) return false;
        targetPlayer = capturer;
        FaceTarget(capturer.position);
        StartAttack();
        return true;
    }

    private void TryReactToCapture()
    {
        if (targetPlayer == null || attackCooldownTimer > 0f || !WasCaptureKey()) return;
        if (Vector2.Distance(transform.position, targetPlayer.position) <= captureThreatRadius)
        {
            FaceTarget(targetPlayer.position);
            StartAttack();
        }
    }

    private void StartAttack()
    {
        if (isEvolving || attackCooldownTimer > 0f || state == State.Die) return;
        ChangeState(State.Attack);
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
            PickNextDecision();
    }

    private void SpawnProjectile()
    {
        Sprite sprite = isEvolved && evolvedProjectileSprite != null ? evolvedProjectileSprite : projectileSprite;
        if (sprite == null) return;

        Vector2 dir = targetPlayer != null
            ? ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized
            : Vector2.right * direction;
        if (dir.sqrMagnitude <= 0.001f) dir = Vector2.right * direction;

        Vector2 offset = new Vector2(projectileSpawnOffset.x * direction, projectileSpawnOffset.y);
        GameObject proj = new GameObject("CleanerSlime_Projectile");
        proj.transform.position = (Vector2)transform.position + offset;
        proj.transform.localScale = new Vector3(Mathf.Sign(dir.x), 1f, 1f);

        SpriteRenderer psr = proj.AddComponent<SpriteRenderer>();
        psr.sprite = sprite;
        psr.sortingOrder = sr.sortingOrder + 1;

        Rigidbody2D prb = proj.AddComponent<Rigidbody2D>();
        prb.bodyType = RigidbodyType2D.Kinematic;
        prb.gravityScale = 0f;

        CircleCollider2D pcol = proj.AddComponent<CircleCollider2D>();
        pcol.radius = 0.2f;
        pcol.isTrigger = true;

        CleanerSlimeProjectile pb = proj.AddComponent<CleanerSlimeProjectile>();
        pb.Launch(dir, projectileSpeed, projectileLifetime);

        professionalFx?.Burst(isEvolved ? bubbleBurstCount + 8 : bubbleBurstCount);
    }

    private void TickBubbleAura()
    {
        bubbleAuraTimer -= Time.deltaTime;
        if (bubbleAuraTimer > 0f || state == State.Die) return;

        bubbleAuraTimer = bubbleAuraCooldown;
        float r = isEvolved ? evolvedBubbleAuraRadius : bubbleAuraRadius;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, r, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            hit.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
            professionalFx?.Burst(isEvolved ? bubbleBurstCount : Mathf.Max(6, bubbleBurstCount / 2));
        }

        if (!isEvolved) return;

        // ── Pulso de Purificacion ────────────────────────────────────────────
        // Emite particulas en anillo y produce recurso
        professionalFx?.Burst(bubbleBurstCount);
        PurificationPulse(r);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddResource(evolvedProducedResource, 1);
    }

    /// <summary>
    /// Efecto visual: escala rapida up/down del glow para simular pulso de purificacion.
    /// </summary>
    private void PurificationPulse(float radius)
    {
        if (glowRenderer == null) return;
        glowRenderer.enabled = true;
        glowRenderer.color = new Color(1f, 0.5f, 0.88f, 0.7f);
        float s = radius * 2.2f;
        glowRenderer.transform.localScale = new Vector3(s, s, 1f);
        // Ocultar despues de un frame con Invoke
        Invoke(nameof(HideGlow), 0.18f);
    }

    // ── Deteccion de jugador ──────────────────────────────────────────────────

    private void FindPlayer()
    {
        if (targetPlayer != null) return;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, playerDetectionRadius, LayerMask.GetMask("Player"));
        if (hit != null) { targetPlayer = hit.transform; return; }
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && Vector2.Distance(transform.position, p.transform.position) <= playerDetectionRadius)
            targetPlayer = p.transform;
    }

    private bool WasCaptureKey()
    {
        bool pressed = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(KeyCode.E);
#endif
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        pressed = pressed || (kb != null && kb.eKey.wasPressedThisFrame);
#endif
        return pressed;
    }

    // ── Animacion ─────────────────────────────────────────────────────────────

    private void Animate()
    {
        Sprite[] f = GetCurrentFrames();
        if (f.Length == 0) return;

        animTimer += Time.deltaTime;
        float ft = 1f / Mathf.Max(1f, GetFrameRate());
        if (animTimer >= ft)
        {
            animTimer -= ft;
            frameIndex = (frameIndex + 1) % f.Length;
        }

        sr.sprite = f[Mathf.Clamp(frameIndex, 0, f.Length - 1)];
        ApplySquash(f.Length);
    }

    private Sprite[] GetCurrentFrames()
    {
        if (isEvolving && evolvedFrames.TryGetValue(State.Evolving, out Sprite[] ev) && ev.Length > 0)
            return ev;
        Dictionary<State, Sprite[]> src = isEvolved ? evolvedFrames : frames;
        if (src.TryGetValue(state, out Sprite[] f) && f.Length > 0) return f;
        return frames.TryGetValue(state, out f) ? f : Array.Empty<Sprite>();
    }

    private float GetFrameRate() => state switch
    {
        State.Bounce => bounceFrameRate,
        State.Evolving => evolutionFrameRate,
        State.Attack => attackFrameRate,
        State.Hurt => hurtFrameRate,
        State.Die => dieFrameRate,
        _ => idleFrameRate
    };

    private void ApplySquash(int count)
    {
        squashIntensity = Mathf.Lerp(squashIntensity, 0f, Time.deltaTime * 8f);
        
        float pulse = 0f;
        if (count > 1) {
            pulse = Mathf.Sin((frameIndex / (float)count) * Mathf.PI);
        }

        float mult = isEvolved ? 1.5f : 1f;

        Vector3 target;
        if (state == State.Bounce || state == State.Attack)
        {
            target = new Vector3(
                baseScale.x * (1f - pulse * squashX * mult * squashIntensity),
                baseScale.y * (1f + pulse * squashY * mult * squashIntensity),
                baseScale.z);
        }
        else if (state == State.Hurt)
        {
            target = new Vector3(baseScale.x * (1f + pulse * 0.2f), baseScale.y * (1f - pulse * 0.15f), baseScale.z);
        }
        else
        {
            target = baseScale;
        }

        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            float stretch = Mathf.Clamp(Mathf.Abs(rb.linearVelocity.y) * 0.03f, 0f, 0.3f);
            target = new Vector3(target.x * (1f - stretch), target.y * (1f + stretch), target.z);
        }

        if (!isEvolving)
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * squashSpeed);
    }

    // ── Evolucion ─────────────────────────────────────────────────────────────

    private void TickAutoEvolution()
    {
        if (!autoEvolve || isEvolved || isEvolving) return;
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= evolutionDelay) StartEvolution();
    }

    private void TickMultiplication()
    {
        if (SlimeReproductionSystem.ReproduccionEcologicaActiva) return;
        if (!autoMultiply || isEvolving || state == State.Die) return;

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
        CleanerSlimeAI cloneAI = clone.GetComponent<CleanerSlimeAI>();
        if (cloneAI != null)
        {
            cloneAI.lifeTimer = 0f;
            cloneAI.multiplicationTimer = 0f;
            Rigidbody2D cloneRb = clone.GetComponent<Rigidbody2D>();
            cloneRb.linearVelocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), jumpForce);
        }

        professionalFx?.Burst(40);
        squashIntensity = 2.0f;
        ChangeState(State.Bounce);
    }

    private void StartEvolution()
    {
        if (isEvolved || isEvolving || !HasEvolvedFrames()) return;
        isEvolving = true;
        isIdling = true;
        evolutionTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        ChangeState(State.Evolving);
        ShowGlow(0f);

        // ── Efecto tipo Pokemon ───────────────────────────────────────────────
        // Lanza el overlay de flash + texto durante toda la duracion de la evolucion
        PokemonEvolutionFX.Play(
            evolutionDuration,
            "Cleaner Slime",
            evolvedSlimeName
        );
    }

    private void TickEvolution()
    {
        evolutionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(evolutionTimer / Mathf.Max(0.1f, evolutionDuration));
        float pulse = Mathf.Sin(progress * Mathf.PI);

        // Pulso de color: blanco → rosa fuerte (tema Purificador)
        sr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.82f, 1f), pulse);
        transform.localScale = originalBaseScale * Mathf.Lerp(1f, evolvedScaleMultiplier * 1.08f, pulse);
        ShowGlow(pulse);

        if (evolutionTimer >= evolutionDuration) FinishEvolution();
    }

    private void FinishEvolution()
    {
        isEvolved = true;
        isEvolving = false;
        sr.color = Color.white;
        baseScale = originalBaseScale * evolvedScaleMultiplier;
        transform.localScale = baseScale;
        GetComponent<SlimeController>()?.SetEvolutionData(evolvedSlimeName, evolvedSlimeType, evolvedProducedResource);
        professionalFx?.SetEvolved(true);
        production?.SetEvolved(true);
        HideGlow();
        ChangeState(State.Idle);
        PickNextDecision();
        // Nota: PokemonEvolutionFX maneja su propio timing internamente;
        // el flash final y el texto de conclusion ya estan programados.
    }

    private bool HasEvolvedFrames()
        => evolvedFrames.TryGetValue(State.Idle, out var a) && a.Length > 0
        && evolvedFrames.TryGetValue(State.Evolving, out var b) && b.Length > 0;

    // ── Utilidades ────────────────────────────────────────────────────────────

    private void ChangeState(State next)
    {
        if (isEvolving && next != State.Evolving) return;
        if (state == next) return;
        state = next;
        frameIndex = 0;
        animTimer = 0f;
    }

    private void FaceTarget(Vector3 t) => direction = t.x >= transform.position.x ? 1f : -1f;
    private void UpdateFacing() => sr.flipX = direction < 0f;

    // ── Carga de sprites ──────────────────────────────────────────────────────

    private void LoadAllFrames()
    {
        Sprite[] s = Resources.LoadAll<Sprite>(resourcePath);
        AddFrames(frames, s, State.Idle, "CleanerSlime_idle");
        AddFrames(frames, s, State.Bounce, "CleanerSlime_bounce");
        AddFrames(frames, s, State.Evolving, "CleanerSlime_evolve");
        AddFrames(frames, s, State.Attack, "CleanerSlime_attack");
        AddFrames(frames, s, State.Hurt, "CleanerSlime_hurt");
        AddFrames(frames, s, State.Die, "CleanerSlime_die");
        projectileSprite = s.FirstOrDefault(x => x.name == "CleanerSlime_Projectile");

        Sprite[] es = Resources.LoadAll<Sprite>(evolvedResourcePath);
        AddFrames(evolvedFrames, es, State.Idle, "CleanerSlimeEvolved_idle");
        AddFrames(evolvedFrames, es, State.Bounce, "CleanerSlimeEvolved_bounce");
        AddFrames(evolvedFrames, es, State.Evolving, "CleanerSlimeEvolved_evolve");
        AddFrames(evolvedFrames, es, State.Attack, "CleanerSlimeEvolved_attack");
        AddFrames(evolvedFrames, es, State.Hurt, "CleanerSlimeEvolved_hurt");
        AddFrames(evolvedFrames, es, State.Die, "CleanerSlimeEvolved_die");
        evolvedProjectileSprite = es.FirstOrDefault(x => x.name == "CleanerSlimeEvolved_Projectile");
    }

    private void AddFrames(Dictionary<State, Sprite[]> dict, Sprite[] all, State s, string prefix)
    {
        dict[s] = all
            .Where(x => x.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(x => x.name)
            .ToArray();
    }

    // ── Glow de evolucion ─────────────────────────────────────────────────────

    private void CreateGlow()
    {
        GameObject go = new GameObject("CleanerSlime_EvolutionGlow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        glowRenderer = go.AddComponent<SpriteRenderer>();
        glowRenderer.sortingOrder = sr.sortingOrder - 1;
        glowRenderer.sprite = BuildGlowSprite();
        glowRenderer.enabled = false;
    }

    private Sprite BuildGlowSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
                // Glow calido rosa-magenta para la evolucion del Purificador
                tex.SetPixel(x, y, new Color(1f, 0.30f, 0.76f, a * 0.72f));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
    }

    private void ShowGlow(float strength)
    {
        if (glowRenderer == null) return;
        glowRenderer.enabled = strength > 0.01f;
        glowRenderer.color = new Color(1f, 0.5f, 0.85f, Mathf.Clamp01(strength));
        float s = Mathf.Lerp(0.9f, 2.4f, strength);
        glowRenderer.transform.localScale = new Vector3(s, s, 1f);
    }

    private void HideGlow()
    {
        if (glowRenderer != null) glowRenderer.enabled = false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
        Gizmos.color = new Color(1f, 0.6f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, captureThreatRadius);
        Gizmos.color = new Color(1f, 0.5f, 0.85f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, isEvolved ? evolvedBubbleAuraRadius : bubbleAuraRadius);
        Gizmos.color = new Color(1f, 0.3f, 0.7f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, panicRadius);
    }
}
