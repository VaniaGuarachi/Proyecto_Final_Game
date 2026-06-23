using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIAlmacen : MonoBehaviour
{
    private static Sprite panelSprite;
    private static Sprite botonSprite;
    private static Sprite barraSprite;
    private static Sprite iconoCuboSprite;

    private AlmacenSistema almacen;
    private Canvas canvas;
    private RectTransform panel;
    private Button botonAbrir;
    private Button botonCrearGranja;
    private Button botonAgrandarGranja;
    private Button botonCrearAlmacenAlimentos;
    private Button botonAgrandarAlmacenAlimentos;
    private TMP_Text textoCubos;
    private TMP_Text textoEstado;
    private TMP_Text textoEstadoAlimentos;
    private TMP_Text textoGranjas;
    private TMP_Text textoAlmacenesAlimentos;
    private Image rellenoProgreso;
    private Image rellenoProgresoAlimentos;
    private GameObject filaProgreso;
    private GameObject filaProgresoAlimentos;
    private GameObject itemGranja;
    private GameObject itemAlmacenAlimentos;

    private void Awake()
    {
        almacen = GetComponent<AlmacenSistema>();
        if (almacen == null)
        {
            almacen = AlmacenSistema.AsegurarInstancia();
        }
    }

    private void Start()
    {
        ConstruirUI();
        almacen.EstadoCambiado += ActualizarUI;
        ActualizarUI();
    }

    private void OnDestroy()
    {
        if (almacen != null)
        {
            almacen.EstadoCambiado -= ActualizarUI;
        }
    }

    private void ConstruirUI()
    {
        CrearSpritesSiFaltan();
        canvas = BuscarOCrearCanvas();
        AsegurarEventSystem();

        botonAbrir = CrearBoton(canvas.transform, "Btn_Almacen", "Almacén");
        RectTransform botonRect = botonAbrir.GetComponent<RectTransform>();
        ConfigurarRect(botonRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(150f, 44f));
        botonAbrir.onClick.RemoveListener(AlternarPanel);
        botonAbrir.onClick.AddListener(AlternarPanel);

        Image icono = CrearImagen(botonAbrir.transform, "Icono_Cubo", iconoCuboSprite);
        RectTransform iconoRect = icono.GetComponent<RectTransform>();
        ConfigurarRect(iconoRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(26f, 26f));

        TMP_Text textoBoton = botonAbrir.GetComponentInChildren<TMP_Text>();
        ConfigurarRect(textoBoton.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, 0f), new Vector2(-42f, 0f));

        panel = CrearPanel(canvas.transform, "Panel_Almacen");
        ConfigurarRect(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -280f), new Vector2(380f, 480f));

        TMP_Text titulo = CrearTexto(panel, "Titulo_Almacen", "ALMACÉN", 21f, TextAlignmentOptions.Center, true);
        ConfigurarRect(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-62f, 34f));

        Button cerrar = CrearBoton(panel, "Btn_Cerrar_Almacen", "X");
        ConfigurarRect(cerrar.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(34f, 34f));
        cerrar.onClick.RemoveAllListeners();
        cerrar.onClick.AddListener(() => panel.gameObject.SetActive(false));

        textoCubos = CrearTexto(panel, "Txt_Cubos_Almacen", "Cubos disponibles: 0/30", 18f, TextAlignmentOptions.Left, false);
        ConfigurarRect(textoCubos.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-52f, 34f));

        botonCrearGranja = CrearBoton(panel, "Btn_Crear_Granja", "Crear granja");
        ConfigurarRect(botonCrearGranja.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(-52f, 44f));
        botonCrearGranja.onClick.RemoveAllListeners();
        botonCrearGranja.onClick.AddListener(() => almacen.IntentarCrearGranja());

        botonAgrandarGranja = CrearBoton(panel, "Btn_Agrandar_Granja", "Agrandar granja");
        ConfigurarRect(botonAgrandarGranja.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(-52f, 44f));
        botonAgrandarGranja.onClick.RemoveAllListeners();
        botonAgrandarGranja.onClick.AddListener(() => almacen.IntentarAgrandarUltimaGranja());

        botonCrearAlmacenAlimentos = CrearBoton(panel, "Btn_Crear_Almacen_Alimentos", "Crear almacén de alimentos");
        ConfigurarRect(botonCrearAlmacenAlimentos.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -234f), new Vector2(-52f, 44f));
        botonCrearAlmacenAlimentos.onClick.RemoveAllListeners();
        botonCrearAlmacenAlimentos.onClick.AddListener(() => almacen.IntentarCrearAlmacenAlimentos());

        botonAgrandarAlmacenAlimentos = CrearBoton(panel, "Btn_Agrandar_Almacen_Alimentos", "Agrandar almacén alimentos");
        ConfigurarRect(botonAgrandarAlmacenAlimentos.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -288f), new Vector2(-52f, 44f));
        botonAgrandarAlmacenAlimentos.onClick.RemoveAllListeners();
        botonAgrandarAlmacenAlimentos.onClick.AddListener(() => almacen.IntentarAgrandarUltimoAlmacenAlimentos());

        filaProgreso = new GameObject("Fila_Progreso_Granja", typeof(RectTransform));
        filaProgreso.transform.SetParent(panel, false);
        RectTransform filaRect = filaProgreso.GetComponent<RectTransform>();
        ConfigurarRect(filaRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(-52f, 50f));

        textoEstado = CrearTexto(filaRect, "Txt_Estado_Granja", "Construyendo granja...", 15f, TextAlignmentOptions.Left, false);
        ConfigurarRect(textoEstado.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 20f));

        Image fondoProgreso = CrearImagen(filaRect, "Barra_Progreso_Fondo", barraSprite);
        fondoProgreso.type = Image.Type.Sliced;
        fondoProgreso.color = new Color(0.08f, 0.10f, 0.10f, 0.96f);
        ConfigurarRect(fondoProgreso.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 17f));

        rellenoProgreso = CrearImagen(fondoProgreso.transform, "Barra_Progreso_Relleno", barraSprite);
        rellenoProgreso.type = Image.Type.Filled;
        rellenoProgreso.fillMethod = Image.FillMethod.Horizontal;
        rellenoProgreso.color = new Color(0.41f, 0.83f, 0.42f, 1f);
        ConfigurarRect(rellenoProgreso.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        filaProgresoAlimentos = new GameObject("Fila_Progreso_Almacen_Alimentos", typeof(RectTransform));
        filaProgresoAlimentos.transform.SetParent(panel, false);
        RectTransform filaAlimentosRect = filaProgresoAlimentos.GetComponent<RectTransform>();
        ConfigurarRect(filaAlimentosRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -396f), new Vector2(-52f, 50f));

        textoEstadoAlimentos = CrearTexto(filaAlimentosRect, "Txt_Estado_Almacen_Alimentos", "Construyendo almacén...", 15f, TextAlignmentOptions.Left, false);
        ConfigurarRect(textoEstadoAlimentos.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 20f));

        Image fondoProgresoAlimentos = CrearImagen(filaAlimentosRect, "Barra_Progreso_Alimentos_Fondo", barraSprite);
        fondoProgresoAlimentos.type = Image.Type.Sliced;
        fondoProgresoAlimentos.color = new Color(0.08f, 0.10f, 0.10f, 0.96f);
        ConfigurarRect(fondoProgresoAlimentos.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 17f));

        rellenoProgresoAlimentos = CrearImagen(fondoProgresoAlimentos.transform, "Barra_Progreso_Alimentos_Relleno", barraSprite);
        rellenoProgresoAlimentos.type = Image.Type.Filled;
        rellenoProgresoAlimentos.fillMethod = Image.FillMethod.Horizontal;
        rellenoProgresoAlimentos.color = new Color(0.88f, 0.48f, 0.24f, 1f);
        ConfigurarRect(rellenoProgresoAlimentos.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        itemGranja = CrearItemGranja(panel);
        itemAlmacenAlimentos = CrearItemAlmacenAlimentos(panel);

        panel.gameObject.SetActive(false);
    }

    private GameObject CrearItemGranja(RectTransform parent)
    {
        GameObject item = new GameObject("Item_Granja_Lista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FarmDragItem));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        ConfigurarRect(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(-52f, 112f));

        Image fondo = item.GetComponent<Image>();
        fondo.sprite = botonSprite;
        fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.12f, 0.16f, 0.14f, 0.96f);

        Image icono = CrearImagen(item.transform, "Icono_Granja", GranjaVisualFactory.ObtenerSpriteGranja());
        icono.preserveAspect = true;
        ConfigurarRect(icono.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(126f, 84f));

        textoGranjas = CrearTexto(item.transform, "Txt_Granja_Lista", "Granja lista x0", 18f, TextAlignmentOptions.Left, true);
        ConfigurarRect(textoGranjas.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(86f, 0f), new Vector2(-186f, -22f));

        FarmDragItem drag = item.GetComponent<FarmDragItem>();
        drag.Configurar(almacen, canvas, icono.sprite);

        return item;
    }

    private GameObject CrearItemAlmacenAlimentos(RectTransform parent)
    {
        GameObject item = new GameObject("Item_Almacen_Alimentos_Lista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FoodStorageDragItem));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        ConfigurarRect(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(-52f, 92f));

        Image fondo = item.GetComponent<Image>();
        fondo.sprite = botonSprite;
        fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.16f, 0.12f, 0.10f, 0.96f);

        Image icono = CrearImagen(item.transform, "Icono_Almacen_Alimentos", AlmacenAlimentosVisualFactory.ObtenerSpriteAlmacenAlimentos());
        icono.preserveAspect = true;
        ConfigurarRect(icono.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(96f, 70f));

        textoAlmacenesAlimentos = CrearTexto(item.transform, "Txt_Almacen_Alimentos_Lista", "Almacén comida x0", 17f, TextAlignmentOptions.Left, true);
        ConfigurarRect(textoAlmacenesAlimentos.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(76f, 0f), new Vector2(-160f, -20f));

        FoodStorageDragItem drag = item.GetComponent<FoodStorageDragItem>();
        drag.Configurar(almacen, canvas, icono.sprite);
        return item;
    }

    private void AlternarPanel()
    {
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (almacen == null || textoCubos == null)
        {
            return;
        }

        textoCubos.text = "Cubos disponibles: " + almacen.CubosDisponibles + "/" + AlmacenSistema.CubosNecesariosGranja;

        bool puedeCrear = almacen.PuedeCrearGranja();
        botonCrearGranja.interactable = puedeCrear;
        Color colorBoton = puedeCrear ? new Color(0.22f, 0.46f, 0.20f, 0.98f) : new Color(0.18f, 0.19f, 0.18f, 0.82f);
        botonCrearGranja.GetComponent<Image>().color = colorBoton;

        bool puedeAgrandar = almacen.PuedeAgrandarUltimaGranja();
        botonAgrandarGranja.interactable = puedeAgrandar;
        botonAgrandarGranja.GetComponent<Image>().color = puedeAgrandar ? new Color(0.26f, 0.36f, 0.58f, 0.98f) : new Color(0.18f, 0.19f, 0.18f, 0.82f);

        bool puedeCrearAlimentos = almacen.PuedeCrearAlmacenAlimentos();
        botonCrearAlmacenAlimentos.interactable = puedeCrearAlimentos;
        botonCrearAlmacenAlimentos.GetComponent<Image>().color = puedeCrearAlimentos ? new Color(0.50f, 0.28f, 0.15f, 0.98f) : new Color(0.18f, 0.19f, 0.18f, 0.82f);

        bool puedeAgrandarAlimentos = almacen.PuedeAgrandarUltimoAlmacenAlimentos();
        botonAgrandarAlmacenAlimentos.interactable = puedeAgrandarAlimentos;
        botonAgrandarAlmacenAlimentos.GetComponent<Image>().color = puedeAgrandarAlimentos ? new Color(0.56f, 0.34f, 0.18f, 0.98f) : new Color(0.18f, 0.19f, 0.18f, 0.82f);

        TMP_Text textoAgrandar = botonAgrandarGranja.GetComponentInChildren<TMP_Text>();
        if (almacen.NivelUltimaGranja >= AlmacenSistema.NivelMaximoGranja)
        {
            textoAgrandar.text = "Granja al maximo";
        }
        else
        {
            textoAgrandar.text = "Agrandar granja (" + AlmacenSistema.CubosNecesariosAgrandarGranja + ")";
        }

        TMP_Text textoAgrandarAlimentos = botonAgrandarAlmacenAlimentos.GetComponentInChildren<TMP_Text>();
        if (almacen.NivelUltimoAlmacenAlimentos >= AlmacenSistema.NivelMaximoAlmacenAlimentos)
        {
            textoAgrandarAlimentos.text = "Almacén alimentos al maximo";
        }
        else
        {
            textoAgrandarAlimentos.text = "Agrandar almacén alimentos (" + AlmacenSistema.CubosNecesariosAgrandarAlmacenAlimentos + ")";
        }

        filaProgreso.SetActive(almacen.ConstruyendoGranja);
        rellenoProgreso.fillAmount = almacen.ProgresoNormalizado;
        textoEstado.text = "Construyendo con bloques: " + almacen.CubosUsadosConstruccion + "/" + AlmacenSistema.CubosNecesariosGranja;

        filaProgresoAlimentos.SetActive(almacen.ConstruyendoAlmacenAlimentos);
        rellenoProgresoAlimentos.fillAmount = almacen.ProgresoAlmacenAlimentosNormalizado;
        textoEstadoAlimentos.text = "Almacén alimentos: " + almacen.CubosUsadosAlmacenAlimentos + "/" + AlmacenSistema.CubosNecesariosAlmacenAlimentos;

        itemGranja.SetActive(almacen.GranjasDisponibles > 0);
        textoGranjas.text = "Granja lista x" + almacen.GranjasDisponibles + "  Nivel " + Mathf.Max(1, almacen.NivelUltimaGranja);

        FarmDragItem drag = itemGranja.GetComponent<FarmDragItem>();
        drag.enabled = almacen.GranjasDisponibles > 0;

        itemAlmacenAlimentos.SetActive(almacen.AlmacenesAlimentosDisponibles > 0);
        textoAlmacenesAlimentos.text = "Almacén comida x" + almacen.AlmacenesAlimentosDisponibles;
        FoodStorageDragItem dragAlimentos = itemAlmacenAlimentos.GetComponent<FoodStorageDragItem>();
        dragAlimentos.enabled = almacen.AlmacenesAlimentosDisponibles > 0;
    }

    private static Canvas BuscarOCrearCanvas()
    {
        Canvas canvasExistente = BuscarObjetoEscena("Canvas_UI")?.GetComponent<Canvas>();
        if (canvasExistente == null)
        {
            canvasExistente = FindFirstObjectByType<Canvas>();
        }

        if (canvasExistente != null)
        {
            ConfigurarCanvas(canvasExistente);
            return canvasExistente;
        }

        GameObject canvasObjeto = new GameObject("Canvas_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas nuevoCanvas = canvasObjeto.GetComponent<Canvas>();
        nuevoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigurarCanvas(nuevoCanvas);
        return nuevoCanvas;
    }

    private static void ConfigurarCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void AsegurarEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private static Button CrearBoton(Transform parent, string nombre, string texto)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image imagen = go.GetComponent<Image>();
        imagen.sprite = botonSprite;
        imagen.type = Image.Type.Sliced;
        imagen.color = new Color(0.14f, 0.18f, 0.18f, 0.96f);

        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = imagen;

        TMP_Text label = CrearTexto(go.transform, "Txt_" + nombre, texto, 17f, TextAlignmentOptions.Center, true);
        ConfigurarRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -8f));

        return boton;
    }

    private static RectTransform CrearPanel(Transform parent, string nombre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(4f, -5f);

        return go.GetComponent<RectTransform>();
    }

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, TextAlignmentOptions alineacion, bool destacado)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        go.transform.SetParent(parent, false);

        TMP_Text label = go.GetComponent<TextMeshProUGUI>();
        label.text = texto;
        label.color = destacado ? new Color(0.98f, 0.96f, 0.86f, 1f) : new Color(0.80f, 0.88f, 0.84f, 1f);
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = tamano;
        label.alignment = alineacion;
        label.fontStyle = destacado ? FontStyles.Bold : FontStyles.Normal;
        label.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        return label;
    }

    private static Image CrearImagen(Transform parent, string nombre, Sprite sprite)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject BuscarObjetoEscena(string nombre)
    {
        GameObject[] objetos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject objeto in objetos)
        {
            if (objeto.name == nombre && objeto.scene.IsValid() && objeto.scene.isLoaded)
            {
                return objeto;
            }
        }

        return null;
    }

    private static void ConfigurarRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void CrearSpritesSiFaltan()
    {
        if (panelSprite != null)
        {
            return;
        }

        panelSprite = CrearSpriteMarco(new Color32(18, 24, 25, 235), new Color32(223, 165, 73, 255), 96, 5);
        botonSprite = CrearSpriteMarco(new Color32(33, 43, 40, 245), new Color32(138, 95, 48, 255), 64, 4);
        barraSprite = CrearSpriteMarco(Color.white, new Color32(31, 22, 15, 255), 32, 2);
        iconoCuboSprite = CrearSpriteCubo();
    }

    private static Sprite CrearSpriteMarco(Color32 relleno, Color32 borde, int tamano, int grosor)
    {
        Texture2D texture = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                bool esBorde = x < grosor || y < grosor || x >= tamano - grosor || y >= tamano - grosor;
                texture.SetPixel(x, y, esBorde ? borde : relleno);
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0f, 0f, tamano, tamano), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(grosor, grosor, grosor, grosor));
    }

    private static Sprite CrearSpriteCubo()
    {
        Texture2D texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 borde = new Color32(44, 29, 22, 255);
        Color32 luz = new Color32(160, 105, 61, 255);
        Color32 baseColor = new Color32(108, 70, 45, 255);
        Color32 sombra = new Color32(64, 41, 31, 255);

        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                texture.SetPixel(x, y, transparente);
            }
        }

        for (int y = 4; y < 20; y++)
        {
            for (int x = 4; x < 20; x++)
            {
                Color32 color = baseColor;
                if (x == 4 || y == 4 || x == 19 || y == 19)
                {
                    color = borde;
                }
                else if (x < 9 || y > 14)
                {
                    color = luz;
                }
                else if (x > 15 || y < 8)
                {
                    color = sombra;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.5f), 24f);
    }
}

public class FarmDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private AlmacenSistema almacen;
    private Canvas canvas;
    private Sprite spriteGranja;
    private RectTransform fantasma;
    private Image imagenFantasma;

    public void Configurar(AlmacenSistema sistema, Canvas canvasObjetivo, Sprite sprite)
    {
        almacen = sistema;
        canvas = canvasObjetivo;
        spriteGranja = sprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (almacen == null || almacen.GranjasDisponibles <= 0 || canvas == null)
        {
            return;
        }

        GameObject go = new GameObject("Drag_Granja_Fantasma", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        fantasma = go.GetComponent<RectTransform>();
        fantasma.sizeDelta = new Vector2(180f, 120f);

        imagenFantasma = go.GetComponent<Image>();
        imagenFantasma.sprite = spriteGranja;
        imagenFantasma.preserveAspect = true;
        imagenFantasma.color = new Color(1f, 1f, 1f, 0.82f);
        imagenFantasma.raycastTarget = false;

        ActualizarFantasma(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ActualizarFantasma(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool colocado = false;

        if (almacen != null && fantasma != null)
        {
            colocado = almacen.IntentarColocarGranja(eventData.position, Camera.main);
        }

        if (fantasma != null)
        {
            Destroy(fantasma.gameObject);
        }

        if (!colocado && imagenFantasma != null)
        {
            imagenFantasma.color = Color.white;
        }
    }

    private void ActualizarFantasma(PointerEventData eventData)
    {
        if (fantasma == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        fantasma.anchoredPosition = localPoint;
    }
}

public class FoodStorageDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private AlmacenSistema almacen;
    private Canvas canvas;
    private Sprite spriteAlmacen;
    private RectTransform fantasma;

    public void Configurar(AlmacenSistema sistema, Canvas canvasObjetivo, Sprite sprite)
    {
        almacen = sistema;
        canvas = canvasObjetivo;
        spriteAlmacen = sprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (almacen == null || almacen.AlmacenesAlimentosDisponibles <= 0 || canvas == null)
        {
            return;
        }

        GameObject go = new GameObject("Drag_Almacen_Alimentos_Fantasma", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        fantasma = go.GetComponent<RectTransform>();
        fantasma.sizeDelta = new Vector2(150f, 110f);

        Image imagen = go.GetComponent<Image>();
        imagen.sprite = spriteAlmacen;
        imagen.preserveAspect = true;
        imagen.color = new Color(1f, 1f, 1f, 0.82f);
        imagen.raycastTarget = false;
        ActualizarFantasma(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ActualizarFantasma(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (almacen != null && fantasma != null)
        {
            almacen.IntentarColocarAlmacenAlimentos(eventData.position, Camera.main);
        }

        if (fantasma != null)
        {
            Destroy(fantasma.gameObject);
        }
    }

    private void ActualizarFantasma(PointerEventData eventData)
    {
        if (fantasma == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        fantasma.anchoredPosition = localPoint;
    }
}

public static class GranjaVisualFactory
{
    private static Sprite spriteGranja;

    public static Sprite ObtenerSpriteGranja()
    {
        if (spriteGranja != null)
        {
            return spriteGranja;
        }

        spriteGranja = Resources.Load<Sprite>("GranjaAlmacen");
        if (spriteGranja != null)
        {
            return spriteGranja;
        }

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 tierra = new Color32(142, 91, 43, 255);
        Color32 tierraOscura = new Color32(86, 53, 31, 255);
        Color32 madera = new Color32(139, 83, 34, 255);
        Color32 maderaLuz = new Color32(202, 143, 60, 255);
        Color32 maderaSombra = new Color32(74, 45, 24, 255);
        Color32 pasto = new Color32(75, 151, 51, 255);
        Color32 agua = new Color32(53, 142, 184, 255);
        Color32 piedra = new Color32(134, 132, 118, 255);
        Color32 borde = new Color32(42, 26, 14, 255);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                texture.SetPixel(x, y, transparente);
            }
        }

        RellenarRect(texture, 8, 9, 48, 38, tierra);
        RellenarRect(texture, 8, 9, 48, 3, tierraOscura);
        RellenarRect(texture, 8, 44, 48, 3, tierraOscura);
        RellenarRect(texture, 6, 8, 52, 4, pasto);
        RellenarRect(texture, 6, 45, 52, 4, pasto);
        RellenarRect(texture, 6, 8, 4, 41, pasto);
        RellenarRect(texture, 54, 8, 4, 41, pasto);

        for (int i = 0; i < 7; i++)
        {
            int x = 7 + i * 8;
            RellenarRect(texture, x, 6, 4, 48, madera);
            RellenarRect(texture, x, 6, 1, 48, maderaLuz);
            RellenarRect(texture, x + 3, 6, 1, 48, maderaSombra);
        }

        RellenarRect(texture, 5, 9, 54, 4, madera);
        RellenarRect(texture, 5, 43, 54, 4, madera);
        RellenarRect(texture, 5, 11, 54, 1, maderaLuz);
        RellenarRect(texture, 5, 45, 54, 1, maderaLuz);
        RellenarRect(texture, 5, 8, 54, 1, borde);
        RellenarRect(texture, 5, 46, 54, 1, borde);

        RellenarRect(texture, 13, 29, 10, 5, agua);
        RellenarRect(texture, 42, 18, 9, 5, agua);
        RellenarRect(texture, 39, 33, 3, 3, piedra);
        RellenarRect(texture, 43, 35, 4, 3, piedra);
        RellenarRect(texture, 17, 18, 5, 3, piedra);
        RellenarRect(texture, 25, 14, 8, 3, pasto);
        RellenarRect(texture, 27, 35, 10, 3, pasto);
        RellenarRect(texture, 46, 29, 5, 11, maderaLuz);
        RellenarRect(texture, 41, 29, 5, 11, madera);
        RellenarRect(texture, 46, 40, 8, 3, maderaSombra);

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        spriteGranja = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 32f);
        spriteGranja.hideFlags = HideFlags.HideAndDontSave;
        return spriteGranja;
    }

    public static GameObject CrearGranjaFallback(Vector3 posicion)
    {
        GameObject granja = new GameObject("Granja_Colocada");
        granja.transform.position = posicion;

        SpriteRenderer renderer = granja.AddComponent<SpriteRenderer>();
        renderer.sprite = ObtenerSpriteGranja();
        renderer.sortingOrder = 42;

        return granja;
    }

    private static void RellenarRect(Texture2D texture, int x, int y, int ancho, int alto, Color32 color)
    {
        for (int py = y; py < y + alto; py++)
        {
            for (int px = x; px < x + ancho; px++)
            {
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
}

public static class AlmacenAlimentosVisualFactory
{
    private static Sprite spriteAlmacen;

    public static Sprite ObtenerSpriteAlmacenAlimentos()
    {
        if (spriteAlmacen != null)
        {
            return spriteAlmacen;
        }

        Texture2D texturaDiseno = Resources.Load<Texture2D>("Generated/Almacen_Alimentos_Diseno");
        if (texturaDiseno != null)
        {
            float pixelesPorUnidad = Mathf.Max(texturaDiseno.width, texturaDiseno.height) * 0.33f;
            spriteAlmacen = Sprite.Create(texturaDiseno, new Rect(0f, 0f, texturaDiseno.width, texturaDiseno.height), new Vector2(0.5f, 0.5f), pixelesPorUnidad);
            spriteAlmacen.hideFlags = HideFlags.HideAndDontSave;
            return spriteAlmacen;
        }

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 madera = new Color32(130, 78, 34, 255);
        Color32 maderaLuz = new Color32(202, 133, 58, 255);
        Color32 maderaSombra = new Color32(70, 42, 25, 255);
        Color32 techo = new Color32(95, 48, 26, 255);
        Color32 borde = new Color32(39, 24, 16, 255);
        Color32 manzana = new Color32(235, 35, 48, 255);
        Color32 agua = new Color32(78, 188, 224, 255);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                texture.SetPixel(x, y, transparente);
            }
        }

        RellenarRect(texture, 10, 18, 44, 34, madera);
        RellenarRect(texture, 8, 16, 48, 5, techo);
        RellenarRect(texture, 6, 21, 52, 4, borde);
        RellenarRect(texture, 10, 50, 44, 3, borde);
        RellenarRect(texture, 13, 22, 4, 28, maderaLuz);
        RellenarRect(texture, 48, 22, 4, 28, maderaSombra);
        RellenarRect(texture, 22, 30, 20, 18, new Color32(96, 55, 30, 255));
        RellenarRect(texture, 24, 32, 6, 6, manzana);
        RellenarRect(texture, 34, 32, 6, 10, agua);
        RellenarRect(texture, 14, 26, 36, 3, maderaLuz);
        RellenarRect(texture, 14, 44, 36, 3, maderaSombra);

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        spriteAlmacen = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 32f);
        spriteAlmacen.hideFlags = HideFlags.HideAndDontSave;
        return spriteAlmacen;
    }

    public static GameObject CrearAlmacenAlimentos(Vector3 posicion)
    {
        GameObject almacen = new GameObject("Almacen_Alimentos_Colocado");
        almacen.transform.position = posicion;
        SpriteRenderer renderer = almacen.AddComponent<SpriteRenderer>();
        renderer.sprite = ObtenerSpriteAlmacenAlimentos();
        renderer.sortingOrder = 43;
        return almacen;
    }

    private static void RellenarRect(Texture2D texture, int x, int y, int ancho, int alto, Color32 color)
    {
        for (int py = y; py < y + alto; py++)
        {
            for (int px = x; px < x + ancho; px++)
            {
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
}
