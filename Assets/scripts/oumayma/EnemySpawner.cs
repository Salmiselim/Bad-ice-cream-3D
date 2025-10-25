using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public Vector2Int enemySpawnGridPos = new Vector2Int(13, 13);

    [Header("References")]
    public GridManager gridManager;

    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        Invoke("SpawnEnemy", 0.5f);
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned to EnemySpawner!");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return;
        }

        Vector3 spawnWorldPos = gridManager.GridToWorldPosition(
            enemySpawnGridPos.x,
            enemySpawnGridPos.y
        );
        spawnWorldPos.y = 0.3f;

        GameObject enemy = Instantiate(enemyPrefab, spawnWorldPos, Quaternion.identity);
        enemy.name = "Enemy";

        GridEnemy enemyScript = enemy.GetComponent<GridEnemy>();
        if (enemyScript != null)
        {
            enemyScript.gridManager = gridManager;
        }

        Debug.Log($"Enemy spawned at grid position {enemySpawnGridPos}");
    }
}