using UnityEngine;
using System.Collections;

public class GridEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float tileSize = 1f;
    public float thinkDelay = 0.5f;

    [Header("References")]
    public GridManager gridManager;
    private Transform player;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Vector2Int currentGridPos;
    private float nextThinkTime = 0f;

    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");
        }

        StartCoroutine(InitializeEnemy());
    }

    System.Collections.IEnumerator InitializeEnemy()
    {
        yield return new WaitForEndOfFrame();

        currentGridPos = gridManager.WorldToGridPosition(transform.position);
        Vector3 worldPos = gridManager.GridToWorldPosition(currentGridPos.x, currentGridPos.y);
        worldPos.y = transform.position.y;
        transform.position = worldPos;
        targetPosition = worldPos;

        Debug.Log($"Enemy initialized at grid position: {currentGridPos}");
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (isMoving)
        {
            MoveToTarget();
        }

        if (!isMoving && Time.time >= nextThinkTime && player != null)
        {
            DecideNextMove();
            nextThinkTime = Time.time + thinkDelay;
        }
    }

    void DecideNextMove()
    {
        Vector2Int direction = GetDirectionToPlayer();

        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
        }
    }

    Vector2Int GetDirectionToPlayer()
    {
        if (player == null) return Vector2Int.zero;

        Vector2Int playerGridPos = gridManager.WorldToGridPosition(player.position);

        int deltaX = playerGridPos.x - currentGridPos.x;
        int deltaZ = playerGridPos.y - currentGridPos.y;

        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaZ))
        {
            Vector2Int horizontalMove = new Vector2Int(deltaX > 0 ? 1 : -1, 0);

            if (CanMoveTo(currentGridPos + horizontalMove))
            {
                return horizontalMove;
            }
            else if (deltaZ != 0)
            {
                Vector2Int verticalMove = new Vector2Int(0, deltaZ > 0 ? 1 : -1);
                if (CanMoveTo(currentGridPos + verticalMove))
                {
                    return verticalMove;
                }
            }
        }
        else if (Mathf.Abs(deltaZ) > 0)
        {
            Vector2Int verticalMove = new Vector2Int(0, deltaZ > 0 ? 1 : -1);

            if (CanMoveTo(currentGridPos + verticalMove))
            {
                return verticalMove;
            }
            else if (deltaX != 0)
            {
                Vector2Int horizontalMove = new Vector2Int(deltaX > 0 ? 1 : -1, 0);
                if (CanMoveTo(currentGridPos + horizontalMove))
                {
                    return horizontalMove;
                }
            }
        }

        return GetRandomValidDirection();
    }

    Vector2Int GetRandomValidDirection()
    {
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);
            Vector2Int temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            if (CanMoveTo(currentGridPos + dir))
            {
                return dir;
            }
        }

        return Vector2Int.zero;
    }

    bool CanMoveTo(Vector2Int gridPos)
    {
        if (!gridManager.IsValidPosition(gridPos.x, gridPos.y))
        {
            return false;
        }

        return gridManager.IsTileWalkable(gridPos.x, gridPos.y);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int nextGridPos = currentGridPos + direction;

        if (CanMoveTo(nextGridPos))
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

    void MoveToTarget()
    {
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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Enemy caught the player!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
        }
    }
}