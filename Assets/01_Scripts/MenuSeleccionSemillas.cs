using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuSeleccionSemillas : MonoBehaviour
{
    public static MenuSeleccionSemillas instancia;

    [Header("Panel")]
    public GameObject panelSeleccion;

    [Header("Textos de botones")]
    public TMP_Text textoBotonVerde;
    public TMP_Text textoBotonAzul;
    public TMP_Text textoBotonAmarilla;
    public TMP_Text textoBotonMorada;

    private MacetaCultivo macetaActual;

    private void Awake()
    {
        instancia = this;
        AsegurarReferencias();
    }

    private void Start()
    {
        AsegurarReferencias();
        ConectarBotones();
        CerrarMenu();
    }

    public static MenuSeleccionSemillas ObtenerOCrear()
    {
        if (instancia != null)
        {
            instancia.AsegurarReferencias();
            return instancia;
        }

        instancia = FindFirstObjectByType<MenuSeleccionSemillas>(FindObjectsInactive.Include);
        if (instancia != null)
        {
            instancia.AsegurarReferencias();
            return instancia;
        }

        GameObject manager = new GameObject("Menu_Semillas_Manager_Runtime");
        instancia = manager.AddComponent<MenuSeleccionSemillas>();
        instancia.AsegurarReferencias();
        return instancia;
    }

    public void AbrirMenu(MacetaCultivo maceta)
    {
        AsegurarReferencias();
        macetaActual = maceta;

        if (panelSeleccion != null)
        {
            panelSeleccion.SetActive(true);
        }

        ConectarBotones();
        ActualizarTextos();
    }

    public void CerrarMenu()
    {
        if (panelSeleccion != null)
        {
            panelSeleccion.SetActive(false);
        }

        macetaActual = null;
    }

    private void ActualizarTextos()
    {
        AsegurarReferencias();

        if (InventarioSemillas.instancia == null)
        {
            return;
        }

        if (textoBotonVerde != null)
        {
            textoBotonVerde.text = "Plantar Verde x" + InventarioSemillas.instancia.semillasVerdes;
        }

        if (textoBotonAzul != null)
        {
            textoBotonAzul.text = "Plantar Azul x" + InventarioSemillas.instancia.semillasAzules;
        }

        if (textoBotonAmarilla != null)
        {
            textoBotonAmarilla.text = "Plantar Amarilla x" + InventarioSemillas.instancia.semillasAmarillas;
        }

        if (textoBotonMorada != null)
        {
            textoBotonMorada.text = "Plantar Morada x" + InventarioSemillas.instancia.semillasMoradas;
        }

        ActualizarDisponibilidadBotones();
    }

    private void ConectarBotones()
    {
        AsegurarReferencias();
        ConectarBoton(textoBotonVerde, ElegirSemillaVerde);
        ConectarBoton(textoBotonAzul, ElegirSemillaAzul);
        ConectarBoton(textoBotonAmarilla, ElegirSemillaAmarilla);
        ConectarBoton(textoBotonMorada, ElegirSemillaMorada);
    }

    private void ConectarBoton(TMP_Text textoBoton, UnityAction accion)
    {
        if (textoBoton == null)
        {
            return;
        }

        Button boton = textoBoton.GetComponentInParent<Button>(true);

        if (boton == null)
        {
            return;
        }

        boton.onClick.RemoveListener(accion);
        boton.onClick.AddListener(accion);
    }

    public void ElegirSemillaVerde()
    {
        ElegirSemilla(TipoSemilla.Verde);
    }

    public void ElegirSemillaAzul()
    {
        ElegirSemilla(TipoSemilla.Azul);
    }

    public void ElegirSemillaAmarilla()
    {
        ElegirSemilla(TipoSemilla.Amarilla);
    }

    public void ElegirSemillaMorada()
    {
        ElegirSemilla(TipoSemilla.Morada);
    }

    public void ElegirSemilla(TipoSemilla tipo)
    {
        if (macetaActual != null)
        {
            bool semillaPlantada = macetaActual.IntentarPlantarDesdeMenu(tipo);

            if (semillaPlantada)
            {
                Debug.Log("Semilla " + tipo + " enviada a la maceta");
                CerrarMenu();
            }
            else
            {
                ActualizarTextos();
            }
        }
        else
        {
            Debug.LogWarning("No hay maceta seleccionada.");
        }
    }

    private void ActualizarDisponibilidadBotones()
    {
        ActualizarDisponibilidadBoton(textoBotonVerde, TipoSemilla.Verde);
        ActualizarDisponibilidadBoton(textoBotonAzul, TipoSemilla.Azul);
        ActualizarDisponibilidadBoton(textoBotonAmarilla, TipoSemilla.Amarilla);
        ActualizarDisponibilidadBoton(textoBotonMorada, TipoSemilla.Morada);
    }

    private void ActualizarDisponibilidadBoton(TMP_Text textoBoton, TipoSemilla tipo)
    {
        if (textoBoton == null)
        {
            return;
        }

        Button boton = textoBoton.GetComponentInParent<Button>(true);
        bool permitido = macetaActual == null || macetaActual.EsSemillaPermitida(tipo);

        if (boton != null)
        {
            boton.gameObject.SetActive(permitido);
            boton.interactable = permitido;
        }
        else
        {
            textoBoton.gameObject.SetActive(permitido);
        }
    }

    private void AsegurarReferencias()
    {
        panelSeleccion ??= BuscarObjetoEscena("Panel_SeleccionSemillas");
        if (panelSeleccion == null)
        {
            CrearPanelSeleccionRuntime();
        }

        textoBotonVerde ??= BuscarTexto("Texto_BotonSemillaVerde", "Boton_SemillaVerde", "Verde");
        textoBotonAzul ??= BuscarTexto("Texto_BotonSemillaAzul", "Boton_SemillaAzul", "Azul");
        textoBotonAmarilla ??= BuscarTexto("Texto_BotonSemillaAmarilla", "Boton_SemillaAmarrilla", "Amarilla");
        textoBotonMorada ??= BuscarTexto("Texto_BotonSemillaMorada", "Boton_SemillaMorada", "Morada");

        textoBotonVerde ??= CrearBotonSemilla("Boton_SemillaVerde", "Texto_BotonSemillaVerde", "Plantar Verde");
        textoBotonAzul ??= CrearBotonSemilla("Boton_SemillaAzul", "Texto_BotonSemillaAzul", "Plantar Azul");
        textoBotonAmarilla ??= CrearBotonSemilla("Boton_SemillaAmarrilla", "Texto_BotonSemillaAmarilla", "Plantar Amarilla");
        textoBotonMorada ??= CrearBotonSemilla("Boton_SemillaMorada", "Texto_BotonSemillaMorada", "Plantar Morada");
    }

    private void CrearPanelSeleccionRuntime()
    {
        Canvas canvas = BuscarOCrearCanvasUI();
        AsegurarEventSystem();

        panelSeleccion = new GameObject("Panel_SeleccionSemillas", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelSeleccion.transform.SetParent(canvas.transform, false);

        RectTransform rect = panelSeleccion.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image overlay = panelSeleccion.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.45f);
        overlay.raycastTarget = true;

        GameObject dialog = new GameObject("UI_Dialogo_SeleccionSemillas", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        dialog.transform.SetParent(panelSeleccion.transform, false);
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(390f, 330f);

        Image dialogImage = dialog.GetComponent<Image>();
        dialogImage.color = new Color(0.055f, 0.075f, 0.105f, 0.95f);

        VerticalLayoutGroup layout = dialog.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(45, 45, 62, 34);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CrearTexto(dialog.transform, "UI_Titulo_SeleccionSemillas", "PLANTAR SEMILLA", 18f, true);
        CrearBotonSemilla("Boton_SemillaVerde", "Texto_BotonSemillaVerde", "Plantar Verde");
        CrearBotonSemilla("Boton_SemillaAzul", "Texto_BotonSemillaAzul", "Plantar Azul");
        CrearBotonSemilla("Boton_SemillaAmarrilla", "Texto_BotonSemillaAmarilla", "Plantar Amarilla");
        CrearBotonSemilla("Boton_SemillaMorada", "Texto_BotonSemillaMorada", "Plantar Morada");

        Button cerrar = CrearBoton(dialog.transform, "Boton_Cerrar", "X");
        cerrar.onClick.AddListener(CerrarMenu);
        panelSeleccion.SetActive(false);
    }

    private TMP_Text CrearBotonSemilla(string nombreBoton, string nombreTexto, string texto)
    {
        if (panelSeleccion == null)
        {
            return null;
        }

        Transform parent = panelSeleccion.transform.Find("UI_Dialogo_SeleccionSemillas") ?? panelSeleccion.transform;
        Transform existente = panelSeleccion.transform.Find(nombreBoton);
        Button boton = existente != null ? existente.GetComponent<Button>() : null;

        if (boton == null)
        {
            boton = CrearBoton(parent, nombreBoton, texto);
        }

        TMP_Text label = boton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.name = nombreTexto;
            label.text = texto;
        }

        return label;
    }

    private static Button CrearBoton(Transform parent, string nombre, string texto)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        objeto.transform.SetParent(parent, false);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 48f);

        Image image = objeto.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.18f, 0.95f);

        Button boton = objeto.GetComponent<Button>();
        boton.targetGraphic = image;

        LayoutElement layout = objeto.GetComponent<LayoutElement>();
        layout.minHeight = 48f;
        layout.preferredHeight = 48f;
        layout.preferredWidth = 300f;

        CrearTexto(objeto.transform, "Text (TMP)", texto, 18f, false);
        return boton;
    }

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, bool titulo)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        objeto.transform.SetParent(parent, false);

        TMP_Text label = objeto.GetComponent<TMP_Text>();
        label.text = texto;
        label.color = Color.white;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = titulo ? 12f : 10f;
        label.fontSizeMax = tamano;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = titulo ? FontStyles.Bold : FontStyles.Normal;
        label.raycastTarget = false;

        LayoutElement layout = objeto.GetComponent<LayoutElement>();
        layout.minHeight = titulo ? 34f : 48f;
        layout.preferredHeight = titulo ? 34f : 48f;
        return label;
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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private static GameObject BuscarObjetoEscena(string nombre)
    {
        GameObject[] objetos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject objeto in objetos)
        {
            if (objeto != null && objeto.name == nombre && objeto.scene.IsValid())
            {
                return objeto;
            }
        }

        return null;
    }

    private static TMP_Text BuscarTexto(string nombreTexto, string nombreBoton, string contiene)
    {
        TMP_Text[] textos = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text texto in textos)
        {
            if (texto != null && texto.name == nombreTexto && texto.gameObject.scene.IsValid())
            {
                return texto;
            }
        }

        foreach (TMP_Text texto in textos)
        {
            if (texto == null || !texto.gameObject.scene.IsValid())
            {
                continue;
            }

            Button boton = texto.GetComponentInParent<Button>(true);
            if (boton != null && boton.name == nombreBoton)
            {
                return texto;
            }
        }

        foreach (TMP_Text texto in textos)
        {
            if (texto != null && texto.gameObject.scene.IsValid() && texto.text.Contains(contiene))
            {
                return texto;
            }
        }

        return null;
    }
}
