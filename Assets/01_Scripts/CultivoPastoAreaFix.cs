using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CultivoPastoAreaFix : MonoBehaviour
{
    private const string SceneName = "SampleScene";
    private const int GrassSortingOrder = -5;
    private const int PlayerSortingOrderInArea = 80;
    private const int PlantSortingOrder = 200;
    private const float PlantScale = 0.72f;
    private const int SlotsX = 12;
    private const int SlotsY = 3;
    private const float SlotColliderScale = 0.62f;
    private const float DirtMinY = 0.12f;
    private const float DirtMaxY = 0.76f;

    private readonly Dictionary<SpriteRenderer, int> originalPlayerOrders = new Dictionary<SpriteRenderer, int>();
    private Bounds cultivoBounds;
    private bool hasBounds;
    private bool playerBoosted;
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstalarSiCorresponde();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstalarSiCorresponde();
    }

    private static void InstalarSiCorresponde()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        if (FindFirstObjectByType<CultivoPastoAreaFix>() != null)
        {
            return;
        }

        new GameObject("Cultivo_Pasto_Area_Fix").AddComponent<CultivoPastoAreaFix>();
    }

    private void Start()
    {
        ConfigurarArea();
        CrearZonasDeCultivoEnPasto();
    }

    private void LateUpdate()
    {
        if (!hasBounds)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null)
        {
            return;
        }

        bool estaEnArea = cultivoBounds.Contains(new Vector3(player.position.x, player.position.y, cultivoBounds.center.z));
        if (estaEnArea == playerBoosted)
        {
            return;
        }

        if (estaEnArea)
        {
            SubirOrdenPlayer();
        }
        else
        {
            RestaurarOrdenPlayer();
        }
    }

    private void ConfigurarArea()
    {
        SpriteRenderer grassRenderer = BuscarRendererPasto();
        if (grassRenderer != null)
        {
            grassRenderer.sortingOrder = GrassSortingOrder;
            cultivoBounds = grassRenderer.bounds;
            cultivoBounds.Expand(new Vector3(0.15f, 0.15f, 0f));
            hasBounds = true;
        }

        if (!hasBounds)
        {
            Collider2D zona = BuscarZonaGranja();
            if (zona != null)
            {
                cultivoBounds = zona.bounds;
                hasBounds = true;
            }
        }
    }

    private void CrearZonasDeCultivoEnPasto()
    {
        if (!hasBounds)
        {
            return;
        }

        GameObject cultivoViejo = GameObject.Find("Cultivo_Directo_Pasto");
        if (cultivoViejo != null)
        {
            Destroy(cultivoViejo);
        }

        GameObject cultivoTierraViejo = GameObject.Find("Cultivo_Directo_Tierra");
        if (cultivoTierraViejo != null)
        {
            Destroy(cultivoTierraViejo);
        }

        GameObject root = new GameObject("Cultivo_Directo_Tierra");
        Sprite semillaVerde = null;
        Sprite semillaAzul = null;
        Sprite semillaAmarilla = null;
        Sprite semillaMorada = null;

        Sprite broteVerde = CargarSpriteOriginal("PLANTAS_PEQUE", "verdePeque", 24f);
        Sprite broteAzul = CargarSpriteOriginal("PLANTAS_PEQUE", "aguaPeque", 24f);
        Sprite broteAmarillo = CargarSpriteOriginal("PLANTAS_PEQUE", "amarilloPeque", 24f);
        Sprite broteMorado = CargarSpriteOriginal("PLANTAS_PEQUE", "moradoPeque", 24f);

        Sprite florVerde = CargarSpriteOriginal("PLANTAS_GRANDES", "verdeGrande", 42f);
        Sprite florAzul = CargarSpriteOriginal("PLANTAS_GRANDES", "aguaGrande", 42f);
        Sprite florAmarilla = CargarSpriteOriginal("PLANTAS_GRANDES", "amarrilloGrande", 42f);
        Sprite florMorada = CargarSpriteOriginal("PLANTAS_GRANDES", "moradoGrande", 42f);

        CrearSlotsCultivo(root.transform, semillaVerde, semillaAzul, semillaAmarilla, semillaMorada, broteVerde, florVerde, broteAzul, florAzul, broteAmarillo, florAmarilla, broteMorado, florMorada);
    }

    private void CrearSlotsCultivo(Transform parent, Sprite semillaVerde, Sprite semillaAzul, Sprite semillaAmarilla, Sprite semillaMorada, Sprite broteVerde, Sprite florVerde, Sprite broteAzul, Sprite florAzul, Sprite broteAmarillo, Sprite florAmarilla, Sprite broteMorado, Sprite florMorada)
    {
        for (int y = 0; y < SlotsY; y++)
        {
            for (int x = 0; x < SlotsX; x++)
            {
                float minX = x / (float)SlotsX;
                float maxX = (x + 1) / (float)SlotsX;
                float minY = Mathf.Lerp(DirtMinY, DirtMaxY, y / (float)SlotsY);
                float maxY = Mathf.Lerp(DirtMinY, DirtMaxY, (y + 1) / (float)SlotsY);
                Vector2 plantPosition = new Vector2((minX + maxX) * 0.5f, Mathf.Lerp(minY, maxY, 0.42f));
                string nombre = $"Zona_Cultivo_Pasto_{x + 1}_{y + 1}";
                int ordenSiembra = y * SlotsX + (y % 2 == 0 ? x : SlotsX - 1 - x);
                CrearZonaCultivo(parent, nombre, ordenSiembra, minX, maxX, minY, maxY, plantPosition, semillaVerde, semillaAzul, semillaAmarilla, semillaMorada, broteVerde, florVerde, broteAzul, florAzul, broteAmarillo, florAmarilla, broteMorado, florMorada);
            }
        }
    }

    private void CrearZonaCultivo(Transform parent, string nombre, int ordenSiembra, float normalizedMinX, float normalizedMaxX, float normalizedMinY, float normalizedMaxY, Vector2 normalizedPlantPosition, Sprite semillaVerde, Sprite semillaAzul, Sprite semillaAmarilla, Sprite semillaMorada, Sprite broteVerde, Sprite florVerde, Sprite broteAzul, Sprite florAzul, Sprite broteAmarillo, Sprite florAmarilla, Sprite broteMorado, Sprite florMorada)
    {
        float minX = Mathf.Lerp(cultivoBounds.min.x, cultivoBounds.max.x, normalizedMinX);
        float maxX = Mathf.Lerp(cultivoBounds.min.x, cultivoBounds.max.x, normalizedMaxX);
        float minY = Mathf.Lerp(cultivoBounds.min.y, cultivoBounds.max.y, normalizedMinY);
        float maxY = Mathf.Lerp(cultivoBounds.min.y, cultivoBounds.max.y, normalizedMaxY);
        Vector3 centroZona = new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            0f);

        GameObject zonaObject = new GameObject(nombre, typeof(BoxCollider2D), typeof(MacetaCultivo));
        zonaObject.transform.SetParent(parent, false);
        zonaObject.transform.position = centroZona;

        BoxCollider2D collider = zonaObject.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(
            Mathf.Abs(maxX - minX) * SlotColliderScale,
            Mathf.Abs(maxY - minY) * SlotColliderScale);

        GameObject visual = new GameObject("Flor_Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(zonaObject.transform, false);
        visual.transform.localScale = new Vector3(PlantScale, PlantScale, 1f);
        visual.transform.position = new Vector3(
            Mathf.Lerp(cultivoBounds.min.x, cultivoBounds.max.x, normalizedPlantPosition.x),
            Mathf.Lerp(cultivoBounds.min.y, cultivoBounds.max.y, normalizedPlantPosition.y),
            0f);

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = PlantSortingOrder;
        renderer.sprite = null;

        MacetaCultivo maceta = zonaObject.GetComponent<MacetaCultivo>();
        maceta.distanciaUsoFallback = 0.85f;
        maceta.tiempoCrecimiento = 30f;
        maceta.plantarEnPosicionJugador = false;
        maceta.usarDistanciaHorizontalParaInteractuar = true;
        maceta.usarOrdenSiembra = true;
        maceta.ordenSiembra = ordenSiembra;
        maceta.preferirSiPuedeAbrirMenu = true;
        maceta.plantaVisual = visual;
        maceta.plantaSpriteRenderer = renderer;
        maceta.semillaVerde = semillaVerde;
        maceta.semillaAzul = semillaAzul;
        maceta.semillaAmarilla = semillaAmarilla;
        maceta.semillaMorada = semillaMorada;
        maceta.broteVerde = broteVerde;
        maceta.plantaVerdeLista = florVerde;
        maceta.broteAzul = broteAzul;
        maceta.plantaAzulLista = florAzul;
        maceta.broteAmarillo = broteAmarillo;
        maceta.plantaAmarillaLista = florAmarilla;
        maceta.broteMorado = broteMorado;
        maceta.plantaMoradaLista = florMorada;
        maceta.limitarSemillasPermitidas = true;
        maceta.permitirVerde = true;
        maceta.permitirAzul = true;
        maceta.permitirAmarilla = true;
        maceta.permitirMorada = true;
    }

    private static Sprite CargarSpriteOriginal(string assetSearchName, string spriteName, float recorteInferiorPixels)
    {
#if UNITY_EDITOR
        string assetPath = null;
        string[] guids = AssetDatabase.FindAssets(assetSearchName + " t:Texture2D", new[] { "Assets/03_Sprites" });
        if (guids.Length > 0)
        {
            assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = "Assets/03_Sprites/" + assetSearchName + ".png";
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return RecortarBase(sprite, recorteInferiorPixels);
            }
        }
#endif
        string resourcesPath = assetSearchName.StartsWith("PLANTAS_PEQUE")
            ? "PLANTAS_PEQUE\u00D1AS"
            : assetSearchName;
        Sprite[] resourcesSprites = Resources.LoadAll<Sprite>(resourcesPath);
        foreach (Sprite sprite in resourcesSprites)
        {
            if (sprite != null && sprite.name == spriteName)
            {
                return RecortarBase(sprite, recorteInferiorPixels);
            }
        }

        Debug.LogWarning("No se encontro el sprite original de planta: " + spriteName);
        return null;
    }

    private static Sprite RecortarBase(Sprite sprite, float recorteInferiorPixels)
    {
        if (sprite == null || recorteInferiorPixels <= 0f || sprite.rect.height <= recorteInferiorPixels + 8f)
        {
            return sprite;
        }

        Rect rect = sprite.rect;
        float recorte = Mathf.Min(recorteInferiorPixels, rect.height * 0.22f);
        Rect nuevoRect = new Rect(rect.x, rect.y + recorte, rect.width, rect.height - recorte);
        float pivotX = sprite.pivot.x / rect.width;
        float pivotY = Mathf.Clamp01((sprite.pivot.y - recorte) / nuevoRect.height);
        Sprite recortado = Sprite.Create(sprite.texture, nuevoRect, new Vector2(pivotX, pivotY), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        recortado.name = sprite.name + "_sin_base";
        return recortado;
    }

    private void SubirOrdenPlayer()
    {
        originalPlayerOrders.Clear();
        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            originalPlayerOrders[renderer] = renderer.sortingOrder;
            renderer.sortingOrder = PlayerSortingOrderInArea;
        }

        playerBoosted = true;
    }

    private void RestaurarOrdenPlayer()
    {
        foreach (KeyValuePair<SpriteRenderer, int> item in originalPlayerOrders)
        {
            if (item.Key != null)
            {
                item.Key.sortingOrder = item.Value;
            }
        }

        originalPlayerOrders.Clear();
        playerBoosted = false;
    }

    private static SpriteRenderer BuscarRendererPasto()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SpriteRenderer mejor = null;
        float mejorArea = 0f;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            string spriteName = renderer.sprite.name.ToLowerInvariant();
            if (!objectName.Contains("pasto") && !spriteName.Contains("pasto"))
            {
                continue;
            }

            float area = renderer.bounds.size.x * renderer.bounds.size.y;
            if (area > mejorArea)
            {
                mejor = renderer;
                mejorArea = area;
            }
        }

        return mejor;
    }

    private static Collider2D BuscarZonaGranja()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.name.Contains("Zona_Granja"))
            {
                return collider;
            }
        }

        return null;
    }

}
