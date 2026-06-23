using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPrincipal : MonoBehaviour
{
    private const string SystemName = "Sistema_MenuPrincipal";
    private const string BackgroundResourceName = "MenuPrincipalFondo";
    private const float MenuMaxWidth = 1080f;
    private const float MenuMaxHeight = 608f;
    private const int MenuSortingOrder = short.MaxValue;

    private static readonly Vector2 StartButtonCenter = new Vector2(0.5f, 0.394f);
    private static readonly Vector2 StartButtonSize = new Vector2(0.350f, 0.104f);
    private static readonly Vector2 RestartButtonCenter = new Vector2(0.5f, 0.279f);
    private static readonly Vector2 RestartButtonSize = new Vector2(0.350f, 0.103f);
    private static readonly Vector2 ExitButtonCenter = new Vector2(0.5f, 0.158f);
    private static readonly Vector2 ExitButtonSize = new Vector2(0.350f, 0.103f);

    private static Sprite backgroundSprite;

    private Canvas menuCanvas;
    private GameObject menuRoot;
    private GameObject characterSelectionRoot;
    private RectTransform backgroundRect;
    private float previousTimeScale = 1f;
    private bool gameStarted;
    
    private AudioSource menuAudio;
    private AudioSource gameAudio;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateIfNeeded();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateIfNeeded();
    }

    private static void CreateIfNeeded()
    {
        if (FindSceneObject(SystemName) != null)
        {
            return;
        }

        GameObject system = new GameObject(SystemName);
        system.AddComponent<MenuPrincipal>();
    }

    private void Awake()
    {
        EnsureBackgroundSprite();
        EnsureEventSystem();
        BuildMenu();
        SetupAudio();
        OpenMenu();
    }

    private void SetupAudio()
    {
        menuAudio = gameObject.AddComponent<AudioSource>();
        menuAudio.clip = Resources.Load<AudioClip>("Sonidos/MenuSonido");
        menuAudio.loop = true;
        menuAudio.playOnAwake = false;

        gameAudio = gameObject.AddComponent<AudioSource>();
        gameAudio.clip = Resources.Load<AudioClip>("Sonidos/JugandoSonido");
        gameAudio.loop = true;
        gameAudio.playOnAwake = false;
    }

    private void Update()
    {
        if (menuRoot != null && menuRoot.activeSelf)
        {
            KeepMenuOnTop();

            if (characterSelectionRoot != null && characterSelectionRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    characterSelectionRoot.SetActive(false);
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || WasHotspotClicked(StartButtonCenter, StartButtonSize))
            {
                StartGame();
                return;
            }

            if (WasHotspotClicked(RestartButtonCenter, RestartButtonSize))
            {
                RestartGame();
                return;
            }

            if (WasHotspotClicked(ExitButtonCenter, ExitButtonSize))
            {
                ExitGame();
                return;
            }
        }

        if (!gameStarted || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (menuRoot != null && menuRoot.activeSelf)
        {
            ResumeGame();
            return;
        }

        OpenMenu();
    }

    private void BuildMenu()
    {
        menuCanvas = FindSceneObject("Canvas_MenuPrincipal")?.GetComponent<Canvas>();
        if (menuCanvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas_MenuPrincipal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            menuCanvas = canvasObject.GetComponent<Canvas>();
        }

        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        KeepMenuOnTop();

        GraphicRaycaster raycaster = menuCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            menuCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = menuCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        menuRoot = new GameObject("Panel_MenuPrincipal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuRoot.transform.SetParent(menuCanvas.transform, false);
        menuRoot.transform.SetAsLastSibling();

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        SetRect(rootRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Image blocker = menuRoot.GetComponent<Image>();
        blocker.color = Color.black;
        blocker.raycastTarget = true;

        backgroundRect = CreateBackground(menuRoot.transform);
        CreateHotspotButton(backgroundRect, "Hotspot_IniciarPartida", StartButtonCenter, StartButtonSize, StartGame);
        CreateHotspotButton(backgroundRect, "Hotspot_Reiniciar", RestartButtonCenter, RestartButtonSize, RestartGame);
        CreateHotspotButton(backgroundRect, "Hotspot_Salir", ExitButtonCenter, ExitButtonSize, ExitGame);
        BuildCharacterSelection();
    }

    private void BuildCharacterSelection()
    {
        characterSelectionRoot = new GameObject("Panel_SeleccionPersonaje", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        characterSelectionRoot.transform.SetParent(menuRoot.transform, false);

        RectTransform rootRect = characterSelectionRoot.GetComponent<RectTransform>();
        SetRect(rootRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Image backdrop = characterSelectionRoot.GetComponent<Image>();
        backdrop.color = new Color(0.008f, 0.018f, 0.025f, 0.94f);
        backdrop.raycastTarget = true;

        RectTransform shadow = CreateRect(characterSelectionRoot.transform, "Sombra_MarcoSeleccion");
        SetRect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1060f, 660f));
        Image shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panel = CreateRect(characterSelectionRoot.transform, "Marco_SeleccionPersonaje");
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 650f));

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.075f, 0.085f, 0.99f);
        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.35f, 0.72f, 0.18f, 0.95f);
        panelOutline.effectDistance = new Vector2(5f, -5f);

        CreateSelectionDecoration(panel);

        CreateSelectionText(panel, "Titulo_Seleccion", "ELIGE TU EXPLORADOR", new Vector2(0f, 270f), new Vector2(850f, 62f), 40f, new Color(1f, 0.82f, 0.20f, 1f));
        CreateSelectionText(panel, "Subtitulo_Seleccion", "DOS AVENTUREROS, UNA MISION: CUIDAR LA COLONIA", new Vector2(0f, 226f), new Vector2(820f, 34f), 17f, new Color(0.70f, 0.92f, 0.88f, 1f));

        Sprite player1Preview = LoadFullCharacterPreview("Player1/Player1_Full");
        Sprite player2Preview = LoadPlayer2Preview();
        CreateCharacterCard(panel, "Tarjeta_Player1", "PLAYER 1", "GUARDIANA DE SLIMES", new Vector2(-235f, -20f), player1Preview, new Color(0.34f, 0.78f, 0.18f, 1f), () => SelectCharacter(1));
        CreateCharacterCard(panel, "Tarjeta_Player2", "PLAYER 2", "AVENTURERO ESCARLATA", new Vector2(235f, -20f), player2Preview, new Color(0.12f, 0.56f, 0.92f, 1f), () => SelectCharacter(2));

        RectTransform backButton = CreateRect(panel, "Boton_VolverSeleccion");
        SetRect(backButton, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -286f), new Vector2(210f, 44f));
        Image backImage = backButton.gameObject.AddComponent<Image>();
        backImage.color = new Color(0.12f, 0.19f, 0.20f, 1f);
        Outline backOutline = backButton.gameObject.AddComponent<Outline>();
        backOutline.effectColor = new Color(0.39f, 0.65f, 0.36f, 0.8f);
        backOutline.effectDistance = new Vector2(2f, -2f);
        Button back = backButton.gameObject.AddComponent<Button>();
        back.targetGraphic = backImage;
        back.onClick.AddListener(() => characterSelectionRoot.SetActive(false));
        CreateSelectionText(backButton, "Txt_Volver", "VOLVER", Vector2.zero, new Vector2(190f, 42f), 21f, Color.white);

        characterSelectionRoot.SetActive(false);
    }

    private void CreateCharacterCard(RectTransform parent, string objectName, string title, string subtitle, Vector2 position, Sprite preview, Color accent, UnityEngine.Events.UnityAction action)
    {
        RectTransform card = CreateRect(parent, objectName);
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(420f, 430f));

        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.98f);
        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(accent.r, accent.g, accent.b, 0.92f);
        cardOutline.effectDistance = new Vector2(4f, -4f);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        RectTransform glow = CreateRect(card, "Resplandor_Personaje");
        SetRect(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(300f, 300f));
        Image glowImage = glow.gameObject.AddComponent<Image>();
        glowImage.color = new Color(accent.r, accent.g, accent.b, 0.13f);
        glowImage.raycastTarget = false;

        RectTransform previewRect = CreateRect(card, "VistaPrevia");
        SetRect(previewRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(286f, 300f));
        Image previewImage = previewRect.gameObject.AddComponent<Image>();
        previewImage.sprite = preview;
        previewImage.color = preview != null ? Color.white : accent;
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        RectTransform titlePlate = CreateRect(card, "Placa_Nombre");
        SetRect(titlePlate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -139f), new Vector2(360f, 55f));
        Image titlePlateImage = titlePlate.gameObject.AddComponent<Image>();
        titlePlateImage.color = new Color(0.025f, 0.045f, 0.05f, 0.90f);
        titlePlateImage.raycastTarget = false;
        CreateSelectionText(titlePlate, "Titulo", title, new Vector2(0f, 5f), new Vector2(340f, 42f), 29f, Color.white);
        CreateSelectionText(card, "Subtitulo", subtitle, new Vector2(0f, -177f), new Vector2(360f, 28f), 15f, new Color(accent.r * 0.8f + 0.2f, accent.g * 0.8f + 0.2f, accent.b * 0.8f + 0.2f, 1f));
        CreateSelectionText(card, "Accion", "SELECCIONAR", new Vector2(0f, -204f), new Vector2(200f, 23f), 13f, new Color(1f, 0.82f, 0.25f, 1f));
    }

    private static void CreateSelectionDecoration(RectTransform panel)
    {
        RectTransform header = CreateRect(panel, "Franja_Superior");
        SetRect(header, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 254f), new Vector2(1000f, 108f));
        Image headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.055f, 0.17f, 0.13f, 0.94f);
        headerImage.raycastTarget = false;

        CreateSelectionText(panel, "Adorno_Izquierdo", "o  o  O", new Vector2(-420f, 270f), new Vector2(150f, 40f), 24f, new Color(0.45f, 0.92f, 0.20f, 0.8f));
        CreateSelectionText(panel, "Adorno_Derecho", "O  o  o", new Vector2(420f, 270f), new Vector2(150f, 40f), 24f, new Color(0.31f, 0.78f, 1f, 0.8f));

        RectTransform divider = CreateRect(panel, "Linea_Dorada");
        SetRect(divider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 199f), new Vector2(900f, 3f));
        Image dividerImage = divider.gameObject.AddComponent<Image>();
        dividerImage.color = new Color(0.96f, 0.68f, 0.12f, 0.78f);
        dividerImage.raycastTarget = false;

        CreateSelectionText(panel, "Consejo_Seleccion", "HAZ CLIC EN TU AVENTURERO PARA COMENZAR", new Vector2(0f, -252f), new Vector2(650f, 25f), 14f, new Color(0.56f, 0.76f, 0.68f, 1f));
    }

    private static void CreateSelectionText(RectTransform parent, string objectName, string content, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        RectTransform rect = CreateRect(parent, objectName);
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = content;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
    }

    private static Sprite LoadFullCharacterPreview(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        return texture == null
            ? null
            : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite LoadPlayer2Preview()
    {
        return LoadFullCharacterPreview("Player2/Player2_Idle");
    }

    private RectTransform CreateBackground(Transform parent)
    {
        RectTransform background = CreateRect(parent, "Fondo_MenuPrincipal");
        SetRect(background, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, CalcularTamanoMenu());

        Image image = background.gameObject.AddComponent<Image>();
        image.sprite = backgroundSprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        if (backgroundSprite == null)
        {
            image.color = new Color(0.03f, 0.04f, 0.05f, 1f);
            return background;
        }

        return background;
    }

    private Vector2 CalcularTamanoMenu()
    {
        if (backgroundSprite == null)
        {
            return new Vector2(MenuMaxWidth, MenuMaxHeight);
        }

        float aspecto = backgroundSprite.rect.width / backgroundSprite.rect.height;
        float ancho = MenuMaxWidth;
        float alto = ancho / aspecto;

        if (alto > MenuMaxHeight)
        {
            alto = MenuMaxHeight;
            ancho = alto * aspecto;
        }

        return new Vector2(ancho, alto);
    }

    private void CreateHotspotButton(RectTransform parent, string name, Vector2 centerNormalized, Vector2 sizeNormalized, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Vector2 halfSize = sizeNormalized * 0.5f;
        rect.anchorMin = centerNormalized - halfSize;
        rect.anchorMax = centerNormalized + halfSize;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = BuildInvisibleButtonColors();
        button.onClick.AddListener(action);
    }

    private void CreateVisibleMenuButton(RectTransform parent, string name, string text, Vector2 centerNormalized, Vector2 sizeNormalized, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Vector2 halfSize = sizeNormalized * 0.5f;
        rect.anchorMin = centerNormalized - halfSize;
        rect.anchorMax = centerNormalized + halfSize;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.73f, 0.05f, 0.96f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = BuildVisibleButtonColors();
        button.onClick.AddListener(action);

        GameObject labelObject = new GameObject("Txt_" + name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        SetRect(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -8f));

        TMP_Text label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = Color.white;
        label.fontSize = 28f;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 28f;
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        Shadow shadow = labelObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void OpenMenu()
    {
        previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
            KeepMenuOnTop();
            menuRoot.transform.SetAsLastSibling();
        }

        if (!gameStarted)
        {
            if (gameAudio != null) gameAudio.Stop();
            if (menuAudio != null && !menuAudio.isPlaying) menuAudio.Play();
        }
    }

    private void ResumeGame()
    {
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (gameStarted)
        {
            if (menuAudio != null) menuAudio.Stop();
            if (gameAudio != null && !gameAudio.isPlaying) gameAudio.Play();
        }
    }

    private void StartGame()
    {
        if (gameStarted)
        {
            ResumeGame();
            return;
        }

        if (characterSelectionRoot != null)
        {
            characterSelectionRoot.SetActive(true);
            characterSelectionRoot.transform.SetAsLastSibling();
        }
    }

    private void SelectCharacter(int characterNumber)
    {
        Player2VisualAnimator.SelectCharacter(characterNumber);
        if (characterSelectionRoot != null)
        {
            characterSelectionRoot.SetActive(false);
        }

        gameStarted = true;
        ResumeGame();
    }

    private void KeepMenuOnTop()
    {
        if (menuCanvas == null)
        {
            return;
        }

        menuCanvas.overrideSorting = true;
        menuCanvas.sortingOrder = MenuSortingOrder;

        if (menuCanvas.transform != null)
        {
            menuCanvas.transform.SetAsLastSibling();
        }
    }

    private bool WasHotspotClicked(Vector2 centerNormalized, Vector2 sizeNormalized)
    {
        if (!Input.GetMouseButtonDown(0) || backgroundRect == null)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = backgroundRect.rect;
        Vector2 normalizedPoint = new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
            Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

        Vector2 halfSize = sizeNormalized * 0.5f;
        return normalizedPoint.x >= centerNormalized.x - halfSize.x
            && normalizedPoint.x <= centerNormalized.x + halfSize.x
            && normalizedPoint.y >= centerNormalized.y - halfSize.y
            && normalizedPoint.y <= centerNormalized.y + halfSize.y;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex >= 0 ? activeScene.buildIndex : 0);
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static ColorBlock BuildInvisibleButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.pressedColor = new Color(1f, 0.88f, 0.25f, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        return colors;
    }

    private static ColorBlock BuildVisibleButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.25f, 0.73f, 0.05f, 0.96f);
        colors.highlightedColor = new Color(0.36f, 0.86f, 0.08f, 1f);
        colors.pressedColor = new Color(0.18f, 0.52f, 0.04f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.18f, 0.22f, 0.16f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        return colors;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject.name == name && sceneObject.scene.IsValid() && sceneObject.scene.isLoaded)
            {
                return sceneObject;
            }
        }

        return null;
    }

    private static void EnsureBackgroundSprite()
    {
        if (backgroundSprite != null)
        {
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>(BackgroundResourceName);
        if (texture != null)
        {
            backgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return;
        }

        backgroundSprite = Resources.Load<Sprite>(BackgroundResourceName);
    }
}
