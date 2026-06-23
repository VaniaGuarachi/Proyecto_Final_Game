using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SmileVerdeEvolucion : MonoBehaviour
{
    [Header("Fotogramas")]
    public int cantidadFotogramas = 8;
    public int etapasEvolucion = 5;
    public float fotogramasPorSegundo = 8f;

    [Header("Evolucion")]
    public float duracionPorFase = 12f;
    public float escalaMundoBase = 1.1f;
    public float escalaBebe = 0.42f;
    public float escalaCrecido = 1.05f;

    [Header("Movimiento por zona")]
    public float radioZona = 2.2f;
    public float velocidadMovimiento = 0.65f;
    public float cambiarDestinoCada = 2.2f;
    public float alturaFlotacion = 0f;

    private SpriteRenderer spriteRenderer;
    private Sprite[,] fotogramas;
    private Vector3 posicionInicial;
    private Vector3 destinoActual;
    private Vector3 escalaBase;
    private float tiempoVida;
    private float tiempoNuevoDestino;
    private float direccionX = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstalarEnSmilesVerdes()
    {
        foreach (SmileBasico smile in FindObjectsByType<SmileBasico>(FindObjectsSortMode.None))
        {
            if (smile.alimentoFavorito == TipoSemilla.Verde && smile.GetComponent<SmileVerdeEvolucion>() == null)
            {
                smile.gameObject.AddComponent<SmileVerdeEvolucion>();
            }
        }

        if (FindFirstObjectByType<SmileVerdeSpawner>() == null)
        {
            new GameObject("SmileVerdeSpawner").AddComponent<SmileVerdeSpawner>();
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        fotogramas = CrearFotogramas();
    }

    private void Start()
    {
        posicionInicial = transform.position;
        destinoActual = posicionInicial;
        escalaBase = Vector3.one * escalaMundoBase;
        transform.localScale = escalaBase * escalaBebe;
        ElegirNuevoDestino();
    }

    private void Update()
    {
        tiempoVida += Time.deltaTime;
        ActualizarFotograma();
        ActualizarMovimiento();
        ActualizarEscala();
    }

    private void ActualizarFotograma()
    {
        if (fotogramas == null || fotogramas.Length == 0)
        {
            return;
        }

        float tiempoEtapa = Mathf.Max(0f, tiempoVida) / Mathf.Max(1f, duracionPorFase);
        int etapa = Mathf.Clamp(Mathf.FloorToInt(tiempoEtapa), 0, etapasEvolucion - 1);
        int indice = Mathf.FloorToInt(Time.time * fotogramasPorSegundo) % cantidadFotogramas;
        spriteRenderer.sprite = fotogramas[etapa, indice];
        spriteRenderer.flipX = direccionX < 0f;
    }

    private void ActualizarMovimiento()
    {
        tiempoNuevoDestino -= Time.deltaTime;
        if (tiempoNuevoDestino <= 0f || Vector2.Distance(transform.position, destinoActual) < 0.05f)
        {
            ElegirNuevoDestino();
        }

        Vector3 posicionAnterior = transform.position;
        Vector3 baseMovimiento = Vector3.MoveTowards(
            new Vector3(transform.position.x, posicionInicial.y, transform.position.z),
            destinoActual,
            velocidadMovimiento * Time.deltaTime
        );

        float flotacion = Mathf.Sin(Time.time * 3.2f) * alturaFlotacion;
        transform.position = new Vector3(baseMovimiento.x, posicionInicial.y + flotacion, posicionInicial.z);

        float deltaX = transform.position.x - posicionAnterior.x;
        if (Mathf.Abs(deltaX) > 0.001f)
        {
            direccionX = Mathf.Sign(deltaX);
        }
    }

    private void ActualizarEscala()
    {
        float evolucion = ObtenerEvolucion();
        float escalaEvolucion = Mathf.Lerp(escalaBebe, escalaCrecido, evolucion);
        float gelatina = Mathf.Sin(Time.time * 5.5f) * 0.025f;

        transform.localScale = new Vector3(
            escalaBase.x * escalaEvolucion * (1f + gelatina),
            escalaBase.y * escalaEvolucion * (1f - gelatina * 0.65f),
            escalaBase.z
        );
    }

    private void ElegirNuevoDestino()
    {
        destinoActual = posicionInicial + new Vector3(Random.Range(-radioZona, radioZona), 0f, 0f);
        tiempoNuevoDestino = cambiarDestinoCada + Random.Range(-0.55f, 0.75f);
    }

    private float ObtenerEvolucion()
    {
        float duracionTotal = Mathf.Max(1f, duracionPorFase * Mathf.Max(1, etapasEvolucion));
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempoVida / duracionTotal));
    }

    public void ConfigurarSpawn(Vector3 posicionBase, float radio, float retrasoCrecimiento)
    {
        posicionInicial = posicionBase;
        destinoActual = posicionInicial;
        radioZona = radio;
        tiempoVida = -Mathf.Max(0f, retrasoCrecimiento);
        transform.position = posicionInicial;
        ElegirNuevoDestino();
    }

    private Sprite[,] CrearFotogramas()
    {
        cantidadFotogramas = Mathf.Max(4, cantidadFotogramas);
        etapasEvolucion = Mathf.Max(2, etapasEvolucion);
        Sprite[,] sprites = new Sprite[etapasEvolucion, cantidadFotogramas];

        for (int etapa = 0; etapa < etapasEvolucion; etapa++)
        {
            float progreso = etapa / (float)(etapasEvolucion - 1);

            for (int i = 0; i < cantidadFotogramas; i++)
            {
                float fase = i / (float)cantidadFotogramas;
                float respiracion = Mathf.Sin(fase * Mathf.PI * 2f);
                float caminata = Mathf.Sin(fase * Mathf.PI * 2f);
                Texture2D textura = CrearTexturaSmile(respiracion, caminata, progreso);
                sprites[etapa, i] = Sprite.Create(
                    textura,
                    new Rect(0f, 0f, textura.width, textura.height),
                    new Vector2(0.5f, 0.22f),
                    100f
                );
            }
        }

        return sprites;
    }

    private Texture2D CrearTexturaSmile(float respiracion, float caminata, float evolucion)
    {
        int ancho = 180;
        int alto = 132;
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Bilinear;

        Color transparente = new Color(0f, 0f, 0f, 0f);
        Color gelBebe = new Color(0.70f, 1f, 0.84f, 0.56f);
        Color gelGrande = new Color(0.38f, 0.94f, 0.18f, 0.95f);
        Color gelSombra = Color.Lerp(new Color(0.28f, 0.70f, 0.42f, 0.24f), new Color(0.13f, 0.55f, 0.06f, 0.92f), evolucion);
        Color gelClaro = Color.Lerp(gelBebe, gelGrande, evolucion);
        Color bordeGel = Color.Lerp(new Color(0.60f, 1f, 0.82f, 0.55f), new Color(0.16f, 0.88f, 0.08f, 0.96f), evolucion);
        Color caraBebe = new Color(0.10f, 0.46f, 0.58f, 0.88f);
        Color caraGrande = new Color(0.02f, 0.03f, 0.02f, 0.96f);
        Color cara = Color.Lerp(caraBebe, caraGrande, evolucion);

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                textura.SetPixel(x, y, transparente);
            }
        }

        float centroX = ancho * 0.5f;
        float centroY = Mathf.Lerp(alto * 0.43f, alto * 0.40f, evolucion);
        float radioX = Mathf.Lerp(48f, 72f, evolucion) + respiracion * Mathf.Lerp(2.4f, 4.4f, evolucion);
        float radioY = Mathf.Lerp(33f, 45f, evolucion) - respiracion * Mathf.Lerp(1.4f, 3f, evolucion);
        float piso = Mathf.Lerp(26f, 30f, evolucion);
        DibujarPatitas(textura, evolucion, caminata, piso);

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float nx = (x - centroX) / radioX;
                float ny = (y - centroY) / radioY;
                float d = nx * nx + ny * ny;

                if (d > 1f || y < piso)
                {
                    continue;
                }

                float bordeAlpha = Mathf.SmoothStep(Mathf.Lerp(0.90f, 0.82f, evolucion), 1f, d);
                float sombraInferior = Mathf.InverseLerp(centroY, piso, y);
                float brilloSuperior = Mathf.InverseLerp(piso, centroY + radioY, y);
                Color color = Color.Lerp(gelClaro, gelSombra, Mathf.Clamp01(sombraInferior) * 0.75f);
                color = Color.Lerp(color, Color.white, Mathf.Clamp01(brilloSuperior) * Mathf.Lerp(0.12f, 0.04f, evolucion));
                color.a *= 1f - bordeAlpha * Mathf.Lerp(0.25f, 0.08f, evolucion);
                color = Color.Lerp(color, bordeGel, bordeAlpha * Mathf.Lerp(0.45f, 0.88f, evolucion));
                textura.SetPixel(x, y, color);
            }
        }

        if (evolucion > 0.52f)
        {
            DibujarCola(textura, evolucion, respiracion, bordeGel, gelClaro, gelSombra);
        }

        int ojoY = Mathf.RoundToInt(Mathf.Lerp(62f, 67f, evolucion) + respiracion * 1.6f);
        int ojoRadioX = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, evolucion));
        int ojoRadioY = Mathf.RoundToInt(Mathf.Lerp(5f, 13f, evolucion));

        DibujarElipse(textura, Mathf.RoundToInt(Mathf.Lerp(62f, 58f, evolucion)), ojoY, ojoRadioX, ojoRadioY, cara);
        DibujarElipse(textura, Mathf.RoundToInt(Mathf.Lerp(103f, 112f, evolucion)), ojoY - Mathf.RoundToInt(respiracion), ojoRadioX, ojoRadioY, cara);

        if (evolucion < 0.48f)
        {
            DibujarElipse(textura, 88, 48, 5, 8, cara);
        }
        else
        {
            DibujarElipse(textura, 82, 47, 3, 2, cara);
            DibujarElipse(textura, 98, 47, 3, 2, cara);
            DibujarElipse(textura, 90, 44, 8, 2, new Color(0.02f, 0.03f, 0.02f, 0.72f));
        }

        DibujarElipse(textura, Mathf.RoundToInt(Mathf.Lerp(58f, 61f, evolucion)), Mathf.RoundToInt(Mathf.Lerp(83f, 91f, evolucion)), Mathf.RoundToInt(Mathf.Lerp(18f, 22f, evolucion)), 7, new Color(1f, 1f, 1f, Mathf.Lerp(0.22f, 0.68f, evolucion)));
        DibujarElipse(textura, 112, 91, 5, 3, new Color(1f, 1f, 1f, Mathf.Lerp(0.08f, 0.55f, evolucion)));
        DibujarElipse(textura, 122, 86, 3, 2, new Color(1f, 1f, 1f, Mathf.Lerp(0.05f, 0.48f, evolucion)));
        DibujarElipse(textura, 90, 23, Mathf.RoundToInt(Mathf.Lerp(38f, 62f, evolucion)), 7, new Color(0.08f, 0.22f, 0.05f, 0.23f));

        textura.Apply();
        return textura;
    }

    private void DibujarPatitas(Texture2D textura, float evolucion, float caminata, float piso)
    {
        float visibilidad = Mathf.SmoothStep(0.12f, 0.85f, evolucion);
        Color verdePie = Color.Lerp(
            new Color(0.48f, 0.96f, 0.62f, 0.42f),
            new Color(0.16f, 0.72f, 0.07f, 0.95f),
            evolucion
        );
        Color sombraPie = new Color(0.04f, 0.20f, 0.03f, 0.22f * visibilidad);

        int yBase = Mathf.RoundToInt(piso - Mathf.Lerp(2f, 5f, evolucion));
        int radioX = Mathf.RoundToInt(Mathf.Lerp(9f, 15f, evolucion));
        int radioY = Mathf.RoundToInt(Mathf.Lerp(4f, 7f, evolucion));
        int separacion = Mathf.RoundToInt(Mathf.Lerp(21f, 34f, evolucion));
        int paso = Mathf.RoundToInt(caminata * Mathf.Lerp(3f, 7f, evolucion));
        int elevacionIzq = Mathf.RoundToInt(Mathf.Max(0f, caminata) * Mathf.Lerp(1f, 5f, evolucion));
        int elevacionDer = Mathf.RoundToInt(Mathf.Max(0f, -caminata) * Mathf.Lerp(1f, 5f, evolucion));

        DibujarElipse(textura, 90 - separacion, yBase - 3, radioX + 3, radioY + 1, sombraPie);
        DibujarElipse(textura, 90 + separacion, yBase - 3, radioX + 3, radioY + 1, sombraPie);
        DibujarElipse(textura, 90 - separacion - paso, yBase + elevacionIzq, radioX, radioY, new Color(verdePie.r, verdePie.g, verdePie.b, verdePie.a * visibilidad));
        DibujarElipse(textura, 90 + separacion + paso, yBase + elevacionDer, radioX, radioY, new Color(verdePie.r, verdePie.g, verdePie.b, verdePie.a * visibilidad));
    }

    private void DibujarCola(Texture2D textura, float evolucion, float respiracion, Color borde, Color gelClaro, Color gelSombra)
    {
        Vector2[] puntos =
        {
            new Vector2(126, 82),
            new Vector2(135, 96 + respiracion * 1.5f),
            new Vector2(132, 108),
            new Vector2(120, 104),
            new Vector2(124, 93),
        };

        float anchoBase = Mathf.Lerp(7f, 11f, evolucion);
        foreach (Vector2 punto in puntos)
        {
            DibujarElipse(textura, Mathf.RoundToInt(punto.x), Mathf.RoundToInt(punto.y), Mathf.RoundToInt(anchoBase + 3f), Mathf.RoundToInt(anchoBase + 3f), borde);
        }

        foreach (Vector2 punto in puntos)
        {
            Color color = Color.Lerp(gelClaro, gelSombra, Mathf.InverseLerp(82f, 108f, punto.y) * 0.35f);
            DibujarElipse(textura, Mathf.RoundToInt(punto.x), Mathf.RoundToInt(punto.y), Mathf.RoundToInt(anchoBase), Mathf.RoundToInt(anchoBase), color);
        }
    }

    private void DibujarElipse(Texture2D textura, int centroX, int centroY, int radioX, int radioY, Color color)
    {
        for (int y = centroY - radioY; y <= centroY + radioY; y++)
        {
            for (int x = centroX - radioX; x <= centroX + radioX; x++)
            {
                if (x < 0 || x >= textura.width || y < 0 || y >= textura.height)
                {
                    continue;
                }

                float nx = (x - centroX) / (float)radioX;
                float ny = (y - centroY) / (float)radioY;
                if (nx * nx + ny * ny > 1f)
                {
                    continue;
                }

                Color actual = textura.GetPixel(x, y);
                textura.SetPixel(x, y, Color.Lerp(actual, color, color.a));
            }
        }
    }
}

public class SmileVerdeSpawner : MonoBehaviour
{
    public int cantidadMaxima = 5;
    public float intervaloSalida = 2f;
    public float separacionEntreSmiles = 1.35f;
    public float radioMovimientoPorSmile = 1.05f;

    private IEnumerator Start()
    {
        yield return null;

        while (ContarSmilesVerdes() < cantidadMaxima)
        {
            SmileBasico original = BuscarSmileVerdeOriginal();
            if (original == null)
            {
                yield break;
            }

            CrearCopiaSmile(original, ContarSmilesVerdes());
            yield return new WaitForSeconds(intervaloSalida);
        }
    }

    private int ContarSmilesVerdes()
    {
        int total = 0;
        foreach (SmileBasico smile in FindObjectsByType<SmileBasico>(FindObjectsSortMode.None))
        {
            if (smile.alimentoFavorito == TipoSemilla.Verde)
            {
                total++;
            }
        }

        return total;
    }

    private SmileBasico BuscarSmileVerdeOriginal()
    {
        SmileBasico mejor = null;

        foreach (SmileBasico smile in FindObjectsByType<SmileBasico>(FindObjectsSortMode.None))
        {
            if (smile.alimentoFavorito != TipoSemilla.Verde)
            {
                continue;
            }

            if (mejor == null || !smile.name.Contains("Clon"))
            {
                mejor = smile;
            }
        }

        return mejor;
    }

    private void CrearCopiaSmile(SmileBasico original, int indice)
    {
        Vector3 basePos = original.transform.position;
        int lado = indice % 2 == 0 ? 1 : -1;
        int distancia = (indice + 1) / 2;
        float offsetX = lado * distancia * separacionEntreSmiles;
        Vector3 posicion = new Vector3(basePos.x + offsetX, basePos.y, basePos.z);

        GameObject copia = Instantiate(original.gameObject, posicion, original.transform.rotation);
        copia.name = "Smile_Verde_Clon_" + indice;
        copia.SetActive(true);

        SmileBasico smile = copia.GetComponent<SmileBasico>();
        if (smile != null)
        {
            smile.jugadorDentro = false;
        }

        SmileVerdeEvolucion evolucion = copia.GetComponent<SmileVerdeEvolucion>();
        if (evolucion == null)
        {
            evolucion = copia.AddComponent<SmileVerdeEvolucion>();
        }

        evolucion.ConfigurarSpawn(posicion, radioMovimientoPorSmile, indice * 1.5f);
    }
}
