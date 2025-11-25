using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance;


    [Header("Grid Data")]
    public int[,] gridData;



    // Track spawned objects so we can remove them later
    public Dictionary<Vector2Int, GameObject> iceBlockObjects = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, GameObject> fruitObjects = new Dictionary<Vector2Int, GameObject>();

    public const int EMPTY = 0;
    public const int ICE_BLOCK = 1;
    public const int WALL = 2;
    public const int FRUIT = 3;



    void Awake()
    {
        // singleton simple pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gridOrigin = new Vector3(-gridWidth / 2f, 0f, -gridHeight / 2f);
        InitializeGrid();
    }

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "level2")
            return;
        GenerateLevel();

        // Start phase 1 timer
        phase1Timer = 0f;
        isPhase1TimerRunning = true;
    }

    void Update()
    {
   
        if (isPhase1TimerRunning)
            phase1Timer += Time.deltaTime;
    }


    #region Grid

    [Header("Grid Settings")]
    public int gridWidth = 15;
    public int gridHeight = 15;
    public float tileSize = 1f;
    void InitializeGrid()
    {
        gridData = new int[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
            for (int z = 0; z < gridHeight; z++)
                gridData[x, z] = EMPTY;

        // border walls
        for (int x = 0; x < gridWidth; x++)
        {
            gridData[x, 0] = WALL;
            gridData[x, gridHeight - 1] = WALL;
        }

        for (int z = 0; z < gridHeight; z++)
        {
            gridData[0, z] = WALL;
            gridData[gridWidth - 1, z] = WALL;
        }

        // sample ice blocks and initial fruits (optional)
        if (IsValidPosition(5, 5)) gridData[5, 5] = ICE_BLOCK;
        if (IsValidPosition(6, 5)) gridData[6, 5] = ICE_BLOCK;

        if (IsValidPosition(3, 3)) gridData[3, 3] = FRUIT;
        if (IsValidPosition(11, 11)) gridData[11, 11] = FRUIT;
        if (IsValidPosition(3, 11)) gridData[3, 11] = FRUIT;
    }

    // Instantiate objects from gridData and populate the dictionaries

    #endregion

    // Phase & timer
    private bool phase2Started = false;
    public float phase1Timer { get; private set; } = 0f;
    private bool isPhase1TimerRunning = false;
    public void GenerateLevel()
    {
        // clear previously spawned fruit objects & clear dict
        foreach (var kvp in fruitObjects)
            if (kvp.Value != null) Destroy(kvp.Value);
        fruitObjects.Clear();

        // destroy ice blocks
        foreach (var kvp in iceBlockObjects)
            if (kvp.Value != null) Destroy(kvp.Value);
        iceBlockObjects.Clear();

        // instantiate items from gridData
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 spawnPos = GridToWorldPosition(x, z);
                switch (gridData[x, z])
                {
                   
                    case WALL:
                        if (wallPrefab != null)
                            Instantiate(wallPrefab, spawnPos, Quaternion.identity, transform);
                        break;
                    case FRUIT:
                        // default to phase1 prefab if available
                        GameObject prefabToUse = !phase2Started && fruitPrefabPhase1 != null ? fruitPrefabPhase1 : fruitPrefabPhase2;
                        if (prefabToUse != null)
                        {
                            GameObject fruit = Instantiate(prefabToUse, spawnPos, Quaternion.identity, transform);
                            fruitObjects[new Vector2Int(x, z)] = fruit;
                        }
                        break;
                }
            }
        }

        // update GameManager if present
        if (GameManager.Instance != null)
            GameManager.Instance.totalFruits = fruitObjects.Count;
    }

    #region fruit

    [Tooltip("Fruit prefab used in phase 1")]
    public GameObject fruitPrefabPhase1;

    [Tooltip("Fruit prefab used in phase 2")]
    public GameObject fruitPrefabPhase2;
    // Add an existing instantiated fruit GameObject into the grid tracking
    public void AddFruit(int x, int z, GameObject fruit)
    {
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != EMPTY) return;

        gridData[x, z] = FRUIT;
        fruitObjects[new Vector2Int(x, z)] = fruit;

        if (GameManager.Instance != null)
            GameManager.Instance.totalFruits = fruitObjects.Count;
    }

    // Convenience: create and register a fruit from prefab (usePhase2 forces phase2 prefab)
    public GameObject CreateFruit(int x, int z, bool usePhase2 = false)
    {
        if (!IsValidPosition(x, z)) return null;
        if (gridData[x, z] != EMPTY) return null;

        GameObject prefabToUse = (!phase2Started && !usePhase2) ? fruitPrefabPhase1 : fruitPrefabPhase2;
        if (prefabToUse == null)
            prefabToUse = usePhase2 ? fruitPrefabPhase2 : fruitPrefabPhase1;

        if (prefabToUse == null) return null;

        Vector3 spawnPos = GridToWorldPosition(x, z);
        GameObject fruit = Instantiate(prefabToUse, spawnPos, Quaternion.identity, transform);
        gridData[x, z] = FRUIT;
        fruitObjects[new Vector2Int(x, z)] = fruit;

        if (GameManager.Instance != null)
            GameManager.Instance.totalFruits = fruitObjects.Count;

        return fruit;
    }

    // Called when a fruit is collected by the player
    public void CollectFruit(int x, int z)
    {
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != FRUIT) return;

        gridData[x, z] = EMPTY;
        Vector2Int key = new Vector2Int(x, z);

        if (fruitObjects.ContainsKey(key))
        {
            if (fruitObjects[key] != null) Destroy(fruitObjects[key]);
            fruitObjects.Remove(key);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.CollectFruit(x, z);

        // If phase 1 not yet started to end and fruits are now zero, trigger phase 2
        if (!phase2Started && fruitObjects.Count == 0)
        {
            // stop timer
            isPhase1TimerRunning = false;
            Debug.Log($"Phase 1 completed in {phase1Timer:F2} sec.");

            StartPhase2();
        }
    }


    #region fruit Phase 2

    // Starts phase 2: spawn fruits using phase2 prefab in chosen positions
    private void StartPhase2()
    {
        phase2Started = true;

        // Example: spawn phase2 fruits in some positions that are empty.
        // You can pick whatever positions suit your level. Here are example locations:
        List<Vector2Int> phase2Positions = new List<Vector2Int>()
        {
            new Vector2Int(2,2),
            new Vector2Int(10,4),
            new Vector2Int(12,10)
        };

        foreach (var p in phase2Positions)
        {
            if (!IsValidPosition(p.x, p.y)) continue;
            if (gridData[p.x, p.y] != EMPTY) continue;
            CreateFruit(p.x, p.y, usePhase2: true);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.totalFruits = fruitObjects.Count;
    }
    #endregion
    #endregion


    [Header("Prefabs")]
    public GameObject iceBlockPrefab;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    #region Ice Block

    // Primary destruction method: call to remove an ice block and play its destruction effect.
    public void DestroyIceBlock(int x, int z)
    {
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != ICE_BLOCK) return;

        Vector2Int key = new Vector2Int(x, z);

        // mark grid empty first (idempotent)
        gridData[x, z] = EMPTY;

        if (iceBlockObjects.ContainsKey(key))
        {
            GameObject obj = iceBlockObjects[key];
            // remove from dictionary immediately to keep consistent state
            iceBlockObjects.Remove(key);

            if (obj != null)
            {
                // prefer calling the block's own destruction method so effects spawn
                DestructibleBlock db = obj.GetComponent<DestructibleBlock>() ?? obj.GetComponentInParent<DestructibleBlock>();
                if (db != null)
                {
                    db.TriggerDestruction();
                }
                else
                {
                    Destroy(obj);
                }
            }
        }
    }

    // overload: world position
    public void DestroyIceBlock(Vector3 worldPos)
    {
        Vector2Int p = WorldToGridPosition(worldPos);
        DestroyIceBlock(p.x, p.y);
    }

    // Optional: safe notify helper used by block scripts if the block calls back directly
    public void NotifyIceBlockDestroyed(int x, int z)
    {
        if (!IsValidPosition(x, z)) return;

        // set empty (idempotent)
        gridData[x, z] = EMPTY;
        Vector2Int key = new Vector2Int(x, z);

        if (iceBlockObjects.ContainsKey(key))
            iceBlockObjects.Remove(key);
    }

    // Convenience world-pos version
    public void NotifyIceBlockDestroyedAtWorldPos(Vector3 worldPos)
    {
        Vector2Int p = WorldToGridPosition(worldPos);
        NotifyIceBlockDestroyed(p.x, p.y);
    }

    // Spawn an ice block (keeps both gridData and dictionary consistent)
    public void CreateIceBlock(int x, int z)
    {
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != EMPTY) return;

        gridData[x, z] = ICE_BLOCK;
        Vector3 spawnPos = GridToWorldPosition(x, z);
        if (iceBlockPrefab != null)
        {
            GameObject ice = Instantiate(iceBlockPrefab, spawnPos, Quaternion.identity, transform);
            iceBlockObjects[new Vector2Int(x, z)] = ice;
        }
    }
    public void CreateIceBlockNetworked(int x, int z)
    {
        if (!IsServer) return; // only the server spawns networked objects
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != EMPTY) return;

        gridData[x, z] = ICE_BLOCK;

        Vector3 spawnPos = GridToWorldPosition(x, z);
        if (iceBlockPrefab != null)
        {
            // Instantiate on server
            GameObject ice = Instantiate(iceBlockPrefab, spawnPos, Quaternion.identity, transform);

            // Make sure prefab has a NetworkObject!
            NetworkObject netObj = ice.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(); // this replicates it to all clients
            }

            iceBlockObjects[new Vector2Int(x, z)] = ice;
        }
    }

    public void DestroyIceBlockNetworked(int x, int z)
    {
        if (!IsServer) return;
        if (!IsValidPosition(x, z)) return;
        if (gridData[x, z] != ICE_BLOCK) return;

        gridData[x, z] = EMPTY;
        Vector2Int key = new Vector2Int(x, z);

        if (iceBlockObjects.TryGetValue(key, out GameObject obj))
        {
            iceBlockObjects.Remove(key);

            if (obj != null)
            {
                NetworkObject netObj = obj.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(); // remove it from all clients
                else
                    Destroy(obj);
            }
        }
    }
    #endregion


    #region Other 

    private Vector3 gridOrigin;
    public Vector3 GridToWorldPosition(int x, int z)
    {
        return gridOrigin + new Vector3(x * tileSize, 0f, z * tileSize);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / tileSize);
        int z = Mathf.RoundToInt((worldPos.z - gridOrigin.z) / tileSize);
        return new Vector2Int(x, z);
    }

    public bool IsValidPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    public bool IsTileWalkable(int x, int z)
    {
        if (!IsValidPosition(x, z)) return false;
        // walkable means not wall and not ice block
        return gridData[x, z] != WALL && gridData[x, z] != ICE_BLOCK;
    }

    void OnDrawGizmos()
    {
        if (gridData == null) return;
        for (int x = 0; x < gridWidth; x++)
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = GridToWorldPosition(x, z);
                switch (gridData[x, z])
                {
                    case EMPTY: Gizmos.color = new Color(0, 1, 0, 0.15f); break;
                    case ICE_BLOCK: Gizmos.color = new Color(0, 0, 1, 0.4f); break;
                    case WALL: Gizmos.color = new Color(0.5f, 0.3f, 0, 0.8f); break;
                    case FRUIT: Gizmos.color = new Color(1, 0.5f, 0, 0.6f); break;
                }
                Gizmos.DrawCube(pos, Vector3.one * 0.9f);
            }
    }
    #endregion

}