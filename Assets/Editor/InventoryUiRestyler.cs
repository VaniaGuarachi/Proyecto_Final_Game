using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InventoryUiRestyler
{
    private const string MenuPath = "Tools/FINAL_GAME/Redisenar UI Inventarios";
    private const string UiFolder = "Assets/03_Sprites/UI";
    private const string PanelSpritePath = UiFolder + "/hud_panel.png";
    private const string SlotSpritePath = UiFolder + "/hud_slot.png";
    private const string DotSpritePath = UiFolder + "/hud_dot.png";
    private const string AutoRunMarkerPath = "Assets/Editor/.apply_inventory_ui_restyle";

    private static readonly Color PanelFill = new Color(0.055f, 0.075f, 0.105f, 0.88f);
    private static readonly Color PanelBorder = new Color(0.96f, 0.78f, 0.36f, 0.92f);
    private static readonly Color SlotFill = new Color(0.11f, 0.145f, 0.17f, 0.78f);
    private static readonly Color SlotBorder = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color TextColor = new Color(0.96f, 0.98f, 0.92f, 1f);
    private static readonly Color MutedTextColor = new Color(0.78f, 0.86f, 0.82f, 1f);

    [InitializeOnLoadMethod]
    private static void RunOnceAfterReload()
    {
        if (!File.Exists(AutoRunMarkerPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoRunMarkerPath))
            {
                return;
            }

            File.Delete(AutoRunMarkerPath);
            RestyleInventoryUi();
            AssetDatabase.Refresh();
        };
    }

    [MenuItem(MenuPath)]
    public static void RestyleInventoryUi()
    {
        Sprite panelSprite = EnsureSprite(PanelSpritePath, PanelFill, PanelBorder, 96, 14, 3);
        Sprite slotSprite = EnsureSprite(SlotSpritePath, SlotFill, SlotBorder, 64, 10, 2);
        Sprite dotSprite = EnsureSprite(DotSpritePath, Color.white, Color.white, 32, 14, 0);

        GameObject canvas = FindSceneObject("Canvas_UI");
        if (canvas == null)
        {
            Debug.LogWarning("No se encontro Canvas_UI para redisenar la interfaz.");
            return;
        }

        ConfigureCanvas(canvas);
        StyleSeedsPanel(FindSceneObject("Panel_Semillas"), panelSprite, slotSprite, dotSprite);
        StyleSidePanel(FindSceneObject("Panel_Cosechas"), "COSECHAS", panelSprite, slotSprite, new[]
        {
            new HudItem("Hoja", FindSceneComponent<UICosechas>()?.textoHojaVerde, new Color(0.30f, 0.86f, 0.34f, 1f)),
            new HudItem("Gota", FindSceneComponent<UICosechas>()?.textoGotaAzul, new Color(0.28f, 0.68f, 1f, 1f)),
            new HudItem("Sol", FindSceneComponent<UICosechas>()?.textoEnergiaSolar, new Color(1f, 0.82f, 0.20f, 1f)),
            new HudItem("Nectar", FindSceneComponent<UICosechas>()?.textoNectarMorado, new Color(0.78f, 0.42f, 1f, 1f)),
        }, new Vector2(20f, -110f));

        GameObject resourcesPanel = FindSceneObject("Panel_RecursosSmiles") ?? FindSceneObjectByPrefix("Panel_RecursosSmiles");
        StyleSidePanel(resourcesPanel, "RECURSOS", panelSprite, slotSprite, BuildResourceItems(resourcesPanel), new Vector2(20f, -350f));

        StyleSeedSelectionMenu(FindSceneObject("Panel_SeleccionSemillas"), panelSprite, slotSprite);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("UI de inventarios redisenada y escena guardada.");
    }

    private static void ConfigureCanvas(GameObject canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void StyleSeedsPanel(GameObject panel, Sprite panelSprite, Sprite slotSprite, Sprite dotSprite)
    {
        if (panel == null)
        {
            Debug.LogWarning("No se encontro Panel_Semillas.");
            return;
        }

        RectTransform rect = panel.GetComponent<RectTransform>();
        SetAnchoredRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(660f, 74f));
        StylePanelImage(panel, panelSprite);
        AddShadow(panel, new Vector2(4f, -5f), 0.45f);

        TMP_Text title = EnsureText(panel.transform, "UI_Titulo_Semillas", "SEMILLAS");
        SetAnchoredRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(110f, 34f));
        StyleTitle(title);

        RectTransform content = EnsureRect(panel.transform, "UI_Slots_Semillas");
        SetAnchoredRect(content, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(-154f, -20f));
        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(0, 12, 8, 8);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        UIInventarioSemillas ui = FindSceneComponent<UIInventarioSemillas>();
        var items = new[]
        {
            new HudItem("Verde", ui?.textoSemillaVerde, new Color(0.30f, 0.86f, 0.34f, 1f)),
            new HudItem("Azul", ui?.textoSemillaAzul, new Color(0.28f, 0.68f, 1f, 1f)),
            new HudItem("Amarilla", ui?.textoSemillaAmarilla, new Color(1f, 0.82f, 0.20f, 1f)),
            new HudItem("Morada", ui?.textoSemillaMorada, new Color(0.78f, 0.42f, 1f, 1f)),
        };

        foreach (HudItem item in items)
        {
            if (item.Text == null)
            {
                continue;
            }

            RectTransform slot = EnsureSlot(content, "Slot_Semilla_" + item.Name, slotSprite, item.Accent, false);
            EnsureDot(slot, "Icono_" + item.Name, dotSprite, item.Accent);
            PrepareText(item.Text, slot, 16f, TextAlignmentOptions.MidlineLeft, true);
            LayoutElement textLayout = EnsureComponent<LayoutElement>(item.Text.gameObject);
            textLayout.preferredWidth = 92f;
        }
    }

    private static void StyleSidePanel(GameObject panel, string titleText, Sprite panelSprite, Sprite slotSprite, HudItem[] items, Vector2 position)
    {
        if (panel == null)
        {
            Debug.LogWarning("No se encontro el panel " + titleText + ".");
            return;
        }

        RectTransform rect = panel.GetComponent<RectTransform>();
        SetAnchoredRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(214f, 218f));
        StylePanelImage(panel, panelSprite);
        AddShadow(panel, new Vector2(4f, -5f), 0.45f);

        TMP_Text title = EnsureText(panel.transform, "UI_Titulo_" + titleText, titleText);
        SetAnchoredRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -23f), new Vector2(-26f, 30f));
        StyleTitle(title);

        RectTransform content = EnsureRect(panel.transform, "UI_Slots_" + titleText);
        SetAnchoredRect(content, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-24f, -64f));
        VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        List<Image> availableIcons = panel.GetComponentsInChildren<Image>(true)
            .Where(image => image != null && image.gameObject != panel && !image.name.StartsWith("UI_") && !image.name.StartsWith("Slot_"))
            .OrderBy(GetVisualOrder)
            .ToList();

        for (int i = 0; i < items.Length; i++)
        {
            HudItem item = items[i];
            if (item.Text == null)
            {
                continue;
            }

            RectTransform slot = EnsureSlot(content, "Slot_" + titleText + "_" + item.Name, slotSprite, item.Accent, true);
            Image icon = FindChildImage(slot, "Icono_" + item.Name);
            if (icon == null && i < availableIcons.Count)
            {
                icon = availableIcons[i];
                icon.name = "Icono_" + item.Name;
            }

            if (icon != null)
            {
                icon.transform.SetParent(slot, false);
                icon.color = Color.white;
                icon.preserveAspect = true;
                LayoutElement iconLayout = EnsureComponent<LayoutElement>(icon.gameObject);
                iconLayout.preferredWidth = 30f;
                iconLayout.preferredHeight = 30f;
                icon.raycastTarget = false;
            }

            PrepareText(item.Text, slot, 18f, TextAlignmentOptions.MidlineLeft, false);
            LayoutElement textLayout = EnsureComponent<LayoutElement>(item.Text.gameObject);
            textLayout.preferredWidth = 100f;
        }
    }

    private static void StyleSeedSelectionMenu(GameObject panel, Sprite panelSprite, Sprite slotSprite)
    {
        if (panel == null)
        {
            return;
        }

        Image overlay = panel.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = new Color(0.02f, 0.025f, 0.03f, 0.48f);
        }

        RectTransform dialog = EnsureRect(panel.transform, "UI_Dialogo_SeleccionSemillas");
        SetAnchoredRect(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 330f));
        StylePanelImage(dialog.gameObject, panelSprite);
        AddShadow(dialog.gameObject, new Vector2(4f, -5f), 0.45f);

        TMP_Text title = EnsureText(dialog, "UI_Titulo_SeleccionSemillas", "PLANTAR SEMILLA");
        SetAnchoredRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-34f, 34f));
        StyleTitle(title);

        RectTransform content = EnsureRect(dialog, "UI_Botones_SeleccionSemillas");
        SetAnchoredRect(content, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(-54f, -86f));
        VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        MenuSeleccionSemillas menu = FindSceneComponent<MenuSeleccionSemillas>();
        TMP_Text[] texts =
        {
            menu?.textoBotonVerde,
            menu?.textoBotonAzul,
            menu?.textoBotonAmarilla,
            menu?.textoBotonMorada,
        };

        foreach (TMP_Text text in texts.Where(t => t != null))
        {
            Button button = text.GetComponentInParent<Button>(true);
            if (button == null)
            {
                continue;
            }

            button.transform.SetParent(content, false);
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = slotSprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.12f, 0.16f, 0.18f, 0.95f);
            }

            LayoutElement buttonLayout = EnsureComponent<LayoutElement>(button.gameObject);
            buttonLayout.minHeight = 48f;
            buttonLayout.preferredHeight = 48f;
            buttonLayout.preferredWidth = 300f;
            PrepareText(text, button.transform, 18f, TextAlignmentOptions.Center, false);
        }

        StyleCloseButton(panel, dialog, slotSprite);
    }

    private static void StyleCloseButton(GameObject panel, RectTransform dialog, Sprite slotSprite)
    {
        Button closeButton = panel.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.name == "Boton_Cerrar");

        if (closeButton == null)
        {
            return;
        }

        closeButton.transform.SetParent(dialog, false);
        closeButton.transform.SetAsLastSibling();

        RectTransform buttonRect = closeButton.GetComponent<RectTransform>();
        SetAnchoredRect(buttonRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(34f, 34f));

        Image image = closeButton.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = slotSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.20f, 0.08f, 0.08f, 0.96f);
            image.raycastTarget = true;
            closeButton.targetGraphic = image;
        }

        LayoutElement layout = EnsureComponent<LayoutElement>(closeButton.gameObject);
        layout.ignoreLayout = true;

        TMP_Text label = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "X";
            PrepareText(label, closeButton.transform, 20f, TextAlignmentOptions.Center, false);
            label.color = TextColor;
            label.fontStyle = FontStyles.Bold;
        }
    }

    private static HudItem[] BuildResourceItems(GameObject panel)
    {
        UIRecursosSmiles ui = FindSceneComponent<UIRecursosSmiles>();
        TMP_Text[] looseTexts = FindLooseTexts(panel, 4);

        return new[]
        {
            new HudItem("Gel", ui?.textoGelVerde ?? looseTexts.ElementAtOrDefault(0), new Color(0.33f, 0.95f, 0.42f, 1f)),
            new HudItem("Gotas", ui?.textoGotasAzul ?? looseTexts.ElementAtOrDefault(1), new Color(0.35f, 0.78f, 1f, 1f)),
            new HudItem("Energia", ui?.textoEnergiaSolar ?? looseTexts.ElementAtOrDefault(2), new Color(1f, 0.86f, 0.22f, 1f)),
            new HudItem("Esencia", ui?.textoNectarMorado ?? looseTexts.ElementAtOrDefault(3), new Color(0.78f, 0.46f, 1f, 1f)),
        };
    }

    private static TMP_Text[] FindLooseTexts(GameObject panel, int count)
    {
        if (panel == null)
        {
            return new TMP_Text[0];
        }

        return panel.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => text != null && !text.name.StartsWith("UI_Titulo") && text.text != "RECURSOS")
            .OrderBy(GetVisualOrder)
            .Take(count)
            .ToArray();
    }

    private static int GetVisualOrder(Component component)
    {
        Transform parent = component.transform.parent;
        if (parent != null && parent.name.StartsWith("Slot_"))
        {
            return parent.GetSiblingIndex();
        }

        return component.transform.GetSiblingIndex();
    }

    private static RectTransform EnsureSlot(RectTransform parent, string name, Sprite sprite, Color accent, bool sidePanel)
    {
        RectTransform slot = EnsureRect(parent, name);
        Image image = EnsureComponent<Image>(slot.gameObject);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(slot.gameObject);
        layout.padding = sidePanel ? new RectOffset(12, 12, 5, 5) : new RectOffset(10, 10, 5, 5);
        layout.spacing = sidePanel ? 9f : 7f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement slotLayout = EnsureComponent<LayoutElement>(slot.gameObject);
        slotLayout.minHeight = sidePanel ? 36f : 42f;
        slotLayout.preferredHeight = sidePanel ? 36f : 42f;

        Outline outline = EnsureComponent<Outline>(slot.gameObject);
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        return slot;
    }

    private static void EnsureDot(RectTransform parent, string name, Sprite sprite, Color color)
    {
        RectTransform dot = EnsureRect(parent, name);
        Image image = EnsureComponent<Image>(dot.gameObject);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        LayoutElement layout = EnsureComponent<LayoutElement>(dot.gameObject);
        layout.preferredWidth = 18f;
        layout.preferredHeight = 18f;
    }

    private static void PrepareText(TMP_Text text, Transform parent, float fontSize, TextAlignmentOptions alignment, bool compact)
    {
        text.transform.SetParent(parent, false);
        text.color = compact ? TextColor : MutedTextColor;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = compact ? 10f : 12f;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        AddTextShadow(text.gameObject);
    }

    private static void StyleTitle(TMP_Text title)
    {
        title.color = TextColor;
        title.fontSize = 17f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 12f;
        title.fontSizeMax = 18f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        AddTextShadow(title.gameObject);
    }

    private static void StylePanelImage(GameObject panel, Sprite sprite)
    {
        Image image = EnsureComponent<Image>(panel);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void AddShadow(GameObject target, Vector2 distance, float alpha)
    {
        Shadow shadow = EnsureComponent<Shadow>(target);
        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddTextShadow(GameObject target)
    {
        Shadow shadow = EnsureComponent<Shadow>(target);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        shadow.useGraphicAlpha = true;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string text)
    {
        RectTransform existing = parent.Find(name) as RectTransform;
        TMP_Text label = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (label == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            label = go.GetComponent<TextMeshProUGUI>();
        }

        label.text = text;
        return label;
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<RectTransform>();
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image FindChildImage(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    private static GameObject FindSceneObject(string name)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == name && go.scene.IsValid() && go.scene.isLoaded);
    }

    private static GameObject FindSceneObjectByPrefix(string prefix)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name.StartsWith(prefix) && go.scene.IsValid() && go.scene.isLoaded);
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(component => component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded);
    }

    private static Sprite EnsureSprite(string path, Color fill, Color border, int size, int radius, int borderWidth)
    {
        if (!Directory.Exists(UiFolder))
        {
            Directory.CreateDirectory(UiFolder);
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float half = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - center) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - center) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = radius - dist;

                if (edge < 0f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else if (borderWidth > 0 && edge < borderWidth)
                {
                    texture.SetPixel(x, y, border);
                }
                else
                {
                    texture.SetPixel(x, y, fill);
                }
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private readonly struct HudItem
    {
        public HudItem(string name, TMP_Text text, Color accent)
        {
            Name = name;
            Text = text;
            Accent = accent;
        }

        public string Name { get; }
        public TMP_Text Text { get; }
        public Color Accent { get; }
    }
}
