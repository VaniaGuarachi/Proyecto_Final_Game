using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AlmacenSistema : MonoBehaviour
{
    public static AlmacenSistema instancia;

    public const int CubosNecesariosGranja = 30;
    public const int CubosNecesariosAgrandarGranja = 20;
    public const int CubosNecesariosAlmacenAlimentos = 30;
    public const int CubosNecesariosAgrandarAlmacenAlimentos = 20;
    public const int NivelMaximoGranja = 4;
    public const int NivelMaximoAlmacenAlimentos = 4;

    private const string KeyCubos = "Almacen_Cubos";
    private const string KeyGranjasDisponibles = "Almacen_GranjasDisponibles";
    private const string KeyAlmacenesAlimentosDisponibles = "Almacen_AlimentosDisponibles";
    private const string KeyConstruyendoAlmacenAlimentos = "Almacen_ConstruyendoAlimentos";
    private const string KeyProgresoAlmacenAlimentos = "Almacen_ProgresoAlimentos";
    private const string KeyAlmacenesAlimentosPosiciones = "Almacen_AlimentosPosiciones";
    private const string KeyAlmacenesAlimentosNiveles = "Almacen_AlimentosNiveles";
    private const string KeyConstruyendo = "Almacen_ConstruyendoGranja";
    private const string KeyProgreso = "Almacen_ProgresoGranja";
    private const string KeyGranjasColocadas = "Almacen_GranjasColocadas";
    private const string KeyGranjasPosiciones = "Almacen_GranjasPosiciones";
    private const string KeyGranjasNiveles = "Almacen_GranjasNiveles";

    [Header("Construccion")]
    public float duracionConstruccionGranja = 5f;
    public bool reiniciarAlmacenEnCeroAlIniciar = true;

    [Header("Colocacion")]
    public GameObject prefabGranja;
    public Vector2 tamanoValidacion = new Vector2(5.2f, 3.4f);
    public LayerMask capasBloqueadas = ~0;
    public bool restaurarGranjasColocadasAlIniciar = false;

    public int CubosDisponibles { get; private set; }
    public int GranjasDisponibles { get; private set; }
    public int AlmacenesAlimentosDisponibles { get; private set; }
    public int GranjasColocadas { get; private set; }
    public bool ConstruyendoGranja { get; private set; }
    public bool ConstruyendoAlmacenAlimentos { get; private set; }
    public float ProgresoConstruccion { get; private set; }
    public float ProgresoAlmacenAlimentos { get; private set; }
    public float ProgresoNormalizado => ConstruyendoGranja ? Mathf.Clamp01(ProgresoConstruccion / Mathf.Max(0.01f, duracionConstruccionGranja)) : 0f;
    public float ProgresoAlmacenAlimentosNormalizado => ConstruyendoAlmacenAlimentos ? Mathf.Clamp01(ProgresoAlmacenAlimentos / Mathf.Max(0.01f, duracionConstruccionGranja)) : 0f;
    public int CubosUsadosConstruccion => ConstruyendoGranja ? Mathf.Clamp(Mathf.CeilToInt(ProgresoNormalizado * CubosNecesariosGranja), 0, CubosNecesariosGranja) : 0;
    public int CubosUsadosAlmacenAlimentos => ConstruyendoAlmacenAlimentos ? Mathf.Clamp(Mathf.CeilToInt(ProgresoAlmacenAlimentosNormalizado * CubosNecesariosAlmacenAlimentos), 0, CubosNecesariosAlmacenAlimentos) : 0;
    public int NivelUltimaGranja => ObtenerNivelUltimaGranja();
    public int NivelUltimoAlmacenAlimentos => ObtenerNivelUltimoAlmacenAlimentos();

    public event Action EstadoCambiado;

    private readonly List<Vector3> posicionesGranjasColocadas = new List<Vector3>();
    private readonly List<int> nivelesGranjasColocadas = new List<int>();
    private readonly List<Vector3> posicionesAlmacenesAlimentos = new List<Vector3>();
    private readonly List<int> nivelesAlmacenesAlimentos = new List<int>();
    private static bool almacenReiniciadoEstaSesion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InicializarEnEscena()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AsegurarInstanciaSiHayGameplay();
    }

    private static void AlCargarEscena(Scene scene, LoadSceneMode mode)
    {
        AsegurarInstanciaSiHayGameplay();
    }

    public static void AgregarCubosGlobal(int cantidad)
    {
        if (cantidad <= 0)
        {
            return;
        }

        AsegurarInstancia();
        instancia.AgregarCubos(cantidad);
    }

    public static AlmacenSistema AsegurarInstancia()
    {
        if (instancia != null)
        {
            return instancia;
        }

        AlmacenSistema existente = FindFirstObjectByType<AlmacenSistema>();
        if (existente != null)
        {
            instancia = existente;
            return instancia;
        }

        GameObject sistema = new GameObject("Sistema_Almacen");
        instancia = sistema.AddComponent<AlmacenSistema>();
        sistema.AddComponent<UIAlmacen>();
        return instancia;
    }

    private static void AsegurarInstanciaSiHayGameplay()
    {
        if (!HayGameplayEnEscena())
        {
            return;
        }

        AlmacenSistema sistema = AsegurarInstancia();
        if (sistema.GetComponent<UIAlmacen>() == null)
        {
            sistema.gameObject.AddComponent<UIAlmacen>();
        }
    }

    private static bool HayGameplayEnEscena()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name == "SampleScene")
        {
            return true;
        }

        return FindFirstObjectByType<Player1>() != null
            || FindFirstObjectByType<SampleSceneMinaController>() != null
            || FindFirstObjectByType<ControlMinado>() != null;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        if (reiniciarAlmacenEnCeroAlIniciar && !almacenReiniciadoEstaSesion)
        {
            LimpiarEstadoPersistente();
            almacenReiniciadoEstaSesion = true;
        }

        CargarEstado();
    }

    private void Start()
    {
        if (restaurarGranjasColocadasAlIniciar)
        {
            RestaurarGranjasColocadas();
            return;
        }

        posicionesGranjasColocadas.Clear();
        nivelesGranjasColocadas.Clear();
        GranjasColocadas = 0;
        GuardarEstado();
        NotificarCambio();
    }

    private void Update()
    {
        if (!ConstruyendoGranja)
        {
            if (!ConstruyendoAlmacenAlimentos)
            {
                return;
            }
        }

        bool cambio = false;
        if (ConstruyendoGranja)
        {
            ProgresoConstruccion += Time.deltaTime;
            if (ProgresoConstruccion >= duracionConstruccionGranja)
            {
                ConstruyendoGranja = false;
                ProgresoConstruccion = 0f;
                GranjasDisponibles++;
            }

            cambio = true;
        }

        if (ConstruyendoAlmacenAlimentos)
        {
            ProgresoAlmacenAlimentos += Time.deltaTime;
            if (ProgresoAlmacenAlimentos >= duracionConstruccionGranja)
            {
                ConstruyendoAlmacenAlimentos = false;
                ProgresoAlmacenAlimentos = 0f;
                AlmacenesAlimentosDisponibles++;
            }

            cambio = true;
        }

        if (cambio)
        {
            GuardarEstado();
            NotificarCambio();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            GuardarEstado();
        }
    }

    private void OnApplicationQuit()
    {
        GuardarEstado();
    }

    public void AgregarCubos(int cantidad)
    {
        if (cantidad <= 0)
        {
            return;
        }

        CubosDisponibles += cantidad;
        GuardarEstado();
        NotificarCambio();
    }

    public bool PuedeCrearGranja()
    {
        return CubosDisponibles >= CubosNecesariosGranja && !ConstruyendoGranja;
    }

    public bool PuedeCrearAlmacenAlimentos()
    {
        return CubosDisponibles >= CubosNecesariosAlmacenAlimentos && !ConstruyendoAlmacenAlimentos;
    }

    public bool IntentarCrearGranja()
    {
        if (!PuedeCrearGranja())
        {
            return false;
        }

        CubosDisponibles -= CubosNecesariosGranja;
        ConstruyendoGranja = true;
        ProgresoConstruccion = 0f;
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool IntentarCrearAlmacenAlimentos()
    {
        if (!PuedeCrearAlmacenAlimentos())
        {
            return false;
        }

        CubosDisponibles -= CubosNecesariosAlmacenAlimentos;
        ConstruyendoAlmacenAlimentos = true;
        ProgresoAlmacenAlimentos = 0f;
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool IntentarColocarGranja(Vector2 posicionPantalla, Camera camara)
    {
        if (GranjasDisponibles <= 0 || camara == null)
        {
            return false;
        }

        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(posicionPantalla.x, posicionPantalla.y, Mathf.Abs(camara.transform.position.z)));
        mundo.z = 0f;
        mundo = AjustarPosicionColocacion(mundo);

        if (!EsZonaValida(mundo, tamanoValidacion, null))
        {
            return false;
        }

        CrearGranjaEnEscena(mundo, posicionesGranjasColocadas.Count);
        posicionesGranjasColocadas.Add(mundo);
        nivelesGranjasColocadas.Add(1);
        GranjasDisponibles--;
        GranjasColocadas = posicionesGranjasColocadas.Count;
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool IntentarColocarAlmacenAlimentos(Vector2 posicionPantalla, Camera camara)
    {
        if (AlmacenesAlimentosDisponibles <= 0 || camara == null)
        {
            return false;
        }

        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(posicionPantalla.x, posicionPantalla.y, Mathf.Abs(camara.transform.position.z)));
        mundo.z = 0f;
        mundo = AjustarPosicionColocacion(mundo);

        if (!EsZonaValida(mundo, tamanoValidacion, null))
        {
            return false;
        }

        CrearAlmacenAlimentosEnEscena(mundo, posicionesAlmacenesAlimentos.Count);
        posicionesAlmacenesAlimentos.Add(mundo);
        nivelesAlmacenesAlimentos.Add(1);
        AlmacenesAlimentosDisponibles--;
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool PuedeAgrandarUltimaGranja()
    {
        return CubosDisponibles >= CubosNecesariosAgrandarGranja
            && posicionesGranjasColocadas.Count > 0
            && ObtenerNivelUltimaGranja() < NivelMaximoGranja;
    }

    public bool IntentarAgrandarUltimaGranja()
    {
        if (!PuedeAgrandarUltimaGranja())
        {
            return false;
        }

        int indice = posicionesGranjasColocadas.Count - 1;
        AsegurarNivelesSincronizados();
        nivelesGranjasColocadas[indice] = Mathf.Clamp(nivelesGranjasColocadas[indice] + 1, 1, NivelMaximoGranja);
        CubosDisponibles -= CubosNecesariosAgrandarGranja;

        GranjaColocada granja = BuscarGranjaColocada(indice);
        if (granja != null)
        {
            granja.AplicarNivel(nivelesGranjasColocadas[indice], tamanoValidacion);
        }

        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool PuedeAgrandarUltimoAlmacenAlimentos()
    {
        return CubosDisponibles >= CubosNecesariosAgrandarAlmacenAlimentos
            && posicionesAlmacenesAlimentos.Count > 0
            && ObtenerNivelUltimoAlmacenAlimentos() < NivelMaximoAlmacenAlimentos;
    }

    public bool IntentarAgrandarUltimoAlmacenAlimentos()
    {
        if (!PuedeAgrandarUltimoAlmacenAlimentos())
        {
            return false;
        }

        int indice = posicionesAlmacenesAlimentos.Count - 1;
        AsegurarNivelesAlmacenesSincronizados();
        int nuevoNivel = Mathf.Clamp(nivelesAlmacenesAlimentos[indice] + 1, 1, NivelMaximoAlmacenAlimentos);
        float escala = AlmacenAlimentosColocado.CalcularEscala(nuevoNivel);

        AlmacenAlimentosColocado almacenColocado = BuscarAlmacenAlimentosColocado(indice);
        Transform raizIgnorada = almacenColocado != null ? almacenColocado.transform : null;
        if (!EsZonaValida(posicionesAlmacenesAlimentos[indice], tamanoValidacion * 0.78f * escala, raizIgnorada))
        {
            return false;
        }

        nivelesAlmacenesAlimentos[indice] = nuevoNivel;
        CubosDisponibles -= CubosNecesariosAgrandarAlmacenAlimentos;

        if (almacenColocado != null)
        {
            almacenColocado.AplicarNivel(nuevoNivel, tamanoValidacion * 0.78f);
        }

        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool IntentarCambiarTamanoGranja(GranjaColocada granja, int cambioNivel)
    {
        if (granja == null || cambioNivel == 0)
        {
            return false;
        }

        int indice = granja.indiceGuardado;
        if (indice < 0 || indice >= posicionesGranjasColocadas.Count)
        {
            return false;
        }

        AsegurarNivelesSincronizados();
        int nivelActual = ObtenerNivelGranja(indice);
        int nuevoNivel = Mathf.Clamp(nivelActual + cambioNivel, 1, NivelMaximoGranja);
        if (nuevoNivel == nivelActual)
        {
            return false;
        }

        float escala = GranjaColocada.CalcularEscala(nuevoNivel);
        if (!EsZonaValida(granja.transform.position, tamanoValidacion * escala, granja.transform))
        {
            return false;
        }

        nivelesGranjasColocadas[indice] = nuevoNivel;
        granja.AplicarNivel(nuevoNivel, tamanoValidacion);
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public bool PuedeCambiarTamanoGranja(GranjaColocada granja, int cambioNivel)
    {
        if (granja == null || cambioNivel == 0)
        {
            return false;
        }

        int indice = granja.indiceGuardado;
        if (indice < 0 || indice >= posicionesGranjasColocadas.Count)
        {
            return false;
        }

        int nuevoNivel = Mathf.Clamp(ObtenerNivelGranja(indice) + cambioNivel, 1, NivelMaximoGranja);
        return nuevoNivel != ObtenerNivelGranja(indice);
    }

    public bool PrevisualizarMoverGranja(GranjaColocada granja, Vector2 posicionPantalla, Camera camara)
    {
        if (granja == null || camara == null)
        {
            return false;
        }

        Vector3 mundo = PantallaAMundoAjustado(posicionPantalla, camara);
        float escala = GranjaColocada.CalcularEscala(granja.nivel);
        if (!EsZonaValida(mundo, tamanoValidacion * escala, granja.transform))
        {
            return false;
        }

        granja.transform.position = mundo;
        return true;
    }

    public bool ConfirmarPosicionGranja(GranjaColocada granja)
    {
        if (granja == null)
        {
            return false;
        }

        int indice = granja.indiceGuardado;
        if (indice < 0 || indice >= posicionesGranjasColocadas.Count)
        {
            return false;
        }

        posicionesGranjasColocadas[indice] = AjustarPosicionColocacion(granja.transform.position);
        granja.transform.position = posicionesGranjasColocadas[indice];
        GuardarEstado();
        NotificarCambio();
        return true;
    }

    public void RestaurarPosicionGranja(GranjaColocada granja, Vector3 posicion)
    {
        if (granja == null)
        {
            return;
        }

        granja.transform.position = posicion;
    }

    private Vector3 PantallaAMundoAjustado(Vector2 posicionPantalla, Camera camara)
    {
        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(posicionPantalla.x, posicionPantalla.y, Mathf.Abs(camara.transform.position.z)));
        mundo.z = 0f;
        return AjustarPosicionColocacion(mundo);
    }

    private Vector3 AjustarPosicionColocacion(Vector3 posicion)
    {
        return new Vector3(Mathf.Round(posicion.x * 2f) * 0.5f, Mathf.Round(posicion.y * 2f) * 0.5f, 0f);
    }

    private bool EsZonaValida(Vector3 posicion, Vector2 tamano, Transform raizIgnorada)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(posicion, tamano, 0f, capasBloqueadas);
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            if (raizIgnorada != null && (collider.transform == raizIgnorada || collider.transform.IsChildOf(raizIgnorada)))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void CrearGranjaEnEscena(Vector3 posicion, int indiceGuardado = -1)
    {
        GameObject granja = prefabGranja != null
            ? Instantiate(prefabGranja, posicion, Quaternion.identity)
            : GranjaVisualFactory.CrearGranjaFallback(posicion);

        granja.name = "Granja_Colocada";

        GranjaColocada colocada = granja.GetComponent<GranjaColocada>();
        if (colocada == null)
        {
            colocada = granja.AddComponent<GranjaColocada>();
        }

        colocada.indiceGuardado = indiceGuardado >= 0 ? indiceGuardado : posicionesGranjasColocadas.Count;
        int nivel = ObtenerNivelGranja(colocada.indiceGuardado);

        BoxCollider2D collider = granja.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = granja.AddComponent<BoxCollider2D>();
        }

        collider.size = tamanoValidacion;
        collider.isTrigger = false;
        colocada.AplicarNivel(nivel, tamanoValidacion);
    }

    private void CrearAlmacenAlimentosEnEscena(Vector3 posicion, int indiceGuardado = -1)
    {
        GameObject almacenAlimentos = AlmacenAlimentosVisualFactory.CrearAlmacenAlimentos(posicion);
        almacenAlimentos.name = "Almacen_Alimentos_Colocado";
        BoxCollider2D collider = almacenAlimentos.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = almacenAlimentos.AddComponent<BoxCollider2D>();
        }

        collider.size = tamanoValidacion * 0.78f;
        collider.isTrigger = false;

        AlmacenAlimentosColocado colocado = almacenAlimentos.GetComponent<AlmacenAlimentosColocado>();
        if (colocado == null)
        {
            colocado = almacenAlimentos.AddComponent<AlmacenAlimentosColocado>();
        }

        colocado.indiceGuardado = indiceGuardado >= 0 ? indiceGuardado : posicionesAlmacenesAlimentos.Count;
        colocado.AplicarNivel(ObtenerNivelAlmacenAlimentos(colocado.indiceGuardado), tamanoValidacion * 0.78f);
    }

    private void CargarEstado()
    {
        CubosDisponibles = Mathf.Max(0, PlayerPrefs.GetInt(KeyCubos, 0));
        GranjasDisponibles = Mathf.Max(0, PlayerPrefs.GetInt(KeyGranjasDisponibles, 0));
        AlmacenesAlimentosDisponibles = Mathf.Max(0, PlayerPrefs.GetInt(KeyAlmacenesAlimentosDisponibles, 0));
        GranjasColocadas = Mathf.Max(0, PlayerPrefs.GetInt(KeyGranjasColocadas, 0));
        ConstruyendoGranja = PlayerPrefs.GetInt(KeyConstruyendo, 0) == 1;
        ConstruyendoAlmacenAlimentos = PlayerPrefs.GetInt(KeyConstruyendoAlmacenAlimentos, 0) == 1;
        ProgresoConstruccion = Mathf.Max(0f, PlayerPrefs.GetFloat(KeyProgreso, 0f));
        ProgresoAlmacenAlimentos = Mathf.Max(0f, PlayerPrefs.GetFloat(KeyProgresoAlmacenAlimentos, 0f));

        posicionesGranjasColocadas.Clear();
        string posicionesJson = PlayerPrefs.GetString(KeyGranjasPosiciones, string.Empty);
        if (!string.IsNullOrEmpty(posicionesJson))
        {
            ListaPosicionesGuardadas guardado = JsonUtility.FromJson<ListaPosicionesGuardadas>(posicionesJson);
            if (guardado != null && guardado.posiciones != null)
            {
                posicionesGranjasColocadas.AddRange(guardado.posiciones);
                GranjasColocadas = Mathf.Max(GranjasColocadas, posicionesGranjasColocadas.Count);
            }
        }

        nivelesGranjasColocadas.Clear();
        string nivelesJson = PlayerPrefs.GetString(KeyGranjasNiveles, string.Empty);
        if (!string.IsNullOrEmpty(nivelesJson))
        {
            ListaNivelesGuardados guardadoNiveles = JsonUtility.FromJson<ListaNivelesGuardados>(nivelesJson);
            if (guardadoNiveles != null && guardadoNiveles.niveles != null)
            {
                nivelesGranjasColocadas.AddRange(guardadoNiveles.niveles);
            }
        }

        AsegurarNivelesSincronizados();

        posicionesAlmacenesAlimentos.Clear();
        string posicionesAlimentosJson = PlayerPrefs.GetString(KeyAlmacenesAlimentosPosiciones, string.Empty);
        if (!string.IsNullOrEmpty(posicionesAlimentosJson))
        {
            ListaPosicionesGuardadas guardadoAlimentos = JsonUtility.FromJson<ListaPosicionesGuardadas>(posicionesAlimentosJson);
            if (guardadoAlimentos != null && guardadoAlimentos.posiciones != null)
            {
                posicionesAlmacenesAlimentos.AddRange(guardadoAlimentos.posiciones);
            }
        }

        nivelesAlmacenesAlimentos.Clear();
        string nivelesAlimentosJson = PlayerPrefs.GetString(KeyAlmacenesAlimentosNiveles, string.Empty);
        if (!string.IsNullOrEmpty(nivelesAlimentosJson))
        {
            ListaNivelesGuardados guardadoNivelesAlimentos = JsonUtility.FromJson<ListaNivelesGuardados>(nivelesAlimentosJson);
            if (guardadoNivelesAlimentos != null && guardadoNivelesAlimentos.niveles != null)
            {
                nivelesAlmacenesAlimentos.AddRange(guardadoNivelesAlimentos.niveles);
            }
        }

        AsegurarNivelesAlmacenesSincronizados();
    }

    private void GuardarEstado()
    {
        GranjasColocadas = posicionesGranjasColocadas.Count;
        AsegurarNivelesSincronizados();
        PlayerPrefs.SetInt(KeyCubos, CubosDisponibles);
        PlayerPrefs.SetInt(KeyGranjasDisponibles, GranjasDisponibles);
        PlayerPrefs.SetInt(KeyAlmacenesAlimentosDisponibles, AlmacenesAlimentosDisponibles);
        PlayerPrefs.SetInt(KeyGranjasColocadas, GranjasColocadas);
        PlayerPrefs.SetInt(KeyConstruyendo, ConstruyendoGranja ? 1 : 0);
        PlayerPrefs.SetInt(KeyConstruyendoAlmacenAlimentos, ConstruyendoAlmacenAlimentos ? 1 : 0);
        PlayerPrefs.SetFloat(KeyProgreso, ProgresoConstruccion);
        PlayerPrefs.SetFloat(KeyProgresoAlmacenAlimentos, ProgresoAlmacenAlimentos);
        PlayerPrefs.SetString(KeyGranjasPosiciones, JsonUtility.ToJson(new ListaPosicionesGuardadas(posicionesGranjasColocadas)));
        PlayerPrefs.SetString(KeyGranjasNiveles, JsonUtility.ToJson(new ListaNivelesGuardados(nivelesGranjasColocadas)));
        PlayerPrefs.SetString(KeyAlmacenesAlimentosPosiciones, JsonUtility.ToJson(new ListaPosicionesGuardadas(posicionesAlmacenesAlimentos)));
        PlayerPrefs.SetString(KeyAlmacenesAlimentosNiveles, JsonUtility.ToJson(new ListaNivelesGuardados(nivelesAlmacenesAlimentos)));
        PlayerPrefs.Save();
    }

    private void NotificarCambio()
    {
        EstadoCambiado?.Invoke();
    }

    private void RestaurarGranjasColocadas()
    {
        if (posicionesGranjasColocadas.Count == 0 || FindObjectsByType<GranjaColocada>(FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        for (int i = 0; i < posicionesGranjasColocadas.Count; i++)
        {
            CrearGranjaEnEscena(posicionesGranjasColocadas[i], i);
        }
    }

    private int ObtenerNivelUltimaGranja()
    {
        if (posicionesGranjasColocadas.Count == 0)
        {
            return 0;
        }

        return ObtenerNivelGranja(posicionesGranjasColocadas.Count - 1);
    }

    private int ObtenerNivelGranja(int indice)
    {
        AsegurarNivelesSincronizados();
        if (indice < 0 || indice >= nivelesGranjasColocadas.Count)
        {
            return 1;
        }

        return Mathf.Clamp(nivelesGranjasColocadas[indice], 1, NivelMaximoGranja);
    }

    private int ObtenerNivelUltimoAlmacenAlimentos()
    {
        if (posicionesAlmacenesAlimentos.Count == 0)
        {
            return 0;
        }

        return ObtenerNivelAlmacenAlimentos(posicionesAlmacenesAlimentos.Count - 1);
    }

    private int ObtenerNivelAlmacenAlimentos(int indice)
    {
        AsegurarNivelesAlmacenesSincronizados();
        if (indice < 0 || indice >= nivelesAlmacenesAlimentos.Count)
        {
            return 1;
        }

        return Mathf.Clamp(nivelesAlmacenesAlimentos[indice], 1, NivelMaximoAlmacenAlimentos);
    }

    private void AsegurarNivelesSincronizados()
    {
        while (nivelesGranjasColocadas.Count < posicionesGranjasColocadas.Count)
        {
            nivelesGranjasColocadas.Add(1);
        }

        while (nivelesGranjasColocadas.Count > posicionesGranjasColocadas.Count)
        {
            nivelesGranjasColocadas.RemoveAt(nivelesGranjasColocadas.Count - 1);
        }

        for (int i = 0; i < nivelesGranjasColocadas.Count; i++)
        {
            nivelesGranjasColocadas[i] = Mathf.Clamp(nivelesGranjasColocadas[i], 1, NivelMaximoGranja);
        }
    }

    private void AsegurarNivelesAlmacenesSincronizados()
    {
        while (nivelesAlmacenesAlimentos.Count < posicionesAlmacenesAlimentos.Count)
        {
            nivelesAlmacenesAlimentos.Add(1);
        }

        while (nivelesAlmacenesAlimentos.Count > posicionesAlmacenesAlimentos.Count)
        {
            nivelesAlmacenesAlimentos.RemoveAt(nivelesAlmacenesAlimentos.Count - 1);
        }

        for (int i = 0; i < nivelesAlmacenesAlimentos.Count; i++)
        {
            nivelesAlmacenesAlimentos[i] = Mathf.Clamp(nivelesAlmacenesAlimentos[i], 1, NivelMaximoAlmacenAlimentos);
        }
    }

    private GranjaColocada BuscarGranjaColocada(int indice)
    {
        foreach (GranjaColocada granja in FindObjectsByType<GranjaColocada>(FindObjectsSortMode.None))
        {
            if (granja.indiceGuardado == indice)
            {
                return granja;
            }
        }

        return null;
    }

    private AlmacenAlimentosColocado BuscarAlmacenAlimentosColocado(int indice)
    {
        foreach (AlmacenAlimentosColocado almacenAlimentos in FindObjectsByType<AlmacenAlimentosColocado>(FindObjectsSortMode.None))
        {
            if (almacenAlimentos.indiceGuardado == indice)
            {
                return almacenAlimentos;
            }
        }

        return null;
    }

    private void LimpiarEstadoPersistente()
    {
        PlayerPrefs.DeleteKey(KeyCubos);
        PlayerPrefs.DeleteKey(KeyGranjasDisponibles);
        PlayerPrefs.DeleteKey(KeyAlmacenesAlimentosDisponibles);
        PlayerPrefs.DeleteKey(KeyConstruyendoAlmacenAlimentos);
        PlayerPrefs.DeleteKey(KeyProgresoAlmacenAlimentos);
        PlayerPrefs.DeleteKey(KeyAlmacenesAlimentosPosiciones);
        PlayerPrefs.DeleteKey(KeyAlmacenesAlimentosNiveles);
        PlayerPrefs.DeleteKey(KeyConstruyendo);
        PlayerPrefs.DeleteKey(KeyProgreso);
        PlayerPrefs.DeleteKey(KeyGranjasColocadas);
        PlayerPrefs.DeleteKey(KeyGranjasPosiciones);
        PlayerPrefs.DeleteKey(KeyGranjasNiveles);
        PlayerPrefs.Save();
    }

    [Serializable]
    private class ListaPosicionesGuardadas
    {
        public List<Vector3> posiciones = new List<Vector3>();

        public ListaPosicionesGuardadas()
        {
        }

        public ListaPosicionesGuardadas(List<Vector3> posiciones)
        {
            this.posiciones = new List<Vector3>(posiciones);
        }
    }

    [Serializable]
    private class ListaNivelesGuardados
    {
        public List<int> niveles = new List<int>();

        public ListaNivelesGuardados()
        {
        }

        public ListaNivelesGuardados(List<int> niveles)
        {
            this.niveles = new List<int>(niveles);
        }
    }
}

public class GranjaColocada : MonoBehaviour
{
    public int indiceGuardado;
    public int nivel = 1;

    private const float DistanciaMinimaArrastre = 6f;

    private Vector2 pantallaInicioArrastre;
    private bool arrastrando;
    private bool movioDuranteArrastre;
    private SpriteRenderer[] renderers;
    private Color[] coloresOriginales;

    public static float CalcularEscala(int nivel)
    {
        return 1f + (Mathf.Clamp(nivel, 1, AlmacenSistema.NivelMaximoGranja) - 1) * 0.25f;
    }

    public void AplicarNivel(int nuevoNivel, Vector2 tamanoCollider)
    {
        nivel = Mathf.Clamp(nuevoNivel, 1, AlmacenSistema.NivelMaximoGranja);
        float escala = CalcularEscala(nivel);
        transform.localScale = Vector3.one * escala;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.size = tamanoCollider;
        collider.isTrigger = false;
    }

    public Vector3 ObtenerPosicionInteriorAleatoria()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        Vector2 tamano = collider != null ? collider.size : new Vector2(5.2f, 3.4f);
        float escala = CalcularEscala(nivel);
        float anchoInterior = tamano.x * escala * 0.28f;
        float altoInterior = tamano.y * escala * 0.16f;

        return transform.position + new Vector3(
            UnityEngine.Random.Range(-anchoInterior, anchoInterior),
            UnityEngine.Random.Range(-altoInterior, altoInterior) + 0.05f,
            0f);
    }

    public Vector3 LimitarPosicionInterior(Vector3 posicion)
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        Vector2 tamano = collider != null ? collider.size : new Vector2(5.2f, 3.4f);
        float escala = CalcularEscala(nivel);
        float anchoInterior = tamano.x * escala * 0.31f;
        float altoInterior = tamano.y * escala * 0.18f;
        Vector3 centro = transform.position + Vector3.up * 0.05f;

        return new Vector3(
            Mathf.Clamp(posicion.x, centro.x - anchoInterior, centro.x + anchoInterior),
            Mathf.Clamp(posicion.y, centro.y - altoInterior, centro.y + altoInterior),
            0f);
    }

    public void MarcarSeleccionada(bool seleccionada)
    {
        AsegurarRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].color = seleccionada
                ? Color.Lerp(coloresOriginales[i], new Color(1f, 0.92f, 0.38f, coloresOriginales[i].a), 0.35f)
                : coloresOriginales[i];
        }
    }

    private void OnMouseDown()
    {
        if (!Input.GetMouseButtonDown(0) || PunteroSobreUI())
        {
            return;
        }

        pantallaInicioArrastre = Input.mousePosition;
        arrastrando = true;
        movioDuranteArrastre = false;
        GranjaControlPanel.Mostrar(this);
    }

    private void OnMouseDrag()
    {
        if (!arrastrando || AlmacenSistema.instancia == null)
        {
            return;
        }

        if (!movioDuranteArrastre && Vector2.Distance(pantallaInicioArrastre, Input.mousePosition) < DistanciaMinimaArrastre)
        {
            return;
        }

        movioDuranteArrastre = true;
        AlmacenSistema.instancia.PrevisualizarMoverGranja(this, Input.mousePosition, Camera.main);
        GranjaControlPanel.Mostrar(this);
    }

    private void OnMouseUp()
    {
        if (!arrastrando)
        {
            return;
        }

        arrastrando = false;
        if (movioDuranteArrastre && AlmacenSistema.instancia != null)
        {
            AlmacenSistema.instancia.ConfirmarPosicionGranja(this);
        }
    }

    private void AsegurarRenderers()
    {
        SpriteRenderer[] actuales = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers != null && coloresOriginales != null && renderers.Length == actuales.Length)
        {
            return;
        }

        renderers = actuales;
        coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            coloresOriginales[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }
    }

    private static bool PunteroSobreUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

public class SmileEnGranja : MonoBehaviour
{
    private const int OrdenVisualEnGranja = 82;
    private const int LimiteSlimesPorGranja = 15;
    private const float IntervaloRevisionLimite = 0.4f;

    private GranjaColocada granja;
    private Vector3 destino;
    private Vector3 escalaBase;
    private float tiempoNuevoDestino;
    private float tiempoRevisionLimite;
    private SpriteRenderer[] renderers;

    public static SmileEnGranja Asignar(GameObject smile, GranjaColocada granja)
    {
        SmileEnGranja residente = smile.GetComponent<SmileEnGranja>();
        if (residente == null)
        {
            residente = smile.AddComponent<SmileEnGranja>();
        }

        residente.Configurar(granja);
        RevisarLimiteGranja(granja);
        return residente;
    }

    private void OnEnable()
    {
        tiempoRevisionLimite = UnityEngine.Random.Range(0f, IntervaloRevisionLimite);
    }

    private void Configurar(GranjaColocada nuevaGranja)
    {
        granja = nuevaGranja;
        escalaBase = transform.localScale;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, OrdenVisualEnGranja);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = true;
            collider.isTrigger = true;
        }

        SmileVerdeEvolucion verde = GetComponent<SmileVerdeEvolucion>();
        if (verde != null)
        {
            verde.enabled = false;
        }

        SmileAmarilloEvolucion amarilla = GetComponent<SmileAmarilloEvolucion>();
        if (amarilla != null)
        {
            amarilla.enabled = false;
        }

        CleanerSlimeAI cleaner = GetComponent<CleanerSlimeAI>();
        if (cleaner != null)
        {
            cleaner.enabled = false;
        }

        ColdSlimeAI cold = GetComponent<ColdSlimeAI>();
        if (cold != null)
        {
            cold.enabled = false;
        }

        FireSlimeAI fire = GetComponent<FireSlimeAI>();
        if (fire != null)
        {
            fire.enabled = false;
        }

        SlimeController slimeController = GetComponent<SlimeController>();
        if (slimeController != null)
        {
            slimeController.enabled = false;
        }

        transform.position = granja.LimitarPosicionInterior(transform.position);
        ElegirNuevoDestino();
        tiempoRevisionLimite = UnityEngine.Random.Range(0f, IntervaloRevisionLimite);
    }

    private void Update()
    {
        if (granja == null)
        {
            enabled = false;
            return;
        }

        tiempoRevisionLimite -= Time.deltaTime;
        if (tiempoRevisionLimite <= 0f)
        {
            tiempoRevisionLimite = IntervaloRevisionLimite;
            RevisarLimiteGranja(granja);
            if (granja == null || !enabled)
            {
                return;
            }
        }

        tiempoNuevoDestino -= Time.deltaTime;
        if (tiempoNuevoDestino <= 0f || Vector2.Distance(transform.position, destino) < 0.04f)
        {
            ElegirNuevoDestino();
        }

        Vector3 posicionAnterior = transform.position;
        Vector3 nuevaPosicion = Vector3.MoveTowards(transform.position, destino, 0.55f * Time.deltaTime);
        float salto = Mathf.Sin(Time.time * 8f + GetInstanceID()) * 0.035f;
        nuevaPosicion.y += salto * Time.deltaTime;
        transform.position = granja.LimitarPosicionInterior(nuevaPosicion);

        float direccion = transform.position.x - posicionAnterior.x;
        if (Mathf.Abs(direccion) > 0.001f)
        {
            transform.localScale = new Vector3(Mathf.Sign(direccion) * Mathf.Abs(escalaBase.x), escalaBase.y, escalaBase.z);
        }
    }

    private void ElegirNuevoDestino()
    {
        destino = granja != null ? granja.ObtenerPosicionInteriorAleatoria() : transform.position;
        tiempoNuevoDestino = UnityEngine.Random.Range(1.2f, 2.6f);
    }

    private static void RevisarLimiteGranja(GranjaColocada granjaObjetivo)
    {
        if (granjaObjetivo == null)
        {
            return;
        }

        SmileEnGranja[] residentes = FindObjectsByType<SmileEnGranja>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<SmileEnGranja> residentesEnGranja = new List<SmileEnGranja>();
        foreach (SmileEnGranja residente in residentes)
        {
            if (residente != null && residente.enabled && residente.granja == granjaObjetivo)
            {
                residentesEnGranja.Add(residente);
            }
        }

        if (residentesEnGranja.Count <= LimiteSlimesPorGranja)
        {
            return;
        }

        residentesEnGranja.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        for (int i = LimiteSlimesPorGranja; i < residentesEnGranja.Count; i++)
        {
            if (residentesEnGranja[i] != null)
            {
                residentesEnGranja[i].ExpulsarDeGranja();
            }
        }

        MensajeEscapeSlimesUI.Mostrar();
    }

    private void ExpulsarDeGranja()
    {
        GranjaColocada granjaActual = granja;
        granja = null;
        enabled = false;

        transform.position = ObtenerPosicionSalida(granjaActual);
        if (escalaBase != Vector3.zero)
        {
            transform.localScale = new Vector3(Mathf.Abs(escalaBase.x), escalaBase.y, escalaBase.z);
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float direccion = transform.position.x >= (granjaActual != null ? granjaActual.transform.position.x : transform.position.x) ? 1f : -1f;
            rb.simulated = true;
            rb.linearVelocity = new Vector2(direccion * 1.6f, 0.45f);
        }

        ReactivarComportamientoLibre();
        Destroy(this);
    }

    private Vector3 ObtenerPosicionSalida(GranjaColocada granjaActual)
    {
        if (granjaActual == null)
        {
            return transform.position + Vector3.right * 1.2f;
        }

        BoxCollider2D collider = granjaActual.GetComponent<BoxCollider2D>();
        Bounds bounds = collider != null ? collider.bounds : new Bounds(granjaActual.transform.position, new Vector3(5.2f, 3.4f, 1f));
        float direccion = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float x = bounds.center.x + direccion * (bounds.extents.x + UnityEngine.Random.Range(0.85f, 1.35f));
        float y = bounds.min.y + Mathf.Max(0.25f, bounds.size.y * 0.18f);
        return new Vector3(x, y, 0f);
    }

    private void ReactivarComportamientoLibre()
    {
        SmileVerdeEvolucion verde = GetComponent<SmileVerdeEvolucion>();
        if (verde != null)
        {
            verde.enabled = true;
        }

        SmileAmarilloEvolucion amarilla = GetComponent<SmileAmarilloEvolucion>();
        if (amarilla != null)
        {
            amarilla.enabled = true;
        }

        CleanerSlimeAI cleaner = GetComponent<CleanerSlimeAI>();
        if (cleaner != null)
        {
            cleaner.enabled = true;
        }

        ColdSlimeAI cold = GetComponent<ColdSlimeAI>();
        if (cold != null)
        {
            cold.enabled = true;
        }

        FireSlimeAI fire = GetComponent<FireSlimeAI>();
        if (fire != null)
        {
            fire.enabled = true;
        }

        SlimeController slimeController = GetComponent<SlimeController>();
        if (slimeController != null)
        {
            slimeController.enabled = true;
        }
    }
}

public class MensajeEscapeSlimesUI : MonoBehaviour
{
    private static MensajeEscapeSlimesUI instancia;

    private TMP_Text texto;
    private float ocultarEn;

    public static void Mostrar()
    {
        if (instancia == null)
        {
            Crear();
        }

        if (instancia == null)
        {
            return;
        }

        instancia.gameObject.SetActive(true);
        instancia.ocultarEn = Time.unscaledTime + 2.4f;
    }

    private static void Crear()
    {
        Canvas canvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas candidato in canvases)
        {
            if (candidato != null && candidato.name == "Canvas_UI")
            {
                canvas = candidato;
                break;
            }
        }

        if (canvas == null && canvases.Length > 0)
        {
            canvas = canvases[0];
        }

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas_Mensaje_Escape_Slimes", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
        }

        GameObject panelGO = new GameObject("Mensaje_Escape_Slimes", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow), typeof(MensajeEscapeSlimesUI));
        panelGO.transform.SetParent(canvas.transform, false);
        panelGO.layer = canvas.gameObject.layer;

        RectTransform rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -86f);
        rect.sizeDelta = new Vector2(470f, 58f);

        Image fondo = panelGO.GetComponent<Image>();
        fondo.color = new Color(0.12f, 0.08f, 0.08f, 0.9f);
        fondo.raycastTarget = false;

        Shadow sombra = panelGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(3f, -3f);

        MensajeEscapeSlimesUI componente = panelGO.GetComponent<MensajeEscapeSlimesUI>();
        componente.texto = CrearTexto(panelGO.transform);
        instancia = componente;
    }

    private static TMP_Text CrearTexto(Transform parent)
    {
        GameObject textoGO = new GameObject("Texto_Mensaje_Escape_Slimes", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        textoGO.transform.SetParent(parent, false);
        RectTransform rect = textoGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(18f, 6f);
        rect.offsetMax = new Vector2(-18f, -6f);

        TMP_Text label = textoGO.GetComponent<TextMeshProUGUI>();
        label.text = "OOPS TUS SLIMES SE ESCAPAN";
        label.color = new Color(1f, 0.92f, 0.35f, 1f);
        label.fontSize = 26f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 26f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        Shadow sombra = textoGO.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(1.5f, -1.5f);
        return label;
    }

    private void Update()
    {
        if (Time.unscaledTime >= ocultarEn)
        {
            gameObject.SetActive(false);
        }
    }
}

public class GranjaControlPanel : MonoBehaviour
{
    private static GranjaControlPanel instancia;

    private Canvas canvas;
    private RectTransform panel;
    private Button botonMover;
    private Button botonMas;
    private Button botonMenos;
    private Button botonSmile;
    private TMP_Text textoNivel;
    private GranjaColocada seleccionada;
    private AlmacenSistema almacen;
    private bool moviendo;
    private Vector3 posicionAntesDeMover;

    public static void Mostrar(GranjaColocada granja)
    {
        if (granja == null)
        {
            return;
        }

        ObtenerInstancia().Seleccionar(granja);
    }

    private static GranjaControlPanel ObtenerInstancia()
    {
        if (instancia != null)
        {
            return instancia;
        }

        GameObject go = new GameObject("Panel_Control_Granja_Runtime");
        instancia = go.AddComponent<GranjaControlPanel>();
        return instancia;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        almacen = AlmacenSistema.AsegurarInstancia();
        ConstruirUI();
    }

    private void Update()
    {
        if (seleccionada == null || !moviendo)
        {
            return;
        }

        almacen.PrevisualizarMoverGranja(seleccionada, Input.mousePosition, Camera.main);

        if (Input.GetMouseButtonDown(0) && !PunteroSobreUI())
        {
            almacen.ConfirmarPosicionGranja(seleccionada);
            moviendo = false;
            ActualizarBotones();
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            almacen.RestaurarPosicionGranja(seleccionada, posicionAntesDeMover);
            moviendo = false;
            ActualizarBotones();
        }
    }

    private void LateUpdate()
    {
        if (seleccionada == null || panel == null || Camera.main == null)
        {
            return;
        }

        Vector3 pantalla = Camera.main.WorldToScreenPoint(seleccionada.transform.position);
        pantalla += new Vector3(0f, 82f, 0f);

        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pantalla, null, out Vector2 localPoint);
        Vector2 mitad = canvasRect.rect.size * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x, -mitad.x + 115f, mitad.x - 115f);
        localPoint.y = Mathf.Clamp(localPoint.y, -mitad.y + 34f, mitad.y - 34f);
        panel.anchoredPosition = localPoint;
    }

    private void Seleccionar(GranjaColocada granja)
    {
        if (seleccionada != null && seleccionada != granja)
        {
            seleccionada.MarcarSeleccionada(false);
        }

        seleccionada = granja;
        almacen = AlmacenSistema.instancia != null ? AlmacenSistema.instancia : AlmacenSistema.AsegurarInstancia();
        seleccionada.MarcarSeleccionada(true);
        panel.gameObject.SetActive(true);
        ActualizarBotones();
    }

    private void ConstruirUI()
    {
        AsegurarEventSystem();

        GameObject canvasObject = new GameObject("Canvas_Controles_Granja", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 4500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Panel_Controles_Granja", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        panelObject.transform.SetParent(canvas.transform, false);
        panel = panelObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(315f, 56f);

        Image fondo = panelObject.GetComponent<Image>();
        fondo.color = new Color(0.06f, 0.08f, 0.08f, 0.92f);

        HorizontalLayoutGroup layout = panelObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 7, 7);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        botonMenos = CrearBoton(panel, "-", 40f, () => CambiarTamano(-1));
        botonMover = CrearBoton(panel, "MOVER", 80f, AlternarMover);
        botonMas = CrearBoton(panel, "+", 40f, () => CambiarTamano(1));
        botonSmile = CrearBoton(panel, "SMILE", 72f, ColocarSmile);
        textoNivel = CrearTexto(panel, "N1", 42f);

        panel.gameObject.SetActive(false);
    }

    private Button CrearBoton(RectTransform parent, string texto, float ancho, UnityEngine.Events.UnityAction accion)
    {
        GameObject go = new GameObject("Btn_Granja_" + texto, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.23f, 0.20f, 0.98f);

        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = image;
        boton.onClick.AddListener(accion);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = ancho;
        layout.minWidth = ancho;

        TMP_Text label = CrearTexto(go.transform, texto, ancho - 8f);
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return boton;
    }

    private TMP_Text CrearTexto(Transform parent, string texto, float ancho)
    {
        GameObject go = new GameObject("Txt_" + texto, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TMP_Text label = go.GetComponent<TextMeshProUGUI>();
        label.text = texto;
        label.color = new Color(0.98f, 0.96f, 0.82f, 1f);
        label.fontSize = 18f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 18f;
        label.raycastTarget = false;

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = ancho;
        layout.minWidth = ancho;

        return label;
    }

    private void CambiarTamano(int cambio)
    {
        if (seleccionada == null || almacen == null)
        {
            return;
        }

        almacen.IntentarCambiarTamanoGranja(seleccionada, cambio);
        ActualizarBotones();
    }

    private void ColocarSmile()
    {
        if (seleccionada == null)
        {
            return;
        }

        SmileSuctionController succionador = BuscarSuccionadorConSmiles();
        if (succionador == null)
        {
            return;
        }

        succionador.ColocarUltimoSmileEnGranja(seleccionada);
        ActualizarBotones();
    }

    private void AlternarMover()
    {
        if (seleccionada == null)
        {
            return;
        }

        moviendo = !moviendo;
        posicionAntesDeMover = seleccionada.transform.position;
        ActualizarBotones();
    }

    private void ActualizarBotones()
    {
        if (seleccionada == null || almacen == null)
        {
            return;
        }

        botonMenos.interactable = almacen.PuedeCambiarTamanoGranja(seleccionada, -1);
        botonMas.interactable = almacen.PuedeCambiarTamanoGranja(seleccionada, 1);
        botonSmile.interactable = BuscarSuccionadorConSmiles() != null;
        botonMover.GetComponentInChildren<TMP_Text>().text = moviendo ? "FIJAR" : "MOVER";
        textoNivel.text = "N" + seleccionada.nivel;
    }

    private static SmileSuctionController BuscarSuccionadorConSmiles()
    {
        foreach (SmileSuctionController succionador in FindObjectsByType<SmileSuctionController>(FindObjectsSortMode.None))
        {
            if (succionador != null && succionador.CantidadSmilesGuardados > 0)
            {
                return succionador;
            }
        }

        return null;
    }

    private static void AsegurarEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static bool PunteroSobreUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
