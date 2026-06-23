using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class GeneradorMundoPruebasSetup
{
    private const string ScenePath = "Assets/00_Scenes/pruebas/Prueba_GeneradorMundo.unity";
    private const string TileFolder = "Assets/00_Scenes/pruebas/Tiles";

    [InitializeOnLoadMethod]
    private static void CrearEscenaSiFalta()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Path.GetFullPath(ScenePath)))
            {
                return;
            }

            CrearEscena(false);
        };
    }

    [MenuItem("Tools/Pruebas/Crear escena GeneradorMundo")]
    public static void CrearEscena()
    {
        CrearEscena(true);
    }

    private static void CrearEscena(bool dejarAbierta)
    {
        Directory.CreateDirectory(TileFolder);

        Tile tileTierra = CrearTileColor("tileTierra", new Color32(111, 76, 48, 255));
        Tile tilePiedra = CrearTileColor("tilePiedra", new Color32(96, 98, 106, 255));

        Scene escenaAnterior = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "Prueba_GeneradorMundo";
        SceneManager.SetActiveScene(scene);

        GameObject gridObject = new GameObject("Grid");
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        GameObject tilemapObject = new GameObject("Tilemap_Mundo");
        tilemapObject.transform.SetParent(gridObject.transform);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        GameObject generatorObject = new GameObject("GeneradorMundo");
        GeneradorMundo generador = generatorObject.AddComponent<GeneradorMundo>();
        generador.tilemap = tilemap;
        generador.tileTierra = tileTierra;
        generador.tilePiedra = tilePiedra;
        generador.ancho = 50;
        generador.alto = 50;
        generador.GenerarMapa();

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 28f;
        camera.backgroundColor = new Color32(25, 29, 36, 255);
        camera.transform.position = new Vector3(24.5f, 24.5f, -10f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!dejarAbierta && escenaAnterior.IsValid() && escenaAnterior.isLoaded)
        {
            SceneManager.SetActiveScene(escenaAnterior);
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"Escena de prueba creada en {ScenePath}");
    }

    private static Tile CrearTileColor(string nombre, Color32 color)
    {
        string texturePath = $"{TileFolder}/{nombre}.png";
        string tilePath = $"{TileFolder}/{nombre}.asset";

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(texturePath);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
