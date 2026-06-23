using UnityEngine;

[DisallowMultipleComponent]
public class Player2VisualAnimator : MonoBehaviour
{
    private const float PixelsPerUnit = 300f;
    private const float WalkFramesPerSecond = 10.5f;
    private const float NormalVisualScale = 1.45f;
    private const float ClimbVisualScale = 1.75f;
    private const string WalkResource = "Player2/Player2_Walk";
    private const string JumpResource = "Player2/Player2_Jump";
    private const string LadderResource = "Player2/Player2_Ladder";

    private Player1 controller;
    private Rigidbody2D body;
    private GameObject visualObject;
    private SpriteRenderer visualRenderer;
    private SpriteRenderer[] player1Renderers;
    private bool[] player1RendererStates;
    private Sprite[] walkFrames;
    private Sprite[] jumpFrames;
    private Sprite[] climbUpFrames;
    private Sprite[] climbDownFrames;
    private bool usingPlayer2;
    private bool climbing;
    private bool climbingDown;
    private float verticalClimbInput;
    private float animationTime;
    private float attackTimeRemaining;
    private readonly Vector3 normalVisualBasePosition = new Vector3(0f, -1.35f, -0.2f);
    private readonly Vector3 climbVisualBasePosition = new Vector3(0f, -1f, -0.2f);

    public bool IsUsingPlayer2 => usingPlayer2;

    private void Awake()
    {
        controller = GetComponent<Player1>();
        body = GetComponent<Rigidbody2D>();
        CachePlayer1Renderers();
        CreatePlayer2Visual();
        LoadFrames();
        ApplyCharacter(1);
    }

    private void Update()
    {
        if (!usingPlayer2 || visualRenderer == null)
        {
            return;
        }

        animationTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.J))
        {
            attackTimeRemaining = 0.5f;
        }

        attackTimeRemaining = Mathf.Max(0f, attackTimeRemaining - Time.deltaTime);
        UpdatePlayer2Frame();
    }

    private void LateUpdate()
    {
        if (!usingPlayer2)
        {
            return;
        }

        SetPlayer1Renderers(false);
    }

    public void ApplyCharacter(int characterNumber)
    {
        usingPlayer2 = characterNumber == 2;
        climbing = false;
        attackTimeRemaining = 0f;
        animationTime = 0f;

        SetPlayer1Renderers(!usingPlayer2);

        if (visualObject != null)
        {
            visualObject.SetActive(usingPlayer2);
            visualObject.transform.localPosition = normalVisualBasePosition;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one * NormalVisualScale;
        }

        if (usingPlayer2 && visualRenderer != null && walkFrames.Length > 0)
        {
            visualRenderer.sprite = walkFrames[0];
        }
    }

    public void SetClimbing(bool active, float input)
    {
        if (!usingPlayer2)
        {
            return;
        }

        climbing = active;
        verticalClimbInput = input;

        if (Mathf.Abs(input) > 0.05f)
        {
            climbingDown = input < 0f;
        }
    }

    public static void SelectCharacter(int characterNumber)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No se encontro el jugador para aplicar la seleccion.");
            return;
        }

        Player2VisualAnimator selector = player.GetComponent<Player2VisualAnimator>();
        if (selector == null)
        {
            selector = player.AddComponent<Player2VisualAnimator>();
        }

        selector.ApplyCharacter(characterNumber);
        Debug.Log("Personaje seleccionado: Player " + characterNumber);
    }

    private void UpdatePlayer2Frame()
    {
        Sprite[] activeClimbFrames = climbingDown && climbDownFrames.Length > 0
            ? climbDownFrames
            : climbUpFrames;

        if (climbing && activeClimbFrames.Length > 0)
        {
            float speed = Mathf.Abs(verticalClimbInput) > 0.05f ? 12f : 0f;
            int frame = speed <= 0f ? 0 : Mathf.FloorToInt(animationTime * speed) % activeClimbFrames.Length;
            visualRenderer.sprite = activeClimbFrames[frame];
            ApplyClimbMotion(speed > 0f);
            return;
        }

        if (attackTimeRemaining > 0f && walkFrames.Length > 0)
        {
            float progress = 1f - attackTimeRemaining / 0.5f;
            int frame = Mathf.Clamp(Mathf.FloorToInt(progress * walkFrames.Length), 0, walkFrames.Length - 1);
            visualRenderer.sprite = walkFrames[frame];
            float kick = Mathf.Sin(progress * Mathf.PI);
            visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, -7f * kick);
            visualObject.transform.localScale = new Vector3(1f + kick * 0.06f, 1f - kick * 0.04f, 1f);
            visualObject.transform.localScale *= NormalVisualScale;
            visualObject.transform.localPosition = normalVisualBasePosition;
            return;
        }

        if (controller != null && !controller.canJump && jumpFrames.Length > 0)
        {
            float verticalSpeed = body != null ? body.linearVelocity.y : 0f;
            float normalized = Mathf.InverseLerp(7f, -7f, verticalSpeed);
            int frame = Mathf.Clamp(Mathf.RoundToInt(normalized * (jumpFrames.Length - 1)), 0, jumpFrames.Length - 1);
            visualRenderer.sprite = jumpFrames[frame];
            ApplySecondaryMotion(0f, Mathf.Lerp(-3f, 4f, normalized));
            return;
        }

        float horizontalSpeed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        if (horizontalSpeed > 0.1f && walkFrames.Length > 0)
        {
            float speedFactor = Mathf.Clamp(horizontalSpeed / 4f, 0.85f, 1.3f);
            int frame = Mathf.FloorToInt(animationTime * WalkFramesPerSecond * speedFactor) % walkFrames.Length;
            visualRenderer.sprite = walkFrames[frame];
            ApplyWalkMotion(speedFactor);
            return;
        }

        if (walkFrames.Length > 0)
        {
            visualRenderer.sprite = walkFrames[0];
        }

        ApplySecondaryMotion(0.012f, Mathf.Sin(animationTime * 2.5f) * 0.8f);
    }

    private void ApplyWalkMotion(float speedFactor)
    {
        visualObject.transform.localPosition = normalVisualBasePosition;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one * NormalVisualScale;
    }

    private void ApplyClimbMotion(bool moving)
    {
        float phase = animationTime * Mathf.PI * 2f * 4.5f;
        float intensity = moving ? 1f : 0.2f;
        float sway = Mathf.Sin(phase) * intensity;
        float lift = Mathf.Abs(Mathf.Cos(phase)) * 0.045f * intensity;
        float stretch = Mathf.Abs(sway);

        visualObject.transform.localPosition = climbVisualBasePosition
            + new Vector3(sway * 0.025f, lift, 0f);
        visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, sway * 3f);
        visualObject.transform.localScale = new Vector3(
            ClimbVisualScale * (1f + stretch * 0.02f),
            ClimbVisualScale * (1f - stretch * 0.015f),
            1f);
    }

    private void ApplySecondaryMotion(float height, float rotation)
    {
        float bob = height <= 0f ? 0f : Mathf.Sin(animationTime * 6f) * height;
        visualObject.transform.localPosition = normalVisualBasePosition + Vector3.up * bob;
        visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        visualObject.transform.localScale = Vector3.one * NormalVisualScale;
    }

    private void CachePlayer1Renderers()
    {
        Animator animator = controller != null ? controller.bodyAnim : GetComponentInChildren<Animator>(true);
        player1Renderers = animator != null
            ? animator.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        player1RendererStates = new bool[player1Renderers.Length];

        for (int i = 0; i < player1Renderers.Length; i++)
        {
            player1RendererStates[i] = player1Renderers[i] != null && player1Renderers[i].enabled;
        }
    }

    private void CreatePlayer2Visual()
    {
        visualObject = new GameObject("Player2_Visual");
        visualObject.transform.SetParent(transform, false);
        visualObject.transform.localPosition = normalVisualBasePosition;

        visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sortingOrder = 20;
        visualRenderer.color = Color.white;
        visualObject.SetActive(false);
    }

    private void LoadFrames()
    {
        walkFrames = Slice(
            Resources.Load<Texture2D>(WalkResource),
            6,
            1,
            new Vector2(0.5f, 0.251f));
        jumpFrames = SliceHorizontalRanges(
            Resources.Load<Texture2D>(JumpResource),
            new[] { 0, 335, 640, 930, 1225, 1536 },
            new Vector2(0.5f, 0.18f));
        Sprite[] ladderFrames = Slice(
            Resources.Load<Texture2D>(LadderResource),
            8,
            2,
            new Vector2(0.5f, 0.12f));
        climbUpFrames = SliceRow(ladderFrames, 0, 8);
        climbDownFrames = SliceRow(ladderFrames, 1, 8);

        if (climbUpFrames.Length == 0)
        {
            climbUpFrames = walkFrames;
        }

        if (climbDownFrames.Length == 0)
        {
            climbDownFrames = climbUpFrames;
        }

        if (walkFrames.Length == 0 || jumpFrames.Length == 0 || climbUpFrames.Length == 0)
        {
            Debug.LogError("Faltan hojas de sprites de Player2 en Resources/Player2.");
        }
    }

    private static Sprite[] Slice(Texture2D texture, int columns, int rows, Vector2 pivot)
    {
        if (texture == null || columns <= 0 || rows <= 0)
        {
            return new Sprite[0];
        }

        int cellWidth = texture.width / columns;
        int cellHeight = texture.height / rows;
        Sprite[] frames = new Sprite[columns * rows];
        int index = 0;

        for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
        {
            int y = texture.height - (rowFromTop + 1) * cellHeight;
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new Rect(column * cellWidth, y, cellWidth, cellHeight);
                frames[index] = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[index].name = texture.name + "_" + index;
                index++;
            }
        }

        return frames;
    }

    private static Sprite[] SliceRow(Sprite[] frames, int rowIndex, int columns)
    {
        if (frames == null || columns <= 0)
        {
            return new Sprite[0];
        }

        int start = rowIndex * columns;
        if (start < 0 || start >= frames.Length)
        {
            return new Sprite[0];
        }

        int count = Mathf.Min(columns, frames.Length - start);
        Sprite[] row = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            row[i] = frames[start + i];
        }

        return row;
    }

    private static Sprite[] SliceHorizontalRanges(Texture2D texture, int[] boundaries, Vector2 pivot)
    {
        if (texture == null || boundaries == null || boundaries.Length < 2)
        {
            return new Sprite[0];
        }

        Sprite[] frames = new Sprite[boundaries.Length - 1];
        for (int i = 0; i < frames.Length; i++)
        {
            int xMin = Mathf.Clamp(boundaries[i], 0, texture.width - 1);
            int xMax = Mathf.Clamp(boundaries[i + 1], xMin + 1, texture.width);
            Rect rect = new Rect(xMin, 0, xMax - xMin, texture.height);
            frames[i] = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[i].name = texture.name + "_" + i;
        }

        return frames;
    }

    private void SetPlayer1Renderers(bool visible)
    {
        for (int i = 0; i < player1Renderers.Length; i++)
        {
            if (player1Renderers[i] != null)
            {
                player1Renderers[i].enabled = visible && player1RendererStates[i];
            }
        }
    }

    private void OnDestroy()
    {
        SetPlayer1Renderers(true);
    }
}
