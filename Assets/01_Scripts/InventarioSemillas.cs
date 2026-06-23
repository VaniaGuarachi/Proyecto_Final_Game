using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventarioSemillas : MonoBehaviour
{
    public static InventarioSemillas instancia;

    [Header("Cantidad de semillas")]
    public int semillasVerdes;
    public int semillasAzules;
    public int semillasAmarillas;
    public int semillasMoradas;

    [Header("UI")]
    public UIInventarioSemillas uiInventarioSemillas;

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
        ActualizarUI();
    }

    public void AgregarSemilla(TipoSemilla tipo, int cantidad)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                semillasVerdes += cantidad;
                break;

            case TipoSemilla.Azul:
                semillasAzules += cantidad;
                break;

            case TipoSemilla.Amarilla:
                semillasAmarillas += cantidad;
                break;

            case TipoSemilla.Morada:
                semillasMoradas += cantidad;
                break;
        }

        Debug.Log("Recogiste semilla " + tipo + " x" + cantidad);
        MostrarInventario();
        ActualizarUI();
    }

    public bool TieneSemilla(TipoSemilla tipo)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                return semillasVerdes > 0;

            case TipoSemilla.Azul:
                return semillasAzules > 0;

            case TipoSemilla.Amarilla:
                return semillasAmarillas > 0;

            case TipoSemilla.Morada:
                return semillasMoradas > 0;

            default:
                return false;
        }
    }

    public bool UsarSemilla(TipoSemilla tipo)
    {
        if (!TieneSemilla(tipo))
        {
            Debug.Log("No tienes semillas de tipo: " + tipo);
            return false;
        }

        switch (tipo)
        {
            case TipoSemilla.Verde:
                semillasVerdes--;
                break;

            case TipoSemilla.Azul:
                semillasAzules--;
                break;

            case TipoSemilla.Amarilla:
                semillasAmarillas--;
                break;

            case TipoSemilla.Morada:
                semillasMoradas--;
                break;
        }

        Debug.Log("Usaste una semilla: " + tipo);
        MostrarInventario();
        ActualizarUI();

        return true;
    }

    public int ObtenerCantidad(TipoSemilla tipo)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                return semillasVerdes;

            case TipoSemilla.Azul:
                return semillasAzules;

            case TipoSemilla.Amarilla:
                return semillasAmarillas;

            case TipoSemilla.Morada:
                return semillasMoradas;

            default:
                return 0;
        }
    }

    private void ActualizarUI()
    {
        AsegurarUI();

        if (uiInventarioSemillas != null)
        {
            uiInventarioSemillas.ActualizarUI();
        }
    }

    private void AsegurarUI()
    {
        if (uiInventarioSemillas != null)
        {
            return;
        }

        uiInventarioSemillas = FindFirstObjectByType<UIInventarioSemillas>(FindObjectsInactive.Include);
        if (uiInventarioSemillas != null)
        {
            return;
        }

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject panel = new GameObject("Panel_Semillas", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIInventarioSemillas));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(660f, 74f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.055f, 0.075f, 0.105f, 0.88f);
        image.raycastTarget = false;

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        UIInventarioSemillas ui = panel.GetComponent<UIInventarioSemillas>();
        ui.textoSemillaVerde = CrearTexto(panel.transform, "Texto_SemillaVerde", "Verde x0");
        ui.textoSemillaAzul = CrearTexto(panel.transform, "Texto_SemillaAzul", "Azul x0");
        ui.textoSemillaAmarilla = CrearTexto(panel.transform, "Texto_SemillaAmarilla", "Amarilla x0");
        ui.textoSemillaMorada = CrearTexto(panel.transform, "Texto_SemillaMorada", "Morada x0");
        uiInventarioSemillas = ui;
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

    private static TextMeshProUGUI CrearTexto(Transform parent, string nombre, string texto)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        objeto.transform.SetParent(parent, false);

        TextMeshProUGUI label = objeto.GetComponent<TextMeshProUGUI>();
        label.text = texto;
        label.color = Color.white;
        label.fontSize = 16f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 16f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        LayoutElement layout = objeto.GetComponent<LayoutElement>();
        layout.preferredHeight = 42f;
        layout.preferredWidth = 120f;
        return label;
    }

    private void MostrarInventario()
    {
        Debug.Log(
            "SEMILLAS | Verde: " + semillasVerdes +
            " | Azul: " + semillasAzules +
            " | Amarilla: " + semillasAmarillas +
            " | Morada: " + semillasMoradas
        );
    }
}

public enum TipoSemilla
{
    Verde,
    Azul,
    Amarilla,
    Morada
}
