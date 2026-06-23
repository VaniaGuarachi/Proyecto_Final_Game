using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZonaFriaBufandaSistema : MonoBehaviour
{
    [SerializeField] private Vector2 zonaFriaMin = new Vector2(-17.5f, -30.5f);
    [SerializeField] private Vector2 zonaFriaMax = new Vector2(-3.5f, -17.5f);
    [SerializeField] private float duracionBufanda = 4f;
    [SerializeField] private int vidaMaxima = 100;
    [SerializeField] private int danoFrioPorTick = 8;
    [SerializeField] private float intervaloDano = 1f;

    private static ZonaFriaBufandaSistema instancia;

    private Transform jugador;
    private CriadorSlimesController controladorJugador;
    private int vidaActual;
    private float tiempoBufanda;
    private float tiempoDano;
    private float tiempoReconectarBoton;
    private bool estabaEnZonaFria;
    private bool mensajeSinBufandaMostrado;

    private RectTransform barraVida;
    private Image rellenoVida;
    private TMP_Text textoVida;
    private TMP_Text textoBufanda;
    private bool avisoVidaCriticaMostrado;

    public static bool VidaAgotada => instancia != null && instancia.vidaActual <= 0;
    public static float VidaNormalizada => instancia == null ? 1f : Mathf.Clamp01(instancia.vidaActual / (float)Mathf.Max(1, instancia.vidaMaxima));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<ZonaFriaBufandaSistema>() != null)
        {
            return;
        }

        new GameObject("ZonaFriaBufandaSistema").AddComponent<ZonaFriaBufandaSistema>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        vidaActual = vidaMaxima;
    }

    private void Update()
    {
        BuscarJugadorSiHaceFalta();
        CrearUIVidaSiHaceFalta();
        ConectarBotonBufandaSiHaceFalta();

        if (jugador == null)
        {
            return;
        }

        bool enZonaFria = EstaEnZonaFria(jugador.position);
        if (enZonaFria && !estabaEnZonaFria)
        {
            MostrarMensajePantalla("ENTRASTE A UNA ZONA FRIA, USA TU BUFANDA");
            mensajeSinBufandaMostrado = false;
        }

        estabaEnZonaFria = enZonaFria;

        if (tiempoBufanda > 0f)
        {
            tiempoBufanda = Mathf.Max(0f, tiempoBufanda - Time.deltaTime);
        }

        if (enZonaFria && tiempoBufanda <= 0f)
        {
            if (!mensajeSinBufandaMostrado)
            {
                MostrarMensajePantalla("HAZ CLICK EN LA BUFANDA PARA USARLA");
                mensajeSinBufandaMostrado = true;
            }

            tiempoDano -= Time.deltaTime;
            if (tiempoDano <= 0f)
            {
                tiempoDano = intervaloDano;
                AplicarDanoFrio();
            }
        }
        else
        {
            tiempoDano = 0f;
        }

        ActualizarUIVida(enZonaFria);
    }

    public static bool UsarBufanda()
    {
        if (instancia == null)
        {
            return false;
        }

        return instancia.IntentarUsarBufanda();
    }

    public static void AplicarDanoGlobal(int cantidad, string mensaje)
    {
        if (instancia == null)
        {
            GameObject sistema = new GameObject("ZonaFriaBufandaSistema");
            instancia = sistema.AddComponent<ZonaFriaBufandaSistema>();
        }

        instancia.AplicarDano(cantidad, mensaje);
    }

    public static void MostrarMensajeGlobal(string mensaje)
    {
        if (instancia == null)
        {
            GameObject sistema = new GameObject("ZonaFriaBufandaSistema");
            instancia = sistema.AddComponent<ZonaFriaBufandaSistema>();
        }

        instancia.MostrarMensajePantalla(mensaje);
    }

    private bool IntentarUsarBufanda()
    {
        if (!estabaEnZonaFria)
        {
            MostrarMensajePantalla("SOLO NECESITAS LA BUFANDA EN LA ZONA FRIA");
            return false;
        }

        if (InventarioRecursosSmiles.instancia == null || !InventarioRecursosSmiles.instancia.ConsumirBufanda(1))
        {
            MostrarMensajePantalla("NO TIENES BUFANDA ANTIFRIO");
            return false;
        }

        tiempoBufanda = duracionBufanda;
        tiempoDano = intervaloDano;
        mensajeSinBufandaMostrado = false;
        MostrarMensajePantalla("BUFANDA ACTIVADA: 4 SEGUNDOS DE CALOR");
        return true;
    }

    private void AplicarDanoFrio()
    {
        AplicarDano(danoFrioPorTick, vidaActual - danoFrioPorTick <= 0 ? "EL FRIO TE DEJO SIN VIDA" : string.Empty);
    }

    private void AplicarDano(int cantidad, string mensaje)
    {
        vidaActual = Mathf.Max(0, vidaActual - Mathf.Max(0, cantidad));
        controladorJugador?.PlayHurtAnimation();
        ActualizarUIVida(estabaEnZonaFria);
        ProfessionalFeedbackSystem.Toast("Vida -" + Mathf.Max(0, cantidad), new Color(1f, 0.18f, 0.16f, 1f));
        if (jugador != null)
        {
            ProfessionalFeedbackSystem.WorldText(jugador.position + Vector3.up * 1.1f, "-" + Mathf.Max(0, cantidad), new Color(1f, 0.18f, 0.16f, 1f));
            ProfessionalFeedbackSystem.Burst(jugador.position + Vector3.up * 0.9f, new Color(1f, 0.18f, 0.16f, 1f), 8);
        }

        if (!string.IsNullOrEmpty(mensaje))
        {
            MostrarMensajePantalla(mensaje);
        }
    }

    private bool EstaEnZonaFria(Vector3 posicion)
    {
        return posicion.x >= zonaFriaMin.x && posicion.x <= zonaFriaMax.x
            && posicion.y >= zonaFriaMin.y && posicion.y <= zonaFriaMax.y;
    }

    private void BuscarJugadorSiHaceFalta()
    {
        if (jugador != null)
        {
            return;
        }

        GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObjeto == null)
        {
            Player1 player = FindFirstObjectByType<Player1>();
            jugadorObjeto = player != null ? player.gameObject : null;
        }

        if (jugadorObjeto == null)
        {
            return;
        }

        jugador = jugadorObjeto.transform;
        controladorJugador = jugadorObjeto.GetComponent<CriadorSlimesController>();
    }

    private void ConectarBotonBufandaSiHaceFalta()
    {
        tiempoReconectarBoton -= Time.deltaTime;
        if (tiempoReconectarBoton > 0f)
        {
            return;
        }

        tiempoReconectarBoton = 1f;
        GameObject slot = GameObject.Find("Slot_RECURSOS_Bufanda");
        if (slot == null)
        {
            return;
        }

        Button boton = slot.GetComponent<Button>();
        if (boton == null)
        {
            boton = slot.AddComponent<Button>();
            boton.onClick.AddListener(() => UsarBufanda());
        }

        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            boton.targetGraphic = image;
        }
    }

    private void CrearUIVidaSiHaceFalta()
    {
        if (barraVida != null)
        {
            return;
        }

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject panel = new GameObject("Panel_Vida_Frio", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panel.transform.SetParent(canvas.transform, false);
        panel.layer = canvas.gameObject.layer;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -28f);
        rect.sizeDelta = new Vector2(300f, 48f);

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.035f, 0.045f, 0.055f, 0.9f);
        fondo.raycastTarget = false;

        Shadow sombra = panel.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(3f, -3f);

        TMP_Text corazon = CrearTexto(panel.transform, "Icono_Corazon_Vida", "HP", 22f, TextAlignmentOptions.Center);
        ConfigurarRect(corazon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(38f, 40f));
        corazon.color = new Color(1f, 0.05f, 0.08f, 1f);

        GameObject fondoBarra = new GameObject("Barra_Vida_Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fondoBarra.transform.SetParent(panel.transform, false);
        Image fondoBarraImage = fondoBarra.GetComponent<Image>();
        fondoBarraImage.color = new Color(0.95f, 0.95f, 0.9f, 0.96f);
        fondoBarraImage.raycastTarget = false;
        ConfigurarRect(fondoBarra.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(-82f, 18f));

        GameObject relleno = new GameObject("Barra_Vida_Relleno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        relleno.transform.SetParent(fondoBarra.transform, false);
        rellenoVida = relleno.GetComponent<Image>();
        rellenoVida.color = new Color(0.22f, 0.85f, 0.28f, 1f);
        rellenoVida.type = Image.Type.Filled;
        rellenoVida.fillMethod = Image.FillMethod.Horizontal;
        rellenoVida.fillOrigin = (int)Image.OriginHorizontal.Left;
        rellenoVida.fillAmount = 1f;
        rellenoVida.raycastTarget = false;
        ConfigurarRect(relleno.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        textoVida = CrearTexto(panel.transform, "Texto_Vida", "100", 14f, TextAlignmentOptions.Center);
        ConfigurarRect(textoVida.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(54f, 26f));

        textoBufanda = CrearTexto(panel.transform, "Texto_Tiempo_Bufanda", "", 11f, TextAlignmentOptions.Center);
        ConfigurarRect(textoBufanda.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(-20f, 20f));
        barraVida = rect;
    }

    private void ActualizarUIVida(bool enZonaFria)
    {
        if (rellenoVida == null)
        {
            return;
        }

        float t = Mathf.Clamp01(vidaActual / (float)Mathf.Max(1, vidaMaxima));
        rellenoVida.fillAmount = t;
        rellenoVida.color = t > 0.55f
            ? new Color(0.22f, 0.85f, 0.28f, 1f)
            : t > 0.25f ? new Color(1f, 0.72f, 0.12f, 1f) : new Color(0.92f, 0.08f, 0.08f, 1f);

        if (textoVida != null)
        {
            textoVida.text = vidaActual.ToString();
        }

        if (textoBufanda != null)
        {
            textoBufanda.text = enZonaFria && tiempoBufanda > 0f ? "Bufanda " + tiempoBufanda.ToString("0.0") + "s" : "";
        }

        if (barraVida != null && t <= 0.25f)
        {
            if (!avisoVidaCriticaMostrado)
            {
                avisoVidaCriticaMostrado = true;
                ProfessionalFeedbackSystem.Toast("Vida critica", new Color(1f, 0.2f, 0.16f, 1f));
            }

            float escala = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.018f;
            barraVida.localScale = new Vector3(escala, escala, 1f);
        }
        else if (barraVida != null)
        {
            avisoVidaCriticaMostrado = false;
            barraVida.localScale = Vector3.one;
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

    private void MostrarMensajePantalla(string mensaje)
    {
        ProfessionalFeedbackSystem.Toast(mensaje, new Color(0.82f, 0.94f, 1f, 1f));

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject mensajeGO = new GameObject("Mensaje_Zona_Fria", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        mensajeGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = mensajeGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -96f);
        rect.sizeDelta = new Vector2(560f, 58f);

        Image fondo = mensajeGO.GetComponent<Image>();
        fondo.color = new Color(0.03f, 0.06f, 0.09f, 0.92f);
        fondo.raycastTarget = false;

        Shadow sombra = mensajeGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.65f);
        sombra.effectDistance = new Vector2(3f, -3f);

        TMP_Text label = CrearTexto(mensajeGO.transform, "Texto_Mensaje_Zona_Fria", mensaje, 20f, TextAlignmentOptions.Center);
        ConfigurarRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-22f, -10f));
        Destroy(mensajeGO, 3f);
    }
}
