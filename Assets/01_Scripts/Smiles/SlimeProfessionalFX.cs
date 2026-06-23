using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SlimeProfessionalFX : MonoBehaviour
{
    public enum SlimeFxKind
    {
        Fire,
        Acid,
        Cold,
        Cleaner
    }

    [SerializeField] private SlimeFxKind kind;
    [SerializeField] private float idleGlowPulse = 1f;
    [SerializeField] private float glowBaseAlpha = 0.22f;
    [SerializeField] private float glowEvolvedAlpha = 0.52f;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer glowRenderer;
    private ParticleSystem ambientParticles;
    private Vector3 glowBaseScale;
    private bool evolved;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateGlow();
        CreateAmbientParticles();
    }

    private void Update()
    {
        float pulseSpeed = evolved ? 3.8f : 2.2f;
        float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        float maxAlpha = evolved ? glowEvolvedAlpha : glowBaseAlpha + 0.1f;
        float minAlpha = evolved ? glowBaseAlpha : 0.12f;

        Color color = kind switch
        {
            SlimeFxKind.Fire    => new Color(1f, 0.42f, 0.06f, Mathf.Lerp(minAlpha, maxAlpha, pulse)),
            SlimeFxKind.Cold    => evolved
                ? new Color(0.72f, 0.96f, 1f, Mathf.Lerp(minAlpha, maxAlpha, pulse))
                : new Color(0.28f, 0.78f, 1f, Mathf.Lerp(minAlpha, maxAlpha - 0.1f, pulse)),
            SlimeFxKind.Cleaner => evolved
                ? new Color(0.85f, 0.22f, 1f,  Mathf.Lerp(minAlpha, maxAlpha, pulse))   // crystal magenta
                : new Color(1f,   0.40f, 0.80f, Mathf.Lerp(minAlpha, maxAlpha, pulse)),  // hot pink
            _ => new Color(0.66f, 0.2f, 1f, Mathf.Lerp(minAlpha, maxAlpha, pulse))
        };

        glowRenderer.color = color;
        float scaleBoost = evolved ? 0.22f : 0.08f;
        float scale = 1f + pulse * idleGlowPulse * scaleBoost;
        glowRenderer.transform.localScale = glowBaseScale * scale;
    }

    public void SetEvolved(bool value)
    {
        evolved = value;

        if (ambientParticles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = ambientParticles.emission;
        emission.rateOverTime = evolved
            ? (kind == SlimeFxKind.Cold || kind == SlimeFxKind.Cleaner ? 18f : 10f)
            : 5f;

        ParticleSystem.MainModule main = ambientParticles.main;
        if (kind == SlimeFxKind.Cold && evolved)
        {
            // Crystal sparkle: rapido, brillante, pequeno
            main.startSize = 0.04f;
            main.startSpeed = 0.85f;
            main.startLifetime = 0.6f;
            main.startColor = new Color(0.85f, 0.98f, 1f, 0.95f);
        }
        else if (kind == SlimeFxKind.Cleaner && evolved)
        {
            // Crystal bubbles: flota lento, grande, magenta
            main.startSize = 0.10f;
            main.startSpeed = 0.40f;
            main.startLifetime = 1.8f;
            main.startColor = new Color(0.90f, 0.25f, 1f, 0.90f);
        }
        else
        {
            main.startSize = evolved ? 0.08f : 0.05f;
            main.startSpeed = evolved ? 0.55f : 0.32f;
        }
    }

    public void Burst(int count)
    {
        if (ambientParticles != null)
        {
            ambientParticles.Emit(count);
        }
    }

    private void CreateGlow()
    {
        GameObject glowObject = new GameObject("Slime_AmbientGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        glowObject.transform.localScale = new Vector3(1.25f, 0.82f, 1f);
        glowBaseScale = glowObject.transform.localScale;

        glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = BuildRadialSprite();
        glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
    }

    private void CreateAmbientParticles()
    {
        GameObject particleObject = new GameObject("Slime_AmbientParticles");
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        ambientParticles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ambientParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = kind == SlimeFxKind.Fire ? 0.75f : 1.4f;
        main.startSpeed = 0.32f;
        main.startSize = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = kind switch
        {
            SlimeFxKind.Fire    => new Color(1f,    0.42f, 0.05f, 0.82f),
            SlimeFxKind.Cold    => new Color(0.58f, 0.90f, 1f,    0.72f),
            SlimeFxKind.Cleaner => new Color(1f,    0.40f, 0.80f, 0.75f),  // burbujas rosas
            _                   => new Color(0.75f, 0.28f, 1f,    0.65f),
        };

        ParticleSystem.EmissionModule emission = ambientParticles.emission;
        emission.rateOverTime = 5f;

        ParticleSystem.ShapeModule shape = ambientParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.32f;

        ParticleSystemRenderer renderer = ambientParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
    }

    private Sprite BuildRadialSprite()
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 96f);
    }
}
