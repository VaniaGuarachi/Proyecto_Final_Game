using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventarioRecursosSmiles : MonoBehaviour
{
    public static InventarioRecursosSmiles instancia;

    [Header("Recursos producidos por smiles")]
    public int gelVerde;
    public int gotasSmileAzul;
    public int energiaSmileAmarilla;
    public int esenciaSmileMorada;
    public int manzanas;
    public int vasosAgua;
    public int bufandasAntifrio;

    [Header("UI")]
    public UIRecursosSmiles uiRecursosSmiles;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            AsegurarUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AsegurarUI();
        InventoryUiRuntimeStyler.Apply();
        ActualizarUI();
    }

    // ------------------- METODOS PARA AGREGAR RECURSOS -------------------
    public void AgregarGelVerde(int cantidad)
    {
        gelVerde += cantidad;
        Debug.Log("Recibiste Gel Verde x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarGotasAzules(int cantidad)
    {
        gotasSmileAzul += cantidad;
        Debug.Log("Recibiste Gotas Smile Azul x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarEnergiaAmarilla(int cantidad)
    {
        energiaSmileAmarilla += cantidad;
        Debug.Log("Recibiste Energia Smile Amarilla x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarEsenciaMorada(int cantidad)
    {
        esenciaSmileMorada += cantidad;
        Debug.Log("Recibiste Esencia Smile Morada x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarManzanas(int cantidad)
    {
        manzanas += cantidad;
        Debug.Log("Recibiste Manzana x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarVasosAgua(int cantidad)
    {
        vasosAgua += cantidad;
        Debug.Log("Recibiste Agua x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public void AgregarBufandas(int cantidad)
    {
        bufandasAntifrio += cantidad;
        Debug.Log("Recibiste Bufanda Antifrio x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    // ------------------- METODOS PARA CONSUMIR RECURSOS -------------------
    public bool ConsumirGelVerde(int cantidad)
    {
        if (gelVerde >= cantidad)
        {
            gelVerde -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }
        Debug.Log("No tienes suficiente Gel Verde");
        return false;
    }

    public bool ConsumirGotasAzules(int cantidad)
    {
        if (gotasSmileAzul >= cantidad)
        {
            gotasSmileAzul -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }
        Debug.Log("No tienes suficientes Gotas Smile Azul");
        return false;
    }

    public bool ConsumirEnergiaAmarilla(int cantidad)
    {
        if (energiaSmileAmarilla >= cantidad)
        {
            energiaSmileAmarilla -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }
        Debug.Log("No tienes suficiente Energia Smile Amarilla");
        return false;
    }

    public bool ConsumirEsenciaMorada(int cantidad)
    {
        if (esenciaSmileMorada >= cantidad)
        {
            esenciaSmileMorada -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }
        Debug.Log("No tienes suficiente Esencia Smile Morada");
        return false;
    }

    public bool ConsumirBufanda(int cantidad)
    {
        if (bufandasAntifrio >= cantidad)
        {
            bufandasAntifrio -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }

        Debug.Log("No tienes bufandas antifrio");
        return false;
    }

    public bool ConsumirManzana(int cantidad)
    {
        if (manzanas >= cantidad)
        {
            manzanas -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }

        Debug.Log("No tienes manzanas");
        return false;
    }

    public bool ConsumirVasosAgua(int cantidad)
    {
        if (vasosAgua >= cantidad)
        {
            vasosAgua -= cantidad;
            MostrarInventario();
            ActualizarUI();
            return true;
        }

        Debug.Log("No tienes agua");
        return false;
    }

    private void ActualizarUI()
    {
        AsegurarUI();

        if (uiRecursosSmiles != null)
        {
            uiRecursosSmiles.ActualizarUI(gelVerde, gotasSmileAzul, energiaSmileAmarilla, esenciaSmileMorada, manzanas, vasosAgua, bufandasAntifrio);
            InventoryUiRuntimeStyler.Apply();
        }
    }

    private void AsegurarUI()
    {
        if (uiRecursosSmiles != null)
        {
            AsegurarTextosRecursosEspeciales();
            return;
        }

        uiRecursosSmiles = FindFirstObjectByType<UIRecursosSmiles>(FindObjectsInactive.Include);
        if (uiRecursosSmiles != null)
        {
            AsegurarTextosRecursosEspeciales();
            return;
        }

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject panel = new GameObject("Panel_RecursosSmiles", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIRecursosSmiles));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -350f);
        rect.sizeDelta = new Vector2(214f, 292f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.055f, 0.075f, 0.105f, 0.88f);
        image.raycastTarget = false;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 40, 14);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        TextMeshProUGUI titulo = CrearTexto(panel.transform, "UI_Titulo_RECURSOS", "RECURSOS", 17f, TextAlignmentOptions.Center);
        titulo.fontStyle = FontStyles.Bold;

        UIRecursosSmiles ui = panel.GetComponent<UIRecursosSmiles>();
        ui.textoGelVerde = CrearTexto(panel.transform, "Texto_GelVerde", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoGotasAzul = CrearTexto(panel.transform, "Texto_GotasAzul", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoEnergiaSolar = CrearTexto(panel.transform, "Texto_RecursoEnergiaSolar", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoNectarMorado = CrearTexto(panel.transform, "Texto_RecursoNectarMorado", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoManzanas = CrearTexto(panel.transform, "Texto_RecursoManzanas", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoAgua = CrearTexto(panel.transform, "Texto_RecursoAgua", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        ui.textoBufandas = CrearTexto(panel.transform, "Texto_RecursoBufandas", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        uiRecursosSmiles = ui;
    }

    private void AsegurarTextosRecursosEspeciales()
    {
        if (uiRecursosSmiles == null)
        {
            return;
        }

        if (uiRecursosSmiles.textoManzanas == null)
        {
            uiRecursosSmiles.textoManzanas = CrearTexto(uiRecursosSmiles.transform, "Texto_RecursoManzanas", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        }

        if (uiRecursosSmiles.textoAgua == null)
        {
            uiRecursosSmiles.textoAgua = CrearTexto(uiRecursosSmiles.transform, "Texto_RecursoAgua", "x0", 18f, TextAlignmentOptions.MidlineLeft);
        }

        if (uiRecursosSmiles.textoBufandas == null)
        {
            uiRecursosSmiles.textoBufandas = CrearTexto(uiRecursosSmiles.transform, "Texto_RecursoBufandas", "x0", 18f, TextAlignmentOptions.MidlineLeft);
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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static TextMeshProUGUI CrearTexto(Transform parent, string nombre, string texto, float tamano, TextAlignmentOptions alineacion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        objeto.transform.SetParent(parent, false);

        TextMeshProUGUI label = objeto.GetComponent<TextMeshProUGUI>();
        label.text = texto;
        label.color = Color.white;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11f;
        label.fontSizeMax = tamano;
        label.alignment = alineacion;
        label.raycastTarget = false;

        LayoutElement layout = objeto.GetComponent<LayoutElement>();
        layout.preferredHeight = 30f;
        return label;
    }

    // ------------------- MOSTRAR INVENTARIO EN CONSOLA -------------------
    private void MostrarInventario()
    {
        Debug.Log(
            "RECURSOS SMILES | Gel Verde: " + gelVerde +
            " | Gotas Smile Azul: " + gotasSmileAzul +
            " | Energia Smile Amarilla: " + energiaSmileAmarilla +
            " | Esencia Smile Morada: " + esenciaSmileMorada +
            " | Manzanas: " + manzanas +
            " | Agua: " + vasosAgua +
            " | Bufandas: " + bufandasAntifrio
        );
    }
}
