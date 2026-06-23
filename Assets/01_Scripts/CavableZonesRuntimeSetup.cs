using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class CavableZonesRuntimeSetup
{
    private const string SceneName = "SampleScene";
    private const string ParentName = "Zonas_Cavables_Tilemap";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureIfNeeded();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureIfNeeded();
    }

    private static void ConfigureIfNeeded()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        GameObject previousParent = GameObject.Find(ParentName);
        if (previousParent != null)
        {
            Object.Destroy(previousParent);
        }

        Sprite dirtSprite = FindTemplateSprite("tierra") ?? FindTemplateSprite("piedras1");
        if (dirtSprite == null)
        {
            Debug.LogWarning("No se encontro sprite de tierra/piedra para crear los tilemaps cavables.");
            return;
        }

        GameObject parent = new GameObject(ParentName);
        Grid grid = parent.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 1f);

        Tile dirtTile = ScriptableObject.CreateInstance<Tile>();
        dirtTile.sprite = dirtSprite;
        dirtTile.colliderType = Tile.ColliderType.Sprite;
        dirtTile.flags = TileFlags.None;

        CreateCavableTilemap(
            parent.transform,
            "Tilemap_Cavable_Arriba",
            new Vector3(30.44f, 2.0884f, 0f),
            new Vector3(0.988f, 1.3808f, 1f),
            10,
            2,
            dirtTile);

        CreateCavableTilemap(
            parent.transform,
            "Tilemap_Cavable_Abajo",
            new Vector3(30.04f, -8.1409f, 0f),
            new Vector3(1.0984f, 1.2205f, 1f),
            11,
            2,
            dirtTile);

        CreateCavableTilemap(
            parent.transform,
            "Tilemap_Cavable_Cueva_Izquierda",
            new Vector3(-36.95f, 1.65f, 0f),
            new Vector3(0.866968f, 0.9799554f, 1f),
            17,
            2,
            dirtTile);
    }

    private static void CreateCavableTilemap(Transform parent, string name, Vector3 origin, Vector3 scale, int width, int height, Tile tile)
    {
        GameObject tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(parent, false);
        tilemapObject.transform.position = origin;
        tilemapObject.transform.localScale = scale;

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 12;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        Rigidbody2D body = tilemapObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        TilemapCollider2D collider = tilemapObject.AddComponent<TilemapCollider2D>();
        collider.isTrigger = false;

        TilemapCavableZone cavable = tilemapObject.AddComponent<TilemapCavableZone>();
        cavable.tipoRecurso = TipoRecursoMinado.Tierra;
        cavable.cantidad = 1;
        cavable.golpesNecesarios = 1;
    }

    private static Sprite FindTemplateSprite(string namePrefix)
    {
        return Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(renderer => renderer != null && renderer.sprite != null && renderer.name.StartsWith(namePrefix))
            .OrderBy(renderer => Mathf.Abs(renderer.transform.position.x - 36f) + Mathf.Abs(renderer.transform.position.y - 5f))
            .Select(renderer => renderer.sprite)
            .FirstOrDefault();
    }
}
