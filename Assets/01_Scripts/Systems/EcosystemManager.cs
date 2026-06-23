using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager instancia;

    [Range(0f, 100f)] public float contaminacion = 18f;
    [Range(0f, 100f)] public float limpieza = 55f;
    [Range(0f, 100f)] public float biodiversidad = 0f;
    [Range(0f, 100f)] public float estabilidad = 55f;
    [Range(0f, 100f)] public float riesgoColapso = 0f;
    public int poblacionTotal;

    private TMP_Text textoPanel;
    private float tickTimer;
    private float uiTimer;
    private readonly HashSet<SlimeSpecies> especiesVistas = new HashSet<SlimeSpecies>();
    private readonly Dictionary<SlimeSpecies, int> conteoEspecies = new Dictionary<SlimeSpecies, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<EcosystemManager>() != null)
        {
            return;
        }

        new GameObject("EcosystemManager").AddComponent<EcosystemManager>();
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
        tickTimer += Time.deltaTime;
        uiTimer += Time.deltaTime;

        if (tickTimer >= 2f)
        {
            tickTimer = 0f;
            Recalcular();
        }

        if (uiTimer >= 0.5f)
        {
            uiTimer = 0f;
            ActualizarUI();
        }
    }

    public static void RegistrarEventoAlimentacion(SlimeGenome genome, bool correcta)
    {
        if (instancia == null || genome == null)
        {
            return;
        }

        if (correcta)
        {
            instancia.limpieza = Mathf.Clamp(instancia.limpieza + 0.8f, 0f, 100f);
            if (genome.energiaInterna > 88f)
            {
                instancia.contaminacion = Mathf.Clamp(instancia.contaminacion + 2.4f, 0f, 100f);
                instancia.riesgoColapso = Mathf.Clamp(instancia.riesgoColapso + 3f, 0f, 100f);
            }
        }
        else
        {
            instancia.contaminacion = Mathf.Clamp(instancia.contaminacion + 1.5f, 0f, 100f);
        }
    }

    public static void RegistrarProduccion(SlimeGenome genome, string recurso, int cantidad)
    {
        if (instancia == null || genome == null)
        {
            return;
        }

        float valor = Mathf.Max(1, cantidad);
        switch (genome.especie)
        {
            case SlimeSpecies.Acid:
                instancia.contaminacion = Mathf.Clamp(instancia.contaminacion + valor * 0.45f, 0f, 100f);
                break;
            case SlimeSpecies.Fire:
                instancia.contaminacion = Mathf.Clamp(instancia.contaminacion + valor * 0.30f, 0f, 100f);
                instancia.riesgoColapso = Mathf.Clamp(instancia.riesgoColapso + valor * 0.25f, 0f, 100f);
                break;
            case SlimeSpecies.Cleaner:
                instancia.limpieza = Mathf.Clamp(instancia.limpieza + valor * 0.55f, 0f, 100f);
                instancia.contaminacion = Mathf.Clamp(instancia.contaminacion - valor * 0.55f, 0f, 100f);
                break;
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde:
                instancia.limpieza = Mathf.Clamp(instancia.limpieza + valor * 0.35f, 0f, 100f);
                break;
            case SlimeSpecies.Cold:
                instancia.estabilidad = Mathf.Clamp(instancia.estabilidad + valor * 0.25f, 0f, 100f);
                break;
        }
    }

    public static void RegistrarReproduccion(SlimeGenome origen, SlimeGenome cria, bool mutacion)
    {
        if (instancia == null)
        {
            return;
        }

        instancia.riesgoColapso = Mathf.Clamp(instancia.riesgoColapso + (mutacion ? 5f : 2f), 0f, 100f);
        if (instancia.poblacionTotal > 24)
        {
            instancia.contaminacion = Mathf.Clamp(instancia.contaminacion + 4f, 0f, 100f);
            EcosystemToast.Mostrar("Poblacion alta: la granja necesita equilibrio");
        }
    }

    public bool EstaEnColapso()
    {
        return riesgoColapso >= 80f || contaminacion >= 85f || poblacionTotal > 45;
    }

    private void Recalcular()
    {
        conteoEspecies.Clear();
        especiesVistas.Clear();
        poblacionTotal = 0;

        float ajusteContaminacion = 0f;
        float ajusteLimpieza = 0f;
        float sumaEstabilidadGenetica = 0f;

        foreach (SlimeGenome genome in SlimeGenomeInstaller.Registrados)
        {
            if (genome == null || !genome.isActiveAndEnabled)
            {
                continue;
            }

            poblacionTotal++;
            especiesVistas.Add(genome.especie);
            conteoEspecies.TryGetValue(genome.especie, out int cantidad);
            conteoEspecies[genome.especie] = cantidad + 1;
            sumaEstabilidadGenetica += genome.estabilidadGenetica;

            switch (genome.especie)
            {
                case SlimeSpecies.Acid:
                    ajusteContaminacion += 0.9f;
                    break;
                case SlimeSpecies.Fire:
                    ajusteContaminacion += 0.6f;
                    break;
                case SlimeSpecies.Cleaner:
                    ajusteLimpieza += 0.9f;
                    break;
                case SlimeSpecies.Nature:
                case SlimeSpecies.Verde:
                    ajusteLimpieza += 0.55f;
                    break;
                case SlimeSpecies.Cold:
                    sumaEstabilidadGenetica += 6f;
                    break;
            }

            if (genome.estadoEcologico == SlimeEcologicalState.Inestable || genome.estadoEcologico == SlimeEcologicalState.Sobrealimentado)
            {
                riesgoColapso = Mathf.Clamp(riesgoColapso + 0.8f, 0f, 100f);
            }
        }

        contaminacion = Mathf.Clamp(contaminacion + ajusteContaminacion - ajusteLimpieza * 0.55f - limpieza * 0.015f, 0f, 100f);
        limpieza = Mathf.Clamp(limpieza + ajusteLimpieza * 0.35f - contaminacion * 0.015f, 0f, 100f);
        biodiversidad = Mathf.Clamp(especiesVistas.Count * 16.7f, 0f, 100f);

        float estabilidadGenetica = poblacionTotal > 0 ? sumaEstabilidadGenetica / poblacionTotal : 50f;
        float presionPoblacion = Mathf.Max(0, poblacionTotal - 18) * 1.8f;
        estabilidad = Mathf.Clamp(45f + limpieza * 0.32f + biodiversidad * 0.25f + estabilidadGenetica * 0.20f - contaminacion * 0.45f - presionPoblacion, 0f, 100f);
        riesgoColapso = Mathf.Clamp(riesgoColapso + contaminacion * 0.015f + presionPoblacion * 0.08f - estabilidad * 0.018f, 0f, 100f);

        RevisarEspeciesEnRiesgo();
        RevisarAvisos();
    }

    private void RevisarEspeciesEnRiesgo()
    {
        SlimeSpecies[] importantes =
        {
            SlimeSpecies.Fire,
            SlimeSpecies.Acid,
            SlimeSpecies.Cold,
            SlimeSpecies.Cleaner,
            SlimeSpecies.Nature,
            SlimeSpecies.Verde,
            SlimeSpecies.Amarillo
        };

        foreach (SlimeSpecies especie in importantes)
        {
            conteoEspecies.TryGetValue(especie, out int cantidad);
            if (cantidad == 0 && poblacionTotal > 0)
            {
                BiologicalResearchSystem.RegistrarEspecieEnRiesgo(especie);
            }
        }
    }

    private void RevisarAvisos()
    {
        if (EstaEnColapso())
        {
            EcosystemToast.Mostrar("ALERTA: colapso ecologico cerca");
            ZonaFriaBufandaSistema.AplicarDanoGlobal(2, "EL ECOSISTEMA ESTA INESTABLE");
        }
        else if (contaminacion > 60f)
        {
            EcosystemToast.Mostrar("Contaminacion alta: usa Cleaner o Nature Slimes");
        }
        else if (poblacionTotal > 32)
        {
            EcosystemToast.Mostrar("Demasiados slimes: sube el riesgo ecologico");
        }
        else if (contaminacion < 30f && estabilidad > 65f)
        {
            BiologicalResearchSystem.RegistrarEstabilizacion();
        }
    }

    private void CrearUI()
    {
        Canvas canvas = BuscarOCrearCanvas();
        GameObject panel = new GameObject("Panel_Ecosistema", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -170f);
        rect.sizeDelta = new Vector2(260f, 116f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.07f, 0.08f, 0.78f);
        image.raycastTarget = false;

        Shadow shadow = panel.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GameObject textGo = new GameObject("Txt_Ecosistema", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        textoPanel = textGo.GetComponent<TMP_Text>();
        textoPanel.fontSize = 15f;
        textoPanel.enableAutoSizing = true;
        textoPanel.fontSizeMin = 9f;
        textoPanel.alignment = TextAlignmentOptions.TopLeft;
        textoPanel.color = new Color(0.86f, 1f, 0.88f, 1f);
        textoPanel.raycastTarget = false;
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoPanel == null)
        {
            return;
        }

        textoPanel.text =
            "ECOSISTEMA\n" +
            "Estabilidad: " + Mathf.RoundToInt(estabilidad) + "\n" +
            "Contaminacion: " + Mathf.RoundToInt(contaminacion) + "\n" +
            "Biodiversidad: " + especiesVistas.Count + " esp.\n" +
            "Poblacion: " + poblacionTotal + "  Riesgo: " + Mathf.RoundToInt(riesgoColapso);
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
