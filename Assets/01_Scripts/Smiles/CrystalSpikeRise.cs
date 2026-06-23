using UnityEngine;

/// <summary>
/// Animacion de aparicion para los picos de cristal del Cold Crystal Slime evolucionado.
/// El pico sube desde el suelo rapidamente y luego se desvanece al final de su vida.
/// </summary>
public class CrystalSpikeRise : MonoBehaviour
{
    [SerializeField] private float riseSpeed = 4.5f;
    [SerializeField] private float fadeDuration = 0.8f;

    private Vector3 targetScale;
    private SpriteRenderer sr;
    private float lifetime;
    private float totalLifetime;
    private bool risingDone;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        targetScale = transform.localScale;
        // Empieza desde escala Y=0
        transform.localScale = new Vector3(targetScale.x, 0f, targetScale.z);

        SlimeHazardZone hz = GetComponent<SlimeHazardZone>();
        // Obtenemos la vida total leyendo el campo por reflection para no modificar HazardZone
        // Fallback: usamos 4 segundos si no podemos leerlo
        totalLifetime = 4f;
        lifetime = totalLifetime;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;

        // Fase de subida
        if (!risingDone)
        {
            float currentY = Mathf.MoveTowards(transform.localScale.y, targetScale.y,
                riseSpeed * Time.deltaTime);
            transform.localScale = new Vector3(targetScale.x, currentY, targetScale.z);

            if (Mathf.Approximately(currentY, targetScale.y))
            {
                risingDone = true;
            }
        }

        // Fase de desvanecimiento al final
        if (lifetime <= fadeDuration && sr != null)
        {
            float alpha = Mathf.Clamp01(lifetime / fadeDuration);
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}
