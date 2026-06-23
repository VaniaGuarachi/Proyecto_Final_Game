using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHambreSistema : MonoBehaviour
{
    [SerializeField] private float comidaMaxima = 100f;
    [SerializeField] private float perdidaPorSegundo = 0.16f;
    [SerializeField] private float comidaRestauradaPorManzana = 34f;
    [SerializeField] private float umbralDano = 50f;
    [SerializeField] private int danoHambre = 5;
    [SerializeField] private float intervaloDano = 2.4f;

    private static PlayerHambreSistema instancia;

    private CriadorSlimesController jugador;
    private float comidaActual;
    private float tiempoDano;
    private float tiempoReconectarBoton;
    private RectTransform panel;
    private Image rellenoComida;
    private TMP_Text textoComida;
    private TMP_Text textoAviso;
    private bool avisoHambreCriticaMostrado;

    public static bool ComidaAgotada => instancia != null && instancia.comidaActual <= 0.01f;
    public static float ComidaNormalizada => instancia == null ? 1f : Mathf.Clamp01(instancia.comidaActual / Mathf.Max(0.01f, instancia.comidaMaxima));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<PlayerHambreSistema>() != null)
        {
            return;
        }

        new GameObject("PlayerHambreSistema").AddComponent<PlayerHambreSistema>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        comidaActual = comidaMaxima;
    }

    private void Update()
    {
        BuscarJugadorSiHaceFalta();
        CrearUIComidaSiHaceFalta();
        ConectarBotonManzanaSiHaceFalta();

        comidaActual = Mathf.Max(0f, comidaActual - perdidaPorSegundo * Time.deltaTime);
        AplicarLentitudPorHambre();

        if (comidaActual <= umbralDano)
        {
            if (!avisoHambreCriticaMostrado)
            {
                avisoHambreCriticaMostrado = true;
                ProfessionalFeedbackSystem.Toast("Hambre critica: come una manzana", new Color(1f, 0.62f, 0.18f, 1f));
            }

            tiempoDano -= Time.deltaTime;
            if (tiempoDano <= 0f)
            {
                tiempoDano = intervaloDano;
                ZonaFriaBufandaSistema.AplicarDanoGlobal(danoHambre, "TIENES HAMBRE, COME UNA MANZANA");
            }
        }
        else
        {
            avisoHambreCriticaMostrado = false;
            tiempoDano = intervaloDano;
        }

        ActualizarUIComida();
    }

    public static bool ComerManzana()
    {
        if (instancia == null)
        {
            return false;
        }

        return instancia.IntentarComerManzana();
    }

    private bool IntentarComerManzana()
    {
        if (InventarioRecursosSmiles.instancia == null || !InventarioRecursosSmiles.instancia.ConsumirManzana(1))
        {
            ZonaFriaBufandaSistema.MostrarMensajeGlobal("NO TIENES MANZANAS PARA COMER");
            return false;
        }

        comidaActual = Mathf.Min(comidaMaxima, comidaActual + comidaRestauradaPorManzana);
        ZonaFriaBufandaSistema.MostrarMensajeGlobal("COMISTE UNA MANZANA");
        ProfessionalFeedbackSystem.Toast("Comida restaurada +" + Mathf.RoundToInt(comidaRestauradaPorManzana), new Color(1f, 0.28f, 0.34f, 1f));
        ProfessionalFeedbackSystem.Pulse(panel, 1.08f);
        ActualizarUIComida();
        return true;
    }

    private void BuscarJugadorSiHaceFalta()
    {
        if (jugador != null)
        {
            return;
        }

        jugador = FindFirstObjectByType<CriadorSlimesController>();
    }

    private void AplicarLentitudPorHambre()
    {
        if (jugador == null)
        {
            return;
        }

        float porcentaje = Mathf.Clamp01(comidaActual / comidaMaxima);
        float multiplicador = Mathf.Lerp(0.55f, 1f, porcentaje);
        jugador.SetMultiplicadorHambre(multiplicador);
    }

    private void ConectarBotonManzanaSiHaceFalta()
    {
        tiempoReconectarBoton -= Time.deltaTime;
        if (tiempoReconectarBoton > 0f)
        {
            return;
        }

        tiempoReconectarBoton = 1f;
        GameObject slot = GameObject.Find("Slot_RECURSOS_Manzana");
        if (slot == null)
        {
            return;
        }

        Button boton = slot.GetComponent<Button>();
        if (boton == null)
        {
            boton = slot.AddComponent<Button>();
            boton.onClick.AddListener(() => ComerManzana());
        }

        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            boton.targetGraphic = image;
        }
    }

    private void CrearUIComidaSiHaceFalta()
    {
        if (panel != null)
        {
            return;
        }

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject panelGO = new GameObject("Panel_Comida_Player", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panelGO.transform.SetParent(canvas.transform, false);
        panelGO.layer = canvas.gameObject.layer;
        panel = panelGO.GetComponent<RectTransform>();
        ConfigurarRect(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -82f), new Vector2(300f, 48f));

        Image fondo = panelGO.GetComponent<Image>();
        fondo.color = new Color(0.045f, 0.035f, 0.055f, 0.90f);
        fondo.raycastTarget = false;

        Shadow sombra = panelGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(3f, -3f);

        TMP_Text icono = CrearTexto(panel, "Icono_Comida", "FOOD", 16f, TextAlignmentOptions.Center);
        icono.color = new Color(1f, 0.28f, 0.34f, 1f);
        ConfigurarRect(icono.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(58f, 34f));

        GameObject fondoBarra = new GameObject("Barra_Comida_Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fondoBarra.transform.SetParent(panel, false);
        Image fondoBarraImage = fondoBarra.GetComponent<Image>();
        fondoBarraImage.color = new Color(0.93f, 0.96f, 0.94f, 0.96f);
        fondoBarraImage.raycastTarget = false;
        ConfigurarRect(fondoBarra.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(-92f, 18f));

        GameObject relleno = new GameObject("Barra_Comida_Relleno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        relleno.transform.SetParent(fondoBarra.transform, false);
        rellenoComida = relleno.GetComponent<Image>();
        rellenoComida.type = Image.Type.Filled;
        rellenoComida.fillMethod = Image.FillMethod.Horizontal;
        rellenoComida.fillOrigin = (int)Image.OriginHorizontal.Left;
        rellenoComida.fillAmount = 1f;
        rellenoComida.raycastTarget = false;
        ConfigurarRect(relleno.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        textoComida = CrearTexto(panel, "Texto_Comida", "100", 14f, TextAlignmentOptions.Center);
        ConfigurarRect(textoComida.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(54f, 26f));

        textoAviso = CrearTexto(panel, "Texto_Aviso_Comida", "", 11f, TextAlignmentOptions.Center);
        ConfigurarRect(textoAviso.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(-20f, 20f));
    }

    private void ActualizarUIComida()
    {
        if (rellenoComida == null)
        {
            return;
        }

        float porcentaje = Mathf.Clamp01(comidaActual / comidaMaxima);
        rellenoComida.fillAmount = porcentaje;
        rellenoComida.color = porcentaje > 0.5f
            ? new Color(1f, 0.18f, 0.25f, 1f)
            : porcentaje > 0.25f ? new Color(1f, 0.62f, 0.12f, 1f) : new Color(0.9f, 0.05f, 0.05f, 1f);

        float pulso = comidaActual <= umbralDano ? 0.75f + Mathf.Sin(Time.time * 8f) * 0.25f : 1f;
        rellenoComida.color = new Color(rellenoComida.color.r, rellenoComida.color.g, rellenoComida.color.b, pulso);

        if (textoComida != null)
        {
            textoComida.text = Mathf.CeilToInt(comidaActual).ToString();
        }

        if (textoAviso != null)
        {
            textoAviso.text = comidaActual <= umbralDano ? "Come manzana" : "";
        }

        if (panel != null && comidaActual <= umbralDano)
        {
            float escala = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.018f;
            panel.localScale = new Vector3(escala, escala, 1f);
        }
        else if (panel != null)
        {
            panel.localScale = Vector3.one;
        }
    }

    private static Canvas BuscarOCrearCanvasUI()
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

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, TextAlignmentOptions alineacion)
    {
        GameObject textoGO = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        textoGO.transform.SetParent(parent, false);
        TMP_Text label = textoGO.GetComponent<TMP_Text>();
        label.text = texto;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = tamano;
        label.alignment = alineacion;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        Shadow sombra = textoGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.72f);
        sombra.effectDistance = new Vector2(1f, -1f);
        return label;
    }

    private static void ConfigurarRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }
}
