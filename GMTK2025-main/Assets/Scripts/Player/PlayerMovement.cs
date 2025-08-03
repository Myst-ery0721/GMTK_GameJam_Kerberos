using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float dashPower = 15f;
    public float attackSpeed = 1.0f;

    [Header("Dash Settings")]
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public bool canDashInAir = true;

    [Header("Double Jump Settings")]
    public bool hasDoubleJump = true;
    public float doubleJumpForce = 8f;

    private float attackCooldownTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool canDash = true;
    private bool canAttack = true;
    private bool isDashing = false;
    private int jumpCount = 0;
    private const int maxJumps = 2; // Ground jump + double jump

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask;

    [Header("Input System")]
    public InputActionAsset inputActionsAsset;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private Rigidbody2D rb;

    public GameObject hitBox;

    private Vector2 moveInput;
    private bool jumpInput;
    private SpriteRenderer characterSprite;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool facingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        characterSprite = GetComponent<SpriteRenderer>();

        // Only try to set up input actions if the asset is assigned
        if (inputActionsAsset != null)
        {
            var playerMap = inputActionsAsset.FindActionMap("Player");
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move");
                jumpAction = playerMap.FindAction("Jump");
                dashAction = playerMap.FindAction("Sprint");
                attackAction = playerMap.FindAction("Attack");

                Debug.Log("Input Actions successfully set up!");
            }
            else
            {
                Debug.LogWarning("Could not find 'Player' action map in Input Actions Asset! Using keyboard fallback.");
            }
        }
        else
        {
            Debug.LogWarning("Input Actions Asset is not assigned! Using keyboard fallback only.");
        }
    }

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJump;
        }

        if (dashAction != null)
        {
            dashAction.Enable();
            dashAction.performed += OnDash;
        }

        if (attackAction != null)
        {
            attackAction.Enable();
            attackAction.performed += OnAttack;
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction.Disable();
        }

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDash;
            dashAction.Disable();
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
            attackAction.Disable();
        }
    }

    void Update()
    {
        // Handle keyboard input as fallback if Input Actions aren't working
        HandleKeyboardInput();

        CheckGrounded();
        HandleMovement();
        HandleJump();
        HandleFlip();
        HandleAttackRotation();
        UpdateCooldowns();
    }

    void HandleKeyboardInput()
    {
        // Fallback keyboard input if Input Actions aren't set up
        if (moveAction == null)
        {
            // Movement using keyboard
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveInput.x = -1f;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveInput.x = 1f;
            }
            else
            {
                moveInput.x = 0f;
            }
        }

        // Jump input
        if (jumpAction == null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
        {
            jumpInput = true;
        }

        // Attack input
        if (attackAction == null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleAttack();
        }

        // Dash input - E key or Left Shift
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            TryDash();
        }
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Attack");
            HandleAttack();
        }
    }

    void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryDash();
        }
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Jump");
            jumpInput = true;
        }
    }

    void HandleAttackRotation()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePosition.z = 0f;

        Vector2 direction = mousePosition - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        hitBox.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void HandleAttack()
    {
        if (canAttack)
        {
            Debug.Log("Attacking");
            hitBox.SetActive(true);
            canAttack = false;
            Invoke(nameof(DisableHitBox), 0.5f);
            attackCooldownTimer = attackSpeed;
        }
    }

    void DisableHitBox()
    {
        hitBox.SetActive(false);
    }

    void TryDash()
    {
        if (canDash && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
        else if (!canDash)
        {
            Debug.Log($"Dash on cooldown! {dashCooldownTimer:F1}s remaining");
        }
    }

    private System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;
        dashCooldownTimer = dashCooldown;

        // Determine dash direction
        Vector2 dashDirection = Vector2.right;
        if (moveInput.x != 0)
        {
            dashDirection = new Vector2(moveInput.x, 0).normalized;
        }
        else
        {
            dashDirection = facingRight ? Vector2.right : Vector2.left;
        }

        // Store original gravity
        float originalGravity = rb.gravityScale;

        // Disable gravity during dash for consistent movement
        rb.gravityScale = 0f;

        // Apply dash force
        rb.linearVelocity = dashDirection * dashPower;

        Debug.Log($"Dashing {dashDirection} for {dashDuration} seconds!");

        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);

        // Restore gravity
        rb.gravityScale = originalGravity;
        isDashing = false;

        Debug.Log("Dash completed!");
    }

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // Reset jump count when landing
        if (!wasGrounded && isGrounded)
        {
            jumpCount = 0;
        }
    }

    void HandleMovement()
    {
        // Don't override movement during dash
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    void HandleJump()
    {
        if (jumpInput)
        {
            // Ground jump
            if (isGrounded && jumpCount == 0)
            {
                Debug.Log("Ground Jump!");
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y velocity
                rb.AddForceY(jumpForce, ForceMode2D.Impulse);
                jumpCount = 1;
            }
            // Double jump
            else if (hasDoubleJump && jumpCount == 1 && jumpCount < maxJumps)
            {
                Debug.Log("Double Jump!");
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y velocity
                rb.AddForceY(doubleJumpForce, ForceMode2D.Impulse);
                jumpCount = 2;
            }
        }
        jumpInput = false;
    }

    void HandleFlip()
    {
        if (moveInput.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        characterSprite.flipX = !facingRight;
    }

    void UpdateCooldowns()
    {
        // Attack cooldown
        if (!canAttack)
        {
            attackCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer <= 0f)
            {
                canAttack = true;
                Debug.Log("Can Attack Now!");
            }
        }

        // Dash cooldown
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
                Debug.Log("Dash ready!");
            }
        }
    }

    // Public methods for buff system to modify stats
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void SetJumpForce(float newJumpForce)
    {
        jumpForce = newJumpForce;
    }

    public void SetDoubleJumpForce(float newDoubleJumpForce)
    {
        doubleJumpForce = newDoubleJumpForce;
    }

    public void SetDashPower(float newDashPower)
    {
        dashPower = newDashPower;
    }

    public void SetDashCooldown(float newCooldown)
    {
        dashCooldown = newCooldown;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // Method to reset dash state - called by death system
    public void ResetDashState()
    {
        StopAllCoroutines(); // Stop any running dash coroutines
        isDashing = false;
        canDash = true;
        dashCooldownTimer = 0f;

        // Reset rigidbody gravity if it was modified during dash
        if (rb != null)
        {
            rb.gravityScale = 1f; // Reset to normal gravity
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Dash state reset!");
    }
}