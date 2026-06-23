using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AlmacenAlimentosColocado : MonoBehaviour
{
    private const string KeyManzanas = "AlmacenAlimentos_Manzanas";
    private const string KeyAgua = "AlmacenAlimentos_Agua";

    public int indiceGuardado;
    public int nivel = 1;

    private static int manzanasGuardadas;
    private static int aguaGuardada;
    private static bool cargado;
    private static PanelAlmacenAlimentos panel;

    private void Awake()
    {
        Cargar();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        ObtenerPanel().Mostrar(this, manzanasGuardadas, aguaGuardada);
    }

    public static float CalcularEscala(int nivel)
    {
        return 1f + (Mathf.Clamp(nivel, 1, AlmacenSistema.NivelMaximoAlmacenAlimentos) - 1) * 0.25f;
    }

    public void AplicarNivel(int nuevoNivel, Vector2 tamanoCollider)
    {
        nivel = Mathf.Clamp(nuevoNivel, 1, AlmacenSistema.NivelMaximoAlmacenAlimentos);
        transform.localScale = Vector3.one * CalcularEscala(nivel);

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.size = tamanoCollider;
        collider.isTrigger = false;
    }

    public void GuardarComida()
    {
        if (InventarioRecursosSmiles.instancia == null)
        {
            return;
        }

        int manzanas = InventarioRecursosSmiles.instancia.manzanas;
        int agua = InventarioRecursosSmiles.instancia.vasosAgua;

        if (manzanas > 0 && InventarioRecursosSmiles.instancia.ConsumirManzana(manzanas))
        {
            manzanasGuardadas += manzanas;
        }

        if (agua > 0 && InventarioRecursosSmiles.instancia.ConsumirVasosAgua(agua))
        {
            aguaGuardada += agua;
        }

        Guardar();
        ObtenerPanel().Mostrar(this, manzanasGuardadas, aguaGuardada);
    }

    public void RetirarComida()
    {
        if (InventarioRecursosSmiles.instancia == null)
        {
            return;
        }

        if (manzanasGuardadas > 0)
        {
            InventarioRecursosSmiles.instancia.AgregarManzanas(manzanasGuardadas);
            manzanasGuardadas = 0;
        }

        if (aguaGuardada > 0)
        {
            InventarioRecursosSmiles.instancia.AgregarVasosAgua(aguaGuardada);
            aguaGuardada = 0;
        }

        Guardar();
        ObtenerPanel().Mostrar(this, manzanasGuardadas, aguaGuardada);
    }

    private static void Cargar()
    {
        if (cargado)
        {
            return;
        }

        manzanasGuardadas = Mathf.Max(0, PlayerPrefs.GetInt(KeyManzanas, 0));
        aguaGuardada = Mathf.Max(0, PlayerPrefs.GetInt(KeyAgua, 0));
        cargado = true;
    }

    private static void Guardar()
    {
        PlayerPrefs.SetInt(KeyManzanas, manzanasGuardadas);
        PlayerPrefs.SetInt(KeyAgua, aguaGuardada);
        PlayerPrefs.Save();
    }

    private static PanelAlmacenAlimentos ObtenerPanel()
    {
        if (panel == null)
        {
            GameObject go = new GameObject("Panel_Almacen_Alimentos_Runtime");
            panel = go.AddComponent<PanelAlmacenAlimentos>();
        }

        return panel;
    }
}

public class PanelAlmacenAlimentos : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
    private TMP_Text textoContenido;
    private AlmacenAlimentosColocado objetivo;

    private void Awake()
    {
        ConstruirUI();
    }

    public void Mostrar(AlmacenAlimentosColocado nuevoObjetivo, int manzanas, int agua)
    {
        objetivo = nuevoObjetivo;
        textoContenido.text = "Manzanas guardadas: " + manzanas + "\nAgua guardada: " + agua;
        panel.gameObject.SetActive(true);
    }

    private void ConstruirUI()
    {
        canvas = BuscarOCrearCanvasUI();
        panel = new GameObject("Panel_Almacen_Alimentos", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow)).GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        ConfigurarRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 230f));

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.055f, 0.075f, 0.075f, 0.94f);

        Shadow sombra = panel.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(4f, -4f);

        TMP_Text titulo = CrearTexto(panel, "Titulo_Almacen_Alimentos", "ALMACEN DE ALIMENTOS", 18f, TextAlignmentOptions.Center, true);
        ConfigurarRect(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-46f, 30f));

        Button cerrar = CrearBoton(panel, "Btn_Cerrar_Alimentos", "X");
        ConfigurarRect(cerrar.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(32f, 32f));
        cerrar.onClick.AddListener(() => panel.gameObject.SetActive(false));

        textoContenido = CrearTexto(panel, "Txt_Contenido_Almacen_Alimentos", "Manzanas guardadas: 0\nAgua guardada: 0", 16f, TextAlignmentOptions.Center, false);
        ConfigurarRect(textoContenido.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(-36f, 58f));

        Button guardar = CrearBoton(panel, "Btn_Guardar_Alimentos", "Guardar manzanas y agua");
        ConfigurarRect(guardar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(-46f, 42f));
        guardar.onClick.AddListener(() => objetivo?.GuardarComida());

        Button retirar = CrearBoton(panel, "Btn_Retirar_Alimentos", "Retirar comida");
        ConfigurarRect(retirar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-46f, 42f));
        retirar.onClick.AddListener(() => objetivo?.RetirarComida());

        panel.gameObject.SetActive(false);
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

    private static Button CrearBoton(Transform parent, string nombre, string texto)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.20f, 0.17f, 0.96f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CrearTexto(go.transform, "Txt_" + nombre, texto, 15f, TextAlignmentOptions.Center, true);
        ConfigurarRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -8f));
        return button;
    }

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, TextAlignmentOptions alineacion, bool destacado)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        go.transform.SetParent(parent, false);
        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = texto;
        label.color = destacado ? new Color(0.98f, 0.96f, 0.86f, 1f) : new Color(0.80f, 0.88f, 0.84f, 1f);
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
        label.fontSizeMax = tamano;
        label.alignment = alineacion;
        label.fontStyle = destacado ? FontStyles.Bold : FontStyles.Normal;
        label.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.4f, -1.4f);
        return label;
    }

    private static void ConfigurarRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;
    }
}
