using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CrystalEndlessWorld : MonoBehaviour
{
    private const float ActivationDistance = 4f;
    private const float GenerationDistance = 34f;
    private const float CleanupDistance = 46f;
    private const int MaxActiveChunks = 10;

    private readonly SortedDictionary<int, GameObject> chunks = new SortedDictionary<int, GameObject>();

    private Transform player;
    private BoxCollider2D anchorFloor;
    private SpriteRenderer anchorFloorRenderer;
    private Collider2D groundTemplateCollider;
    private SpriteRenderer groundTemplateRenderer;
    private SpriteRenderer backgroundTemplate;
    private Transform generatedRoot;
    private float startX;
    private float floorY;
    private float chunkWidth = 12f;
    private int nextChunkIndex;
    private Vector3 lastSafePosition;
    private float nextSafePositionSample;
    private bool initialized;
    private bool active;

    private IEnumerator Start()
    {
        yield return null;
        InitializeCrystalExtension();
    }

    private void Update()
    {
        if (!initialized || player == null)
        {
            return;
        }

        if (!active)
        {
            if (player.position.x < startX - ActivationDistance)
            {
                return;
            }

            ActivateExtension();
        }

        GenerateAhead(player.position.x + GenerationDistance);
        CleanupBehind(player.position.x - CleanupDistance);
        UpdateSafeRespawn();
    }

    private void InitializeCrystalExtension()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : FindFirstObjectByType<Player1>()?.transform;
        anchorFloor = FindRightmostCrystalFloor();

        if (player == null || anchorFloor == null)
        {
            Debug.LogWarning("No se pudo iniciar la extension infinita del bioma de cristal.");
            enabled = false;
            return;
        }

        anchorFloorRenderer = anchorFloor.GetComponent<SpriteRenderer>();
        floorY = anchorFloor.bounds.max.y;
        backgroundTemplate = FindCrystalBackground(anchorFloor.bounds.center);
        startX = FindRoomRightEdge(anchorFloor.bounds.max.x) + 0.05f;

        if (backgroundTemplate != null)
        {
            chunkWidth = Mathf.Clamp(backgroundTemplate.bounds.size.x, 10f, 18f);
        }

        GameObject root = new GameObject("MundoCristal_Infinito");
        generatedRoot = root.transform;
        initialized = true;
    }

    private void ActivateExtension()
    {
        active = true;
        FindGroundUnderPlayer();
        backgroundTemplate = FindCrystalBackground(new Vector2(player.position.x, floorY));
        lastSafePosition = player.position;
        OpenRightPassage();

        Camera mainCamera = Camera.main;
        CameraFollow2D cameraFollow = mainCamera != null ? mainCamera.GetComponent<CameraFollow2D>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.useLimits = false;
        }

        GenerateAhead(player.position.x + GenerationDistance);
        Debug.Log("Extension infinita de cristal activada.");
    }

    private void FindGroundUnderPlayer()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll((Vector2)player.position + Vector2.up * 0.5f, Vector2.down, 30f);
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(player))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                groundTemplateCollider = hit.collider;
                floorY = hit.point.y;
            }
        }

        groundTemplateRenderer = groundTemplateCollider != null
            ? groundTemplateCollider.GetComponent<SpriteRenderer>()
            : null;

        if (groundTemplateRenderer == null)
        {
            groundTemplateRenderer = anchorFloorRenderer;
            groundTemplateCollider = anchorFloor;
        }
    }

    private BoxCollider2D FindRightmostCrystalFloor()
    {
        BoxCollider2D best = null;
        float bestRightEdge = float.NegativeInfinity;

        foreach (BoxCollider2D candidate in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            SpriteRenderer renderer = candidate != null ? candidate.GetComponent<SpriteRenderer>() : null;
            if (candidate == null || candidate.isTrigger || renderer == null || renderer.sprite == null)
            {
                continue;
            }

            if (!renderer.sprite.name.ToLowerInvariant().Contains("pisocristal"))
            {
                continue;
            }

            if (candidate.bounds.max.x > bestRightEdge)
            {
                best = candidate;
                bestRightEdge = candidate.bounds.max.x;
            }
        }

        return best;
    }

    private SpriteRenderer FindCrystalBackground(Vector2 anchorPosition)
    {
        SpriteRenderer best = null;
        float bestScore = float.NegativeInfinity;

        foreach (SpriteRenderer candidate in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.sprite == null || candidate == anchorFloorRenderer)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;
            if (bounds.size.x < 5f || bounds.size.x > 28f || bounds.size.y < 3f || bounds.size.y > 16f)
            {
                continue;
            }

            float horizontalDistance = Mathf.Abs(bounds.center.x - anchorPosition.x);
            float verticalDistance = Mathf.Abs(bounds.center.y - (floorY + 2.5f));
            float containsBonus = bounds.min.x <= anchorPosition.x && bounds.max.x >= anchorPosition.x ? 20f : 0f;
            float score = containsBonus + bounds.size.x * bounds.size.y * 0.08f - horizontalDistance - verticalDistance * 2f;

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private float FindRoomRightEdge(float minimumEdge)
    {
        float edge = minimumEdge;

        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Bounds bounds = renderer.bounds;
            bool overlapsCrystalHeight = bounds.max.y >= floorY - 1f && bounds.min.y <= floorY + 7f;
            bool isRoomPiece = bounds.size.x < 20f && bounds.size.y < 14f;

            if (overlapsCrystalHeight && isRoomPiece && bounds.max.x > edge && bounds.max.x < minimumEdge + 12f)
            {
                edge = bounds.max.x;
            }
        }

        return edge;
    }

    private void OpenRightPassage()
    {
        foreach (BoxCollider2D candidate in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate == anchorFloor || candidate.isTrigger)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;
            bool blocksExit = bounds.max.x >= startX - 1.5f
                && bounds.min.x <= startX + 0.2f
                && bounds.min.y < floorY + 2.6f
                && bounds.max.y > floorY + 0.1f
                && bounds.size.y > bounds.size.x * 1.2f;

            if (!blocksExit)
            {
                continue;
            }

            candidate.enabled = false;
            SpriteRenderer wallRenderer = candidate.GetComponent<SpriteRenderer>();
            if (wallRenderer != null)
            {
                wallRenderer.enabled = false;
            }
        }
    }

    private void GenerateAhead(float targetX)
    {
        while (startX + nextChunkIndex * chunkWidth < targetX)
        {
            SpawnChunk(nextChunkIndex);
            nextChunkIndex++;
        }
    }

    private void SpawnChunk(int index)
    {
        float left = startX + index * chunkWidth;
        GameObject chunk = new GameObject("Chunk_Cristal_" + index);
        chunk.transform.SetParent(generatedRoot, false);
        chunks[index] = chunk;

        CreateBackground(chunk.transform, left);
        CreateMarioGround(chunk.transform, left, index);
    }

    private void CreateBackground(Transform parent, float left)
    {
        if (backgroundTemplate == null)
        {
            return;
        }

        float templateWidth = Mathf.Max(0.1f, backgroundTemplate.bounds.size.x);
        int copies = Mathf.CeilToInt(chunkWidth / templateWidth);

        for (int i = 0; i < copies; i++)
        {
            float centerX = left + Mathf.Min((i + 0.5f) * templateWidth, chunkWidth - templateWidth * 0.5f);
            GameObject background = new GameObject("Fondo_Cristal");
            background.transform.SetParent(parent, false);
            background.transform.position = new Vector3(centerX, backgroundTemplate.transform.position.y, backgroundTemplate.transform.position.z);
            background.transform.localScale = backgroundTemplate.transform.lossyScale;

            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            CopyRenderer(backgroundTemplate, renderer);
        }
    }

    private void CreateMarioGround(Transform parent, float left, int index)
    {
        System.Random random = new System.Random(1709 + index * 7919);
        float gapWidth = Mathf.Lerp(1.35f, 2.25f, (float)random.NextDouble());
        float gapCenter = left + Mathf.Lerp(4.2f, chunkWidth - 4.2f, (float)random.NextDouble());
        float gapLeft = gapCenter - gapWidth * 0.5f;
        float gapRight = gapCenter + gapWidth * 0.5f;

        CreateGroundSegment(parent, left, gapLeft - left, floorY, 1.25f, "Terreno_Izquierdo");
        CreateGroundSegment(parent, gapRight, left + chunkWidth - gapRight, floorY, 1.25f, "Terreno_Derecho");

        float bridgeHeight = floorY + Mathf.Lerp(1.1f, 1.65f, (float)random.NextDouble());
        float bridgeWidth = gapWidth + Mathf.Lerp(0.55f, 1.25f, (float)random.NextDouble());
        CreateCrystalPlatform(parent, gapCenter, bridgeHeight, bridgeWidth);

        if (random.NextDouble() > 0.45)
        {
            float bonusX = left + (random.NextDouble() > 0.5 ? chunkWidth * 0.25f : chunkWidth * 0.75f);
            float bonusTop = floorY + Mathf.Lerp(2.35f, 3.15f, (float)random.NextDouble());
            CreateCrystalPlatform(parent, bonusX, bonusTop, Mathf.Lerp(1.8f, 2.8f, (float)random.NextDouble()));
        }
    }

    private void CreateGroundSegment(Transform parent, float left, float width, float topY, float depth, string objectName)
    {
        if (width <= 0.1f)
        {
            return;
        }

        GameObject ground = new GameObject(objectName);
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(left + width * 0.5f, topY - depth * 0.5f, 0f);

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width + 0.04f, depth);
        CreateGroundVisual(ground.transform, width, topY);
    }

    private void CreateGroundVisual(Transform parent, float width, float topY)
    {
        if (groundTemplateRenderer == null || groundTemplateRenderer.sprite == null)
        {
            return;
        }

        GameObject visual = new GameObject("Visual_Terreno");
        visual.transform.SetParent(parent, false);

        float sourceWorldWidth = Mathf.Max(0.1f, groundTemplateRenderer.bounds.size.x);
        float sourceTopOffset = groundTemplateRenderer.bounds.max.y - groundTemplateRenderer.transform.position.y;
        float surfaceDecoration = groundTemplateCollider != null
            ? groundTemplateRenderer.bounds.max.y - groundTemplateCollider.bounds.max.y
            : 0f;

        visual.transform.position = new Vector3(
            parent.position.x,
            topY + surfaceDecoration - sourceTopOffset,
            groundTemplateRenderer.transform.position.z);
        visual.transform.localScale = new Vector3(
            groundTemplateRenderer.transform.lossyScale.x * (width / sourceWorldWidth),
            groundTemplateRenderer.transform.lossyScale.y,
            1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        CopyRenderer(groundTemplateRenderer, renderer);
    }

    private void CreateCrystalPlatform(Transform parent, float centerX, float topY, float width)
    {
        GameObject platform = new GameObject("Plataforma_Cristal");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, topY - 0.16f, 0f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, 0.32f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(platform.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.02f, anchorFloorRenderer.transform.position.z);

        float spriteWidth = Mathf.Max(0.1f, anchorFloorRenderer.sprite.bounds.size.x);
        float scaleX = width / spriteWidth;
        visual.transform.localScale = new Vector3(scaleX, anchorFloorRenderer.transform.lossyScale.y * 0.55f, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        CopyRenderer(anchorFloorRenderer, renderer);
    }

    private void UpdateSafeRespawn()
    {
        if (player.position.y < floorY - 7f)
        {
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            Vector3 respawnPosition = lastSafePosition + Vector3.up * 0.45f;
            player.position = respawnPosition;

            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector2.zero;
            }

            return;
        }

        if (Time.unscaledTime < nextSafePositionSample)
        {
            return;
        }

        nextSafePositionSample = Time.unscaledTime + 0.2f;
        Player1 controller = player.GetComponent<Player1>();
        if (controller == null || !controller.canJump)
        {
            return;
        }

        RaycastHit2D[] groundHits = Physics2D.RaycastAll((Vector2)player.position, Vector2.down, 1.6f);
        foreach (RaycastHit2D hit in groundHits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(player))
            {
                continue;
            }

            lastSafePosition = player.position;
            break;
        }
    }

    private void CleanupBehind(float minimumX)
    {
        List<int> toRemove = new List<int>();

        foreach (KeyValuePair<int, GameObject> pair in chunks)
        {
            float right = startX + (pair.Key + 1) * chunkWidth;
            if ((right < minimumX || chunks.Count - toRemove.Count > MaxActiveChunks) && pair.Key < nextChunkIndex - 3)
            {
                toRemove.Add(pair.Key);
            }
            else
            {
                break;
            }
        }

        foreach (int index in toRemove)
        {
            Destroy(chunks[index]);
            chunks.Remove(index);
        }
    }

    private static void CopyRenderer(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sprite = source.sprite;
        destination.color = source.color;
        destination.sharedMaterial = source.sharedMaterial;
        destination.flipX = source.flipX;
        destination.flipY = source.flipY;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
    }
}

public static class CrystalEndlessWorldBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGenerator()
    {
        if (Object.FindFirstObjectByType<CrystalEndlessWorld>() != null)
        {
            return;
        }

        new GameObject("Sistema_MundoCristalInfinito").AddComponent<CrystalEndlessWorld>();
    }
}
