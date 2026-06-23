using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlimeEvolutionSystem : MonoBehaviour
{
    [SerializeField] private float intervaloChequeo = 3f;
    [SerializeField] private float alimentacionesRequeridas = 2f;
    [SerializeField] private float segundosBiomaRequeridos = 18f;
    [SerializeField] private float energiaRequerida = 58f;
    [SerializeField] private float estabilidadRequerida = 45f;

    private float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<SlimeEvolutionSystem>() != null)
        {
            return;
        }

        new GameObject("SlimeEvolutionSystem").AddComponent<SlimeEvolutionSystem>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < intervaloChequeo)
        {
            return;
        }

        timer = 0f;
        RevisarEvoluciones();
    }

    public bool CanEvolve(SlimeController slime)
    {
        SlimeGenome genome = slime != null ? SlimeGenomeInstaller.Ensure(slime.gameObject) : null;
        return PuedeEvolucionar(genome);
    }

    public void TryEvolve(SlimeController slime)
    {
        SlimeGenome genome = slime != null ? SlimeGenomeInstaller.Ensure(slime.gameObject) : null;
        if (PuedeEvolucionar(genome))
        {
            Evolucionar(genome);
        }
    }

    private void RevisarEvoluciones()
    {
        foreach (SlimeGenome genome in FindObjectsByType<SlimeGenome>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (PuedeEvolucionar(genome))
            {
                Evolucionar(genome);
            }
        }
    }

    private bool PuedeEvolucionar(SlimeGenome genome)
    {
        if (genome == null || genome.evolucionado)
        {
            return false;
        }

        if (genome.energiaInterna < energiaRequerida || genome.estabilidadGenetica < estabilidadRequerida)
        {
            return false;
        }

        bool alimentado = genome.alimentacionesCorrectas >= alimentacionesRequeridas;
        bool bioma = genome.segundosEnBiomaFavorito >= segundosBiomaRequeridos || BiomaClaveCumplido(genome);
        bool recurso = RecursoClaveDisponible(genome);

        switch (genome.especie)
        {
            case SlimeSpecies.Cleaner:
                return alimentado && (bioma || recurso || EcosystemManager.instancia != null && EcosystemManager.instancia.contaminacion < 35f);
            case SlimeSpecies.Acid:
                return alimentado && (genome.BiomaActual == SlimeBiomeType.Toxico || recurso);
            case SlimeSpecies.Fire:
                return alimentado && (genome.BiomaActual == SlimeBiomeType.Volcanico || recurso);
            case SlimeSpecies.Cold:
                return alimentado && (genome.BiomaActual == SlimeBiomeType.Frio || genome.BiomaActual == SlimeBiomeType.Cristalino || recurso);
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde:
                return alimentado && (genome.BiomaActual == SlimeBiomeType.Fertil || recurso);
            case SlimeSpecies.Amarillo:
                return alimentado && (genome.BiomaActual == SlimeBiomeType.Humedo || recurso);
            default:
                return alimentado && bioma;
        }
    }

    private bool BiomaClaveCumplido(SlimeGenome genome)
    {
        return genome.exposicionBiomaClave >= segundosBiomaRequeridos;
    }

    private bool RecursoClaveDisponible(SlimeGenome genome)
    {
        if (InventarioRecursosSmiles.instancia == null)
        {
            return false;
        }

        InventarioRecursosSmiles inv = InventarioRecursosSmiles.instancia;
        switch (genome.especie)
        {
            case SlimeSpecies.Cleaner:
                return inv.vasosAgua > 0 || inv.gelVerde > 1;
            case SlimeSpecies.Acid:
                return inv.esenciaSmileMorada > 1;
            case SlimeSpecies.Fire:
                return inv.energiaSmileAmarilla > 1 || inv.bufandasAntifrio > 0;
            case SlimeSpecies.Cold:
                return inv.gotasSmileAzul > 1;
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde:
                return inv.gelVerde > 1 || inv.manzanas > 0;
            case SlimeSpecies.Amarillo:
                return inv.vasosAgua > 0;
            default:
                return false;
        }
    }

    private void Evolucionar(SlimeGenome genome)
    {
        if (genome == null || genome.evolucionado)
        {
            return;
        }

        bool usoEvolucionExistente = InvocarEvolucionExistente(genome.gameObject);
        AplicarDatosEvolucion(genome);
        genome.RegistrarEvolucion(TraitEvolucion(genome.especie));
        MostrarFeedback(genome.transform.position, NombreEvolucion(genome.especie));

        if (!usoEvolucionExistente)
        {
            AplicarEvolucionVisualSimple(genome);
        }
    }

    private bool InvocarEvolucionExistente(GameObject slime)
    {
        Component[] componentes = slime.GetComponents<Component>();
        foreach (Component componente in componentes)
        {
            if (componente == null)
            {
                continue;
            }

            MethodInfo metodo = componente.GetType().GetMethod("StartEvolution", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (metodo == null)
            {
                continue;
            }

            metodo.Invoke(componente, null);
            return true;
        }

        return false;
    }

    private void AplicarDatosEvolucion(SlimeGenome genome)
    {
        SlimeController controller = genome.GetComponent<SlimeController>();
        if (controller != null)
        {
            controller.SetEvolutionData(NombreEvolucion(genome.especie), TipoEvolucion(genome.especie), RecursoEvolucion(genome.especie));
        }

        SlimeProduction production = genome.GetComponent<SlimeProduction>();
        if (production != null)
        {
            production.Configure(RecursoBase(genome.especie), RecursoEvolucion(genome.especie), 1, 2, 9f, 5f);
            production.SetEvolved(true);
        }
    }

    private void AplicarEvolucionVisualSimple(SlimeGenome genome)
    {
        genome.transform.localScale *= 1.18f;
        SpriteRenderer renderer = genome.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.Lerp(renderer.color, Color.white, 0.25f);
        }
    }

    private void MostrarFeedback(Vector3 posicion, string nombre)
    {
        EcosystemToast.Mostrar(nombre + " evoluciono por equilibrio ecologico");
        GameObject go = new GameObject("Txt_Evolucion_Ecologica", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Canvas canvas = BuscarOCrearCanvas();
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.position = Camera.main != null ? Camera.main.WorldToScreenPoint(posicion + Vector3.up * 1.2f) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        rect.sizeDelta = new Vector2(260f, 42f);

        TMP_Text texto = go.GetComponent<TMP_Text>();
        texto.text = "EVOLUCION";
        texto.fontSize = 20f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(0.72f, 1f, 0.82f, 1f);
        texto.raycastTarget = false;
        Destroy(go, 2.2f);
    }

    private static string TraitEvolucion(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Cleaner: return "purificacion avanzada";
            case SlimeSpecies.Acid: return "acido estable";
            case SlimeSpecies.Fire: return "nucleo termico";
            case SlimeSpecies.Cold: return "cristal frio";
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde: return "biomasa fertil";
            case SlimeSpecies.Amarillo: return "hidratacion pura";
            default: return "adaptacion avanzada";
        }
    }

    private static string NombreEvolucion(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Cleaner: return "Slime Purificador";
            case SlimeSpecies.Acid: return "Slime Acido Estable";
            case SlimeSpecies.Fire: return "FireSlime Termico";
            case SlimeSpecies.Cold: return "ColdSlime Cristal";
            case SlimeSpecies.Nature: return "NatureSlime Fertil";
            case SlimeSpecies.Verde: return "Smile Verde Vital";
            case SlimeSpecies.Amarillo: return "Smile Azul Hidratado";
            default: return "Slime Adaptado";
        }
    }

    private static string TipoEvolucion(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Cleaner: return "Purificador";
            case SlimeSpecies.Acid: return "Acido evolucionado";
            case SlimeSpecies.Fire: return "Volcanico evolucionado";
            case SlimeSpecies.Cold: return "Cristalino evolucionado";
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde: return "Fertil evolucionado";
            case SlimeSpecies.Amarillo: return "Humedo evolucionado";
            default: return "Adaptado";
        }
    }

    private static string RecursoBase(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Fire: return "Calor";
            case SlimeSpecies.Cold: return "Frio";
            case SlimeSpecies.Acid: return "Acido";
            case SlimeSpecies.Cleaner: return "Biomasa";
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde: return "Biomasa";
            case SlimeSpecies.Amarillo: return "Agua";
            default: return "Biomasa";
        }
    }

    private static string RecursoEvolucion(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Fire: return "Nucleo de calor";
            case SlimeSpecies.Cold: return "Cristales";
            case SlimeSpecies.Acid: return "Acido mineral disolvente";
            case SlimeSpecies.Cleaner: return "Agua Purificada";
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde: return "Biomasa fertil";
            case SlimeSpecies.Amarillo: return "Agua viva";
            default: return "Biomasa avanzada";
        }
    }

    private static Canvas BuscarOCrearCanvas()
    {
        GameObject canvasObject = GameObject.Find("Canvas_UI");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas != null)
        {
            return canvas;
        }

        canvasObject = new GameObject("Canvas_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        return canvas;
    }
}
