using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum TipoRecursoMinado
{
    Tierra,
    Piedra,
    Cobre,
    Oro
}

[DisallowMultipleComponent]
public class SpriteMinableBlock : MonoBehaviour
{
    public TipoRecursoMinado tipoRecurso = TipoRecursoMinado.Piedra;
    public int cantidad = 1;
    public int golpesNecesarios = 1;
    public bool requiereClicksContinuos;
    public float segundosClicksContinuos = 3f;
    public float pausaMaximaEntreClicks = 0.65f;

    private int golpesActuales;
    private float progresoClicksContinuos;
    private float ultimoClickTime = -999f;
    private SpriteRenderer spriteRenderer;
    private bool rompiendose;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        AsegurarColliderMinable();
    }

    public void Picar()
    {
        if (rompiendose)
        {
            return;
        }

        if (requiereClicksContinuos)
        {
            PicarConClicksContinuos();
            return;
        }

        golpesActuales++;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.88f, 0.45f, 1f), 0.45f);
        }

        if (golpesActuales >= Mathf.Max(1, golpesNecesarios))
        {
            StartCoroutine(Romper());
        }
    }

    private void PicarConClicksContinuos()
    {
        float pausa = Time.time - ultimoClickTime;
        if (pausa > pausaMaximaEntreClicks)
        {
            progresoClicksContinuos = 0f;
        }
        else
        {
            progresoClicksContinuos += pausa;
        }

        ultimoClickTime = Time.time;
        float progresoNormalizado = Mathf.Clamp01(progresoClicksContinuos / Mathf.Max(0.1f, segundosClicksContinuos));

        if (spriteRenderer != null)
        {
            Color golpe = Color.Lerp(new Color(1f, 0.88f, 0.45f, 1f), new Color(1f, 0.58f, 0.28f, 1f), progresoNormalizado);
            spriteRenderer.color = Color.Lerp(Color.white, golpe, Mathf.Lerp(0.35f, 0.85f, progresoNormalizado));
        }

        if (progresoClicksContinuos >= segundosClicksContinuos)
        {
            StartCoroutine(Romper());
        }
    }

    private IEnumerator Romper()
    {
        rompiendose = true;
        yield return new WaitForSeconds(0.05f);

        RegistrarRecurso();
        CrearFragmentos();
        Destroy(gameObject);
    }

    private void RegistrarRecurso()
    {
        switch (tipoRecurso)
        {
            case TipoRecursoMinado.Tierra:
            case TipoRecursoMinado.Piedra:
                AlmacenSistema.AgregarCubosGlobal(cantidad);
                break;
            case TipoRecursoMinado.Cobre:
                InventarioMinerales.AgregarCobre(cantidad);
                break;
            case TipoRecursoMinado.Oro:
                InventarioMinerales.AgregarOro(cantidad);
                break;
        }
    }

    private void CrearFragmentos()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject fragmento = new GameObject("Fragmento_" + tipoRecurso);
            fragmento.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.08f);
            fragmento.transform.localScale = transform.lossyScale * Random.Range(0.12f, 0.22f);

            SpriteRenderer rendererFragmento = fragmento.AddComponent<SpriteRenderer>();
            rendererFragmento.sprite = spriteRenderer.sprite;
            rendererFragmento.color = spriteRenderer.color;
            rendererFragmento.sortingOrder = spriteRenderer.sortingOrder + 8;

            AnimacionFragmentoBloque animacion = fragmento.AddComponent<AnimacionFragmentoBloque>();
            animacion.velocidad = new Vector3(Random.Range(-2.1f, 2.1f), Random.Range(1.1f, 3.2f), 0f);
            animacion.gravedad = Random.Range(5f, 8f);
            animacion.tiempoVida = Random.Range(0.3f, 0.55f);
        }
    }

    private void AsegurarColliderMinable()
    {
        Collider2D colliderExistente = GetComponent<Collider2D>();
        if (colliderExistente != null)
        {
            return;
        }

        Collider collider3D = GetComponent<Collider>();
        if (collider3D != null)
        {
            DestroyImmediate(collider3D);
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            box = gameObject.AddComponent<BoxCollider2D>();
        }

        if (box != null)
        {
            box.isTrigger = true;
        }
    }
}

public class SpriteMinadoRuntime : MonoBehaviour
{
    public bool ignorarClicksSobreUI = true;
    public bool limitarMinadoPorMirada = true;
    public float margenMinadoPorMirada = 0.15f;
    public float anchoMinadoHaciaAbajo = 1.25f;
    public float alturaMinadoHaciaAbajo = 0.25f;
    public Vector2 rocaDuraZonaMin = new Vector2(9.4f, -16.9f);
    public Vector2 rocaDuraZonaMax = new Vector2(16.9f, -9.3f);
    public float segundosRocaDura = 3f;

    private Camera camaraPrincipal;
    private Transform jugador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnSampleScene()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AsegurarInstancia();
    }

    private static void AlCargarEscena(Scene scene, LoadSceneMode mode)
    {
        AsegurarInstancia();
    }

    private static void AsegurarInstancia()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" || FindFirstObjectByType<SpriteMinadoRuntime>() != null)
        {
            return;
        }

        GameObject sistema = new GameObject("Sistema_Minado_Sprites");
        sistema.AddComponent<SpriteMinadoRuntime>();
    }

    private void Awake()
    {
        camaraPrincipal = Camera.main;
        AsegurarJugador();
        ConvertirSpritesMinables();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (ignorarClicksSobreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        IntentarPicarSprite();
    }

    private void ConvertirSpritesMinables()
    {
        foreach (SpriteRenderer renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || renderer.GetComponent<SpriteMinableBlock>() != null || !EsSpriteMinable(renderer, out TipoRecursoMinado tipo))
            {
                continue;
            }

            SpriteMinableBlock bloque = renderer.gameObject.AddComponent<SpriteMinableBlock>();
            bloque.tipoRecurso = tipo;
            bloque.cantidad = tipo == TipoRecursoMinado.Oro ? 2 : 1;
            bloque.golpesNecesarios = tipo == TipoRecursoMinado.Tierra ? 1 : 2;

            if (EsRocaDuraDeZona(renderer))
            {
                bloque.tipoRecurso = TipoRecursoMinado.Piedra;
                bloque.cantidad = 1;
                bloque.requiereClicksContinuos = true;
                bloque.segundosClicksContinuos = segundosRocaDura;
            }
        }
    }

    private void IntentarPicarSprite()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (camaraPrincipal == null)
        {
            return;
        }

        Vector3 mundo = camaraPrincipal.ScreenToWorldPoint(Input.mousePosition);
        mundo.z = 0f;

        if (!PuedeMinarEnDireccionDeMirada(mundo))
        {
            return;
        }

        TilemapCavableZone zonaCavable = BuscarZonaTilemapCavableEnPunto(mundo);
        if (zonaCavable != null && zonaCavable.Picar(mundo))
        {
            return;
        }

        SpriteMinableBlock bloque = BuscarBloqueMinableEnPunto(mundo);
        if (bloque == null)
        {
            return;
        }

        bloque.Picar();
    }

    private TilemapCavableZone BuscarZonaTilemapCavableEnPunto(Vector3 mundo)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(mundo);
        foreach (Collider2D collider in colliders)
        {
            TilemapCavableZone zona = collider.GetComponent<TilemapCavableZone>();
            if (zona != null)
            {
                return zona;
            }
        }

        return null;
    }

    private SpriteMinableBlock BuscarBloqueMinableEnPunto(Vector3 mundo)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(mundo);
        SpriteMinableBlock mejor = null;
        int mejorOrden = int.MinValue;

        foreach (Collider2D collider in colliders)
        {
            SpriteMinableBlock bloque = collider.GetComponent<SpriteMinableBlock>();
            if (bloque == null)
            {
                continue;
            }

            SpriteRenderer renderer = bloque.GetComponent<SpriteRenderer>();
            int orden = renderer != null ? renderer.sortingOrder : 0;
            if (mejor == null || orden >= mejorOrden)
            {
                mejor = bloque;
                mejorOrden = orden;
            }
        }

        return mejor;
    }

    private bool PuedeMinarEnDireccionDeMirada(Vector3 objetivo)
    {
        if (!limitarMinadoPorMirada)
        {
            return true;
        }

        AsegurarJugador();
        if (jugador == null)
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

    private void AsegurarJugador()
    {
        if (jugador != null)
        {
            return;
        }

        GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
        jugador = jugadorObjeto != null ? jugadorObjeto.transform : null;
    }

    private bool EsSpriteMinable(SpriteRenderer renderer, out TipoRecursoMinado tipo)
    {
        tipo = TipoRecursoMinado.Piedra;
        string nombreObjeto = renderer.gameObject.name.ToLowerInvariant();
        string nombreSprite = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
        string nombre = nombreSprite + " " + nombreObjeto;
        bool marcadoComoCavable = nombreObjeto.Contains("cavable")
            || nombreObjeto.Contains("minable")
            || nombreObjeto.Contains("cavar")
            || nombreObjeto.Contains("cabar")
            || nombreObjeto.Contains("cubo_cavable")
            || nombreObjeto.Contains("zona_cavable");

        if (!marcadoComoCavable && !EsRocaDuraDeZona(renderer))
        {
            return false;
        }

        if (nombre.Contains("fondo") || nombre.Contains("piso") || nombre.Contains("contaminado") || nombre.Contains("granja"))
        {
            return false;
        }

        if (nombre.Contains("oro") || nombre.Contains("gold"))
        {
            tipo = TipoRecursoMinado.Oro;
            return true;
        }

        if (nombre.Contains("cobre") || nombre.Contains("copper") || nombre.Contains("plata"))
        {
            tipo = TipoRecursoMinado.Cobre;
            return true;
        }

        if (nombre.Contains("tierra"))
        {
            tipo = TipoRecursoMinado.Tierra;
            return true;
        }

        if (nombre.Contains("piedra") || nombre.Contains("piedras") || nombre.Contains("rock") || nombre.Contains("stone"))
        {
            tipo = TipoRecursoMinado.Piedra;
            return true;
        }

        if (nombre.Contains("minable") || nombre.Contains("cavable") || nombre.Contains("cavar") || nombre.Contains("cabar"))
        {
            tipo = TipoRecursoMinado.Tierra;
            return true;
        }

        return false;
    }

    private bool EsRocaDuraDeZona(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        string nombreObjeto = renderer.gameObject.name.ToLowerInvariant();
        string nombreSprite = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
        string nombre = nombreSprite + " " + nombreObjeto;
        bool esRoca = nombre.Contains("piedra") || nombre.Contains("piedras") || nombre.Contains("rock") || nombre.Contains("stone");
        if (!esRoca)
        {
            return false;
        }

        Bounds bounds = renderer.bounds;
        return IntersectaRect(bounds, rocaDuraZonaMin, rocaDuraZonaMax)
            || IntersectaRect(bounds, new Vector2(-15.8f, -28.8f), new Vector2(-6.3f, -20.0f));
    }

    private static bool IntersectaRect(Bounds bounds, Vector2 minimo, Vector2 maximo)
    {
        return bounds.max.x >= minimo.x
            && bounds.min.x <= maximo.x
            && bounds.max.y >= minimo.y
            && bounds.min.y <= maximo.y;
    }
}

public static class InventarioMinerales
{
    private const string KeyCobre = "InventarioMinerales_Cobre";
    private const string KeyOro = "InventarioMinerales_Oro";

    public static int Cobre => PlayerPrefs.GetInt(KeyCobre, 0);
    public static int Oro => PlayerPrefs.GetInt(KeyOro, 0);

    public static void AgregarCobre(int cantidad)
    {
        Agregar(KeyCobre, "Cobre", cantidad);
    }

    public static void AgregarOro(int cantidad)
    {
        Agregar(KeyOro, "Oro", cantidad);
    }

    private static void Agregar(string key, string nombre, int cantidad)
    {
        if (cantidad <= 0)
        {
            return;
        }

        int total = PlayerPrefs.GetInt(key, 0) + cantidad;
        PlayerPrefs.SetInt(key, total);
        PlayerPrefs.Save();
        Debug.Log("Minaste " + nombre + " x" + cantidad + ". Total: " + total);
    }
}
