using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efecto visual tipo Pokemon para evoluciones de slime.
/// Genera una Canvas de overlay en runtime con:
///   - Flashes blancos pulsantes
///   - Texto "¡[nombre] está evolucionando!"
///   - Texto final "¡[nombre] evolucionó a [evolucionado]!"
///
/// Uso:
///   PokemonEvolutionFX.Play(duration, fromName, toName, onComplete);
/// </summary>
public class PokemonEvolutionFX : MonoBehaviour
{
    // ── Singleton ligero ──────────────────────────────────────────────────────
    private static PokemonEvolutionFX _instance;
    private static PokemonEvolutionFX Instance
    {
        get
        {
            if (_instance != null) return _instance;
            GameObject go = new GameObject("[PokemonEvolutionFX]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PokemonEvolutionFX>();
            return _instance;
        }
    }

    // ── UI Runtime ────────────────────────────────────────────────────────────
    private Canvas canvas;
    private Image flashOverlay;
    private Text mainText;
    private Text subText;
    private bool isPlaying;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        BuildCanvas();
    }

    // ── API Publica ───────────────────────────────────────────────────────────

    /// <summary>
    /// Lanza el efecto de evolucion tipo Pokemon.
    /// </summary>
    /// <param name="duration">Cuantos segundos dura el efecto (igual que evolutionDuration).</param>
    /// <param name="fromName">Nombre del slime base, ej: "Cleaner Slime".</param>
    /// <param name="toName">Nombre del slime evolucionado, ej: "Purificador".</param>
    /// <param name="onComplete">Callback cuando el efecto termina (opcional).</param>
    public static void Play(float duration, string fromName, string toName, System.Action onComplete = null)
    {
        Instance.StartCoroutine(Instance.RunEffect(duration, fromName, toName, onComplete));
    }

    // ── Coroutine Principal ───────────────────────────────────────────────────

    private IEnumerator RunEffect(float duration, string fromName, string toName, System.Action onComplete)
    {
        if (isPlaying) yield break;
        isPlaying = true;

        canvas.enabled = true;
        mainText.text = $"¡{fromName}\nestá evolucionando!";
        subText.text = "";
        SetFlash(0f);
        SetTextAlpha(mainText, 0f);
        SetTextAlpha(subText, 0f);

        // ── Fase 1: Aparece el texto (0 ~ 15% del tiempo) ────────────────────
        float fadeDur = duration * 0.12f;
        yield return FadeText(mainText, 0f, 1f, fadeDur);

        // ── Fase 2: Flashes blancos pulsantes ─────────────────────────────────
        float flashPhase = duration * 0.70f;
        float elapsed = 0f;
        float flashFreq = 3.5f; // flashes por segundo

        while (elapsed < flashPhase)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashPhase;
            float flash = Mathf.Abs(Mathf.Sin(elapsed * flashFreq * Mathf.PI));
            // Flashes se intensifican al 60%, luego se suavizan
            float envelope = t < 0.6f
                ? Mathf.Lerp(0.35f, 0.85f, t / 0.6f)
                : Mathf.Lerp(0.85f, 0.5f, (t - 0.6f) / 0.4f);
            SetFlash(flash * envelope);
            yield return null;
        }

        // ── Fase 3: Gran flash blanco final ───────────────────────────────────
        yield return FadeFlash(0f, 1f, 0.18f);
        yield return new WaitForSeconds(0.08f);

        // ── Fase 4: Revelar texto de evolucion completada ─────────────────────
        SetTextAlpha(mainText, 0f);
        subText.text = $"¡{fromName} evolucionó\na {toName}!";
        SetFlash(0.92f);
        yield return FadeFlash(0.92f, 0f, 0.32f);
        yield return FadeText(subText, 0f, 1f, 0.22f);
        yield return new WaitForSeconds(1.4f);

        // ── Fase 5: Fade out ──────────────────────────────────────────────────
        yield return FadeText(subText, 1f, 0f, 0.3f);
        canvas.enabled = false;
        isPlaying = false;

        onComplete?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator FadeFlash(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetFlash(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetFlash(to);
    }

    private IEnumerator FadeText(Text txt, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetTextAlpha(txt, Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetTextAlpha(txt, to);
    }

    private void SetFlash(float alpha)
    {
        Color c = flashOverlay.color;
        c.a = Mathf.Clamp01(alpha);
        flashOverlay.color = c;
    }

    private void SetTextAlpha(Text txt, float alpha)
    {
        Color c = txt.color;
        c.a = Mathf.Clamp01(alpha);
        txt.color = c;
    }

    // ── Construccion del Canvas en Runtime ────────────────────────────────────

    private void BuildCanvas()
    {
        // Canvas raiz
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Flash overlay (cubre toda la pantalla)
        GameObject flashGO = new GameObject("FlashOverlay");
        flashGO.transform.SetParent(canvas.transform, false);
        flashOverlay = flashGO.AddComponent<Image>();
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        RectTransform frt = flashOverlay.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // Texto principal centrado
        mainText = BuildText("MainEvText", 30, new Color(0.06f, 0.04f, 0.14f, 0f),
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));

        // Texto de resultado (un poco mas abajo)
        subText = BuildText("SubEvText", 32, new Color(0.06f, 0.04f, 0.14f, 0f),
            new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.58f));

        canvas.enabled = false;
    }

    private Text BuildText(string name, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);

        // Sombra
        GameObject shadow = new GameObject(name + "_Shadow");
        shadow.transform.SetParent(go.transform, false);
        Text shadowTxt = shadow.AddComponent<Text>();
        shadowTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shadowTxt.fontSize = fontSize;
        shadowTxt.fontStyle = FontStyle.Bold;
        shadowTxt.alignment = TextAnchor.MiddleCenter;
        shadowTxt.color = new Color(0f, 0f, 0f, 0f);
        RectTransform srt = shadowTxt.rectTransform;
        srt.anchorMin = anchorMin;
        srt.anchorMax = anchorMax;
        srt.offsetMin = new Vector2(3f, -3f);
        srt.offsetMax = new Vector2(3f, -3f);

        Text txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        RectTransform rt = txt.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return txt;
    }
}
