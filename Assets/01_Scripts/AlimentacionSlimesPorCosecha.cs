using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AlimentacionSlimesPorCosecha : MonoBehaviour
{
    [SerializeField] private KeyCode teclaAlimentar = KeyCode.P;
    [SerializeField] private float radioAlimentacion = 2.35f;
    [SerializeField] private int costoCosecha = 1;
    [SerializeField] private int recuperarHambre = 20;
    [SerializeField] private int ganarFelicidad = 15;
    [SerializeField] private float demoraRecursoEspecial = 10f;

    private static AlimentacionSlimesPorCosecha instancia;
    private Transform jugador;
    private GameObject slimeSeleccionado;
    private GameObject menuAlimentacion;
    private TMP_Text[] textosOpciones;

    private readonly TipoSemilla[] opcionesCosecha =
    {
        TipoSemilla.Verde,
        TipoSemilla.Azul,
        TipoSemilla.Amarilla,
        TipoSemilla.Morada
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscena()
    {
        if (FindFirstObjectByType<AlimentacionSlimesPorCosecha>() != null)
        {
            return;
        }

        GameObject controlador = new GameObject("AlimentacionSlimesPorCosecha");
        controlador.AddComponent<AlimentacionSlimesPorCosecha>();
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
        if (menuAlimentacion != null && menuAlimentacion.activeSelf)
        {
            ProcesarMenuAbierto();
        }

        if (!TeclaAlimentarPresionada())
        {
            return;
        }

        if (menuAlimentacion != null && menuAlimentacion.activeSelf)
        {
            CerrarMenu();
            return;
        }

        BuscarJugadorSiHaceFalta();
        if (jugador == null)
        {
            return;
        }

        GameObject slime = BuscarSlimeMasCercano();
        if (slime == null)
        {
            return;
        }

        AbrirMenu(slime);
    }

    private void ProcesarMenuAbierto()
    {
        if (TeclaEscapePresionada())
        {
            CerrarMenu();
            return;
        }

        int indice = ObtenerIndiceOpcionPresionada();
        if (indice >= 0 && indice < opcionesCosecha.Length)
        {
            ElegirCosecha(opcionesCosecha[indice]);
        }
    }

    private void ElegirCosecha(TipoSemilla cosechaElegida)
    {
        if (slimeSeleccionado == null)
        {
            CerrarMenu();
            return;
        }

        if (!ObtenerAlimento(slimeSeleccionado, out TipoSemilla alimento, out Color colorFeedback))
        {
            return;
        }

        if (cosechaElegida != alimento)
        {
            SlimeGenomeInstaller.Ensure(slimeSeleccionado)?.RegistrarAlimentacion(cosechaElegida, false);
            MostrarTextoMundo(slimeSeleccionado.transform.position, "NO LE GUSTA", new Color(1f, 0.55f, 0.25f, 1f));
            return;
        }

        if (InventarioCosechas.instancia == null || !InventarioCosechas.instancia.ConsumirCosecha(cosechaElegida, costoCosecha))
        {
            MostrarTextoMundo(slimeSeleccionado.transform.position, "SIN COSECHA", new Color(1f, 0.35f, 0.35f, 1f));
            return;
        }

        AplicarBeneficio(slimeSeleccionado);
        SlimeGenomeInstaller.Ensure(slimeSeleccionado)?.RegistrarAlimentacion(cosechaElegida, true);
        ProgramarRecursoEspecial(slimeSeleccionado);
        StartCoroutine(EfectoAlimentacion(slimeSeleccionado.transform, colorFeedback));
        MostrarTextoMundo(slimeSeleccionado.transform.position, "+1", colorFeedback);
        Debug.Log("Alimentaste a " + slimeSeleccionado.name + " con " + cosechaElegida);
        CerrarMenu();
    }

    private void ProgramarRecursoEspecial(GameObject slime)
    {
        if (slime == null)
        {
            return;
        }

        if (slime.GetComponent<SmileVerdeEvolucion>() != null)
        {
            StartCoroutine(GenerarRecursoEspecialTrasEspera(slime.transform.position, RecursoAlimentacionTipo.Manzana));
            return;
        }

        if (slime.GetComponent<SmileAmarilloEvolucion>() != null)
        {
            StartCoroutine(GenerarRecursoEspecialTrasEspera(slime.transform.position, RecursoAlimentacionTipo.Agua));
            return;
        }

        if (slime.GetComponent<FireSlimeAI>() != null)
        {
            StartCoroutine(GenerarRecursoEspecialTrasEspera(slime.transform.position, RecursoAlimentacionTipo.Bufanda));
        }
    }

    private IEnumerator GenerarRecursoEspecialTrasEspera(Vector3 posicionBase, RecursoAlimentacionTipo tipo)
    {
        yield return new WaitForSeconds(demoraRecursoEspecial);

        Vector3 posicion = posicionBase + new Vector3(Random.Range(-0.55f, 0.55f), 0.65f, 0f);
        CrearRecursoRecolectable(posicion, tipo);
        MostrarMensajePantalla(MensajeGeneracionRecurso(tipo));
    }

    private void CrearRecursoRecolectable(Vector3 posicion, RecursoAlimentacionTipo tipo)
    {
        GameObject recurso = new GameObject(NombreObjetoRecurso(tipo));
        recurso.transform.position = posicion;
        recurso.transform.localScale = Vector3.one * EscalaRecurso(tipo);

        SpriteRenderer renderer = recurso.AddComponent<SpriteRenderer>();
        renderer.sprite = CargarSpriteRecurso(tipo);
        renderer.sortingOrder = 92;

        CircleCollider2D collider = recurso.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.75f;

        Rigidbody2D rb = recurso.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        RecursoAlimentacionRecolectable recolectable = recurso.AddComponent<RecursoAlimentacionRecolectable>();
        recolectable.Configurar(tipo);
    }

    private static Sprite CargarSpriteRecurso(RecursoAlimentacionTipo tipo)
    {
        string ruta = RutaSpriteRecurso(tipo);
        Texture2D textura = Resources.Load<Texture2D>(ruta);
        if (textura == null)
        {
            return CrearSpriteCircular();
        }

        return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), Mathf.Max(textura.width, textura.height) * 0.45f);
    }

    private static string RutaSpriteRecurso(RecursoAlimentacionTipo tipo)
    {
        switch (tipo)
        {
            case RecursoAlimentacionTipo.Manzana:
                return "Generated/Recurso_Manzana";
            case RecursoAlimentacionTipo.Agua:
                return "Generated/Recurso_Agua";
            case RecursoAlimentacionTipo.Bufanda:
                return "Generated/Recurso_Bufanda";
            default:
                return "Generated/Recurso_Manzana";
        }
    }

    private static string NombreObjetoRecurso(RecursoAlimentacionTipo tipo)
    {
        switch (tipo)
        {
            case RecursoAlimentacionTipo.Manzana:
                return "Recurso_Manzana";
            case RecursoAlimentacionTipo.Agua:
                return "Recurso_Agua";
            case RecursoAlimentacionTipo.Bufanda:
                return "Recurso_Bufanda";
            default:
                return "Recurso_Especial";
        }
    }

    private static string MensajeGeneracionRecurso(RecursoAlimentacionTipo tipo)
    {
        switch (tipo)
        {
            case RecursoAlimentacionTipo.Manzana:
                return "SE GENERO UNA MANZANA, RECOGELA";
            case RecursoAlimentacionTipo.Agua:
                return "SE GENERO AGUA, VE A RECOGERLA";
            case RecursoAlimentacionTipo.Bufanda:
                return "SE GENERO UNA BUFANDA ANTIFRIO, RECOGELA";
            default:
                return "SE GENERO UN RECURSO, RECOGELO";
        }
    }

    private static float EscalaRecurso(RecursoAlimentacionTipo tipo)
    {
        switch (tipo)
        {
            case RecursoAlimentacionTipo.Manzana:
                return 0.34f;
            case RecursoAlimentacionTipo.Agua:
                return 0.28f;
            case RecursoAlimentacionTipo.Bufanda:
                return 0.34f;
            default:
                return 0.3f;
        }
    }

    private void AbrirMenu(GameObject slime)
    {
        slimeSeleccionado = slime;
        if (menuAlimentacion == null)
        {
            CrearMenu();
        }

        if (menuAlimentacion == null)
        {
            return;
        }

        ActualizarMenu();
        menuAlimentacion.SetActive(true);
    }

    private void CerrarMenu()
    {
        if (menuAlimentacion != null)
        {
            menuAlimentacion.SetActive(false);
        }

        slimeSeleccionado = null;
    }

    private void CrearMenu()
    {
        Canvas canvas = BuscarOCrearCanvasUI();
        AsegurarEventSystem();

        menuAlimentacion = new GameObject("Menu_Alimentacion_Slimes", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
        menuAlimentacion.transform.SetParent(canvas.transform, false);
        menuAlimentacion.layer = canvas.gameObject.layer;

        RectTransform panelRect = menuAlimentacion.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -22f);
        panelRect.sizeDelta = new Vector2(360f, 332f);

        Image fondo = menuAlimentacion.GetComponent<Image>();
        fondo.color = new Color(0.045f, 0.064f, 0.075f, 0.96f);

        Outline borde = menuAlimentacion.GetComponent<Outline>();
        borde.effectColor = new Color(1f, 0.75f, 0.25f, 0.92f);
        borde.effectDistance = new Vector2(2.2f, -2.2f);

        Shadow sombra = menuAlimentacion.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.62f);
        sombra.effectDistance = new Vector2(5f, -5f);

        VerticalLayoutGroup layout = menuAlimentacion.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 16);
        layout.spacing = 9;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        TMP_Text titulo = CrearTextoUI(menuAlimentacion.transform, "Titulo_Menu_Alimentacion", "ELEGIR COSECHA", 18f, 32f, new Color(1f, 0.94f, 0.74f, 1f));
        Shadow tituloSombra = titulo.gameObject.AddComponent<Shadow>();
        tituloSombra.effectColor = new Color(0f, 0f, 0f, 0.72f);
        tituloSombra.effectDistance = new Vector2(1.4f, -1.4f);

        textosOpciones = new TMP_Text[opcionesCosecha.Length];
        for (int i = 0; i < opcionesCosecha.Length; i++)
        {
            int indice = i;
            textosOpciones[i] = CrearBotonOpcion(menuAlimentacion.transform, opcionesCosecha[i], indice + 1);
            Button boton = textosOpciones[i].GetComponentInParent<Button>();
            boton.onClick.AddListener(() => ElegirCosecha(opcionesCosecha[indice]));
        }

        CrearTextoUI(menuAlimentacion.transform, "Ayuda_Menu_Alimentacion", "Click o teclas 1-4", 12f, 22f, new Color(0.72f, 0.82f, 0.86f, 1f));
        menuAlimentacion.SetActive(false);
    }

    private static Canvas BuscarOCrearCanvasUI()
    {
        GameObject canvasObject = GameObject.Find("Canvas_UI");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas != null)
        {
            return canvas;
        }

        canvas = FindFirstObjectByType<Canvas>();
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

    private static void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private TMP_Text CrearBotonOpcion(Transform parent, TipoSemilla tipo, int numero)
    {
        GameObject botonGO = new GameObject("Boton_Cosecha_" + tipo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button), typeof(LayoutElement));
        botonGO.transform.SetParent(parent, false);
        botonGO.layer = parent.gameObject.layer;

        LayoutElement layout = botonGO.GetComponent<LayoutElement>();
        layout.preferredHeight = 48f;

        Image imagen = botonGO.GetComponent<Image>();
        Color baseColor = ColorPorCosecha(tipo);
        imagen.color = Color.Lerp(baseColor, new Color(0.035f, 0.045f, 0.055f, 1f), 0.68f);

        Outline borde = botonGO.GetComponent<Outline>();
        borde.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.58f);
        borde.effectDistance = new Vector2(1.2f, -1.2f);

        ColorBlock colores = botonGO.GetComponent<Button>().colors;
        colores.normalColor = imagen.color;
        colores.highlightedColor = Color.Lerp(imagen.color, baseColor, 0.28f);
        colores.pressedColor = Color.Lerp(imagen.color, Color.black, 0.12f);
        botonGO.GetComponent<Button>().colors = colores;

        GameObject franjaGO = new GameObject("Franja_Color_" + tipo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        franjaGO.transform.SetParent(botonGO.transform, false);
        franjaGO.layer = botonGO.layer;
        RectTransform franjaRect = franjaGO.GetComponent<RectTransform>();
        franjaRect.anchorMin = new Vector2(0f, 0f);
        franjaRect.anchorMax = new Vector2(0f, 1f);
        franjaRect.pivot = new Vector2(0f, 0.5f);
        franjaRect.anchoredPosition = Vector2.zero;
        franjaRect.sizeDelta = new Vector2(8f, 0f);
        Image franja = franjaGO.GetComponent<Image>();
        franja.color = baseColor;
        franja.raycastTarget = false;

        TMP_Text label = CrearTextoUI(botonGO.transform, "Texto_Boton_Cosecha_" + tipo, "", 15f, 48f, Color.white);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.offsetMin = new Vector2(22f, 0f);
        label.rectTransform.offsetMax = new Vector2(-14f, 0f);
        label.text = numero + ". " + NombreCosecha(tipo);

        Shadow textoSombra = label.gameObject.AddComponent<Shadow>();
        textoSombra.effectColor = new Color(0f, 0f, 0f, 0.74f);
        textoSombra.effectDistance = new Vector2(1f, -1f);
        return label;
    }

    private static TMP_Text CrearTextoUI(Transform parent, string nombre, string texto, float tamano, float alto, Color color)
    {
        GameObject textoGO = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textoGO.transform.SetParent(parent, false);
        textoGO.layer = parent.gameObject.layer;

        LayoutElement layout = textoGO.GetComponent<LayoutElement>();
        layout.preferredHeight = alto;

        RectTransform rect = textoGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        TMP_Text label = textoGO.GetComponent<TMP_Text>();
        label.text = texto;
        label.color = color;
        label.fontSize = tamano;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
        label.fontSizeMax = tamano;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private void ActualizarMenu()
    {
        if (textosOpciones == null)
        {
            return;
        }

        for (int i = 0; i < opcionesCosecha.Length && i < textosOpciones.Length; i++)
        {
            if (textosOpciones[i] != null)
            {
                TipoSemilla tipo = opcionesCosecha[i];
                textosOpciones[i].text = (i + 1) + ".  " + NombreCosecha(tipo) + "     x" + ObtenerCantidadCosecha(tipo);
            }
        }
    }

    private static int ObtenerCantidadCosecha(TipoSemilla tipo)
    {
        if (InventarioCosechas.instancia == null)
        {
            return 0;
        }

        switch (tipo)
        {
            case TipoSemilla.Verde:
                return InventarioCosechas.instancia.hojasVerdes;
            case TipoSemilla.Azul:
                return InventarioCosechas.instancia.gotasAzules;
            case TipoSemilla.Amarilla:
                return InventarioCosechas.instancia.energiaSolar;
            case TipoSemilla.Morada:
                return InventarioCosechas.instancia.nectarMorado;
            default:
                return 0;
        }
    }

    private static string NombreCosecha(TipoSemilla tipo)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                return "Hoja verde";
            case TipoSemilla.Azul:
                return "Gota azul";
            case TipoSemilla.Amarilla:
                return "Sol amarillo";
            case TipoSemilla.Morada:
                return "Nectar morado";
            default:
                return "Cosecha";
        }
    }

    private static Color ColorPorCosecha(TipoSemilla tipo)
    {
        switch (tipo)
        {
            case TipoSemilla.Verde:
                return new Color(0.34f, 1f, 0.35f, 1f);
            case TipoSemilla.Azul:
                return new Color(0.25f, 0.72f, 1f, 1f);
            case TipoSemilla.Amarilla:
                return new Color(1f, 0.78f, 0.18f, 1f);
            case TipoSemilla.Morada:
                return new Color(0.78f, 0.35f, 1f, 1f);
            default:
                return Color.white;
        }
    }

    private bool TeclaAlimentarPresionada()
    {
        bool presionada = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        presionada = Input.GetKeyDown(teclaAlimentar);
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && teclaAlimentar == KeyCode.P)
        {
            presionada = presionada || keyboard.pKey.wasPressedThisFrame;
        }
#endif

        return presionada;
    }

    private bool TeclaEscapePresionada()
    {
        bool presionada = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        presionada = Input.GetKeyDown(KeyCode.Escape);
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            presionada = presionada || keyboard.escapeKey.wasPressedThisFrame;
        }
#endif

        return presionada;
    }

    private int ObtenerIndiceOpcionPresionada()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
        }
#endif

        return -1;
    }

    private void BuscarJugadorSiHaceFalta()
    {
        if (jugador != null)
        {
            return;
        }

        GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObjeto != null)
        {
            jugador = jugadorObjeto.transform;
            return;
        }

        Player1 player = FindFirstObjectByType<Player1>();
        if (player != null)
        {
            jugador = player.transform;
            return;
        }

        CriadorSlimesController criador = FindFirstObjectByType<CriadorSlimesController>();
        jugador = criador != null ? criador.transform : null;
    }

    private GameObject BuscarSlimeMasCercano()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(jugador.position, radioAlimentacion);
        GameObject mejorSlime = null;
        float mejorDistancia = float.MaxValue;

        foreach (Collider2D collider in colliders)
        {
            if (collider == null || collider.transform == jugador || collider.transform.IsChildOf(jugador))
            {
                continue;
            }

            GameObject slime = ObtenerRaizSlime(collider);
            if (slime == null)
            {
                continue;
            }

            float distancia = Vector2.Distance(jugador.position, collider.bounds.ClosestPoint(jugador.position));
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorSlime = slime;
            }
        }

        return mejorSlime;
    }

    private static GameObject ObtenerRaizSlime(Collider2D collider)
    {
        FireSlimeAI fire = collider.GetComponentInParent<FireSlimeAI>();
        if (fire != null) return fire.gameObject;

        ColdSlimeAI cold = collider.GetComponentInParent<ColdSlimeAI>();
        if (cold != null) return cold.gameObject;

        CleanerSlimeAI cleaner = collider.GetComponentInParent<CleanerSlimeAI>();
        if (cleaner != null) return cleaner.gameObject;

        AcidSlimeAI acid = collider.GetComponentInParent<AcidSlimeAI>();
        if (acid != null) return acid.gameObject;

        SmileAmarilloEvolucion amarillo = collider.GetComponentInParent<SmileAmarilloEvolucion>();
        if (amarillo != null) return amarillo.gameObject;

        SmileVerdeEvolucion verde = collider.GetComponentInParent<SmileVerdeEvolucion>();
        if (verde != null) return verde.gameObject;

        SmileBasico smile = collider.GetComponentInParent<SmileBasico>();
        return smile != null ? smile.gameObject : null;
    }

    private static bool ObtenerAlimento(GameObject slime, out TipoSemilla alimento, out Color colorFeedback)
    {
        if (slime.GetComponent<FireSlimeAI>() != null)
        {
            alimento = TipoSemilla.Amarilla;
            colorFeedback = new Color(1f, 0.78f, 0.18f, 1f);
            return true;
        }

        if (slime.GetComponent<ColdSlimeAI>() != null || slime.GetComponent<SmileAmarilloEvolucion>() != null)
        {
            alimento = TipoSemilla.Azul;
            colorFeedback = new Color(0.25f, 0.72f, 1f, 1f);
            return true;
        }

        if (slime.GetComponent<CleanerSlimeAI>() != null || slime.GetComponent<SmileVerdeEvolucion>() != null)
        {
            alimento = TipoSemilla.Verde;
            colorFeedback = new Color(0.34f, 1f, 0.35f, 1f);
            return true;
        }

        if (slime.GetComponent<AcidSlimeAI>() != null)
        {
            alimento = TipoSemilla.Morada;
            colorFeedback = new Color(0.78f, 0.35f, 1f, 1f);
            return true;
        }

        SmileBasico smile = slime.GetComponent<SmileBasico>();
        if (smile != null)
        {
            alimento = smile.alimentoFavorito;
            colorFeedback = Color.white;
            return true;
        }

        alimento = TipoSemilla.Verde;
        colorFeedback = Color.white;
        return false;
    }

    private void AplicarBeneficio(GameObject slime)
    {
        SlimeController controller = slime.GetComponent<SlimeController>();
        if (controller != null)
        {
            controller.Feed(recuperarHambre, ganarFelicidad);
        }

        SmileBasico smile = slime.GetComponent<SmileBasico>();
        if (smile != null)
        {
            smile.hambre = Mathf.Max(0, smile.hambre - recuperarHambre);
            smile.felicidad = Mathf.Min(100, smile.felicidad + ganarFelicidad);
        }
    }

    private IEnumerator EfectoAlimentacion(Transform slime, Color color)
    {
        if (slime == null)
        {
            yield break;
        }

        Vector3 escalaOriginal = slime.localScale;
        float tiempo = 0f;
        while (tiempo < 0.18f && slime != null)
        {
            tiempo += Time.deltaTime;
            float pulso = Mathf.Sin((tiempo / 0.18f) * Mathf.PI) * 0.12f;
            slime.localScale = escalaOriginal * (1f + pulso);
            yield return null;
        }

        if (slime != null)
        {
            slime.localScale = escalaOriginal;
        }

        for (int i = 0; i < 5; i++)
        {
            if (slime == null)
            {
                yield break;
            }

            CrearParticula(slime.position, color);
        }
    }

    private void CrearParticula(Vector3 origen, Color color)
    {
        GameObject particula = new GameObject("FX_AlimentoSlime");
        SpriteRenderer renderer = particula.AddComponent<SpriteRenderer>();
        renderer.sprite = CrearSpriteCircular();
        renderer.color = color;
        renderer.sortingOrder = 120;

        particula.transform.position = origen + new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(0.25f, 0.55f), 0f);
        particula.transform.localScale = Vector3.one * Random.Range(0.08f, 0.14f);
        StartCoroutine(SubirYDesvanecer(particula, renderer));
    }

    private IEnumerator SubirYDesvanecer(GameObject particula, SpriteRenderer renderer)
    {
        float tiempo = 0f;
        Vector3 inicio = particula.transform.position;
        Vector3 fin = inicio + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.45f, 0.75f), 0f);
        Color colorInicial = renderer.color;

        while (tiempo < 0.55f && particula != null)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / 0.55f);
            particula.transform.position = Vector3.Lerp(inicio, fin, t);
            renderer.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, 1f - t);
            yield return null;
        }

        if (particula != null)
        {
            Destroy(particula);
        }
    }

    private void MostrarTextoMundo(Vector3 posicion, string texto, Color color)
    {
        GameObject textoGO = new GameObject("Texto_AlimentoSlime", typeof(TextMeshPro));
        textoGO.transform.position = posicion + Vector3.up * 0.95f;
        TMP_Text label = textoGO.GetComponent<TMP_Text>();
        label.text = texto;
        label.color = color;
        label.fontSize = 2.8f;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        MeshRenderer meshRenderer = textoGO.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 130;
        }

        StartCoroutine(SubirYDestruirTexto(textoGO, label));
    }

    private void MostrarMensajePantalla(string mensaje)
    {
        Canvas canvas = BuscarOCrearCanvasUI();
        GameObject mensajeGO = new GameObject("Mensaje_Recurso_Alimentacion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        mensajeGO.transform.SetParent(canvas.transform, false);
        mensajeGO.layer = canvas.gameObject.layer;

        RectTransform rect = mensajeGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        rect.sizeDelta = new Vector2(520f, 58f);

        Image fondo = mensajeGO.GetComponent<Image>();
        fondo.color = new Color(0.045f, 0.06f, 0.07f, 0.92f);
        fondo.raycastTarget = false;

        Shadow sombra = mensajeGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.65f);
        sombra.effectDistance = new Vector2(3f, -3f);

        GameObject textoGO = new GameObject("Texto_Mensaje_Recurso_Alimentacion", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        textoGO.transform.SetParent(mensajeGO.transform, false);
        RectTransform textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(16f, 6f);
        textoRect.offsetMax = new Vector2(-16f, -6f);

        TMP_Text label = textoGO.GetComponent<TMP_Text>();
        label.text = mensaje;
        label.color = new Color(1f, 0.94f, 0.68f, 1f);
        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 22f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        Shadow textoSombra = textoGO.GetComponent<Shadow>();
        textoSombra.effectColor = new Color(0f, 0f, 0f, 0.75f);
        textoSombra.effectDistance = new Vector2(1.3f, -1.3f);
        StartCoroutine(DestruirMensajePantalla(mensajeGO, 3f));
    }

    private IEnumerator DestruirMensajePantalla(GameObject mensajeGO, float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (mensajeGO != null)
        {
            Destroy(mensajeGO);
        }
    }

    private IEnumerator SubirYDestruirTexto(GameObject textoGO, TMP_Text label)
    {
        float tiempo = 0f;
        Vector3 inicio = textoGO.transform.position;
        Color colorInicial = label.color;

        while (tiempo < 0.75f && textoGO != null)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / 0.75f);
            textoGO.transform.position = inicio + Vector3.up * (0.5f * t);
            label.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, 1f - t);
            yield return null;
        }

        if (textoGO != null)
        {
            Destroy(textoGO);
        }
    }

    private static Sprite spriteCircular;

    private static Sprite CrearSpriteCircular()
    {
        if (spriteCircular != null)
        {
            return spriteCircular;
        }

        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Bilinear;
        Vector2 centro = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), centro);
                float alpha = Mathf.Clamp01(1f - ((distancia - 5.2f) / 2.3f));
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        textura.Apply();
        spriteCircular = Sprite.Create(textura, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        return spriteCircular;
    }
}

public enum RecursoAlimentacionTipo
{
    Manzana,
    Agua,
    Bufanda
}

public class RecursoAlimentacionRecolectable : MonoBehaviour
{
    private RecursoAlimentacionTipo tipo;
    private bool recolectado;
    private Vector3 posicionBase;

    public void Configurar(RecursoAlimentacionTipo nuevoTipo)
    {
        tipo = nuevoTipo;
        posicionBase = transform.position;
    }

    private void Update()
    {
        transform.position = posicionBase + Vector3.up * (Mathf.Sin(Time.time * 4.8f) * 0.08f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recolectado || !other.CompareTag("Player"))
        {
            return;
        }

        recolectado = true;
        if (InventarioRecursosSmiles.instancia != null)
        {
            if (tipo == RecursoAlimentacionTipo.Manzana)
            {
                InventarioRecursosSmiles.instancia.AgregarManzanas(1);
            }
            else if (tipo == RecursoAlimentacionTipo.Bufanda)
            {
                InventarioRecursosSmiles.instancia.AgregarBufandas(1);
            }
            else
            {
                InventarioRecursosSmiles.instancia.AgregarVasosAgua(1);
            }
        }

        Destroy(gameObject);
    }
}
