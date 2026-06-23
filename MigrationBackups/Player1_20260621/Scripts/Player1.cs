using UnityEngine;

public class Player1 : MonoBehaviour
{
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
    private int ladderContacts;
    private float originalGravityScale;
    private AnimacionEscaleraPlayer animacionEscalera;


    void Start()
    {
        originalGravityScale = rb.gravityScale;
        SetupLadderTriggers();
        bodyAnim.SetBool("Groundded", true);
        EnsureSmileSuctionController();
        EnsureLadderAnimator();
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

        if (isOnLadder && Mathf.Abs(y) > 0.1f)
        {
            isClimbing = true;
        }

        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(x * moveSpeed, y * ladderSpeed);
            bodyAnim.SetBool("Groundded", true);
            bodyAnim.SetFloat("Speed", 0f);
            animacionEscalera?.ActualizarEscalera(true, y);
            return;
        }

        animacionEscalera?.ActualizarEscalera(false, 0f);
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);
        bodyAnim.SetFloat("Speed", Mathf.Abs(x));
    }


    void Mirror()
    {
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
                canJump = true;
                bodyAnim.SetBool("Groundded", true);
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsLadder(collision.gameObject))
        {
            ladderContacts++;
            isOnLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsLadder(collision.gameObject))
        {
            ladderContacts = Mathf.Max(0, ladderContacts - 1);

            if (ladderContacts == 0)
            {
                isOnLadder = false;
                isClimbing = false;
                animacionEscalera?.ActualizarEscalera(false, 0f);
                rb.gravityScale = originalGravityScale;
                bodyAnim.SetBool("Groundded", canJump);
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
                ladderCollider.size = spriteRenderer.size;
            }

            ladderCollider.isTrigger = true;
        }
    }

    private bool IsLadder(GameObject sceneObject)
    {
        return sceneObject.name.ToLower().Contains("escalera");
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
