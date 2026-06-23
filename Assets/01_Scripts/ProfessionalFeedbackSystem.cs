using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfessionalFeedbackSystem : MonoBehaviour
{
    private static ProfessionalFeedbackSystem instancia;
    private static Sprite particleSprite;

    private Canvas canvas;
    private RectTransform toastRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<ProfessionalFeedbackSystem>() != null)
        {
            return;
        }

        new GameObject("ProfessionalFeedbackSystem").AddComponent<ProfessionalFeedbackSystem>();
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

    public static void Toast(string mensaje, Color color)
    {
        Ensure().MostrarToast(mensaje, color);
    }

    public static void WorldText(Vector3 posicion, string texto, Color color)
    {
        Ensure().MostrarTextoMundo(posicion, texto, color);
    }

    public static void Burst(Vector3 posicion, Color color, int cantidad = 12)
    {
        Ensure().CrearBurst(posicion, color, cantidad);
    }

    public static void Pulse(Transform target, float intensidad = 1.08f)
    {
        if (target == null)
        {
            return;
        }

        Ensure().StartCoroutine(PulseRoutine(target, intensidad));
    }

    private static ProfessionalFeedbackSystem Ensure()
    {
        if (instancia == null)
        {
            instancia = new GameObject("ProfessionalFeedbackSystem").AddComponent<ProfessionalFeedbackSystem>();
        }

        return instancia;
    }

    private void MostrarToast(string mensaje, Color color)
    {
        CrearCanvasSiHaceFalta();

        GameObject toast = new GameObject("Toast_Feedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Shadow));
        toast.transform.SetParent(toastRoot, false);
        toast.transform.SetAsLastSibling();

        RectTransform rect = toast.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -12f);
        rect.sizeDelta = new Vector2(560f, 46f);

        Image fondo = toast.GetComponent<Image>();
        fondo.color = new Color(0.035f, 0.045f, 0.06f, 0.92f);
        fondo.raycastTarget = false;

        Shadow sombra = toast.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.65f);
        sombra.effectDistance = new Vector2(3f, -3f);

        TMP_Text label = CrearTexto(toast.transform, "Texto_Toast", mensaje, 18f, color);
        ConfigurarRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-28f, -8f));

        StartCoroutine(ToastRoutine(toast.GetComponent<CanvasGroup>(), rect));
    }

    private void MostrarTextoMundo(Vector3 posicion, string texto, Color color)
    {
        CrearCanvasSiHaceFalta();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Toast(texto, color);
            return;
        }

        GameObject go = new GameObject("WorldText_Feedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, posicion);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPoint, null, out Vector2 localPoint);
        rect.anchoredPosition = localPoint;
        rect.sizeDelta = new Vector2(220f, 42f);

        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = texto;
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;

        StartCoroutine(WorldTextRoutine(go.GetComponent<CanvasGroup>(), rect));
    }

    private void CrearBurst(Vector3 posicion, Color color, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            GameObject particle = new GameObject("Particle_Feedback", typeof(SpriteRenderer));
            particle.name = "Particle_Feedback";
            particle.transform.position = posicion + new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(0f, 0.28f), 0f);
            particle.transform.localScale = Vector3.one * Random.Range(0.035f, 0.075f);

            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
            renderer.sprite = ObtenerSpriteParticula();
            renderer.color = color;
            renderer.sortingOrder = 320;

            StartCoroutine(ParticleRoutine(particle.transform, renderer, Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.4f)));
        }
    }

    private void CrearCanvasSiHaceFalta()
    {
        if (canvas != null && toastRoot != null)
        {
            return;
        }

        GameObject canvasObject = GameObject.Find("Canvas_UI");
        canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas == null)
        {
            canvasObject = new GameObject("Canvas_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
        }

        GameObject rootObject = GameObject.Find("Toast_Feedback_Root");
        if (rootObject == null)
        {
            rootObject = new GameObject("Toast_Feedback_Root", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
        }

        toastRoot = rootObject.GetComponent<RectTransform>();
        toastRoot.anchorMin = new Vector2(0.5f, 1f);
        toastRoot.anchorMax = new Vector2(0.5f, 1f);
        toastRoot.pivot = new Vector2(0.5f, 1f);
        toastRoot.anchoredPosition = new Vector2(0f, -112f);
        toastRoot.sizeDelta = new Vector2(600f, 220f);
    }

    private static TMP_Text CrearTexto(Transform parent, string nombre, string texto, float tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = texto;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = tamano;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static System.Collections.IEnumerator ToastRoutine(CanvasGroup group, RectTransform rect)
    {
        float t = 0f;
        Vector2 start = rect.anchoredPosition + new Vector2(0f, 18f);
        Vector2 end = rect.anchoredPosition;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.22f);
            group.alpha = k;
            rect.anchoredPosition = Vector2.Lerp(start, end, k);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.6f);

        t = 0f;
        while (t < 0.28f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / 0.28f);
            rect.anchoredPosition = end + new Vector2(0f, t * 28f);
            yield return null;
        }

        Destroy(group.gameObject);
    }

    private static System.Collections.IEnumerator WorldTextRoutine(CanvasGroup group, RectTransform rect)
    {
        float t = 0f;
        Vector2 start = rect.anchoredPosition;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.9f);
            group.alpha = 1f - k;
            rect.anchoredPosition = start + new Vector2(0f, 48f * k);
            rect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        Destroy(group.gameObject);
    }

    private static System.Collections.IEnumerator ParticleRoutine(Transform particle, SpriteRenderer renderer, Vector2 velocity)
    {
        float t = 0f;
        Vector3 start = particle.position;
        while (t < 0.65f && particle != null)
        {
            t += Time.deltaTime;
            Vector3 offset = new Vector3(velocity.x * t, velocity.y * t + Mathf.Sin(t * Mathf.PI) * 0.28f, 0f);
            particle.position = start + offset;
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f - Mathf.Clamp01(t / 0.65f);
                renderer.color = color;
            }
            yield return null;
        }

        if (particle != null)
        {
            Destroy(particle.gameObject);
        }
    }

    private static Sprite ObtenerSpriteParticula()
    {
        if (particleSprite != null)
        {
            return particleSprite;
        }

        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float dx = x - 3.5f;
                float dy = y - 3.5f;
                texture.SetPixel(x, y, dx * dx + dy * dy <= 12f ? Color.white : clear);
            }
        }

        texture.Apply();
        particleSprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        particleSprite.name = "FeedbackParticleSprite";
        return particleSprite;
    }

    private static System.Collections.IEnumerator PulseRoutine(Transform target, float intensidad)
    {
        Vector3 original = target.localScale;
        float t = 0f;
        while (t < 0.22f && target != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / 0.22f) * Mathf.PI);
            target.localScale = original * Mathf.Lerp(1f, intensidad, k);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = original;
        }
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
