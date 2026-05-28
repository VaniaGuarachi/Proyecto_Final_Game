using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class CriadorSlimesController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Deteccion de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.45f, 0.12f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Acciones")]
    [SerializeField] private float actionAnimationTime = 0.25f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerInteraction playerInteraction;
    private float horizontalInput;
    private bool isGrounded;
    private bool isRunning;
    private bool isFacingRight = true;
    private bool wasGrounded;
    private float interactTimer;
    private float feedTimer;
    private float buildTimer;
    private float landTimer;
    private string currentActionState = "Interact";
    private string currentAnimationState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        horizontalInput = ReadHorizontalInput();
        isRunning = IsRunHeld() && Mathf.Abs(horizontalInput) > 0.01f;
        isGrounded = CheckGrounded();

        if (!wasGrounded && isGrounded)
        {
            landTimer = 0.18f;
        }

        if (WasKeyPressed(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        if (WasKeyPressed(KeyCode.E))
        {
            TriggerInteraction();
        }

        if (WasKeyPressed(KeyCode.F))
        {
            TriggerFeed();
        }

        if (WasKeyPressed(KeyCode.C))
        {
            TriggerBuild();
        }

        UpdateFacingDirection();
        TickActionTimers();
        UpdateAnimator();
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private float ReadHorizontalInput()
    {
        float input = 0f;

#if ENABLE_LEGACY_INPUT_MANAGER
        input = Input.GetAxisRaw("Horizontal");
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input += 1f;
            }
        }
#endif

        return Mathf.Clamp(input, -1f, 1f);
    }

    private bool WasKeyPressed(KeyCode keyCode)
    {
        bool pressed = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(keyCode);
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return pressed;
        }

        pressed = pressed || keyCode switch
        {
            KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
            KeyCode.E => keyboard.eKey.wasPressedThisFrame,
            KeyCode.F => keyboard.fKey.wasPressedThisFrame,
            KeyCode.C => keyboard.cKey.wasPressedThisFrame,
            _ => false
        };
#endif

        return pressed;
    }

    private bool IsRunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
        {
            return Physics2D.OverlapBox(transform.position + Vector3.down * 0.55f, groundCheckSize, 0f, groundLayer);
        }

        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void UpdateFacingDirection()
    {
        if (horizontalInput > 0f && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0f && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isFacingRight;
            return;
        }

        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void TriggerInteraction()
    {
        interactTimer = actionAnimationTime;
        currentActionState = playerInteraction != null && playerInteraction.HasNearbyMachine ? "UseMachine" : "Interact";
        playerInteraction?.Interact();
    }

    private void TriggerFeed()
    {
        feedTimer = actionAnimationTime;
        playerInteraction?.FeedSlime();
    }

    private void TriggerBuild()
    {
        buildTimer = actionAnimationTime;
        playerInteraction?.BuildOrPlace();
    }

    private void TickActionTimers()
    {
        interactTimer = Mathf.Max(0f, interactTimer - Time.deltaTime);
        feedTimer = Mathf.Max(0f, feedTimer - Time.deltaTime);
        buildTimer = Mathf.Max(0f, buildTimer - Time.deltaTime);
        landTimer = Mathf.Max(0f, landTimer - Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float verticalVelocity = rb.linearVelocity.y;
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) * (isRunning ? 2f : 1f));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", !isGrounded && verticalVelocity > 0.05f);
        animator.SetBool("IsFalling", !isGrounded && verticalVelocity < -0.05f);
        animator.SetBool("IsInteracting", interactTimer > 0f);
        animator.SetBool("IsFeeding", feedTimer > 0f);
        animator.SetBool("IsBuilding", buildTimer > 0f);

        string nextAnimationState = GetAnimationStateName();
        if (currentAnimationState == nextAnimationState)
        {
            return;
        }

        currentAnimationState = nextAnimationState;
        animator.CrossFade(nextAnimationState, 0.08f);
    }

    public void PlayHurtAnimation()
    {
        animator?.SetTrigger("Hurt");
        animator?.CrossFade("Hurt", 0.05f);
    }

    private string GetAnimationStateName()
    {
        if (feedTimer > 0f)
        {
            return "FeedSlime";
        }

        if (buildTimer > 0f)
        {
            return "Build";
        }

        if (interactTimer > 0f)
        {
            return currentActionState;
        }

        if (!isGrounded)
        {
            return rb.linearVelocity.y > 0.05f ? "Jump" : "Fall";
        }

        if (landTimer > 0f)
        {
            return "Land";
        }

        if (Mathf.Abs(horizontalInput) <= 0.01f)
        {
            return "Idle";
        }

        return isRunning ? "Run" : "Walk";
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.55f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(checkPosition, groundCheckSize);
    }
}
