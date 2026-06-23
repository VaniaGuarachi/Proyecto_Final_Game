using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventarioCosechas : MonoBehaviour
{
    public static InventarioCosechas instancia;

    [Header("UI")]
    public UICosechas uiCosechas; // Aqui arrastras Panel_Cosechas desde el Inspector

    [Header("Cosechas")]
    public int hojasVerdes;
    public int gotasAzules;
    public int energiaSolar;
    public int nectarMorado;

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

    public void AgregarCosecha(TipoSemilla tipo, int cantidad)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                hojasVerdes += cantidad;
                Debug.Log("Cosechaste Hoja Verde x" + cantidad);
                break;

            case TipoSemilla.Azul:
                gotasAzules += cantidad;
                Debug.Log("Cosechaste Gota Azul x" + cantidad);
                break;

            case TipoSemilla.Amarilla:
                energiaSolar += cantidad;
                Debug.Log("Cosechaste Energia Solar x" + cantidad);
                break;

            case TipoSemilla.Morada:
                nectarMorado += cantidad;
                Debug.Log("Cosechaste Nectar Morado x" + cantidad);
                break;
        }

        MostrarInventario();
        ActualizarUI();
    }

    public bool ConsumirCosecha(TipoSemilla tipo, int cantidad)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                if (hojasVerdes < cantidad)
                {
                    return false;
                }

                hojasVerdes -= cantidad;
                break;

            case TipoSemilla.Azul:
                if (gotasAzules < cantidad)
                {
                    return false;
                }

                gotasAzules -= cantidad;
                break;

            case TipoSemilla.Amarilla:
                if (energiaSolar < cantidad)
                {
                    return false;
                }

                energiaSolar -= cantidad;
                break;

            case TipoSemilla.Morada:
                if (nectarMorado < cantidad)
                {
                    return false;
                }

                nectarMorado -= cantidad;
                break;
        }

        MostrarInventario();
        ActualizarUI();
        return true;
    }

    private void ActualizarUI()
    {
        AsegurarUI();

        if (uiCosechas != null)
        {
            uiCosechas.ActualizarUI(hojasVerdes, gotasAzules, energiaSolar, nectarMorado);
        }
    }

    private void AsegurarUI()
    {
        if (uiCosechas != null)
        {
            return;
        }

        uiCosechas = FindFirstObjectByType<UICosechas>();
        if (uiCosechas != null)
        {
            return;
        }

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject panel = new GameObject("Panel_Cosechas", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UICosechas));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -110f);
        panelRect.sizeDelta = new Vector2(214f, 218f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.055f, 0.075f, 0.105f, 0.88f);
        panelImage.raycastTarget = false;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 40, 14);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        TextMeshProUGUI titulo = CrearTexto(panel.transform, "UI_Titulo_COSECHAS", "COSECHAS", 17, TextAlignmentOptions.Center);
        titulo.fontStyle = FontStyles.Bold;

        UICosechas ui = panel.GetComponent<UICosechas>();
        ui.textoHojaVerde = CrearTexto(panel.transform, "Texto_HojaVerde", "Hoja x0", 18, TextAlignmentOptions.MidlineLeft);
        ui.textoGotaAzul = CrearTexto(panel.transform, "Texto_GotaAzul", "Gota x0", 18, TextAlignmentOptions.MidlineLeft);
        ui.textoEnergiaSolar = CrearTexto(panel.transform, "Texto_EnergiaSolar", "Sol x0", 18, TextAlignmentOptions.MidlineLeft);
        ui.textoNectarMorado = CrearTexto(panel.transform, "Texto_NectarMorado", "Nectar x0", 18, TextAlignmentOptions.MidlineLeft);
        uiCosechas = ui;
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

    private void MostrarInventario()
    {
        Debug.Log(
            "COSECHAS | Hojas: " + hojasVerdes +
            " | Gotas: " + gotasAzules +
            " | Energia: " + energiaSolar +
            " | Nectar: " + nectarMorado
        );
    }
}
