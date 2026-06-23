using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class SampleSceneMinaController : MonoBehaviour
{
    [Header("Zona minable")]
    public Vector2 esquinaInferiorIzquierda = new Vector2(-28f, -30f);
    public Vector2 esquinaSuperiorDerecha = new Vector2(28f, 14f);
    public float tamanoCelda = 0.75f;
    public Vector2 zonaCasaMin = new Vector2(-4.8f, 0.55f);
    public Vector2 zonaCasaMax = new Vector2(8.15f, 6.35f);
    public Vector2 zonaJugadorMin = new Vector2(-4.35f, 0.65f);
    public Vector2 zonaJugadorMax = new Vector2(-1.15f, 3.9f);
    public Vector2 zonaEscenaLilaMin = new Vector2(7.8f, -0.8f);
    public Vector2 zonaEscenaLilaMax = new Vector2(25.4f, 7.8f);
    public Vector2 zonaRocaDuraDerechaMin = new Vector2(9.4f, -16.9f);
    public Vector2 zonaRocaDuraDerechaMax = new Vector2(16.9f, -9.3f);
    public Vector2 zonaRocaDuraHieloMin = new Vector2(-15.8f, -28.8f);
    public Vector2 zonaRocaDuraHieloMax = new Vector2(-6.3f, -20.0f);
    public bool crearHuecosNaturales = true;
    [Min(1)] public int multiplicadorHuecosNaturales = 6;
    public Vector2 posicionTilemapReferencia = new Vector2(4.1162f, -1.324f);
    public Vector2 escalaTilemapReferencia = new Vector2(0.882483f, 0.892289f);

    [Header("Bloques")]
    public Tilemap tilemapBloques;
    public Tile tileBloque;
    public int ordenVisualBloques = 35;
    public bool usarColision = true;

    [Header("Minado")]
    public Camera camaraPrincipal;
    public GameObject prefabSemilla;
    public GameObject[] prefabsSemillas;
    [Range(0f, 1f)] public float probabilidadSemilla = 0.33f;
    public bool ignorarClicksSobreUI = true;
    public bool limitarDistanciaAlJugador = false;
    public float distanciaMaximaMinado = 6f;
    public bool limitarMinadoPorMirada = true;
    public float margenMinadoPorMirada = 0.15f;
    public float anchoMinadoHaciaAbajo = 1.25f;
    public float alturaMinadoHaciaAbajo = 0.25f;
    [Range(0, 8)] public int bloquesExtraPorPicada = 6;

    private readonly HashSet<Vector3Int> celdasRompiendose = new HashSet<Vector3Int>();
    private Sprite spriteFragmento;
    private Transform jugador;
    private Animator animadorJugador;
    private int siguienteTipoSemilla;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnSampleSceneSiFalta()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            return;
        }

        if (FindFirstObjectByType<SampleSceneMinaController>() != null)
        {
            return;
        }

        GameObject sistema = new GameObject("Sistema_Minado_SampleScene");
        sistema.AddComponent<SampleSceneMinaController>();
    }

    private void Awake()
    {
        AsegurarReferencias();
        GenerarMina();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            IntentarPicarConMouse();
        }
    }

    [ContextMenu("Regenerar mina")]
    public void GenerarMina()
    {
        AsegurarReferencias();

        if (tilemapBloques == null || tileBloque == null)
        {
            return;
        }

        QuitarPiedrasGrisesEnZonaCafe();

        tilemapBloques.ClearAllTiles();
        celdasRompiendose.Clear();

        int minX = Mathf.FloorToInt(esquinaInferiorIzquierda.x / tamanoCelda);
        int minY = Mathf.FloorToInt(esquinaInferiorIzquierda.y / tamanoCelda);
        int maxX = Mathf.CeilToInt(esquinaSuperiorDerecha.x / tamanoCelda);
        int maxY = Mathf.CeilToInt(esquinaSuperiorDerecha.y / tamanoCelda);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int celda = new Vector3Int(x, y, 0);
                Vector3 centro = tilemapBloques.GetCellCenterWorld(celda);

                if (EstaDentroZonaLibre(centro) || EstaDentroHuecoNatural(centro))
                {
                    continue;
                }

                tilemapBloques.SetTile(celda, tileBloque);
                tilemapBloques.SetTileFlags(celda, TileFlags.None);
                tilemapBloques.SetColor(celda, ColorBloque(centro));
            }
        }

        TilemapCollider2D collider = tilemapBloques.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.enabled = usarColision;
            collider.ProcessTilemapChanges();
        }
    }

    private void IntentarPicarConMouse()
    {
        if (ignorarClicksSobreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        AsegurarReferencias();

        if (camaraPrincipal == null || tilemapBloques == null)
        {
            return;
        }

        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(camaraPrincipal.transform.position.z - tilemapBloques.transform.position.z);
        Vector3 mundo = camaraPrincipal.ScreenToWorldPoint(mouse);
        mundo.z = tilemapBloques.transform.position.z;

        if (limitarDistanciaAlJugador && jugador != null && Vector2.Distance(jugador.position, mundo) > distanciaMaximaMinado)
        {
            return;
        }

        if (!PuedeMinarEnDireccionDeMirada(mundo))
        {
            return;
        }

        Vector3Int celda = tilemapBloques.WorldToCell(mundo);
        celda.z = 0;

        if (!tilemapBloques.HasTile(celda) || celdasRompiendose.Contains(celda))
        {
            return;
        }

        animadorJugador?.SetTrigger("Attack");
        StartCoroutine(RomperBloque(celda));
    }

    private IEnumerator RomperBloque(Vector3Int celda)
    {
        celdasRompiendose.Add(celda);

        tilemapBloques.SetTileFlags(celda, TileFlags.None);
        Color colorOriginal = tilemapBloques.GetColor(celda);
        tilemapBloques.SetColor(celda, Color.Lerp(colorOriginal, Color.white, 0.45f));
        yield return new WaitForSeconds(0.06f);

        List<Vector3Int> celdasHueco = ObtenerCeldasHuecoMinado(celda);
        Vector3 centro = tilemapBloques.GetCellCenterWorld(celda);

        int cubosRecolectados = 0;
        foreach (Vector3Int celdaHueco in celdasHueco)
        {
            if (!tilemapBloques.HasTile(celdaHueco))
            {
                continue;
            }

            tilemapBloques.SetTileFlags(celdaHueco, TileFlags.None);
            Color colorBloque = tilemapBloques.GetColor(celdaHueco);
            tilemapBloques.SetTile(celdaHueco, null);
            CrearFragmentos(tilemapBloques.GetCellCenterWorld(celdaHueco), colorBloque);
            cubosRecolectados++;
        }

        AlmacenSistema.AgregarCubosGlobal(cubosRecolectados);

        if (Random.value <= probabilidadSemilla)
        {
            CrearSemilla(centro);
        }

        TilemapCollider2D collider = tilemapBloques.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.ProcessTilemapChanges();
        }

        foreach (Vector3Int celdaHueco in celdasHueco)
        {
            celdasRompiendose.Remove(celdaHueco);
        }
    }

    private void AsegurarReferencias()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (jugador == null)
        {
            GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
            jugador = jugadorObjeto != null ? jugadorObjeto.transform : null;
            animadorJugador = jugadorObjeto != null ? jugadorObjeto.GetComponent<Player1>()?.bodyAnim : null;
        }

        if (tileBloque == null)
        {
            tileBloque = CrearTileBloque();
        }

        if (tilemapBloques == null)
        {
            CrearTilemapBloques();
        }
    }

    private void CrearTilemapBloques()
    {
        GameObject gridObjeto = new GameObject("Grid_Mina_SampleScene");
        gridObjeto.transform.SetParent(transform);
        gridObjeto.transform.position = new Vector3(posicionTilemapReferencia.x, posicionTilemapReferencia.y, 0f);
        gridObjeto.transform.localScale = new Vector3(escalaTilemapReferencia.x, escalaTilemapReferencia.y, 1f);
        Grid grid = gridObjeto.AddComponent<Grid>();
        grid.cellSize = new Vector3(tamanoCelda, tamanoCelda, 1f);

        GameObject tilemapObjeto = new GameObject("Tilemap_Bloques_Minables");
        tilemapObjeto.transform.SetParent(gridObjeto.transform);
        tilemapObjeto.transform.localPosition = Vector3.zero;
        tilemapBloques = tilemapObjeto.AddComponent<Tilemap>();

        TilemapRenderer rendererBloques = tilemapObjeto.AddComponent<TilemapRenderer>();
        rendererBloques.sortingOrder = ordenVisualBloques;

        if (usarColision)
        {
            tilemapObjeto.AddComponent<TilemapCollider2D>();
        }
    }

    private void QuitarPiedrasGrisesEnZonaCafe()
    {
        foreach (SpriteRenderer renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || !EsPiedraGrisDeZonaCafe(renderer))
            {
                continue;
            }

            Destroy(renderer.gameObject);
        }
    }

    private bool EsPiedraGrisDeZonaCafe(SpriteRenderer renderer)
    {
        string nombreObjeto = renderer.gameObject.name.ToLowerInvariant();
        string nombreSprite = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
        string nombre = nombreObjeto + " " + nombreSprite;
        bool esPiedra = nombre.Contains("piedra") || nombre.Contains("piedras") || nombre.Contains("rock") || nombre.Contains("stone");
        return esPiedra && IntersectaRectangulo(renderer.bounds, zonaRocaDuraDerechaMin, zonaRocaDuraDerechaMax);
    }

    private static bool IntersectaRectangulo(Bounds bounds, Vector2 minimo, Vector2 maximo)
    {
        return bounds.max.x >= minimo.x
            && bounds.min.x <= maximo.x
            && bounds.max.y >= minimo.y
            && bounds.min.y <= maximo.y;
    }

    private bool EstaDentroZonaLibre(Vector3 posicion)
    {
        return EstaDentroRectangulo(posicion, zonaCasaMin, zonaCasaMax)
            || EstaDentroRectangulo(posicion, zonaJugadorMin, zonaJugadorMax)
            || EstaDentroRectangulo(posicion, zonaEscenaLilaMin, zonaEscenaLilaMax)
            || EstaDentroRectangulo(posicion, zonaRocaDuraHieloMin, zonaRocaDuraHieloMax);
    }

    private bool EstaDentroHuecoNatural(Vector3 posicion)
    {
        if (!crearHuecosNaturales)
        {
            return false;
        }

        return EstaDentroElipse(posicion, new Vector2(-15.6f, 8.1f), new Vector2(2.1f, 1.05f))
            || EstaDentroElipse(posicion, new Vector2(-9.7f, 3.9f), new Vector2(2.8f, 1.15f))
            || EstaDentroElipse(posicion, new Vector2(-3.3f, 7.3f), new Vector2(2.4f, 0.9f))
            || EstaDentroElipse(posicion, new Vector2(8.4f, 7.4f), new Vector2(2.7f, 1.05f))
            || EstaDentroElipse(posicion, new Vector2(13.6f, 2.8f), new Vector2(2.4f, 1.0f))
            || EstaDentroElipse(posicion, new Vector2(-14.2f, -4.4f), new Vector2(3.0f, 1.25f))
            || EstaDentroElipse(posicion, new Vector2(-6.2f, -10.8f), new Vector2(3.1f, 1.45f))
            || EstaDentroElipse(posicion, new Vector2(3.4f, -7.8f), new Vector2(3.4f, 1.25f))
            || EstaDentroElipse(posicion, new Vector2(12.3f, -13.4f), new Vector2(3.2f, 1.5f))
            || EstaDentroElipse(posicion, new Vector2(-16.5f, -18.5f), new Vector2(3.5f, 1.35f))
            || EstaDentroElipse(posicion, new Vector2(1.5f, -20.8f), new Vector2(3.7f, 1.35f))
            || EstaDentroElipse(posicion, new Vector2(15.5f, -23.4f), new Vector2(3.6f, 1.45f))
            || EstaDentroElipse(posicion, new Vector2(-22.5f, 4.9f), new Vector2(2.7f, 1.1f))
            || EstaDentroElipse(posicion, new Vector2(20.8f, 5.6f), new Vector2(2.9f, 1.2f))
            || EstaDentroElipse(posicion, new Vector2(-21.2f, -8.6f), new Vector2(3.2f, 1.35f))
            || EstaDentroElipse(posicion, new Vector2(20.5f, -6.9f), new Vector2(3.4f, 1.25f))
            || EstaDentroElipse(posicion, new Vector2(-10.8f, -16.4f), new Vector2(3.1f, 1.2f))
            || EstaDentroElipse(posicion, new Vector2(9.6f, -19.2f), new Vector2(3.4f, 1.35f))
            || EstaDentroElipse(posicion, new Vector2(-5.5f, -25.2f), new Vector2(3.8f, 1.45f))
            || EstaDentroElipse(posicion, new Vector2(22.5f, -25.8f), new Vector2(3.0f, 1.2f))
            || EstaDentroHuecoAdicional(posicion);
    }

    private bool EstaDentroHuecoAdicional(Vector3 posicion)
    {
        int huecosBase = 20;
        int huecosExtra = huecosBase * Mathf.Max(0, multiplicadorHuecosNaturales - 1);

        for (int i = 0; i < huecosExtra; i++)
        {
            float x = Mathf.Lerp(esquinaInferiorIzquierda.x + 1.5f, esquinaSuperiorDerecha.x - 1.5f, ValorDeterministico01(i * 17 + 3));
            float y = Mathf.Lerp(esquinaInferiorIzquierda.y + 1.5f, esquinaSuperiorDerecha.y - 1.5f, ValorDeterministico01(i * 29 + 11));
            Vector2 centro = new Vector2(x, y);

            if (EstaDentroZonaLibre(centro))
            {
                continue;
            }

            Vector2 radio = new Vector2(
                Mathf.Lerp(0.95f, 2.25f, ValorDeterministico01(i * 41 + 7)),
                Mathf.Lerp(0.38f, 0.95f, ValorDeterministico01(i * 53 + 13))
            );

            if (EstaDentroElipse(posicion, centro, radio))
            {
                return true;
            }
        }

        return false;
    }

    private float ValorDeterministico01(int semilla)
    {
        return Mathf.Repeat(Mathf.Sin(semilla * 12.9898f) * 43758.5453f, 1f);
    }

    private bool PuedeMinarEnDireccionDeMirada(Vector3 objetivo)
    {
        if (!limitarMinadoPorMirada || jugador == null)
        {
            return true;
        }

        float diferenciaX = objetivo.x - jugador.position.x;
        float diferenciaY = objetivo.y - jugador.position.y;
        if (diferenciaY < -alturaMinadoHaciaAbajo && Mathf.Abs(diferenciaX) <= anchoMinadoHaciaAbajo)
        {
            return true;
        }

        if (Mathf.Abs(diferenciaX) <= margenMinadoPorMirada)
        {
            return true;
        }

        bool miraIzquierda = jugador.rotation.eulerAngles.y > 90f && jugador.rotation.eulerAngles.y < 270f;
        return miraIzquierda ? diferenciaX < 0f : diferenciaX > 0f;
    }

    private List<Vector3Int> ObtenerCeldasHuecoMinado(Vector3Int centro)
    {
        List<Vector3Int> celdas = new List<Vector3Int> { centro };

        if (bloquesExtraPorPicada <= 0)
        {
            return celdas;
        }

        Vector3Int[] vecinos =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
        };

        int intentos = 0;
        while (celdas.Count < bloquesExtraPorPicada + 1 && intentos < vecinos.Length * 3)
        {
            intentos++;
            Vector3Int candidata = centro + vecinos[Random.Range(0, vecinos.Length)];

            if (celdas.Contains(candidata) || !tilemapBloques.HasTile(candidata) || celdasRompiendose.Contains(candidata))
            {
                continue;
            }

            celdas.Add(candidata);
            celdasRompiendose.Add(candidata);
        }

        return celdas;
    }

    private bool EstaDentroElipse(Vector3 posicion, Vector2 centro, Vector2 radio)
    {
        float dx = (posicion.x - centro.x) / Mathf.Max(0.01f, radio.x);
        float dy = (posicion.y - centro.y) / Mathf.Max(0.01f, radio.y);
        return dx * dx + dy * dy <= 1f;
    }

    private bool EstaDentroRectangulo(Vector3 posicion, Vector2 minimo, Vector2 maximo)
    {
        return posicion.x >= minimo.x
            && posicion.x <= maximo.x
            && posicion.y >= minimo.y
            && posicion.y <= maximo.y;
    }

    private Color ColorBloque(Vector3 posicion)
    {
        float ruido = Mathf.PerlinNoise(posicion.x * 1.7f, posicion.y * 1.7f);
        return Color.Lerp(new Color32(74, 48, 33, 255), new Color32(128, 82, 52, 255), ruido);
    }

    private Tile CrearTileBloque()
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color32 oscuro = new Color32(44, 29, 22, 255);
        Color32 baseColor = new Color32(106, 68, 45, 255);
        Color32 luz = new Color32(151, 101, 62, 255);
        Color32 sombra = new Color32(60, 39, 30, 255);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Color32 color = baseColor;

                if (x <= 1 || y <= 1 || x >= 30 || y >= 30)
                {
                    color = oscuro;
                }
                else if (x < 8 || y > 23)
                {
                    color = luz;
                }
                else if (x > 23 || y < 8)
                {
                    color = sombra;
                }

                if ((x + y) % 11 == 0)
                {
                    color = Color32.Lerp(color, oscuro, 0.25f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        spriteFragmento = sprite;

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        tile.hideFlags = HideFlags.HideAndDontSave;
        return tile;
    }

    private void CrearFragmentos(Vector3 centro, Color color)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject fragmento = new GameObject("Fragmento_Bloque");
            fragmento.transform.position = centro + (Vector3)(Random.insideUnitCircle * 0.1f);
            fragmento.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

            SpriteRenderer rendererFragmento = fragmento.AddComponent<SpriteRenderer>();
            rendererFragmento.sprite = spriteFragmento != null ? spriteFragmento : tileBloque.sprite;
            rendererFragmento.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.2f));
            rendererFragmento.sortingOrder = ordenVisualBloques + 8;

            AnimacionFragmentoBloque animacion = fragmento.AddComponent<AnimacionFragmentoBloque>();
            animacion.velocidad = new Vector3(Random.Range(-2.4f, 2.4f), Random.Range(1.2f, 3.8f), 0f);
            animacion.gravedad = Random.Range(5f, 8f);
            animacion.tiempoVida = Random.Range(0.35f, 0.6f);
        }
    }

    private void CrearSemilla(Vector3 centro)
    {
        GameObject semillaPrefab = ObtenerPrefabSemilla();

        if (semillaPrefab != null)
        {
            GameObject semilla = Instantiate(semillaPrefab, centro, Quaternion.identity);
            semilla.name = semillaPrefab.name;
            foreach (SpriteRenderer rendererSemilla in semilla.GetComponentsInChildren<SpriteRenderer>())
            {
                rendererSemilla.sortingOrder = ordenVisualBloques + 12;
            }
            return;
        }

        CrearSemillaFallback(centro);
    }

    private GameObject ObtenerPrefabSemilla()
    {
        if (prefabsSemillas != null && prefabsSemillas.Length > 0)
        {
            List<GameObject> disponibles = new List<GameObject>();
            foreach (GameObject prefab in prefabsSemillas)
            {
                if (prefab != null)
                {
                    disponibles.Add(prefab);
                }
            }

            if (disponibles.Count > 0)
            {
                return disponibles[Random.Range(0, disponibles.Count)];
            }
        }

        return null;
    }

    private void CrearSemillaFallback(Vector3 centro)
    {
        GameObject semilla = new GameObject("Semilla_Encontrada");
        semilla.transform.position = centro;
        semilla.transform.localScale = Vector3.one * 0.32f;

        TipoSemilla tipo = ObtenerSiguienteTipoSemilla();

        SpriteRenderer rendererSemilla = semilla.AddComponent<SpriteRenderer>();
        rendererSemilla.sprite = CrearSpriteSemillaFallback();
        rendererSemilla.color = ColorSemilla(tipo);
        rendererSemilla.sortingOrder = ordenVisualBloques + 12;

        CircleCollider2D collider = semilla.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.35f;

        SemillaRecolectable recolectable = semilla.AddComponent<SemillaRecolectable>();
        recolectable.tipoSemilla = tipo;
        recolectable.cantidad = 1;
        recolectable.reaparecerDespuesDeRecolectar = false;
    }

    private TipoSemilla ObtenerSiguienteTipoSemilla()
    {
        TipoSemilla tipo = (TipoSemilla)(siguienteTipoSemilla % 4);
        siguienteTipoSemilla++;
        return tipo;
    }

    private Color32 ColorSemilla(TipoSemilla tipo)
    {
        switch (tipo)
        {
            case TipoSemilla.Azul:
                return new Color32(79, 196, 255, 255);
            case TipoSemilla.Amarilla:
                return new Color32(255, 216, 74, 255);
            case TipoSemilla.Morada:
                return new Color32(189, 105, 255, 255);
            default:
                return new Color32(106, 226, 98, 255);
        }
    }

    private Sprite CrearSpriteSemillaFallback()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 verde = new Color32(106, 226, 98, 255);
        Color32 luz = new Color32(205, 255, 160, 255);

        for (int i = 0; i < 16 * 16; i++)
        {
            texture.SetPixel(i % 16, i / 16, transparente);
        }

        for (int y = 3; y < 13; y++)
        {
            for (int x = 5; x < 11; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                if ((dx * dx / 9f) + (dy * dy / 25f) <= 1f)
                {
                    texture.SetPixel(x, y, verde);
                }
            }
        }

        texture.SetPixel(7, 10, luz);
        texture.SetPixel(8, 10, luz);
        texture.SetPixel(7, 9, luz);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}

public class AnimacionFragmentoBloque : MonoBehaviour
{
    public Vector3 velocidad;
    public float gravedad = 6f;
    public float tiempoVida = 0.5f;

    private float tiempo;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        tiempo += Time.deltaTime;
        velocidad.y -= gravedad * Time.deltaTime;
        transform.position += velocidad * Time.deltaTime;
        transform.Rotate(0f, 0f, 320f * Time.deltaTime);

        float t = Mathf.Clamp01(tiempo / Mathf.Max(0.01f, tiempoVida));
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, t * 0.35f);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - t;
            spriteRenderer.color = color;
        }

        if (tiempo >= tiempoVida)
        {
            Destroy(gameObject);
        }
    }
}
