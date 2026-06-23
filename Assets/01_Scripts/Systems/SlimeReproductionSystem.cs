using UnityEngine;

public class SlimeReproductionSystem : MonoBehaviour
{
    public static bool ReproduccionEcologicaActiva = true;

    [SerializeField] private float intervaloChequeo = 5f;
    [SerializeField] private int poblacionMaximaGlobal = 36;
    [SerializeField] private float energiaMinima = 82f;
    [SerializeField] private float masaMinima = 72f;
    [SerializeField] private float felicidadMinima = 64f;

    private float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<SlimeReproductionSystem>() != null)
        {
            return;
        }

        new GameObject("SlimeReproductionSystem").AddComponent<SlimeReproductionSystem>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < intervaloChequeo)
        {
            return;
        }

        timer = 0f;
        TickReproduccion();
    }

    private void TickReproduccion()
    {
        SlimeGenome[] genomes = FindObjectsByType<SlimeGenome>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (genomes.Length >= poblacionMaximaGlobal)
        {
            if (EcosystemManager.instancia != null)
            {
                EcosystemManager.instancia.riesgoColapso = Mathf.Clamp(EcosystemManager.instancia.riesgoColapso + 3f, 0f, 100f);
            }
            return;
        }

        foreach (SlimeGenome genome in genomes)
        {
            if (genome == null || !PuedeDividirse(genome))
            {
                continue;
            }

            if (ContarCercanos(genome.transform.position, 5f) > 14)
            {
                genome.estabilidadGenetica = Mathf.Clamp(genome.estabilidadGenetica - 8f, 0f, 100f);
                EcosystemToast.Mostrar("Demasiados slimes juntos: riesgo de descontrol");
                continue;
            }

            Dividir(genome);
            break;
        }
    }

    private bool PuedeDividirse(SlimeGenome genome)
    {
        bool energiaSuficiente = genome.energiaInterna >= energiaMinima
            && genome.masaBiologica >= masaMinima
            && genome.felicidad >= felicidadMinima
            && genome.estabilidadGenetica >= 32f;

        return energiaSuficiente && (!genome.evolucionado || genome.energiaInterna >= 90f);
    }

    private void Dividir(SlimeGenome origen)
    {
        Vector3 offset = new Vector3(Random.Range(-0.75f, 0.75f), 0.25f, 0f);
        GameObject copia = Instantiate(origen.gameObject, origen.transform.position + offset, Quaternion.identity);
        copia.name = origen.gameObject.name + "_Cria";
        copia.SetActive(true);

        SlimeGenome cria = SlimeGenomeInstaller.Ensure(copia);
        bool mutacion = Random.value > Mathf.Clamp01(origen.estabilidadGenetica / 100f);
        cria.HeredarDesde(origen, mutacion);

        origen.energiaInterna = Mathf.Clamp(origen.energiaInterna * 0.48f, 0f, 100f);
        origen.masaBiologica = Mathf.Clamp(origen.masaBiologica * 0.55f, 0f, 100f);
        origen.felicidad = Mathf.Clamp(origen.felicidad - 12f, 0f, 100f);

        if (mutacion)
        {
            origen.estabilidadGenetica = Mathf.Clamp(origen.estabilidadGenetica - 10f, 0f, 100f);
        }

        EcosystemManager.RegistrarReproduccion(origen, cria, mutacion);
        EcosystemToast.Mostrar(mutacion ? "Division con mutacion detectada" : "Un slime se dividio");
    }

    private static int ContarCercanos(Vector3 posicion, float radio)
    {
        int total = 0;
        foreach (SlimeGenome genome in FindObjectsByType<SlimeGenome>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (genome != null && Vector2.Distance(posicion, genome.transform.position) <= radio)
            {
                total++;
            }
        }

        return total;
    }
}
