using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public Vector2Int enemySpawnGridPos = new Vector2Int(13, 13);

    [Header("References")]
    public GridManager gridManager;

    private bool hasSpawned = false; // Prevent double spawn

    private void Start()
    {
        if (hasSpawned) return;
        StartCoroutine(WaitAndSpawn());
    }

    private IEnumerator WaitAndSpawn()
    {
        Debug.Log("[EnemySpawner] Starting wait coroutine");

        // Wait for NetworkManager to exist
        while (NetworkManager.Singleton == null)
        {
            Debug.Log("[EnemySpawner] Waiting for NetworkManager.Singleton...");
            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log("[EnemySpawner] NetworkManager found: " + NetworkManager.Singleton.gameObject.name);

        // Wait for server mode
        while (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[EnemySpawner] Waiting for IsServer = true (current: " + NetworkManager.Singleton.IsServer + ")");
            yield return new WaitForSeconds(0.2f);
        }

        if (hasSpawned)
            yield break;

        hasSpawned = true;
        Debug.Log("[EnemySpawner] HOST CONFIRMED — spawning enemy");

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("[EnemySpawner] NO GRIDMANAGER IN SCENE!");
                yield break;
            }
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab not assigned in Inspector!");
            yield break;
        }

        Vector3 pos = gridManager.GridToWorldPosition(enemySpawnGridPos.x, enemySpawnGridPos.y);
        pos.y = 0.3f;

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        enemy.name = "Enemy";

        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab missing NetworkObject component!");
            Destroy(enemy);
            yield break;
        }

        netObj.Spawn();
        Debug.Log("[EnemySpawner] Enemy spawned and replicated!");

        GridEnemy script = enemy.GetComponent<GridEnemy>();
        if (script != null)
            script.gridManager = gridManager;
    }
}