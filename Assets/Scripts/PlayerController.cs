using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI scoreText;
    [SerializeField]
    private GameObject winPanel;

    [SerializeField]
    private float speed = 5f;
    [SerializeField]
    private float jumpForce = 15f;
    [SerializeField]
    private float fallMultiplier = 5f;
    [SerializeField]
    private float lowJumpMultiplier = 4f;
    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private float rayLength = 0.1f;

    private const float jumpBufferTime = 0.2f; // Time window to buffer jump input

    // References
    private new Rigidbody2D rigidbody;
    private BoxCollider2D boxCollider;
    private Animator animator;

    // Fields
    private Vector2 direction;
    private bool isGrounded;
    private float jumpBufferCounter;

    private void Awake()
    {
        winPanel.SetActive(false);
        rigidbody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        direction = Vector2.zero;
        isGrounded = true;
        jumpBufferCounter = 0;
    }

    private void Update()
    {
        HandleMovements();

        UpdateJumpBuffer();
    }

    private void FixedUpdate()
    {
        isGrounded = IsGrounded();

        // If the player is grounded and there is a buffered jump input, 
        // the jump is executed even if the button was pressed slightly before landing.
        if (isGrounded && jumpBufferCounter > 0)
        {
            Jump();
        }

        AdjustGravityScale();

        animator.SetFloat("VerticalVelocity", rigidbody.linearVelocityY);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectable"))
        {
            Destroy(collision.gameObject);
            Debug.Log("Collected!");
            scoreText.text = (int.Parse(scoreText.text) + 1).ToString();
        }

        if (collision.CompareTag("Door"))
        {
            Debug.Log("Level complete!");
            winPanel.SetActive(true);
        }

        if (collision.CompareTag("Water"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void UpdateJumpBuffer()
    {
        // This ensures that if the player presses jump before landing, it will still register.
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleMovements()
    {
        Vector2 velocity = rigidbody.linearVelocity;

        if (!isGrounded && direction.x == 0)
        {
            // Gradually reduce horizontal velocity when falling and no horizontal input
            velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime);
        }
        else if (isGrounded || velocity.y < 0)
        {
            // Allow horizontal movement if grounded or falling
            velocity.x = direction.x * speed;
        }

        rigidbody.linearVelocity = velocity;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (winPanel.activeSelf)
        {
            direction = Vector2.zero;
            animator.SetBool("Running", false);
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Performed:
                direction = context.ReadValue<Vector2>();
                transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
                break;

            case InputActionPhase.Canceled:
                direction = Vector2.zero;
                break;
        }

        animator.SetBool("Running", Mathf.Abs(direction.x) > 0.1f);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (winPanel.activeSelf)
        {
            return;
        }

        if (context.performed)
        {
            // When the jump button is pressed, we buffer the input for a short time.
            // This allows the player to press jump just before landing and still jump as soon as they're grounded.
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private bool IsGrounded()
    {
        Vector2 rayOrigin = new (transform.position.x, boxCollider.bounds.min.y);
        return Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayer).collider != null;
    }

    private void Jump()
    {
        rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpBufferCounter = 0;
    }

    private void AdjustGravityScale()
    {
        if (isGrounded)
        {
            rigidbody.gravityScale = 1;
        }
        else
        {
            rigidbody.gravityScale = rigidbody.linearVelocityY > 0.1f ? lowJumpMultiplier : fallMultiplier;
        }
    }

    //private void OnDrawGizmos()
    //{
    //    if (boxCollider == null)
    //    {
    //        boxCollider = GetComponent<BoxCollider2D>();
    //    }

    //    // Visualize the raycast in the Scene view for debugging
    //    Gizmos.color = Color.black;
    //    Vector2 rayOrigin = new (transform.position.x, boxCollider.bounds.min.y);
    //    Gizmos.DrawLine(rayOrigin, new (rayOrigin.x, rayOrigin.y - rayLength));
    //}
}
