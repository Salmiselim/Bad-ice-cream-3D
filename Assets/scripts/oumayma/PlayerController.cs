using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float tileSize = 1f;

    [Header("Ice Mechanics")]
    public int maxIceLineLength = 5;
    public float iceCreationDelay = 0.1f;
    public GameObject iceBlockPrefab;

    [Header("References")]
    public GridManager gridManager;
    public Animator animator; // ← ADD THIS LINE


    public Vector3 targetPosition;
    private bool isMoving = false;
    public Vector2Int currentGridPos;
    private Vector2Int facingDirection = Vector2Int.up;

    [Header("Spawn Settings")]
    public Vector2Int spawnGridPosition = new Vector2Int(1, 1);


    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>(); // FIXED: Updated deprecated method
        }
        // ADD THESE LINES
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        StartCoroutine(InitializePlayer());
    }

    System.Collections.IEnumerator InitializePlayer()
    {
        yield return new WaitForEndOfFrame();

        currentGridPos = spawnGridPosition;
        Vector3 worldPos = gridManager.GridToWorldPosition(currentGridPos.x, currentGridPos.y);
        worldPos.y = transform.position.y;
        transform.position = worldPos;
        targetPosition = worldPos;

        Debug.Log($"Player spawned at grid position: {currentGridPos}");
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!GameManager.Instance.IsGameActive) return;
        HandleInput();
        MoveToTarget();
        HandleIceAction();
        UpdateAnimation(); // ← ADD THIS LINE

    }


    void UpdateAnimation()
    {
        if (animator != null)
        {
            // Set isWalking to true if player is moving
            animator.SetBool("isWalking", isMoving);
        }
    }
    void HandleInput()
    {
        if (!IsOwner) return;
        if (isMoving) return;

        Vector2Int moveDirection = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            moveDirection = new Vector2Int(0, 1);
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            moveDirection = new Vector2Int(0, -1);
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveDirection = new Vector2Int(-1, 0);
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveDirection = new Vector2Int(1, 0);
            facingDirection = moveDirection;
        }

        if (moveDirection != Vector2Int.zero)
        {
            TryMove(moveDirection);
        }
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int nextGridPos = currentGridPos + direction;

        if (gridManager.IsValidPosition(nextGridPos.x, nextGridPos.y))
        {
            if (gridManager.IsTileWalkable(nextGridPos.x, nextGridPos.y))
            {
                currentGridPos = nextGridPos;
                targetPosition = gridManager.GridToWorldPosition(currentGridPos.x, currentGridPos.y);
                targetPosition.y = transform.position.y;
                isMoving = true;

                if (direction != Vector2Int.zero)
                {
                    Vector3 lookDirection = new Vector3(direction.x, 0, direction.y);
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
    }

    void MoveToTarget()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    void HandleIceAction()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2Int checkPos = currentGridPos + facingDirection;

            if (!gridManager.IsValidPosition(checkPos.x, checkPos.y))
                return;

            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            if (tileType == GridManager.ICE_BLOCK)
            {
                StartCoroutine(DestroyIceLineCoroutine());
            }
            else if (tileType == GridManager.EMPTY)
            {
                StartCoroutine(CreateIceLineCoroutine());
            }
        }
    }

    System.Collections.IEnumerator CreateIceLineCoroutine()
    {
        Vector2Int checkPos = currentGridPos + facingDirection;
        int blocksCreated = 0;

        while (blocksCreated < maxIceLineLength)
        {
            if (!gridManager.IsValidPosition(checkPos.x, checkPos.y))
            {
                Debug.Log("Ice creation stopped: Hit boundary");
                break;
            }

            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            if (tileType == GridManager.WALL || tileType == GridManager.ICE_BLOCK)
            {
                Debug.Log($"Ice creation stopped: Hit obstacle at {checkPos}");
                break;
            }

            if (tileType == GridManager.EMPTY)
            {
                RequestCreateIceBlockServerRpc(checkPos.x, checkPos.y);
                Debug.Log($"Created ice block at {checkPos}");
                blocksCreated++;

                yield return new WaitForSeconds(iceCreationDelay);

                checkPos += facingDirection;
            }
            else
            {
                Debug.Log($"Ice creation stopped: Hit object at {checkPos}");
                break;
            }
        }

        if (blocksCreated >= maxIceLineLength)
        {
            Debug.Log("Ice creation stopped: Max length reached");
        }
    }

    System.Collections.IEnumerator DestroyIceLineCoroutine()
    {
        Vector2Int checkPos = currentGridPos + facingDirection;
        int blocksDestroyed = 0;

        while (gridManager.IsValidPosition(checkPos.x, checkPos.y))
        {
            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            if (tileType == GridManager.ICE_BLOCK)
            {
                RequestDestroyIceBlockServerRpc(checkPos.x, checkPos.y);
                Debug.Log($"Destroyed ice block at {checkPos}");
                blocksDestroyed++;

                yield return new WaitForSeconds(iceCreationDelay);

                checkPos += facingDirection;
            }
            else
            {
                Debug.Log($"Ice destruction stopped: Hit non-ice at {checkPos}");
                break;
            }
        }

        if (blocksDestroyed > 0)
        {
            Debug.Log($"Destroyed {blocksDestroyed} ice blocks total");
        }
    }
    [ServerRpc]
    private void RequestCreateIceBlockServerRpc(int x, int y)
    {
        if (GridManager.Instance != null)
            GridManager.Instance.CreateIceBlockNetworked(x, y);
    }

    [ServerRpc]
    private void RequestDestroyIceBlockServerRpc(int x, int y)
    {
        if (GridManager.Instance != null)
            GridManager.Instance.DestroyIceBlockNetworked(x, y);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (other.CompareTag("Fruit"))
        {
            Debug.Log($"Collision with: {other.gameObject.name}");

            Vector2Int fruitGridPos = gridManager.WorldToGridPosition(other.transform.position);
            Debug.Log($"Fruit grid position: {fruitGridPos}");

            // Check if valid position
            if (!gridManager.IsValidPosition(fruitGridPos.x, fruitGridPos.y))
            {
                Debug.LogWarning("Invalid grid position!");
                return;
            }

            // Check if tile is actually a fruit
            int tileType = gridManager.gridData[fruitGridPos.x, fruitGridPos.y];

            if (tileType != GridManager.FRUIT)
            {
                Debug.LogWarning($"Tile at {fruitGridPos} is not a fruit! Type: {tileType}");
                return;
            }

            Debug.Log($"Valid fruit at {fruitGridPos}, collecting...");

            // Get points
            FruitCollection fruitScript = other.GetComponent<FruitCollection>();
            int points = 10;
            if (fruitScript != null)
            {
                points = fruitScript.points;
            }

            // Update game state
            GameManager.Instance.AddScore(points);
            GameManager.Instance.CollectFruit(fruitGridPos.x, fruitGridPos.y); // FIXED: Added x, z parameters

            // Trigger collection animation
            gridManager.CollectFruit(fruitGridPos.x, fruitGridPos.y);

            Debug.Log($"Fruit collection complete. Score: {GameManager.Instance.currentScore}, Collected: {GameManager.Instance.fruitsCollected}/{GameManager.Instance.totalFruits}");
        }
    }
}