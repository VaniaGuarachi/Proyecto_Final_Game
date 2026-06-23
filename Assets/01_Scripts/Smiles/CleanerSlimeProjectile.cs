using UnityEngine;

/// <summary>
/// Proyectil de burbuja de jabon del Cleaner Slime.
/// Al impactar deja una nube de burbujas que limpia y ralentiza al jugador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class CleanerSlimeProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private float lifetime;
    private SpriteRenderer sr;
    private float scaleTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 direction, float speed, float life)
    {
        rb.linearVelocity = direction * speed;
        lifetime = life;
        // Rotacion hacia la direccion de disparo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            SpawnBubblePop();
            Destroy(gameObject);
            return;
        }

        // Pulsacion visual de la burbuja
        scaleTimer += Time.deltaTime * 6f;
        float pulse = 1f + Mathf.Sin(scaleTimer) * 0.08f;
        transform.localScale = new Vector3(pulse, pulse, 1f);

        // Desvanece al final de vida
        if (sr != null && lifetime < 0.4f)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, lifetime / 0.4f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
            SpawnBubblePop();
            Destroy(gameObject);
        }
    }

    private void SpawnBubblePop()
    {
        // Nube de burbujas al impactar — limpia recursos del suelo
        GameObject cloud = new GameObject("CleanerSlime_BubbleCloud");
        cloud.transform.position = transform.position;

        CircleCollider2D col = cloud.AddComponent<CircleCollider2D>();
        col.radius = 0.6f;
        col.isTrigger = true;

        SpriteRenderer cloudSr = cloud.AddComponent<SpriteRenderer>();
        cloudSr.sprite = BuildBubbleSprite();
        cloudSr.color = new Color(1f, 0.5f, 0.85f, 0.38f);
        cloudSr.sortingOrder = 2;
        cloud.transform.localScale = Vector3.one * 1.2f;

        SlimeHazardZone hz = cloud.AddComponent<SlimeHazardZone>();
        hz.Configure(2f, 0.9f, false, "Biomasa", 1);
    }

    private Sprite BuildBubbleSprite()
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f);
                tex.SetPixel(x, y, new Color(1f, 0.6f, 0.9f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }
}
