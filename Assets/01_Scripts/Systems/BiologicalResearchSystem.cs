using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiologicalResearchSystem : MonoBehaviour
{
    private static BiologicalResearchSystem instancia;
    private readonly Queue<string> recientes = new Queue<string>();
    private readonly HashSet<string> descubiertos = new HashSet<string>();
    private TMP_Text texto;
    private float ocultarSiVacioTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<BiologicalResearchSystem>() != null)
        {
            return;
        }

        new GameObject("BiologicalResearchSystem").AddComponent<BiologicalResearchSystem>();
    }

    private void Awake()
    {
        instancia = this;
    }

    private void Start()
    {
        CrearUI();
    }

    private void Update()
    {
        if (texto == null || recientes.Count > 0)
        {
            return;
        }

        ocultarSiVacioTimer += Time.deltaTime;
        if (ocultarSiVacioTimer > 8f)
        {
            texto.transform.parent.gameObject.SetActive(false);
        }
    }

    public static void RegistrarAlimentacion(SlimeGenome genome, bool correcta)
    {
        if (genome == null)
        {
            return;
        }

        if (correcta)
        {
            Registrar("Dieta compatible: " + NombreEspecie(genome.especie));
        }
    }

    public static void RegistrarEvolucion(SlimeGenome genome)
    {
        if (genome != null)
        {
            Registrar("Evolucion observada: " + NombreEspecie(genome.especie));
        }
    }

    public static void RegistrarMutacion(SlimeGenome genome)
    {
        if (genome != null)
        {
            Registrar("Mutacion registrada: " + NombreEspecie(genome.especie));
        }
    }

    public static void RegistrarBioma(SlimeBiomeType bioma)
    {
        if (bioma != SlimeBiomeType.Neutral)
        {
            Registrar("Bioma descubierto: " + NombreBioma(bioma));
        }
    }

    public static void RegistrarEstabilizacion()
    {
        Registrar("Contaminacion estabilizada");
    }

    public static void RegistrarEspecieEnRiesgo(SlimeSpecies especie)
    {
        if (especie != SlimeSpecies.Desconocido)
        {
            Registrar("Especie en riesgo: " + NombreEspecie(especie));
        }
    }

    private static void Registrar(string descubrimiento)
    {
        if (string.IsNullOrWhiteSpace(descubrimiento))
        {
            return;
        }

        if (instancia == null)
        {
            GameObject go = new GameObject("BiologicalResearchSystem");
            instancia = go.AddComponent<BiologicalResearchSystem>();
        }

        if (!instancia.descubiertos.Add(descubrimiento))
        {
            return;
        }

        instancia.recientes.Enqueue(descubrimiento);
        while (instancia.recientes.Count > 5)
        {
            instancia.recientes.Dequeue();
        }

        instancia.ActualizarUI();
        EcosystemToast.Mostrar("Investigacion: " + descubrimiento);
    }

    private void CrearUI()
    {
        Canvas canvas = BuscarOCrearCanvas();
        GameObject panel = new GameObject("Panel_Investigacion_Biologica", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -296f);
        rect.sizeDelta = new Vector2(260f, 104f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.065f, 0.055f, 0.09f, 0.78f);
        image.raycastTarget = false;

        Shadow shadow = panel.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GameObject textGo = new GameObject("Txt_Investigacion_Biologica", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 8f);
        textRect.offsetMax = new Vector2(-10f, -8f);

        texto = textGo.GetComponent<TMP_Text>();
        texto.fontSize = 14f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 8f;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.color = new Color(0.95f, 0.88f, 1f, 1f);
        texto.raycastTarget = false;

        panel.SetActive(false);
    }

    private void ActualizarUI()
    {
        if (texto == null)
        {
            CrearUI();
        }

        texto.transform.parent.gameObject.SetActive(true);
        ocultarSiVacioTimer = 0f;
        texto.text = "INVESTIGACION\n";
        foreach (string item in recientes)
        {
            texto.text += "- " + item + "\n";
        }
    }

    private static string NombreEspecie(SlimeSpecies especie)
    {
        switch (especie)
        {
            case SlimeSpecies.Fire: return "FireSlime";
            case SlimeSpecies.Acid: return "AcidSlime";
            case SlimeSpecies.Cold: return "ColdSlime";
            case SlimeSpecies.Cleaner: return "CleanerSlime";
            case SlimeSpecies.Nature: return "NatureSlime";
            case SlimeSpecies.Verde: return "Smile Verde";
            case SlimeSpecies.Amarillo: return "Smile Amarillo";
            default: return "Slime desconocido";
        }
    }

    private static string NombreBioma(SlimeBiomeType bioma)
    {
        switch (bioma)
        {
            case SlimeBiomeType.Fertil: return "fertil";
            case SlimeBiomeType.Humedo: return "humedo";
            case SlimeBiomeType.Toxico: return "toxico";
            case SlimeBiomeType.Volcanico: return "volcanico";
            case SlimeBiomeType.Frio: return "frio";
            case SlimeBiomeType.Cristalino: return "cristalino";
            case SlimeBiomeType.Ruinas: return "ruinas";
            default: return "neutral";
        }
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
