using System.Collections.Generic;
using UnityEngine;

public class Player1 : MonoBehaviour
{
    private const float GroundedVisualOffsetY = -0.20f;

    [Header("Estadisticas")]
    public float moveSpeed = 4f;
    public float jumpForce = 7f;
    public float ladderSpeed = 3f;
    public bool canJump = true;

    [Header("Referencias")]
    public Rigidbody2D rb;
    public Animator bodyAnim;

    private bool isOnLadder;
    private bool isClimbing;
    private float originalGravityScale;
    private AnimacionEscaleraPlayer animacionEscalera;
    private Collider2D currentLadder;
    private Collider2D[] playerColliders = new Collider2D[0];
    private readonly HashSet<Collider2D> ladderContacts = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> ignoredLadderBlockers = new HashSet<Collider2D>();
    private readonly List<Collider2D> ignoredLadderBlockersBuffer = new List<Collider2D>();


    void Start()
    {
        originalGravityScale = rb.gravityScale;
        AlignPlayer1VisualWithFeet();
        NormalizeFloorColliderSurfaces();
        SetupLadderTriggers();
        CachePlayerColliders();
        bodyAnim.SetBool("Groundded", true);
        EnsureSmileSuctionController();
        EnsureLadderAnimator();
    }

    private void AlignPlayer1VisualWithFeet()
    {
        if (bodyAnim == null)
        {
            return;
        }

        Transform visualRoot = bodyAnim.transform;
        Vector3 localPosition = visualRoot.localPosition;
        localPosition.y = GroundedVisualOffsetY;
        visualRoot.localPosition = localPosition;
    }

    private static void NormalizeFloorColliderSurfaces()
    {
        BoxCollider2D[] floorColliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);

        foreach (BoxCollider2D floorCollider in floorColliders)
        {
            if (floorCollider == null || floorCollider.isTrigger)
            {
                continue;
            }

            SpriteRenderer floorRenderer = floorCollider.GetComponent<SpriteRenderer>();
            if (floorRenderer == null || floorRenderer.sprite == null)
            {
                continue;
            }

            float surfaceInset = GetFloorSurfaceInset(floorRenderer.sprite.name);
            if (surfaceInset <= 0f)
            {
                continue;
            }

            // Only normalize automatic full-sprite colliders. Hand-tuned colliders stay untouched.
            float spriteHeight = floorRenderer.sprite.bounds.size.y;
            if (Mathf.Abs(floorCollider.size.y - spriteHeight) > 0.06f || floorCollider.size.y <= surfaceInset + 0.05f)
            {
                continue;
            }

            Vector2 size = floorCollider.size;
            Vector2 offset = floorCollider.offset;
            size.y -= surfaceInset;
            offset.y -= surfaceInset * 0.5f;
            floorCollider.size = size;
            floorCollider.offset = offset;
        }
    }

    private static float GetFloorSurfaceInset(string spriteName)
    {
        string normalizedName = spriteName.ToLowerInvariant();

        if (normalizedName.Contains("pisohonguitos")) return 2.03f;
        if (normalizedName.Contains("pisocristal")) return 2.13f;
        if (normalizedName.Contains("pisocontaminado")) return 1.70f;
        if (normalizedName.Contains("pisohielo")) return 0.48f;
        if (normalizedName.Contains("pisopasto")) return 0.66f;

        return 0f;
    }

    void Update()
    {
        Movement();
        Mirror();
        Jump();
        MeleeAtack();
        bodyAnim.SetFloat("FallingSpeed", isClimbing ? 0f : rb.linearVelocity.y);

    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal"); //-1 a 1
        float y = Input.GetAxis("Vertical");
        RestoreSeparatedLadderBlockers();

        if (isOnLadder && Mathf.Abs(y) > 0.1f)
        {
            StartClimbing();
        }

        if (isClimbing && (!isOnLadder || currentLadder == null))
        {
            StopClimbing(x);
        }

        if (isClimbing)
        {
            if (Mathf.Abs(x) > 0.1f && Mathf.Abs(y) < 0.1f)
            {
                StopClimbing(x);
            }
            else
            {
                UpdateLadderPassThrough(y);
                rb.gravityScale = 0f;
                rb.linearVelocity = new Vector2(0f, y * ladderSpeed);

                Vector3 position = transform.position;
                if (currentLadder != null)
                {
                    position.x = Mathf.MoveTowards(position.x, currentLadder.bounds.center.x, moveSpeed * Time.deltaTime * 3f);
                }
                transform.position = position;

                bodyAnim.SetBool("Groundded", true);
                bodyAnim.SetFloat("Speed", 0f);
                animacionEscalera?.ActualizarEscalera(true, y);
                return;
            }
        }

        animacionEscalera?.ActualizarEscalera(false, 0f);
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);
        bodyAnim.SetFloat("Speed", Mathf.Abs(x));
    }

    private void CachePlayerColliders()
    {
        playerColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void UpdateLadderPassThrough(float verticalInput)
    {
        if (currentLadder == null || Mathf.Abs(verticalInput) < 0.05f)
        {
            return;
        }

        Bounds playerBounds = GetPlayerBounds();
        Bounds ladderBounds = currentLadder.bounds;
        float direction = Mathf.Sign(verticalInput);
        Vector2 probeCenter = new Vector2(
            ladderBounds.center.x,
            playerBounds.center.y + direction * 0.45f);
        Vector2 probeSize = new Vector2(
            Mathf.Clamp(ladderBounds.size.x * 0.45f, 0.55f, 1.35f),
            Mathf.Max(playerBounds.size.y + 0.35f, 2.2f));

        foreach (Collider2D blocker in Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f))
        {
            if (!ShouldIgnoreForLadder(blocker, ladderBounds))
            {
                continue;
            }

            SetIgnoreCollisionWithPlayer(blocker, true);
            ignoredLadderBlockers.Add(blocker);
        }
    }

    private bool ShouldIgnoreForLadder(Collider2D blocker, Bounds ladderBounds)
    {
        if (blocker == null
            || blocker.isTrigger
            || blocker == currentLadder
            || blocker.transform.IsChildOf(transform)
            || ladderContacts.Contains(blocker))
        {
            return false;
        }

        Rigidbody2D blockerBody = blocker.attachedRigidbody;
        if (blockerBody != null && blockerBody.bodyType != RigidbodyType2D.Static)
        {
            return false;
        }

        Bounds bounds = blocker.bounds;
        float ladderCenterX = ladderBounds.center.x;
        bool crossesLadderShaft = bounds.min.x < ladderCenterX + 0.28f
            && bounds.max.x > ladderCenterX - 0.28f
            && bounds.max.y > ladderBounds.min.y - 0.35f
            && bounds.min.y < ladderBounds.max.y + 0.35f;

        if (!crossesLadderShaft)
        {
            return false;
        }

        string nameLower = blocker.gameObject.name.ToLowerInvariant();
        bool looksLikeFloor = nameLower.Contains("piso")
            || nameLower.Contains("suelo")
            || nameLower.Contains("plataforma")
            || nameLower.Contains("terreno");
        bool horizontalShape = bounds.size.x > bounds.size.y * 1.35f;

        return looksLikeFloor || horizontalShape;
    }

    private Bounds GetPlayerBounds()
    {
        bool hasBounds = false;
        Bounds combined = new Bounds(transform.position, Vector3.one);

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null || playerCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                combined = playerCollider.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(playerCollider.bounds);
            }
        }

        return combined;
    }

    private void RestoreSeparatedLadderBlockers()
    {
        if (ignoredLadderBlockers.Count == 0)
        {
            return;
        }

        Bounds playerBounds = GetPlayerBounds();
        ignoredLadderBlockersBuffer.Clear();

        foreach (Collider2D blocker in ignoredLadderBlockers)
        {
            if (blocker == null || !playerBounds.Intersects(blocker.bounds))
            {
                ignoredLadderBlockersBuffer.Add(blocker);
            }
        }

        foreach (Collider2D blocker in ignoredLadderBlockersBuffer)
        {
            if (blocker != null)
            {
                SetIgnoreCollisionWithPlayer(blocker, false);
            }

            ignoredLadderBlockers.Remove(blocker);
        }
    }

    private void RestoreAllLadderBlockers()
    {
        foreach (Collider2D blocker in ignoredLadderBlockers)
        {
            if (blocker != null)
            {
                SetIgnoreCollisionWithPlayer(blocker, false);
            }
        }

        ignoredLadderBlockers.Clear();
    }

    private void SetIgnoreCollisionWithPlayer(Collider2D other, bool ignore)
    {
        if (other == null)
        {
            return;
        }

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null || playerCollider == other)
            {
                continue;
            }

            Physics2D.IgnoreCollision(playerCollider, other, ignore);
        }
    }


    void Mirror()
    {
        if (isClimbing)
        {
            transform.rotation = Quaternion.identity;
            return;
        }

        if (rb.linearVelocity.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (rb.linearVelocity.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void Jump()
    {
        if (isClimbing)
        {
            return;
        }

        if (canJump)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                canJump = false;
                bodyAnim.SetBool("Groundded", false);
                bodyAnim.SetFloat("FallingSpeed", rb.linearVelocity.y);
                bodyAnim.Play("Player1_Jump", 0, 0f);
            }
        }
    }
    void MeleeAtack()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            bodyAnim.SetTrigger("Attack");
        }

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    private void CheckGround(Collision2D collision)
    {
        if (rb.linearVelocity.y > 0.1f)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                bool justLanded = !canJump || !bodyAnim.GetBool("Groundded");
                canJump = true;
                bodyAnim.SetBool("Groundded", true);
                bodyAnim.SetFloat("FallingSpeed", 0f);

                if (justLanded)
                {
                    string groundedState = Mathf.Abs(rb.linearVelocity.x) > 0.1f ? "Player1_Walk" : "Player1_Idle";
                    bodyAnim.Play(groundedState, 0, 0f);
                }

                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsLadder(collision.gameObject))
        {
            RegisterLadder(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsLadder(collision.gameObject))
        {
            RegisterLadder(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsLadder(collision.gameObject))
        {
            ladderContacts.Remove(collision);
            RefreshCurrentLadder();

            if (!isOnLadder)
            {
                StopClimbing(0f);
            }
        }
    }

    private void SetupLadderTriggers()
    {
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject sceneObject in sceneObjects)
        {
            if (!IsLadder(sceneObject))
            {
                continue;
            }

            BoxCollider2D ladderCollider = sceneObject.GetComponent<BoxCollider2D>();

            if (ladderCollider == null)
            {
                ladderCollider = sceneObject.AddComponent<BoxCollider2D>();
            }

            SpriteRenderer spriteRenderer = sceneObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                ladderCollider.size = new Vector2(
                    spriteRenderer.size.x,
                    spriteRenderer.size.y + 0.9f);
            }

            ladderCollider.isTrigger = true;
        }
    }

    private bool IsLadder(GameObject sceneObject)
    {
        return sceneObject.name.ToLower().Contains("escalera");
    }

    private void RegisterLadder(Collider2D ladder)
    {
        ladderContacts.Add(ladder);
        currentLadder = ladder;
        isOnLadder = true;
    }

    private void RefreshCurrentLadder()
    {
        ladderContacts.RemoveWhere(ladder => ladder == null);
        currentLadder = null;

        foreach (Collider2D ladder in ladderContacts)
        {
            currentLadder = ladder;
            break;
        }

        isOnLadder = currentLadder != null;
    }

    private void StartClimbing()
    {
        if (isClimbing)
        {
            return;
        }

        isClimbing = true;
        canJump = false;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        bodyAnim.SetFloat("FallingSpeed", 0f);
    }

    private void StopClimbing(float horizontalInput)
    {
        bool wasClimbing = isClimbing;
        isClimbing = false;
        rb.gravityScale = originalGravityScale;
        animacionEscalera?.ActualizarEscalera(false, 0f);

        if (!wasClimbing)
        {
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        bodyAnim.SetBool("Groundded", canJump);
        bodyAnim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        bodyAnim.SetFloat("FallingSpeed", canJump ? 0f : rb.linearVelocity.y);

        if (canJump)
        {
            bodyAnim.Play(Mathf.Abs(horizontalInput) > 0.1f ? "Player1_Walk" : "Player1_Idle", 0, 0f);
        }
        else if (rb.linearVelocity.y <= 0f)
        {
            bodyAnim.Play("Player1_Falling", 0, 0f);
        }
    }

    private void OnDisable()
    {
        RestoreAllLadderBlockers();
    }

    private void EnsureSmileSuctionController()
    {
        if (GetComponent<SmileSuctionController>() == null)
        {
            gameObject.AddComponent<SmileSuctionController>();
        }
    }

    private void EnsureLadderAnimator()
    {
        animacionEscalera = GetComponent<AnimacionEscaleraPlayer>();

        if (animacionEscalera == null)
        {
            animacionEscalera = gameObject.AddComponent<AnimacionEscaleraPlayer>();
        }
    }
}
