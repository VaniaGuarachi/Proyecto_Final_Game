using System.Collections;
using UnityEngine;

public class MacetaCultivo : MonoBehaviour
{
    private const int EtapasCrecimiento = 12;

    [Header("Estado de la maceta")]
    public bool jugadorDentro;
    public bool tieneSemilla;
    public bool plantaLista;
    public bool usarOrdenSiembra;
    public int ordenSiembra = -1;

    [Header("Semilla plantada")]
    public TipoSemilla semillaPlantada;

    [Header("Tiempo de crecimiento")]
    public float tiempoCrecimiento = 5f;
    public KeyCode teclaUso = KeyCode.T;
    public float distanciaUsoFallback = 3.2f;
    public bool plantarPrimeraSemillaConT = false;

    [Header("Restriccion local de semillas")]
    public bool limitarSemillasPermitidas = false;
    public bool permitirVerde = true;
    public bool permitirAzul = true;
    public bool permitirAmarilla = true;
    public bool permitirMorada = true;

    [Header("Visual de la planta")]
    public GameObject plantaVisual;
    public SpriteRenderer plantaSpriteRenderer;
    public bool plantarEnPosicionJugador;
    public bool usarDistanciaHorizontalParaInteractuar;
    public Vector2 offsetVisualAlPlantar = new Vector2(0f, 0.25f);
    public bool preferirSiPuedeAbrirMenu;

    [Header("Animacion de crecimiento")]
    [Range(0.01f, 0.3f)] public float escalaInicial = 0.06f;
    [Range(0.35f, 0.85f)] public float momentoCambioAPlantaGrande = 0.62f;
    [Range(0f, 12f)] public float balanceoGrados = 3.5f;
    [Range(0f, 0.15f)] public float reboteVertical = 0.035f;

    [Header("Sprites semillas")]
    public Sprite semillaVerde;
    public Sprite semillaAzul;
    public Sprite semillaAmarilla;
    public Sprite semillaMorada;

    [Header("Sprites verdes")]
    public Sprite broteVerde;
    public Sprite plantaVerdeLista;

    [Header("Sprites azules")]
    public Sprite broteAzul;
    public Sprite plantaAzulLista;

    [Header("Sprites amarillos")]
    public Sprite broteAmarillo;
    public Sprite plantaAmarillaLista;

    [Header("Sprites morados")]
    public Sprite broteMorado;
    public Sprite plantaMoradaLista;

    private Coroutine crecimientoCoroutine;
    private Transform jugador;
    private Vector3 escalaVisualBase = Vector3.one;
    private Vector3 posicionVisualBase;
    private Quaternion rotacionVisualBase = Quaternion.identity;
    private int etapaVisualActual = -1;
    private static readonly Sprite[] semillasPequenas = new Sprite[4];
    private static Sprite[] etapasAgua;
    private static readonly Sprite[][] etapasEspeciales = new Sprite[4][];

    private void Awake()
    {
        AsegurarComponentes();
    }

    private void Start()
    {
        AsegurarComponentes();

        if (plantaVisual != null)
        {
            GuardarTransformVisualBase();
            plantaVisual.SetActive(false);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(teclaUso))
        {
            return;
        }

        AsegurarComponentes();
        MacetaCultivo macetaCercana = BuscarMacetaMasCercana(jugador);
        if (macetaCercana == this && PuedeUsarsePorJugador())
        {
            UsarMaceta();
        }
    }

    private void UsarMaceta()
    {
        if (!tieneSemilla)
        {
            if (plantarPrimeraSemillaConT && PlantarPrimeraSemillaDisponible())
            {
                return;
            }

            AbrirMenuSemillas();
            return;
        }

        if (plantaLista)
        {
            Cosechar();
            return;
        }

        Debug.Log("La planta todavía está creciendo...");
    }

    private void AbrirMenuSemillas()
    {
        MenuSeleccionSemillas menu = MenuSeleccionSemillas.ObtenerOCrear();
        if (menu != null)
        {
            menu.AbrirMenu(this);
        }
        else
        {
            Debug.LogWarning("No existe MenuSeleccionSemillas en la escena.");
        }
    }

    private bool PlantarPrimeraSemillaDisponible()
    {
        if (InventarioSemillas.instancia == null)
        {
            Debug.LogWarning("No existe InventarioSemillas en la escena.");
            return false;
        }

        TipoSemilla[] prioridad =
        {
            TipoSemilla.Verde,
            TipoSemilla.Azul,
            TipoSemilla.Amarilla,
            TipoSemilla.Morada,
        };

        foreach (TipoSemilla tipo in prioridad)
        {
            if (InventarioSemillas.instancia.TieneSemilla(tipo))
            {
                return IntentarPlantarDesdeMenu(tipo);
            }
        }

        Debug.Log("No tienes semillas para plantar.");
        return false;
    }

    public bool IntentarPlantarDesdeMenu(TipoSemilla tipo)
    {
        if (!PuedeUsarsePorJugador())
        {
            Debug.Log("Jugador no dentro de la maceta");
            return false;
        }

        if (tieneSemilla)
        {
            Debug.Log("Esta maceta ya tiene una semilla plantada.");
            return false;
        }

        if (!EsSemillaPermitida(tipo))
        {
            Debug.Log("Esta zona no permite plantar semilla: " + tipo);
            return false;
        }

        if (InventarioSemillas.instancia == null)
        {
            Debug.LogWarning("No existe InventarioSemillas en la escena.");
            return false;
        }

        if (!InventarioSemillas.instancia.UsarSemilla(tipo))
        {
            return false;
        }

        Plantar(tipo);
        ProfessionalFeedbackSystem.Toast("Semilla plantada: " + tipo, new Color(0.72f, 1f, 0.45f, 1f));
        ProfessionalFeedbackSystem.WorldText(transform.position + Vector3.up * 0.8f, "+" + tipo, new Color(0.72f, 1f, 0.45f, 1f));
        ProfessionalFeedbackSystem.Burst(transform.position + Vector3.up * 0.35f, new Color(0.72f, 1f, 0.45f, 1f), 10);
        return true;
    }

    private void Plantar(TipoSemilla tipo)
    {
        tieneSemilla = true;
        plantaLista = false;
        semillaPlantada = tipo;

        Debug.Log("Plantaste semilla: " + tipo);

        if (plantarEnPosicionJugador && plantaVisual != null && jugador != null)
        {
            plantaVisual.transform.position = new Vector3(
                jugador.position.x + offsetVisualAlPlantar.x,
                jugador.position.y + offsetVisualAlPlantar.y,
                plantaVisual.transform.position.z);
        }

        if (plantaVisual != null)
        {
            GuardarTransformVisualBase();
            plantaVisual.SetActive(true);
        }

        etapaVisualActual = -1;
        AplicarEtapaCrecimiento(0f);

        if (crecimientoCoroutine != null)
        {
            StopCoroutine(crecimientoCoroutine);
        }

        crecimientoCoroutine = StartCoroutine(CrecerPlanta());
    }

    public bool EsSemillaPermitida(TipoSemilla tipo)
    {
        if (!limitarSemillasPermitidas)
        {
            return true;
        }

        return tipo switch
        {
            TipoSemilla.Verde => permitirVerde,
            TipoSemilla.Azul => permitirAzul,
            TipoSemilla.Amarilla => permitirAmarilla,
            TipoSemilla.Morada => permitirMorada,
            _ => false
        };
    }

    private IEnumerator CrecerPlanta()
    {
        float duracion = Mathf.Max(0.15f, tiempoCrecimiento);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            AplicarEtapaCrecimiento(progreso);
            yield return null;
        }

        plantaLista = true;
        CambiarSpritePlantaLista(semillaPlantada);
        RestaurarTransformVisual();
        etapaVisualActual = -1;
        crecimientoCoroutine = null;

        Debug.Log("La planta " + semillaPlantada + " ya está lista para cosechar.");
    }

    private void Cosechar()
    {
        Debug.Log("Cosechaste planta de tipo: " + semillaPlantada);
        MenuSeleccionSemillas.instancia?.CerrarMenu();

        if (InventarioCosechas.instancia != null)
        {
            InventarioCosechas.instancia.AgregarCosecha(semillaPlantada, 1);
            ProfessionalFeedbackSystem.Toast("Cosecha obtenida: " + semillaPlantada, new Color(1f, 0.88f, 0.35f, 1f));
            ProfessionalFeedbackSystem.WorldText(transform.position + Vector3.up * 0.9f, "COSECHA +1", new Color(1f, 0.88f, 0.35f, 1f));
            ProfessionalFeedbackSystem.Burst(transform.position + Vector3.up * 0.45f, new Color(1f, 0.88f, 0.35f, 1f), 14);
        }
        else
        {
            Debug.LogWarning("No existe InventarioCosechas en la escena.");
        }

        tieneSemilla = false;
        plantaLista = false;

        if (crecimientoCoroutine != null)
        {
            StopCoroutine(crecimientoCoroutine);
            crecimientoCoroutine = null;
        }

        if (plantaSpriteRenderer != null)
        {
            plantaSpriteRenderer.sprite = null;
        }

        if (plantaVisual != null)
        {
            RestaurarTransformVisual();
            plantaVisual.SetActive(false);
        }

        Debug.Log("La planta fue retirada de la maceta.");
    }

    private void CambiarSpriteBrote(TipoSemilla tipo)
    {
        if (plantaSpriteRenderer == null)
        {
            Debug.LogWarning("No asignaste Planta Sprite Renderer en la maceta.");
            return;
        }

        switch (tipo)
        {
            case TipoSemilla.Verde:
                plantaSpriteRenderer.sprite = broteVerde;
                break;

            case TipoSemilla.Azul:
                plantaSpriteRenderer.sprite = broteAzul;
                break;

            case TipoSemilla.Amarilla:
                plantaSpriteRenderer.sprite = broteAmarillo;
                break;

            case TipoSemilla.Morada:
                plantaSpriteRenderer.sprite = broteMorado;
                break;
        }
    }

    private void CambiarSpriteSemilla(TipoSemilla tipo)
    {
        if (plantaSpriteRenderer == null)
        {
            Debug.LogWarning("No asignaste Planta Sprite Renderer en la maceta.");
            return;
        }

        Sprite spriteSemilla = ObtenerSpriteSemillaPequena(tipo);
        if (spriteSemilla != null)
        {
            plantaSpriteRenderer.sprite = spriteSemilla;
            return;
        }

        CambiarSpriteBrote(tipo);
    }

    private Sprite ObtenerSpriteSemilla(TipoSemilla tipo)
    {
        return tipo switch
        {
            TipoSemilla.Verde => semillaVerde,
            TipoSemilla.Azul => semillaAzul,
            TipoSemilla.Amarilla => semillaAmarilla,
            TipoSemilla.Morada => semillaMorada,
            _ => null
        };
    }

    private static Sprite ObtenerSpriteSemillaPequena(TipoSemilla tipo)
    {
        int indice = Mathf.Clamp((int)tipo, 0, semillasPequenas.Length - 1);
        if (semillasPequenas[indice] != null)
        {
            return semillasPequenas[indice];
        }

        semillasPequenas[indice] = CrearSpriteSemillaPequena(tipo);
        return semillasPequenas[indice];
    }

    private static Sprite CrearSpriteSemillaPequena(TipoSemilla tipo)
    {
        const int width = 28;
        const int height = 20;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Semilla_Plantada_" + tipo;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 borde = new Color32(62, 36, 16, 255);
        Color32 baseColor = tipo switch
        {
            TipoSemilla.Azul => new Color32(132, 104, 58, 255),
            TipoSemilla.Amarilla => new Color32(176, 125, 44, 255),
            TipoSemilla.Morada => new Color32(118, 82, 68, 255),
            _ => new Color32(151, 103, 45, 255)
        };
        Color32 brillo = new Color32(
            (byte)Mathf.Min(baseColor.r + 58, 255),
            (byte)Mathf.Min(baseColor.g + 48, 255),
            (byte)Mathf.Min(baseColor.b + 34, 255),
            255);

        Vector2 centro = new Vector2(13.5f, 9.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - centro.x) / 10.5f;
                float dy = (y - centro.y) / 6.2f;
                float d = dx * dx + dy * dy;
                Color32 color = transparente;

                if (d <= 1.05f)
                {
                    color = d > 0.78f ? borde : baseColor;
                    if (x > 9 && x < 17 && y > 10 && y < 15 && d < 0.62f)
                    {
                        color = brillo;
                    }
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.35f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    private void CambiarSpritePlantaLista(TipoSemilla tipo)
    {
        if (plantaSpriteRenderer == null)
        {
            Debug.LogWarning("No asignaste Planta Sprite Renderer en la maceta.");
            return;
        }

        switch (tipo)
        {
            case TipoSemilla.Verde:
                plantaSpriteRenderer.sprite = plantaVerdeLista;
                break;

            case TipoSemilla.Azul:
                plantaSpriteRenderer.sprite = plantaAzulLista;
                break;

            case TipoSemilla.Amarilla:
                plantaSpriteRenderer.sprite = plantaAmarillaLista;
                break;

            case TipoSemilla.Morada:
                plantaSpriteRenderer.sprite = plantaMoradaLista;
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;
            jugador = other.transform;
            Debug.Log("Presiona T para usar la maceta.");
        }
    }

    private void AplicarEtapaCrecimiento(float progreso)
    {
        if (plantaVisual == null)
        {
            return;
        }

        int etapa = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progreso) * EtapasCrecimiento), 0, EtapasCrecimiento - 1);
        if (UsaEtapasEspeciales(semillaPlantada))
        {
            AplicarEtapaEspecial(progreso, etapa);
            return;
        }

        if (etapa != etapaVisualActual)
        {
            etapaVisualActual = etapa;
            if (etapa <= 2)
            {
                CambiarSpriteSemilla(semillaPlantada);
            }
            else if (etapa <= 6)
            {
                CambiarSpriteBrote(semillaPlantada);
            }
            else
            {
                CambiarSpritePlantaLista(semillaPlantada);
            }
        }

        float escala = ObtenerEscalaEtapa(etapa);
        float hundimiento = ObtenerHundimientoEtapa(etapa);
        float balanceo = Mathf.Sin((progreso * EtapasCrecimiento + ordenSiembra * 0.13f) * Mathf.PI) * balanceoGrados * Mathf.InverseLerp(2f, EtapasCrecimiento - 1f, etapa);
        float rebote = Mathf.Sin(progreso * Mathf.PI * 4f) * reboteVertical * Mathf.Sin(progreso * Mathf.PI);

        plantaVisual.transform.localScale = new Vector3(
            escalaVisualBase.x * escala,
            escalaVisualBase.y * escala,
            escalaVisualBase.z);
        plantaVisual.transform.localPosition = posicionVisualBase + new Vector3(0f, hundimiento + rebote, 0f);
        plantaVisual.transform.localRotation = rotacionVisualBase * Quaternion.Euler(0f, 0f, etapa <= 1 ? Mathf.Lerp(-48f, -18f, etapa) : balanceo);
    }

    private void AplicarEtapaEspecial(float progreso, int etapa)
    {
        if (plantaSpriteRenderer == null || plantaVisual == null)
        {
            return;
        }

        Sprite[] sprites = ObtenerEtapasEspeciales(semillaPlantada);
        int indiceSprite = Mathf.Clamp(Mathf.FloorToInt(etapa / 2f), 0, sprites.Length - 1);
        if (indiceSprite != etapaVisualActual)
        {
            etapaVisualActual = indiceSprite;
            plantaSpriteRenderer.sprite = sprites[indiceSprite];
        }

        float escala = Mathf.Lerp(0.42f, 1f, indiceSprite / (float)(sprites.Length - 1));
        float rebote = Mathf.Sin(progreso * Mathf.PI * 4f) * reboteVertical * Mathf.Sin(progreso * Mathf.PI);
        float balanceo = indiceSprite <= 1 ? 0f : Mathf.Sin((progreso * 5f + ordenSiembra * 0.17f) * Mathf.PI) * balanceoGrados * 0.45f;

        plantaVisual.transform.localScale = new Vector3(
            escalaVisualBase.x * escala,
            escalaVisualBase.y * escala,
            escalaVisualBase.z);
        plantaVisual.transform.localPosition = posicionVisualBase + new Vector3(0f, rebote, 0f);
        plantaVisual.transform.localRotation = rotacionVisualBase * Quaternion.Euler(0f, 0f, balanceo);
    }

    private static bool UsaEtapasEspeciales(TipoSemilla tipo)
    {
        return tipo == TipoSemilla.Verde
            || tipo == TipoSemilla.Azul
            || tipo == TipoSemilla.Amarilla
            || tipo == TipoSemilla.Morada;
    }

    private static Sprite[] ObtenerEtapasEspeciales(TipoSemilla tipo)
    {
        if (tipo == TipoSemilla.Azul)
        {
            return ObtenerEtapasAgua();
        }

        int indice = Mathf.Clamp((int)tipo, 0, etapasEspeciales.Length - 1);
        if (etapasEspeciales[indice] != null)
        {
            return etapasEspeciales[indice];
        }

        Sprite[] sprites = new Sprite[7];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = CrearSpritePlantaEspecial(tipo, i);
        }

        etapasEspeciales[indice] = sprites;
        return sprites;
    }

    private static Sprite[] ObtenerEtapasAgua()
    {
        if (etapasAgua != null)
        {
            return etapasAgua;
        }

        etapasAgua = new Sprite[7];
        for (int i = 0; i < etapasAgua.Length; i++)
        {
            etapasAgua[i] = CrearSpriteAgua(i);
        }

        return etapasAgua;
    }

    private static Sprite CrearSpritePlantaEspecial(TipoSemilla tipo, int etapa)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Planta_" + tipo + "_Etapa_" + etapa;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 transparente = new Color32(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, transparente);
            }
        }

        DibujarMonticulo(texture, 64, 22, etapa >= 1 ? 35 : 25, etapa >= 1 ? 11 : 8);

        if (etapa == 0)
        {
            DibujarSemillaOval(texture, 64, 36, 14, 20, ObtenerColorSemilla(tipo), ObtenerColorBorde(tipo));
        }
        else if (etapa == 1)
        {
            DibujarSemillaOval(texture, 60, 33, 12, 17, ObtenerColorSemilla(tipo), ObtenerColorBorde(tipo));
            DibujarBroteCurvo(texture, tipo, 64, 38, 19);
        }
        else
        {
            int alto = etapa >= 5 ? 76 : etapa >= 4 ? 70 : etapa >= 3 ? 58 : 48;
            DibujarTallo(texture, 64, 30, 64, alto);
            DibujarHojasAgua(texture, 64, etapa >= 4 ? 50 : 43, etapa >= 5 ? 6 : etapa >= 4 ? 5 : etapa >= 3 ? 3 : 2);

            if (tipo == TipoSemilla.Amarilla)
            {
                DibujarAmarilla(texture, etapa);
            }
            else if (tipo == TipoSemilla.Morada)
            {
                DibujarMorada(texture, etapa);
            }
            else
            {
                DibujarVerde(texture, etapa);
            }
        }

        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.16f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    private static void DibujarAmarilla(Texture2D texture, int etapa)
    {
        if (etapa <= 3)
        {
            DibujarSemillaOval(texture, 64, 65, 12, 16, new Color32(165, 210, 36, 255), new Color32(64, 118, 20, 255));
            return;
        }

        if (etapa == 4)
        {
            DibujarGotaColor(texture, 64, 77, 18, 25, new Color32(255, 205, 25, 255), new Color32(194, 120, 12, 255), new Color32(255, 248, 125, 255));
            DibujarCara(texture, 64, 75, 0.72f);
            DibujarChispas(texture, 64, 82, new Color32(255, 217, 38, 255), 5);
            return;
        }

        DibujarFlor(texture, 64, etapa == 5 ? 83 : 86, etapa == 5 ? 8 : 13, new Color32(255, 203, 34, 255), new Color32(209, 126, 17, 255));
        DibujarCirculo(texture, 64, etapa == 5 ? 83 : 86, etapa == 5 ? 18 : 23, new Color32(255, 218, 45, 255), new Color32(180, 111, 12, 255), new Color32(255, 250, 132, 255));
        DibujarCara(texture, 64, etapa == 5 ? 82 : 85, etapa == 5 ? 0.75f : 1f);
        DibujarChispas(texture, 64, 88, new Color32(255, 219, 35, 255), etapa == 5 ? 7 : 10);
    }

    private static void DibujarMorada(Texture2D texture, int etapa)
    {
        if (etapa <= 3)
        {
            return;
        }

        if (etapa <= 4)
        {
            DibujarGotaColor(texture, 64, 78, 16, 24, new Color32(190, 60, 199, 255), new Color32(105, 31, 128, 255), new Color32(246, 137, 255, 255));
            return;
        }

        DibujarFlor(texture, 64, etapa == 5 ? 84 : 88, etapa == 5 ? 8 : 12, new Color32(191, 59, 203, 255), new Color32(116, 33, 135, 255));
        DibujarCirculo(texture, 64, etapa == 5 ? 84 : 88, etapa == 5 ? 15 : 21, new Color32(255, 211, 31, 255), new Color32(176, 108, 10, 255), new Color32(255, 246, 116, 255));
        DibujarCara(texture, 64, etapa == 5 ? 83 : 87, etapa == 5 ? 0.68f : 0.94f);
        DibujarChispas(texture, 64, 90, new Color32(255, 192, 22, 255), etapa == 5 ? 4 : 8);
    }

    private static void DibujarVerde(Texture2D texture, int etapa)
    {
        if (etapa <= 3)
        {
            DibujarSemillaOval(texture, 64, 69, 13, 17, new Color32(143, 220, 30, 255), new Color32(55, 121, 18, 255));
            return;
        }

        if (etapa == 4)
        {
            DibujarHojasAgua(texture, 64, 65, 6);
            DibujarSemillaOval(texture, 64, 82, 15, 20, new Color32(150, 222, 31, 255), new Color32(57, 124, 18, 255));
            return;
        }

        int centroY = etapa == 5 ? 84 : 88;
        for (int i = 0; i < (etapa == 5 ? 9 : 14); i++)
        {
            float angulo = (360f / (etapa == 5 ? 9 : 14)) * i;
            float rad = angulo * Mathf.Deg2Rad;
            int x = 64 + Mathf.RoundToInt(Mathf.Cos(rad) * (etapa == 5 ? 21 : 27));
            int y = centroY + Mathf.RoundToInt(Mathf.Sin(rad) * (etapa == 5 ? 13 : 18));
            DibujarHoja(texture, x, y, etapa == 5 ? 15 : 18, etapa == 5 ? 8 : 10, angulo);
        }

        DibujarCirculo(texture, 64, centroY, etapa == 5 ? 17 : 24, new Color32(154, 220, 29, 255), new Color32(62, 130, 16, 255), new Color32(215, 255, 86, 255));
        DibujarCara(texture, 64, centroY - 1, etapa == 5 ? 0.75f : 1.05f);
        DibujarChispas(texture, 64, 91, new Color32(166, 239, 28, 255), etapa == 5 ? 5 : 9);
    }

    private static Sprite CrearSpriteAgua(int etapa)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Planta_Agua_Etapa_" + etapa;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 transparente = new Color32(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, transparente);
            }
        }

        DibujarMonticulo(texture, 64, 22, etapa >= 1 ? 34 : 25, etapa >= 1 ? 11 : 8);

        if (etapa <= 1)
        {
            DibujarGota(texture, 64, etapa == 0 ? 34 : 42, etapa == 0 ? 15 : 22, etapa == 0 ? 21 : 28);
        }
        else
        {
            int hojas = etapa >= 5 ? 6 : etapa >= 4 ? 5 : etapa >= 3 ? 3 : 2;
            DibujarTallo(texture, 64, 30, 64, etapa >= 4 ? 75 : 66);
            DibujarHojasAgua(texture, 64, etapa >= 4 ? 50 : 45, hojas);
            DibujarGota(texture, 64, etapa >= 5 ? 86 : etapa >= 4 ? 78 : 67, etapa >= 5 ? 25 : etapa >= 4 ? 21 : 15, etapa >= 5 ? 35 : etapa >= 4 ? 30 : 22);
        }

        if (etapa >= 5)
        {
            DibujarGota(texture, 34, 88, 7, 12);
            DibujarGota(texture, 94, 90, 7, 12);
            DibujarGota(texture, 43, 104, 5, 9);
            DibujarGota(texture, 86, 106, 5, 9);
        }

        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.16f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    private static void DibujarMonticulo(Texture2D texture, int cx, int cy, int rx, int ry)
    {
        Color32 sombra = new Color32(67, 39, 18, 255);
        Color32 tierra = new Color32(111, 71, 30, 255);
        Color32 luz = new Color32(148, 101, 45, 255);
        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                float d = dx * dx + dy * dy;
                if (d > 1f)
                {
                    continue;
                }

                Color32 color = y < cy ? luz : tierra;
                if (d > 0.72f)
                {
                    color = sombra;
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DibujarTallo(Texture2D texture, int x, int y0, int y1, int y2)
    {
        Color32 tallo = new Color32(72, 155, 28, 255);
        Color32 borde = new Color32(34, 87, 18, 255);
        for (int y = y0; y <= y2; y++)
        {
            int px = Mathf.RoundToInt(Mathf.Lerp(x - 2, x + 2, Mathf.InverseLerp(y0, y2, y)));
            texture.SetPixel(px - 1, y, borde);
            texture.SetPixel(px, y, tallo);
            texture.SetPixel(px + 1, y, tallo);
        }
    }

    private static void DibujarHojasAgua(Texture2D texture, int cx, int cy, int cantidad)
    {
        if (cantidad >= 2)
        {
            DibujarHoja(texture, cx - 18, cy, 20, 9, -18f);
            DibujarHoja(texture, cx + 18, cy, 20, 9, 18f);
        }
        if (cantidad >= 3)
        {
            DibujarHoja(texture, cx, cy + 12, 17, 8, 78f);
        }
        if (cantidad >= 5)
        {
            DibujarHoja(texture, cx - 26, cy + 16, 19, 9, -28f);
            DibujarHoja(texture, cx + 26, cy + 16, 19, 9, 28f);
        }
        if (cantidad >= 6)
        {
            DibujarHoja(texture, cx - 12, cy + 27, 16, 8, 45f);
            DibujarHoja(texture, cx + 12, cy + 27, 16, 8, -45f);
        }
    }

    private static void DibujarHoja(Texture2D texture, int cx, int cy, int rx, int ry, float grados)
    {
        Color32 borde = new Color32(35, 96, 18, 255);
        Color32 verde = new Color32(99, 197, 34, 255);
        Color32 luz = new Color32(164, 233, 62, 255);
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        for (int y = cy - rx; y <= cy + rx; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float lx = (x - cx) * cos + (y - cy) * sin;
                float ly = -(x - cx) * sin + (y - cy) * cos;
                float d = (lx * lx) / (rx * rx) + (ly * ly) / (ry * ry);
                if (d > 1f)
                {
                    continue;
                }

                texture.SetPixel(x, y, d > 0.72f ? borde : (ly > 0f ? luz : verde));
            }
        }
    }

    private static void DibujarGota(Texture2D texture, int cx, int cy, int rx, int ry)
    {
        Color32 borde = new Color32(4, 93, 162, 255);
        Color32 azul = new Color32(15, 178, 241, 255);
        Color32 luz = new Color32(146, 239, 255, 255);
        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                float d = dx * dx + dy * dy;
                bool punta = y > cy + ry * 0.38f && Mathf.Abs(dx) < Mathf.InverseLerp(cy + ry, cy + ry * 0.38f, y) * 0.48f;
                if (d > 1f && !punta)
                {
                    continue;
                }

                Color32 color = d > 0.82f && !punta ? borde : azul;
                if (x < cx - rx * 0.25f && y > cy + ry * 0.12f && d < 0.45f)
                {
                    color = luz;
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static Color32 ObtenerColorSemilla(TipoSemilla tipo)
    {
        return tipo switch
        {
            TipoSemilla.Amarilla => new Color32(135, 85, 32, 255),
            TipoSemilla.Morada => new Color32(139, 72, 154, 255),
            TipoSemilla.Verde => new Color32(111, 190, 35, 255),
            _ => new Color32(132, 104, 58, 255)
        };
    }

    private static Color32 ObtenerColorBorde(TipoSemilla tipo)
    {
        return tipo switch
        {
            TipoSemilla.Amarilla => new Color32(64, 39, 17, 255),
            TipoSemilla.Morada => new Color32(76, 37, 98, 255),
            TipoSemilla.Verde => new Color32(47, 102, 20, 255),
            _ => new Color32(62, 36, 16, 255)
        };
    }

    private static void DibujarSemillaOval(Texture2D texture, int cx, int cy, int rx, int ry, Color32 baseColor, Color32 borde)
    {
        Color32 brillo = new Color32(
            (byte)Mathf.Min(baseColor.r + 70, 255),
            (byte)Mathf.Min(baseColor.g + 65, 255),
            (byte)Mathf.Min(baseColor.b + 50, 255),
            255);

        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                float d = dx * dx + dy * dy;
                if (d > 1f)
                {
                    continue;
                }

                Color32 color = d > 0.78f ? borde : baseColor;
                if (x < cx && y > cy && d < 0.46f)
                {
                    color = brillo;
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DibujarBroteCurvo(Texture2D texture, TipoSemilla tipo, int cx, int cy, int alto)
    {
        Color32 verde = new Color32(111, 203, 31, 255);
        Color32 borde = new Color32(42, 103, 18, 255);
        for (int i = 0; i < alto; i++)
        {
            float t = i / (float)Mathf.Max(1, alto - 1);
            int x = cx + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI * 1.15f) * 6f);
            int y = cy + i;
            texture.SetPixel(x - 1, y, borde);
            texture.SetPixel(x, y, verde);
            texture.SetPixel(x + 1, y, verde);
        }

        DibujarHoja(texture, cx + 8, cy + alto - 2, 10, 5, tipo == TipoSemilla.Morada ? 40f : 25f);
    }

    private static void DibujarGotaColor(Texture2D texture, int cx, int cy, int rx, int ry, Color32 baseColor, Color32 borde, Color32 brillo)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                float d = dx * dx + dy * dy;
                bool punta = y > cy + ry * 0.38f && Mathf.Abs(dx) < Mathf.InverseLerp(cy + ry, cy + ry * 0.38f, y) * 0.48f;
                if (d > 1f && !punta)
                {
                    continue;
                }

                Color32 color = d > 0.82f && !punta ? borde : baseColor;
                if (x < cx - rx * 0.25f && y > cy + ry * 0.1f && d < 0.45f)
                {
                    color = brillo;
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DibujarFlor(Texture2D texture, int cx, int cy, int petalos, Color32 colorPetalo, Color32 borde)
    {
        for (int i = 0; i < petalos; i++)
        {
            float angulo = i * 360f / petalos;
            float rad = angulo * Mathf.Deg2Rad;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * 19f);
            int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * 16f);
            DibujarPetalo(texture, x, y, 13, 7, angulo, colorPetalo, borde);
        }
    }

    private static void DibujarPetalo(Texture2D texture, int cx, int cy, int rx, int ry, float grados, Color32 baseColor, Color32 borde)
    {
        Color32 luz = new Color32(
            (byte)Mathf.Min(baseColor.r + 35, 255),
            (byte)Mathf.Min(baseColor.g + 35, 255),
            (byte)Mathf.Min(baseColor.b + 30, 255),
            255);
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        for (int y = cy - rx; y <= cy + rx; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float lx = (x - cx) * cos + (y - cy) * sin;
                float ly = -(x - cx) * sin + (y - cy) * cos;
                float d = (lx * lx) / (rx * rx) + (ly * ly) / (ry * ry);
                if (d > 1f)
                {
                    continue;
                }

                texture.SetPixel(x, y, d > 0.74f ? borde : (ly > 0f ? luz : baseColor));
            }
        }
    }

    private static void DibujarCirculo(Texture2D texture, int cx, int cy, int radio, Color32 baseColor, Color32 borde, Color32 brillo)
    {
        for (int y = cy - radio; y <= cy + radio; y++)
        {
            for (int x = cx - radio; x <= cx + radio; x++)
            {
                float dx = (x - cx) / (float)radio;
                float dy = (y - cy) / (float)radio;
                float d = dx * dx + dy * dy;
                if (d > 1f)
                {
                    continue;
                }

                Color32 color = d > 0.82f ? borde : baseColor;
                if (x < cx - radio * 0.2f && y > cy + radio * 0.2f && d < 0.45f)
                {
                    color = brillo;
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DibujarCara(Texture2D texture, int cx, int cy, float escala)
    {
        Color32 negro = new Color32(23, 19, 18, 255);
        Color32 brillo = new Color32(255, 255, 255, 255);
        int ojoOffset = Mathf.RoundToInt(7f * escala);
        int ojoRadio = Mathf.Max(2, Mathf.RoundToInt(3.5f * escala));
        DibujarCirculo(texture, cx - ojoOffset, cy + 3, ojoRadio, negro, negro, brillo);
        DibujarCirculo(texture, cx + ojoOffset, cy + 3, ojoRadio, negro, negro, brillo);
        texture.SetPixel(cx - ojoOffset - 1, cy + 5, brillo);
        texture.SetPixel(cx + ojoOffset - 1, cy + 5, brillo);

        for (int i = -5; i <= 5; i++)
        {
            float t = i / 5f;
            int x = cx + i;
            int y = cy - 5 - Mathf.RoundToInt((1f - t * t) * 3f);
            texture.SetPixel(x, y, negro);
        }
    }

    private static void DibujarChispas(Texture2D texture, int cx, int cy, Color32 color, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            float angulo = i * 137.5f * Mathf.Deg2Rad;
            float radio = 30f + (i % 3) * 5f;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(angulo) * radio);
            int y = cy + Mathf.RoundToInt(Mathf.Sin(angulo) * radio * 0.7f);
            if (x < 3 || x >= 125 || y < 3 || y >= 125)
            {
                continue;
            }

            texture.SetPixel(x, y, color);
            texture.SetPixel(x - 1, y, color);
            texture.SetPixel(x + 1, y, color);
            texture.SetPixel(x, y - 1, color);
            texture.SetPixel(x, y + 1, color);
        }
    }

    private static float ObtenerEscalaEtapa(int etapa)
    {
        return etapa switch
        {
            0 => 0.16f,
            1 => 0.18f,
            2 => 0.20f,
            3 => 0.26f,
            4 => 0.40f,
            5 => 0.55f,
            6 => 0.46f,
            7 => 0.58f,
            8 => 0.70f,
            9 => 0.82f,
            10 => 0.93f,
            _ => 1f
        };
    }

    private static float ObtenerHundimientoEtapa(int etapa)
    {
        return etapa switch
        {
            0 => -0.26f,
            1 => -0.18f,
            2 => -0.12f,
            3 => -0.08f,
            4 => -0.04f,
            5 => -0.02f,
            _ => 0f
        };
    }

    private void AnimarCrecimientoVisual(float progreso, bool plantaGrande)
    {
        if (plantaVisual == null)
        {
            return;
        }

        float cambio = Mathf.Clamp(momentoCambioAPlantaGrande, 0.35f, 0.85f);
        float escala;

        if (!plantaGrande)
        {
            float faseBrote = Mathf.Clamp01(progreso / cambio);
            float suave = faseBrote * faseBrote * (3f - 2f * faseBrote);
            escala = Mathf.Lerp(escalaInicial, 0.72f, suave);
        }
        else
        {
            float faseGrande = Mathf.InverseLerp(cambio, 1f, progreso);
            float salidaElastica = 1f - Mathf.Pow(2f, -8f * faseGrande) * Mathf.Cos(faseGrande * Mathf.PI * 2.4f);
            escala = Mathf.LerpUnclamped(0.72f, 1f, salidaElastica);
        }

        float intensidadMovimiento = Mathf.Sin(progreso * Mathf.PI);
        float balanceo = Mathf.Sin(progreso * Mathf.PI * 5f) * balanceoGrados * intensidadMovimiento;
        float rebote = Mathf.Sin(progreso * Mathf.PI * 4f) * reboteVertical * intensidadMovimiento;

        plantaVisual.transform.localScale = new Vector3(
            escalaVisualBase.x * escala,
            escalaVisualBase.y * escala,
            escalaVisualBase.z);
        plantaVisual.transform.localPosition = posicionVisualBase + Vector3.up * rebote;
        plantaVisual.transform.localRotation = rotacionVisualBase * Quaternion.Euler(0f, 0f, balanceo);
    }

    private void GuardarTransformVisualBase()
    {
        if (plantaVisual == null)
        {
            return;
        }

        escalaVisualBase = plantaVisual.transform.localScale;
        posicionVisualBase = plantaVisual.transform.localPosition;
        rotacionVisualBase = plantaVisual.transform.localRotation;
    }

    private void RestaurarTransformVisual()
    {
        if (plantaVisual == null)
        {
            return;
        }

        plantaVisual.transform.localScale = escalaVisualBase;
        plantaVisual.transform.localPosition = posicionVisualBase;
        plantaVisual.transform.localRotation = rotacionVisualBase;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;
            jugador = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;
        }
    }

    private void AsegurarComponentes()
    {
        if (plantaVisual == null)
        {
            SpriteRenderer rendererHijo = GetComponentInChildren<SpriteRenderer>(true);
            if (rendererHijo != null && rendererHijo.gameObject != gameObject)
            {
                plantaVisual = rendererHijo.gameObject;
            }
        }

        if (plantaSpriteRenderer == null && plantaVisual != null)
        {
            plantaSpriteRenderer = plantaVisual.GetComponent<SpriteRenderer>();
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        if (jugador == null)
        {
            GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
            jugador = jugadorObjeto != null ? jugadorObjeto.transform : null;
        }
    }

    private bool PuedeUsarsePorJugador()
    {
        AsegurarComponentes();

        if (jugadorDentro)
        {
            return true;
        }

        return jugador != null && CalcularDistanciaAlJugador(jugador) <= distanciaUsoFallback;
    }

    private float CalcularDistanciaAlJugador(Transform jugadorTransform)
    {
        if (jugadorTransform == null)
        {
            return float.MaxValue;
        }

        if (plantaVisual != null)
        {
            if (usarDistanciaHorizontalParaInteractuar)
            {
                return Mathf.Abs(jugadorTransform.position.x - plantaVisual.transform.position.x);
            }

            return Vector2.Distance(jugadorTransform.position, plantaVisual.transform.position);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Vector3 puntoCercano = collider.bounds.ClosestPoint(jugadorTransform.position);
            return Vector2.Distance(jugadorTransform.position, puntoCercano);
        }

        return Vector2.Distance(jugadorTransform.position, transform.position);
    }

    private static MacetaCultivo BuscarMacetaMasCercana(Transform jugadorTransform)
    {
        if (jugadorTransform == null)
        {
            return null;
        }

        MacetaCultivo mejor = null;
        float mejorDistancia = float.MaxValue;
        MacetaCultivo mejorDisponible = null;
        float mejorDistanciaDisponible = float.MaxValue;
        MacetaCultivo cosechaCercana = null;
        float mejorDistanciaCosecha = float.MaxValue;
        MacetaCultivo siguienteEnOrden = null;
        int mejorOrdenDisponible = int.MaxValue;
        MacetaCultivo[] macetas = FindObjectsByType<MacetaCultivo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (MacetaCultivo maceta in macetas)
        {
            if (maceta == null || !maceta.isActiveAndEnabled)
            {
                continue;
            }

            float distancia = maceta.CalcularDistanciaAlJugador(jugadorTransform);
            if (distancia < mejorDistancia)
            {
                mejor = maceta;
                mejorDistancia = distancia;
            }

            bool enRango = distancia <= maceta.distanciaUsoFallback || maceta.jugadorDentro;
            if (maceta.plantaLista && enRango && distancia < mejorDistanciaCosecha)
            {
                cosechaCercana = maceta;
                mejorDistanciaCosecha = distancia;
            }

            if (maceta.usarOrdenSiembra && !maceta.tieneSemilla && enRango && maceta.ordenSiembra < mejorOrdenDisponible)
            {
                siguienteEnOrden = maceta;
                mejorOrdenDisponible = maceta.ordenSiembra;
            }

            bool disponible = maceta.preferirSiPuedeAbrirMenu && (!maceta.tieneSemilla || maceta.plantaLista);
            if (disponible && distancia < mejorDistanciaDisponible)
            {
                mejorDisponible = maceta;
                mejorDistanciaDisponible = distancia;
            }
        }

        if (cosechaCercana != null)
        {
            return cosechaCercana;
        }

        if (siguienteEnOrden != null)
        {
            return siguienteEnOrden;
        }

        return mejorDisponible != null ? mejorDisponible : mejor;
    }
}
