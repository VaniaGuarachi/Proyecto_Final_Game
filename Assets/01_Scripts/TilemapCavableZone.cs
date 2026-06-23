using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapCavableZone : MonoBehaviour
{
    public TipoRecursoMinado tipoRecurso = TipoRecursoMinado.Tierra;
    public int cantidad = 1;
    public int golpesNecesarios = 1;
    public float duracionAnimacionCavado = 0.12f;

    private readonly Dictionary<Vector3Int, int> golpesPorCelda = new Dictionary<Vector3Int, int>();
    private readonly HashSet<Vector3Int> celdasAnimandose = new HashSet<Vector3Int>();
    private Tilemap tilemap;
    private AudioSource cavarAudio;
    private float stopCavarTime;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        
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
    }

    public bool Picar(Vector3 mundo)
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }

        Vector3Int celda = EncontrarCeldaCavable(mundo);
        if (!tilemap.HasTile(celda))
        {
            return false;
        }

        if (celdasAnimandose.Contains(celda))
        {
            return true;
        }

        golpesPorCelda.TryGetValue(celda, out int golpesActuales);
        golpesActuales++;

        if (golpesActuales < Mathf.Max(1, golpesNecesarios))
        {
            golpesPorCelda[celda] = golpesActuales;
            StartCoroutine(AnimarGolpe(celda, false));
            
            if (cavarAudio != null)
            {
                cavarAudio.Play();
                stopCavarTime = Time.time + 2f;
            }
            
            return true;
        }

        golpesPorCelda.Remove(celda);
        StartCoroutine(AnimarGolpe(celda, true));
        
        if (cavarAudio != null)
        {
            cavarAudio.Play();
            stopCavarTime = Time.time + 2f;
        }
        
        return true;
    }

    private Vector3Int EncontrarCeldaCavable(Vector3 mundo)
    {
        Vector3Int baseCell = tilemap.WorldToCell(mundo);
        if (tilemap.HasTile(baseCell))
        {
            return baseCell;
        }

        Vector3Int mejor = baseCell;
        float mejorDistancia = float.MaxValue;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int candidato = baseCell + new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(candidato))
                {
                    continue;
                }

                Vector3 centro = tilemap.GetCellCenterWorld(candidato);
                float distancia = Vector2.Distance(mundo, centro);
                if (distancia < mejorDistancia)
                {
                    mejor = candidato;
                    mejorDistancia = distancia;
                }
            }
        }

        return mejor;
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

    private IEnumerator AnimarGolpe(Vector3Int celda, bool romper)
    {
        celdasAnimandose.Add(celda);
        Matrix4x4 matrizOriginal = tilemap.GetTransformMatrix(celda);
        Color colorOriginal = tilemap.GetColor(celda);
        Color colorGolpe = romper ? new Color(1f, 0.72f, 0.34f, 1f) : new Color(1f, 0.88f, 0.45f, 1f);
        float tiempo = 0f;

        while (tiempo < duracionAnimacionCavado && tilemap.HasTile(celda))
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacionCavado);
            float sacudida = Mathf.Sin(t * Mathf.PI * 6f) * 0.035f * (1f - t);
            tilemap.SetColor(celda, Color.Lerp(colorGolpe, colorOriginal, t));
            tilemap.SetTransformMatrix(celda, Matrix4x4.TRS(new Vector3(sacudida, 0f, 0f), Quaternion.identity, Vector3.one));
            yield return null;
        }

        if (tilemap.HasTile(celda))
        {
            tilemap.SetTransformMatrix(celda, matrizOriginal);
            tilemap.SetColor(celda, romper ? colorOriginal : Color.Lerp(Color.white, colorGolpe, 0.35f));
        }

        if (romper)
        {
            CrearFragmentos(celda);
            tilemap.SetTile(celda, null);
            RegistrarRecurso();
        }

        celdasAnimandose.Remove(celda);
    }

    private void CrearFragmentos(Vector3Int celda)
    {
        Sprite sprite = tilemap.GetSprite(celda);
        if (sprite == null)
        {
            return;
        }

        Vector3 centro = tilemap.GetCellCenterWorld(celda);
        for (int i = 0; i < 4; i++)
        {
            GameObject fragmento = new GameObject("Fragmento_Tile_Cavado");
            fragmento.transform.position = centro + (Vector3)(Random.insideUnitCircle * 0.08f);
            fragmento.transform.localScale = transform.lossyScale * Random.Range(0.12f, 0.20f);

            SpriteRenderer renderer = fragmento.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tilemap.color;
            renderer.sortingLayerID = GetComponent<TilemapRenderer>() != null ? GetComponent<TilemapRenderer>().sortingLayerID : 0;
            renderer.sortingOrder = GetComponent<TilemapRenderer>() != null ? GetComponent<TilemapRenderer>().sortingOrder + 3 : 15;

            AnimacionFragmentoBloque animacion = fragmento.AddComponent<AnimacionFragmentoBloque>();
            animacion.velocidad = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(0.8f, 2.4f), 0f);
            animacion.gravedad = Random.Range(4.5f, 7f);
            animacion.tiempoVida = Random.Range(0.22f, 0.42f);
        }
    }
}
