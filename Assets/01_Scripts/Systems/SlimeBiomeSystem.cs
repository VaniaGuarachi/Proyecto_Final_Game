using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlimeBiomeZone : MonoBehaviour
{
    public SlimeBiomeType tipo = SlimeBiomeType.Neutral;
    public string nombre = "Bioma";
    public Vector2 tamano = new Vector2(10f, 7f);

    private BoxCollider2D zona;

    public static SlimeBiomeZone Crear(string nombreZona, SlimeBiomeType tipoZona, Vector2 centro, Vector2 tamanoZona)
    {
        GameObject go = new GameObject("Bioma_" + tipoZona);
        go.transform.position = centro;
        SlimeBiomeZone bioma = go.AddComponent<SlimeBiomeZone>();
        bioma.tipo = tipoZona;
        bioma.nombre = nombreZona;
        bioma.tamano = tamanoZona;
        bioma.ConfigurarCollider();
        return bioma;
    }

    private void Awake()
    {
        ConfigurarCollider();
    }

    private void ConfigurarCollider()
    {
        zona = GetComponent<BoxCollider2D>();
        if (zona == null)
        {
            zona = gameObject.AddComponent<BoxCollider2D>();
        }

        zona.isTrigger = true;
        zona.size = tamano;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SlimeGenome genome = other.GetComponentInParent<SlimeGenome>();
        if (genome != null)
        {
            genome.SetBiomaActual(tipo);
        }

        if (other.GetComponentInParent<CriadorSlimesController>() != null)
        {
            BiologicalResearchSystem.RegistrarBioma(tipo);
            EcosystemToast.Mostrar("Entraste a bioma " + NombreBioma(tipo));
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        SlimeGenome genome = other.GetComponentInParent<SlimeGenome>();
        if (genome != null)
        {
            genome.SetBiomaActual(tipo);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SlimeGenome genome = other.GetComponentInParent<SlimeGenome>();
        if (genome != null && genome.BiomaActual == tipo)
        {
            genome.SetBiomaActual(SlimeBiomeType.Neutral);
        }
    }

    private static string NombreBioma(SlimeBiomeType tipo)
    {
        switch (tipo)
        {
            case SlimeBiomeType.Fertil: return "fertil";
            case SlimeBiomeType.Humedo: return "humedo";
            case SlimeBiomeType.Toxico: return "toxico";
            case SlimeBiomeType.Volcanico: return "volcanico";
            case SlimeBiomeType.Frio: return "frio";
            case SlimeBiomeType.Cristalino: return "cristalino";
            case SlimeBiomeType.Ruinas: return "ruinas";
            default: return "neutral";
        }
    }
}

public class SlimeBiomeRuntimeSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<SlimeBiomeRuntimeSetup>() != null)
        {
            return;
        }

        new GameObject("SlimeBiomeRuntimeSetup").AddComponent<SlimeBiomeRuntimeSetup>();
    }

    private void Start()
    {
        if (FindObjectsByType<SlimeBiomeZone>(FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        SlimeBiomeZone.Crear("Zona fertil", SlimeBiomeType.Fertil, new Vector2(-4f, -2f), new Vector2(13f, 8f));
        SlimeBiomeZone.Crear("Zona humeda", SlimeBiomeType.Humedo, new Vector2(5f, -2f), new Vector2(12f, 8f));
        SlimeBiomeZone.Crear("Zona toxica", SlimeBiomeType.Toxico, new Vector2(14f, -2f), new Vector2(12f, 8f));
        SlimeBiomeZone.Crear("Zona volcanica", SlimeBiomeType.Volcanico, new Vector2(-13f, 1f), new Vector2(12f, 8f));
        SlimeBiomeZone.Crear("Zona fria cristalina", SlimeBiomeType.Frio, new Vector2(-10f, -23f), new Vector2(15f, 12f));
        SlimeBiomeZone.Crear("Ruinas", SlimeBiomeType.Ruinas, new Vector2(4f, -23f), new Vector2(16f, 12f));
    }
}

public class EcosystemToast : MonoBehaviour
{
    private static EcosystemToast instancia;
    private TMP_Text texto;
    private float visibleHasta;

    public static void Mostrar(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return;
        }

        if (instancia == null)
        {
            CrearInstancia();
        }

        instancia.texto.text = mensaje;
        instancia.visibleHasta = Time.time + 3.2f;
        instancia.texto.gameObject.SetActive(true);
    }

    private static void CrearInstancia()
    {
        Canvas canvas = BuscarOCrearCanvas();
        GameObject go = new GameObject("Toast_Ecosistema", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow), typeof(EcosystemToast));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -116f);
        rect.sizeDelta = new Vector2(640f, 42f);

        instancia = go.GetComponent<EcosystemToast>();
        instancia.texto = go.GetComponent<TMP_Text>();
        instancia.texto.text = string.Empty;
        instancia.texto.fontSize = 20f;
        instancia.texto.enableAutoSizing = true;
        instancia.texto.fontSizeMin = 12f;
        instancia.texto.fontStyle = FontStyles.Bold;
        instancia.texto.alignment = TextAlignmentOptions.Center;
        instancia.texto.color = new Color(1f, 0.94f, 0.62f, 1f);
        instancia.texto.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
        go.SetActive(true);
        instancia.texto.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (texto != null && texto.gameObject.activeSelf && Time.time > visibleHasta)
        {
            texto.gameObject.SetActive(false);
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
