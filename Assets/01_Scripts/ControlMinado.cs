using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class ControlMinado : MonoBehaviour
{
    public Tilemap tilemap;
    public Tile tileTierra;
    public GameObject prefabSemilla;
    public bool limitarMinadoPorMirada = true;
    public float margenMinadoPorMirada = 0.15f;
    public float anchoMinadoHaciaAbajo = 1.25f;
    public float alturaMinadoHaciaAbajo = 0.25f;
    [Range(0, 8)] public int bloquesExtraPorPicada = 4;

    private Camera camaraPrincipal;
    private Transform jugador;
    private static Sprite spriteSemillaTemporal;
    
    private AudioSource cavarAudio;
    private float stopCavarTime;

    private void Awake()
    {
        camaraPrincipal = Camera.main;
        AsegurarReferencias();
        
        cavarAudio = gameObject.AddComponent<AudioSource>();
        cavarAudio.clip = Resources.Load<AudioClip>("Sonidos/cavar");
        cavarAudio.playOnAwake = false;
    }

    private void Update()
    {
        if (cavarAudio != null && cavarAudio.isPlaying && Time.time >= stopCavarTime)
        {
            cavarAudio.Stop();
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        AsegurarReferencias();

        if (tilemap == null || tileTierra == null)
        {
            Debug.LogWarning("ControlMinado necesita un Tilemap y tileTierra asignados.");
            return;
        }

        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (camaraPrincipal == null)
        {
            Debug.LogWarning("ControlMinado necesita una camara principal en la escena.");
            return;
        }

        Vector3 posicionMouse = Input.mousePosition;
        posicionMouse.z = Mathf.Abs(camaraPrincipal.transform.position.z - tilemap.transform.position.z);

        Vector3 posicionMundo = camaraPrincipal.ScreenToWorldPoint(posicionMouse);
        posicionMundo.z = tilemap.transform.position.z;

        Vector3Int coordenada = tilemap.WorldToCell(posicionMundo);
        coordenada.z = 0;

        if (!PuedeMinarEnDireccionDeMirada(posicionMundo))
        {
            return;
        }

        TileBase tileActual = tilemap.GetTile(coordenada);

        if (tileActual != tileTierra)
        {
            return;
        }

        List<Vector3Int> celdasHueco = ObtenerCeldasHuecoMinado(coordenada);
        int cubosRecolectados = 0;
        foreach (Vector3Int celda in celdasHueco)
        {
            if (tilemap.GetTile(celda) == tileTierra)
            {
                tilemap.SetTile(celda, null);
                cubosRecolectados++;
            }
        }

        if (cubosRecolectados > 0 && cavarAudio != null)
        {
            cavarAudio.Play();
            stopCavarTime = Time.time + 2f;
        }

        AlmacenSistema.AgregarCubosGlobal(cubosRecolectados);

        if (prefabSemilla != null && Random.value < 0.5f)
        {
            Vector3 centroCelda = tilemap.GetCellCenterWorld(coordenada);
            Instantiate(prefabSemilla, centroCelda, Quaternion.identity);
        }
        else if (prefabSemilla == null && Random.value < 0.5f)
        {
            CrearSemillaVisible(tilemap.GetCellCenterWorld(coordenada));
        }
    }

    private void AsegurarReferencias()
    {
        if (jugador == null)
        {
            GameObject jugadorObjeto = GameObject.FindGameObjectWithTag("Player");
            jugador = jugadorObjeto != null ? jugadorObjeto.transform : null;
        }

        if (tilemap != null && tileTierra != null)
        {
            return;
        }

        GeneradorMundo generador = GetComponent<GeneradorMundo>();
        if (generador == null)
        {
            return;
        }

        generador.GenerarMapa();

        if (tilemap == null)
        {
            tilemap = generador.tilemap;
        }

        if (tileTierra == null)
        {
            tileTierra = generador.tileTierra;
        }
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

            if (celdas.Contains(candidata) || tilemap.GetTile(candidata) != tileTierra)
            {
                continue;
            }

            celdas.Add(candidata);
        }

        return celdas;
    }

    private void CrearSemillaVisible(Vector3 posicion)
    {
        GameObject semilla = new GameObject("Semilla_Minado");
        semilla.transform.position = posicion;
        semilla.transform.localScale = Vector3.one * 0.35f;

        SpriteRenderer rendererSemilla = semilla.AddComponent<SpriteRenderer>();
        rendererSemilla.sprite = ObtenerSpriteSemillaTemporal();
        rendererSemilla.color = new Color32(108, 218, 95, 255);
        rendererSemilla.sortingOrder = 10;
    }

    private Sprite ObtenerSpriteSemillaTemporal()
    {
        if (spriteSemillaTemporal != null)
        {
            return spriteSemillaTemporal;
        }

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32 transparente = new Color32(0, 0, 0, 0);
        Color32 verde = new Color32(108, 218, 95, 255);
        Color32 claro = new Color32(180, 255, 150, 255);
        Color32[] pixels = new Color32[16 * 16];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparente;
        }

        for (int y = 3; y < 13; y++)
        {
            for (int x = 5; x < 11; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                if ((dx * dx / 9f) + (dy * dy / 25f) <= 1f)
                {
                    pixels[y * 16 + x] = verde;
                }
            }
        }

        pixels[9 * 16 + 7] = claro;
        pixels[10 * 16 + 7] = claro;
        pixels[10 * 16 + 8] = claro;

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;

        spriteSemillaTemporal = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        spriteSemillaTemporal.hideFlags = HideFlags.HideAndDontSave;
        return spriteSemillaTemporal;
    }
}
