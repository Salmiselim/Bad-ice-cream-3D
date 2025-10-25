using UnityEngine;

public class PlayerControlller : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 inputDirection; // Store input for FixedUpdate
    public float spawnCooldown = 1f;
    private float lastSpawnTime;
    private GameObject currentBlock; // Track the block the player is colliding with

    [Header("Ground Check")]
    public Transform groundCheck; // Empty child object at feet, or use transform.position
    public float groundDistance = 0.4f;
    public LayerMask groundMask; // Set to your ground/wall layers in inspector

    [Header("Block Spawning")]
    public GameObject blockPrefab; // Prefab for the blocks to spawn
    public int blockCount = 5; // Number of blocks in the line
    public float blockSpacing = 1.5f; // Distance between blocks

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.freezeRotation = true; // Prevents tumbling
    }

    void Update()
    {
        // Capture input once per frame
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Rotate toward movement direction (smooth it if needed)
        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime); // Smooth rotation
        }

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Jump();
        }

        // Spawn block line input
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastSpawnTime + spawnCooldown)
        {
            SpawnBlockLine();
            lastSpawnTime = Time.time;
        }

        // Destroy block input
        if (Input.GetKeyDown(KeyCode.R) && currentBlock != null)
        {
            Destroy(currentBlock);
            currentBlock = null; // Clear reference after destruction
        }
    }

    void FixedUpdate()
    {
        // Ground check (raycast for reliability)
        if (!IsGrounded())
        {
            isGrounded = false;
        }

        // Physics-based movement: Use velocity for better collision handling
        Vector3 movement = inputDirection * speed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z); // Preserve Y for gravity/jump
    }

    bool IsGrounded()
    {
        // Raycast down from groundCheck point (or transform.position if no child)
        Vector3 checkPos = groundCheck ? groundCheck.position : transform.position;
        return Physics.CheckSphere(checkPos, groundDistance, groundMask); // More reliable than tags
    }

    void Jump()
    {
        // Use AddForce for physics impulse (upward, ignores mass)
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    void SpawnBlockLine()
    {
        if (blockPrefab == null)
        {
            Debug.LogWarning("Block prefab not assigned in PlayerController: " + gameObject.name);
            return;
        }

        // Get the player's forward direction (XZ plane, ignore Y)
        Vector3 forward = transform.forward;
        forward.y = 0f; // Ensure blocks stay on the ground
        forward = forward.normalized;

        // Spawn blocks in a line
        for (int i = 0; i < blockCount; i++)
        {
            // Calculate position: start at player's position, offset by spacing
            Vector3 spawnPosition = transform.position + forward * (blockSpacing * (i + 1));
            // Ensure blocks are at the same Y level as the player
            spawnPosition.y = transform.position.y;
            // Instantiate the block
            GameObject block = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
            block.tag = "block"; // Ensure the block has the "block" tag
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check for ground collision
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = true;
        }

        // Check for block collision
        if (collision.gameObject.CompareTag("block"))
        {
            currentBlock = collision.gameObject; // Store the block being collided with
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Check for ground exit
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = false; // Reset on exit
        }

        // Check for block exit
        if (collision.gameObject.CompareTag("block") && currentBlock == collision.gameObject)
        {
            currentBlock = null; // Clear block reference when leaving
        }
    }

    // Visualize ground check in Scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}