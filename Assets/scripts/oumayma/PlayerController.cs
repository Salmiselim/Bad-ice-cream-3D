using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float tileSize = 1f;

    [Header("Ice Mechanics")]
    public int maxIceLineLength = 5; // How far ice can shoot
    public float iceCreationDelay = 0.1f; // Delay between each ice block
    public GameObject iceBlockPrefab; // Visual prefab for instant ice creation

    [Header("References")]
    public GridManager gridManager;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Vector2Int currentGridPos;
    private Vector2Int facingDirection = Vector2Int.up; // Direction player is facing

    [Header("Spawn Settings")]
    public Vector2Int spawnGridPosition = new Vector2Int(1, 1);

    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
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
        HandleInput();
        MoveToTarget();
        HandleIceAction();
    }

    void HandleInput()
    {
        if (isMoving) return;

        Vector2Int moveDirection = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            moveDirection = new Vector2Int(0, 1); // Forward in grid (Z+)
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            moveDirection = new Vector2Int(0, -1); // Back in grid (Z-)
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveDirection = new Vector2Int(-1, 0); // Left (X-)
            facingDirection = moveDirection;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveDirection = new Vector2Int(1, 0); // Right (X+)
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
            // Check what's in front of player
            Vector2Int checkPos = currentGridPos + facingDirection;

            if (!gridManager.IsValidPosition(checkPos.x, checkPos.y))
                return;

            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            if (tileType == GridManager.ICE_BLOCK)
            {
                // Destroy ice
                DestroyIceLine();
            }
            else if (tileType == GridManager.EMPTY)
            {
                // Create ice
                CreateIceLine();
            }
        }
    }

    void CreateIceLine()
    {
        StartCoroutine(CreateIceLineCoroutine());
    }

    System.Collections.IEnumerator CreateIceLineCoroutine()
    {
        Vector2Int checkPos = currentGridPos + facingDirection;
        int blocksCreated = 0;

        while (blocksCreated < maxIceLineLength)
        {
            // Check if position is valid
            if (!gridManager.IsValidPosition(checkPos.x, checkPos.y))
            {
                Debug.Log("Ice creation stopped: Hit boundary");
                break;
            }

            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            // Stop if we hit a wall or existing ice block
            if (tileType == GridManager.WALL || tileType == GridManager.ICE_BLOCK)
            {
                Debug.Log($"Ice creation stopped: Hit obstacle at {checkPos}");
                break;
            }

            // Create ice block
            if (tileType == GridManager.EMPTY)
            {
                gridManager.CreateIceBlock(checkPos.x, checkPos.y);
                Debug.Log($"Created ice block at {checkPos}");
                blocksCreated++;

                // Wait before creating next block
                yield return new WaitForSeconds(iceCreationDelay);

                // Move to next position
                checkPos += facingDirection;
            }
            else
            {
                // Hit something else (future: enemy, fruit)
                Debug.Log($"Ice creation stopped: Hit object at {checkPos}");
                break;
            }
        }

        if (blocksCreated >= maxIceLineLength)
        {
            Debug.Log("Ice creation stopped: Max length reached");
        }
    }

    void DestroyIceLine()
    {
        Vector2Int checkPos = currentGridPos + facingDirection;

        // Destroy ice blocks one by one in the direction player is facing
        while (gridManager.IsValidPosition(checkPos.x, checkPos.y))
        {
            int tileType = gridManager.gridData[checkPos.x, checkPos.y];

            if (tileType == GridManager.ICE_BLOCK)
            {
                // Destroy this ice block
                gridManager.DestroyIceBlock(checkPos.x, checkPos.y);
                Debug.Log($"Destroyed ice block at {checkPos}");

                // Move to next position
                checkPos += facingDirection;
            }
            else
            {
                // Stop when we hit non-ice
                break;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collision detected with: {other.gameObject.name}, Tag: {other.tag}");
        if (other.CompareTag("Fruit"))
        {
            Vector2Int fruitGridPos = gridManager.WorldToGridPosition(other.transform.position);
            Debug.Log($"Attempting to collect fruit at grid position: {fruitGridPos}");
            gridManager.CollectFruit(fruitGridPos.x, fruitGridPos.y);
            FruitCollection fruitScript = other.GetComponent<FruitCollection>();
            if (fruitScript != null)
            {
                GameManager.Instance.AddScore(fruitScript.points);
            }
            GameManager.Instance.CollectFruit();
        }
    }
}