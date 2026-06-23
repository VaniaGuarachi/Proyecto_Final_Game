using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeController : MonoBehaviour
{
    [Header("Datos del slime")]
    [SerializeField] private string slimeName = "Slime";
    [SerializeField] private string slimeType = "Naturaleza";
    [SerializeField] private string producedResource = "Biomasa";

    [Header("Estado")]
    [Range(0, 100)] [SerializeField] private int hunger = 50;
    [Range(0, 100)] [SerializeField] private int happiness = 50;

    [Header("Movimiento")]
    [SerializeField] private bool controlsMovement = true;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float directionChangeTime = 2f;

    private Rigidbody2D rb;
    private float direction = 1f;
    private float directionTimer;

    public string SlimeName => slimeName;
    public string SlimeType => slimeType;
    public string ProducedResource => producedResource;
    public int Hunger => hunger;
    public int Happiness => happiness;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!controlsMovement)
        {
            return;
        }

        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            direction *= -1f;
            directionTimer = directionChangeTime;
        }
    }

    private void FixedUpdate()
    {
        if (!controlsMovement)
        {
            return;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    public void Feed(int hungerRecovered, int happinessGained)
    {
        hunger = Mathf.Clamp(hunger - hungerRecovered, 0, 100);
        happiness = Mathf.Clamp(happiness + happinessGained, 0, 100);

        SlimeGenome genome = SlimeGenomeInstaller.Ensure(gameObject);
        if (genome != null)
        {
            genome.energiaInterna = Mathf.Clamp(genome.energiaInterna + hungerRecovered * 0.6f, 0f, 100f);
            genome.felicidad = Mathf.Clamp(genome.felicidad + happinessGained, 0f, 100f);
            genome.masaBiologica = Mathf.Clamp(genome.masaBiologica + hungerRecovered * 0.35f, 0f, 100f);
        }
    }

    public void SetEvolutionData(string newName, string newType, string newProducedResource)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            slimeName = newName;
        }

        if (!string.IsNullOrWhiteSpace(newType))
        {
            slimeType = newType;
        }

        if (!string.IsNullOrWhiteSpace(newProducedResource))
        {
            producedResource = newProducedResource;
        }

        SlimeGenome genome = SlimeGenomeInstaller.Ensure(gameObject);
        if (genome != null && !genome.evolucionado)
        {
            genome.RegistrarEvolucion("evolucion ai");
        }
    }
}
