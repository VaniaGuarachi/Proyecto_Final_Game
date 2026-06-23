using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EcosystemVictorySystem : MonoBehaviour
{
    [SerializeField] private float estabilidadObjetivo = 70f;
    [SerializeField] private float contaminacionMaxima = 35f;
    [SerializeField] private int especiesMinimas = 4;
    [SerializeField] private float duracionObjetivo = 180f;

    private TMP_Text texto;
    private float progreso;
    private bool logrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<EcosystemVictorySystem>() != null)
        {
            return;
        }

        new GameObject("EcosystemVictorySystem").AddComponent<EcosystemVictorySystem>();
    }

    private void Start()
    {
        CrearUI();
    }

    private void Update()
    {
        EcosystemManager eco = EcosystemManager.instancia;
        if (eco == null || logrado)
        {
            return;
        }

        bool cumple = eco.estabilidad >= estabilidadObjetivo
            && eco.contaminacion <= contaminacionMaxima
            && eco.biodiversidad >= especiesMinimas * 16.7f
            && !eco.EstaEnColapso();

        if (cumple)
        {
            progreso += Time.deltaTime;
            if (progreso >= duracionObjetivo)
            {
                logrado = true;
                EcosystemToast.Mostrar("ECOSISTEMA AUTOSUSTENTABLE LOGRADO");
                BiologicalResearchSystem.RegistrarEstabilizacion();
            }
        }
        else if (eco.EstaEnColapso() || eco.contaminacion > contaminacionMaxima + 20f)
        {
            progreso = 0f;
        }
        else
        {
            progreso = Mathf.Max(0f, progreso - Time.deltaTime * 0.35f);
        }

        ActualizarUI(cumple);
    }

    private void CrearUI()
    {
        Canvas canvas = BuscarOCrearCanvas();
        GameObject panel = new GameObject("Panel_Victoria_Ecosistema", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -70f);
        rect.sizeDelta = new Vector2(520f, 42f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.08f, 0.06f, 0.72f);
        image.raycastTarget = false;

        Shadow shadow = panel.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(2f, -2f);

        GameObject textGo = new GameObject("Txt_Victoria_Ecosistema", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        texto = textGo.GetComponent<TMP_Text>();
        texto.fontSize = 16f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 9f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(0.86f, 1f, 0.78f, 1f);
        texto.raycastTarget = false;
        ActualizarUI(false);
    }

    private void ActualizarUI(bool cumple)
    {
        if (texto == null)
        {
            return;
        }

        if (logrado)
        {
            texto.text = "ECOSISTEMA AUTOSUSTENTABLE LOGRADO";
            texto.color = new Color(0.55f, 1f, 0.58f, 1f);
            return;
        }

        int segundos = Mathf.Clamp(Mathf.FloorToInt(duracionObjetivo - progreso), 0, Mathf.CeilToInt(duracionObjetivo));
        texto.text = cumple
            ? "Equilibrio ecologico estable: " + segundos + "s restantes"
            : "Meta: estabilidad 70+, contaminacion <35, 4 especies por 3 min";
        texto.color = cumple ? new Color(0.62f, 1f, 0.72f, 1f) : new Color(0.92f, 0.96f, 0.82f, 1f);
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
