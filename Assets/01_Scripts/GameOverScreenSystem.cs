using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreenSystem : MonoBehaviour
{
    private static GameOverScreenSystem instancia;

    private CanvasGroup canvasGroup;
    private RectTransform root;
    private bool mostrado;
    private float tiempoMostrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<GameOverScreenSystem>() != null)
        {
            return;
        }

        new GameObject("GameOverScreenSystem").AddComponent<GameOverScreenSystem>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
    }

    private void Update()
    {
        if (!mostrado && PlayerHambreSistema.ComidaAgotada && ZonaFriaBufandaSistema.VidaAgotada)
        {
            MostrarGameOver();
        }

        if (!mostrado || canvasGroup == null)
        {
            return;
        }

        tiempoMostrado += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempoMostrado / 0.45f));

        if (root != null)
        {
            float escala = Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(tiempoMostrado / 0.55f));
            root.localScale = new Vector3(escala, escala, 1f);
        }
    }

    private void MostrarGameOver()
    {
        mostrado = true;
        tiempoMostrado = 0f;

        GameManager.Instance?.EndGame();
        Time.timeScale = 0f;

        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject overlay = new GameObject("Pantalla_Game_Over", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsLastSibling();

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image fondo = overlay.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.72f);
        fondo.raycastTarget = true;

        canvasGroup = overlay.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        CrearCapaBlur(overlay.transform, new Vector2(-8f, 6f), new Color(0.12f, 0.16f, 0.18f, 0.18f));
        CrearCapaBlur(overlay.transform, new Vector2(8f, -6f), new Color(0.04f, 0.08f, 0.12f, 0.22f));
        CrearCapaBlur(overlay.transform, Vector2.zero, new Color(0.02f, 0.025f, 0.035f, 0.35f));

        GameObject content = new GameObject("Game_Over_Content", typeof(RectTransform));
        content.transform.SetParent(overlay.transform, false);
        root = content.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(820f, 420f);

        CrearTitulo(root, "GAME", new Vector2(0f, 95f));
        CrearTitulo(root, "OVER", new Vector2(0f, -78f));

        TMP_Text subtitulo = CrearTexto(root, "Texto_Game_Over_Subtitulo", "COMIDA Y VIDA AGOTADAS", 24f, new Color(1f, 0.9f, 0.55f, 1f));
        ConfigurarRect(subtitulo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(620f, 44f));

        Button reiniciar = CrearBoton(root, "Boton_Reiniciar_GameOver", "REINICIAR", new Vector2(-130f, -245f));
        reiniciar.onClick.AddListener(ReiniciarEscena);

        Button salir = CrearBoton(root, "Boton_Salir_GameOver", "MENU", new Vector2(130f, -245f));
        salir.onClick.AddListener(ReiniciarEscena);

        ProfessionalFeedbackSystem.Toast("Game Over", new Color(1f, 0.75f, 0.08f, 1f));
    }

    private static void CrearCapaBlur(Transform parent, Vector2 offset, Color color)
    {
        GameObject capa = new GameObject("Fondo_Blur_Simulado", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        capa.transform.SetParent(parent, false);
        RectTransform rect = capa.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-24f + offset.x, -24f + offset.y);
        rect.offsetMax = new Vector2(24f + offset.x, 24f + offset.y);

        Image image = capa.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CrearTitulo(Transform parent, string texto, Vector2 posicion)
    {
        TMP_Text sombraLejana = CrearTexto(parent, "Sombra_" + texto, texto, 124f, new Color(0.07f, 0.02f, 0.06f, 1f));
        ConfigurarRect(sombraLejana.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion + new Vector2(10f, -10f), new Vector2(760f, 150f));

        TMP_Text bordeRojo = CrearTexto(parent, "Borde_Rojo_" + texto, texto, 124f, new Color(0.92f, 0.08f, 0.08f, 1f));
        ConfigurarRect(bordeRojo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion + new Vector2(4f, -6f), new Vector2(760f, 150f));

        TMP_Text cuerpo = CrearTexto(parent, "Titulo_" + texto, texto, 124f, new Color(1f, 0.78f, 0.05f, 1f));
        ConfigurarRect(cuerpo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion, new Vector2(760f, 150f));

        TMP_Text brillo = CrearTexto(parent, "Brillo_" + texto, texto, 124f, new Color(1f, 0.96f, 0.58f, 0.64f));
        ConfigurarRect(brillo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posicion + new Vector2(-5f, 9f), new Vector2(760f, 150f));
    }

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, Color color)
    {
        GameObject textoGO = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(parent, false);
        TMP_Text label = textoGO.GetComponent<TMP_Text>();
        label.text = texto;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = tamano;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 0f;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Button CrearBoton(Transform parent, string nombre, string texto, Vector2 posicion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Shadow));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        ConfigurarRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), posicion, new Vector2(220f, 54f));

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.055f, 0.035f, 0.96f);

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(3f, -3f);

        TMP_Text label = CrearTexto(go.transform, "Texto_" + nombre, texto, 22f, new Color(1f, 0.84f, 0.28f, 1f));
        ConfigurarRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -8f));

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static void ReiniciarEscena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    private static void ConfigurarRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }
}
