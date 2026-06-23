using System.Collections.Generic;
using UnityEngine;

public class SlimeAutoSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject fireSlimePrefab;
    [SerializeField] private GameObject acidSlimePrefab;
    [SerializeField] private GameObject coldSlimePrefab;
    [SerializeField] private GameObject cleanerSlimePrefab;

    [Header("Reglas de generacion")]
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private int minFireSlimes = 3;
    [SerializeField] private int maxFireSlimes = 5;
    [SerializeField] private int maxAcidSlimes = 20;
    [SerializeField] private int minColdSlimes = 3;
    [SerializeField] private int maxColdSlimes = 5;
    [SerializeField] private int minCleanerSlimes = 3;
    [SerializeField] private int maxCleanerSlimes = 5;
    [SerializeField] private bool spawnImmediately = true;

    [Header("Zona")]
    [SerializeField] private Vector2 fireSpawnCenter = new Vector2(-3.5f, 1f);
    [SerializeField] private Vector2 acidSpawnCenter = new Vector2(3.5f, 1f);
    [SerializeField] private Vector2 coldSpawnCenter = new Vector2(-7f, 1f);
    [SerializeField] private Vector2 cleanerSpawnCenter = new Vector2(7f, 1f);
    [SerializeField] private Vector2 spawnRange = new Vector2(3.2f, 0.6f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundProbeHeight = 4f;
    [SerializeField] private float groundProbeDistance = 8f;
    [SerializeField] private float groundYOffset = 0.72f;

    private float fireTimer;
    private float acidTimer;
    private float coldTimer;
    private float cleanerTimer;
    private GameObject fireTemplate;
    private GameObject coldTemplate;
    private GameObject cleanerTemplate;
    private static SlimeAutoSpawner instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInScene()
    {
        if (FindFirstObjectByType<SlimeAutoSpawner>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        new GameObject("SlimeAutoSpawner_Runtime").AddComponent<SlimeAutoSpawner>();
    }

    private void Awake()
    {
        instancia = this;
        fireTimer = spawnImmediately ? spawnInterval : 0f;
        acidTimer = spawnImmediately ? spawnInterval : 0f;
        coldTimer = spawnImmediately ? spawnInterval : 0f;
        cleanerTimer = spawnImmediately ? spawnInterval : 0f;
        CacheSceneTemplates();
    }

    private void Start()
    {
        MaintainAll();
    }

    public static void ReponerAhora()
    {
        if (instancia == null)
        {
            instancia = FindFirstObjectByType<SlimeAutoSpawner>(FindObjectsInactive.Include);
        }

        if (instancia != null)
        {
            instancia.MaintainAll();
        }
    }

    private void MaintainAll()
    {
        MaintainFireSlimes();
        MaintainColdSlimes();
        MaintainCleanerSlimes();
    }

    private void Update()
    {
        fireTimer += Time.deltaTime;
        acidTimer += Time.deltaTime;
        coldTimer += Time.deltaTime;
        cleanerTimer += Time.deltaTime;

        if (fireTimer >= spawnInterval) { fireTimer = 0f; MaintainFireSlimes(); }
        if (acidTimer >= spawnInterval) { acidTimer = 0f; TrySpawnAcidSlime(); }
        if (coldTimer >= spawnInterval) { coldTimer = 0f; MaintainColdSlimes(); }
        if (cleanerTimer >= spawnInterval) { cleanerTimer = 0f; MaintainCleanerSlimes(); }
    }

    private void MaintainFireSlimes()
    {
        List<FireSlimeAI> slimes = GetWildSlimes(FindObjectsByType<FireSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        TrimExcess(slimes, maxFireSlimes);

        int activeCount = Mathf.Min(slimes.Count, maxFireSlimes);
        while (activeCount < minFireSlimes)
        {
            if (!Spawn(fireSlimePrefab != null ? fireSlimePrefab : fireTemplate, fireSpawnCenter, "FireSlime_Auto"))
            {
                return;
            }

            activeCount++;
        }
    }

    private void TrySpawnAcidSlime()
    {
        if (CountActiveAcidSlimes() >= maxAcidSlimes)
        {
            return;
        }

        Spawn(acidSlimePrefab, acidSpawnCenter, "AcidSlime_Auto");
    }

    private void MaintainColdSlimes()
    {
        List<ColdSlimeAI> slimes = GetWildSlimes(FindObjectsByType<ColdSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        TrimExcess(slimes, maxColdSlimes);

        int activeCount = Mathf.Min(slimes.Count, maxColdSlimes);
        while (activeCount < minColdSlimes)
        {
            if (!Spawn(coldSlimePrefab != null ? coldSlimePrefab : coldTemplate, coldSpawnCenter, "ColdSlime_Auto"))
            {
                return;
            }

            activeCount++;
        }
    }

    private void MaintainCleanerSlimes()
    {
        List<CleanerSlimeAI> slimes = GetWildSlimes(FindObjectsByType<CleanerSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        TrimExcess(slimes, maxCleanerSlimes);

        int activeCount = Mathf.Min(slimes.Count, maxCleanerSlimes);
        while (activeCount < minCleanerSlimes)
        {
            if (!Spawn(cleanerSlimePrefab != null ? cleanerSlimePrefab : cleanerTemplate, cleanerSpawnCenter, "CleanerSlime_Auto"))
            {
                return;
            }

            activeCount++;
        }
    }

    private int CountActiveFireSlimes()
    {
        return FindObjectsByType<FireSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private int CountActiveAcidSlimes()
    {
        return FindObjectsByType<AcidSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private int CountActiveCleanerSlimes()
        => FindObjectsByType<CleanerSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

    private int CountActiveColdSlimes()
        => FindObjectsByType<ColdSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

    private bool Spawn(GameObject prefab, Vector2 center, string baseName)
    {
        if (prefab == null)
        {
            return false;
        }

        Vector3 spawnPosition = PickSpawnPosition(center);
        GameObject slime = Instantiate(prefab, spawnPosition, Quaternion.identity);
        slime.name = $"{baseName}_{Time.frameCount}";
        slime.SetActive(true);
        return true;
    }

    private void CacheSceneTemplates()
    {
        FireSlimeAI fireSource = FindFirstObjectByType<FireSlimeAI>(FindObjectsInactive.Exclude);
        ColdSlimeAI coldSource = FindFirstObjectByType<ColdSlimeAI>(FindObjectsInactive.Exclude);
        CleanerSlimeAI cleanerSource = FindFirstObjectByType<CleanerSlimeAI>(FindObjectsInactive.Exclude);

        if (fireSlimePrefab == null && fireSource != null)
        {
            fireSpawnCenter = fireSource.transform.position;
        }

        if (coldSlimePrefab == null && coldSource != null)
        {
            coldSpawnCenter = coldSource.transform.position;
        }

        if (cleanerSlimePrefab == null && cleanerSource != null)
        {
            cleanerSpawnCenter = cleanerSource.transform.position;
        }

        fireTemplate = fireSlimePrefab != null ? fireSlimePrefab : CreateTemplateFromScene(fireSource);
        coldTemplate = coldSlimePrefab != null ? coldSlimePrefab : CreateTemplateFromScene(coldSource);
        cleanerTemplate = cleanerSlimePrefab != null ? cleanerSlimePrefab : CreateTemplateFromScene(cleanerSource);
    }

    private GameObject CreateTemplateFromScene(Component source)
    {
        if (source == null)
        {
            return null;
        }

        GameObject template = Instantiate(source.gameObject);
        template.name = source.gameObject.name + "_RespawnTemplate";
        template.SetActive(false);
        template.transform.SetParent(transform, false);
        return template;
    }

    private List<T> GetWildSlimes<T>(T[] activeSlimes) where T : Component
    {
        List<T> wildSlimes = new List<T>();
        foreach (T slime in activeSlimes)
        {
            if (slime != null && slime.GetComponent<SmileEnGranja>() == null)
            {
                wildSlimes.Add(slime);
            }
        }

        return wildSlimes;
    }

    private void TrimExcess<T>(List<T> activeSlimes, int maxAllowed) where T : Component
    {
        if (activeSlimes.Count <= maxAllowed)
        {
            return;
        }

        for (int i = maxAllowed; i < activeSlimes.Count; i++)
        {
            if (activeSlimes[i] != null)
            {
                activeSlimes[i].gameObject.SetActive(false);
            }
        }
    }

    private Vector3 PickSpawnPosition(Vector2 center)
    {
        Vector2 randomOffset = new Vector2(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y));

        Vector2 probeOrigin = center + randomOffset + Vector2.up * groundProbeHeight;
        RaycastHit2D groundHit = Physics2D.Raycast(probeOrigin, Vector2.down, groundProbeDistance, groundLayer);

        if (groundHit.collider != null)
        {
            return new Vector3(groundHit.point.x, groundHit.point.y + groundYOffset, 0f);
        }

        return new Vector3(center.x + randomOffset.x, center.y + randomOffset.y, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        DrawSpawnArea(fireSpawnCenter, new Color(1f, 0.35f, 0.05f, 0.35f));
        DrawSpawnArea(acidSpawnCenter, new Color(0.65f, 0.2f, 1f, 0.35f));
        DrawSpawnArea(coldSpawnCenter, new Color(0.25f, 0.75f, 1f, 0.35f));
        DrawSpawnArea(cleanerSpawnCenter, new Color(1f, 0.4f, 0.8f, 0.35f));
    }

    private void DrawSpawnArea(Vector2 center, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(center, new Vector3(spawnRange.x * 2f, spawnRange.y * 2f, 0f));
    }
}
