using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GridEnemy : NetworkBehaviour
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

    public override void OnNetworkSpawn()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        // Find player - prefer the one with authority (host or client player)
        // We'll just find any object with "Player" tag - works for 2-player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        StartCoroutine(InitializeEnemy());
    }

    IEnumerator InitializeEnemy()
    {
        yield return new WaitForEndOfFrame();

        currentGridPos = gridManager.WorldToGridPosition(transform.position);
        Vector3 worldPos = gridManager.GridToWorldPosition(currentGridPos.x, currentGridPos.y);
        worldPos.y = transform.position.y;
        transform.position = worldPos;
        targetPosition = worldPos;
    }

    void Update()
    {
        // Only the server runs AI logic
        if (!IsServer)
        {
            // Clients only follow the synced position (NetworkTransform handles this)
            return;
        }

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
            Vector2Int horizontal = new Vector2Int(deltaX > 0 ? 1 : -1, 0);
            if (CanMoveTo(currentGridPos + horizontal)) return horizontal;

            if (deltaZ != 0)
            {
                Vector2Int vertical = new Vector2Int(0, deltaZ > 0 ? 1 : -1);
                if (CanMoveTo(currentGridPos + vertical)) return vertical;
            }
        }
        else if (Mathf.Abs(deltaZ) > 0)
        {
            Vector2Int vertical = new Vector2Int(0, deltaZ > 0 ? 1 : -1);
            if (CanMoveTo(currentGridPos + vertical)) return vertical;

            if (deltaX != 0)
            {
                Vector2Int horizontal = new Vector2Int(deltaX > 0 ? 1 : -1, 0);
                if (CanMoveTo(currentGridPos + horizontal)) return horizontal;
            }
        }

        return GetRandomValidDirection();
    }

    Vector2Int GetRandomValidDirection()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = dirs[i];
            dirs[i] = dirs[j];
            dirs[j] = temp;
        }

        foreach (var dir in dirs)
        {
            if (CanMoveTo(currentGridPos + dir))
                return dir;
        }
        return Vector2Int.zero;
    }

    bool CanMoveTo(Vector2Int pos)
    {
        if (!gridManager.IsValidPosition(pos.x, pos.y)) return false;
        return gridManager.IsTileWalkable(pos.x, pos.y);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int next = currentGridPos + direction;
        if (!CanMoveTo(next)) return;

        currentGridPos = next;
        targetPosition = gridManager.GridToWorldPosition(currentGridPos.x, currentGridPos.y);
        targetPosition.y = transform.position.y;
        isMoving = true;

        if (direction != Vector2Int.zero)
        {
            Vector3 lookDir = new Vector3(direction.x, 0, direction.y);
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only server detects collision

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