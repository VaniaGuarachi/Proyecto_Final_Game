using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class GeneradorMundo : MonoBehaviour
{
    public Tilemap tilemap;
    public Tile tilePiedra;
    public Tile tileTierra;

    public int ancho = 50;
    public int alto = 50;

    private void Start()
    {
        GenerarMapa();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            GenerarMapa();
        }
    }

    [ContextMenu("Generar Mapa")]
    public void GenerarMapa()
    {
        AsegurarReferencias();

        if (tilemap == null || tilePiedra == null || tileTierra == null)
        {
            Debug.LogWarning("GeneradorMundo necesita un Tilemap, tilePiedra y tileTierra asignados.");
            return;
        }

        tilemap.ClearAllTiles();

        for (int x = 0; x < ancho; x++)
        {
            for (int y = 0; y < alto; y++)
            {
                Vector3Int posicion = new Vector3Int(x, y, 0);
                bool esBorde = x == 0 || x == ancho - 1 || y == 0 || y == alto - 1;

                tilemap.SetTile(posicion, esBorde ? tilePiedra : tileTierra);
            }
        }
    }

    private void AsegurarReferencias()
    {
        if (tilemap == null)
        {
            tilemap = GetComponentInChildren<Tilemap>();
        }

        if (tilemap == null)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(transform);
            gridObject.transform.localPosition = Vector3.zero;
            gridObject.AddComponent<Grid>();

            GameObject tilemapObject = new GameObject("Tilemap_Mundo");
            tilemapObject.transform.SetParent(gridObject.transform);
            tilemapObject.transform.localPosition = Vector3.zero;
            tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
        }

        if (tileTierra == null)
        {
            tileTierra = CrearTileTemporal(new Color32(111, 76, 48, 255));
        }

        if (tilePiedra == null)
        {
            tilePiedra = CrearTileTemporal(new Color32(96, 98, 106, 255));
        }
    }

    private Tile CrearTileTemporal(Color32 color)
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[16 * 16];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        sprite.hideFlags = HideFlags.HideAndDontSave;

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.hideFlags = HideFlags.HideAndDontSave;
        return tile;
    }
}
