using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 15;
    public int gridHeight = 15;
    public float tileSize = 1f;

    [Header("Prefabs")]
    public GameObject iceBlockPrefab;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject fruitPrefab; // NEW: Drag your Fruit prefab here

    [Header("Grid Data")]
    public int[,] gridData;

    // Dictionary to track spawned ice blocks
    private Dictionary<Vector2Int, GameObject> iceBlockObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> fruitObjects = new Dictionary<Vector2Int, GameObject>();

    // Grid codes
    public const int EMPTY = 0;
    public const int ICE_BLOCK = 1;
    public const int WALL = 2;
    public const int FRUIT = 3;

    private Vector3 gridOrigin;

    void Awake()
    {
        gridOrigin = new Vector3(-gridWidth / 2f, 0, -gridHeight / 2f);
        InitializeGrid();
    }

    void Start()
    {
        GenerateLevel();
    }

    void InitializeGrid()
    {
        gridData = new int[gridWidth, gridHeight];

        // Fill everything with EMPTY first
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                gridData[x, z] = EMPTY;
            }
        }

        // Create borders only
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

        // Add just a few ice blocks for testing
        gridData[5, 5] = ICE_BLOCK;
        gridData[6, 5] = ICE_BLOCK;
        gridData[5, 6] = ICE_BLOCK;
        gridData[6, 6] = ICE_BLOCK;

        gridData[9, 9] = ICE_BLOCK;
        gridData[10, 9] = ICE_BLOCK;
        gridData[9, 10] = ICE_BLOCK;
        gridData[10, 10] = ICE_BLOCK;

        // Add fruits
        gridData[3, 3] = FRUIT;
        gridData[4, 4] = FRUIT;
        gridData[8, 8] = FRUIT;
    }

    void GenerateLevel()
    {
        // Clear existing fruits
        foreach (var kvp in fruitObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        fruitObjects.Clear();


        // Clear any existing ice block objects
        foreach (var kvp in iceBlockObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        iceBlockObjects.Clear();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 spawnPosition = GridToWorldPosition(x, z);

                switch (gridData[x, z])
                {
                    case ICE_BLOCK:
                        if (iceBlockPrefab != null)
                        {
                            GameObject ice = Instantiate(iceBlockPrefab, spawnPosition, Quaternion.identity, transform);
                            iceBlockObjects[new Vector2Int(x, z)] = ice;
                        }
                        break;
                    case WALL:
                        if (wallPrefab != null)
                            Instantiate(wallPrefab, spawnPosition, Quaternion.identity, transform);
                        break;
                    case FRUIT:
                        if (fruitPrefab != null)
                        {
                            GameObject fruit = Instantiate(fruitPrefab, spawnPosition, Quaternion.identity, transform);
                            fruitObjects[new Vector2Int(x, z)] = fruit;
                        }
                        break;
                }
            }
        }

        int fruitCount = 0;
        foreach (var kvp in fruitObjects)
        {
            fruitCount++;
        }
        GameManager.Instance.totalFruits = fruitCount;
    }


    public void CollectFruit(int x, int z)
    {
        if (IsValidPosition(x, z) && gridData[x, z] == FRUIT)
        {
            gridData[x, z] = EMPTY;

            Vector2Int key = new Vector2Int(x, z);
            if (fruitObjects.ContainsKey(key))
            {
                if (fruitObjects[key] != null)
                {
                    // Trigger collection animation
                    FruitCollection collection = fruitObjects[key].GetComponent<FruitCollection>();
                    if (collection != null)
                    {
                        collection.Collect();
                    }
                    else
                    {
                        Destroy(fruitObjects[key]);
                    }
                }
                fruitObjects.Remove(key);
            }
        }
    }
    public Vector3 GridToWorldPosition(int x, int z)
    {
        return gridOrigin + new Vector3(x * tileSize, 0, z * tileSize);
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
        return gridData[x, z] != WALL && gridData[x, z] != ICE_BLOCK;
    }

    public void DestroyIceBlock(int x, int z)
    {
        if (IsValidPosition(x, z) && gridData[x, z] == ICE_BLOCK)
        {
            gridData[x, z] = EMPTY;

            Vector2Int key = new Vector2Int(x, z);
            if (iceBlockObjects.ContainsKey(key))
            {
                if (iceBlockObjects[key] != null)
                {
                    // Check if it has the destruction script
                    IceBlockDestruction destruction = iceBlockObjects[key].GetComponent<IceBlockDestruction>();
                    if (destruction != null)
                    {
                        destruction.Shatter();
                    }
                    else
                    {
                        Destroy(iceBlockObjects[key]);
                    }
                }
                iceBlockObjects.Remove(key);
            }
        }
    }

    public void CreateIceBlock(int x, int z)
    {
        if (IsValidPosition(x, z) && gridData[x, z] == EMPTY)
        {
            gridData[x, z] = ICE_BLOCK;

            // Create the visual ice block
            Vector3 spawnPos = GridToWorldPosition(x, z);
            GameObject ice = Instantiate(iceBlockPrefab, spawnPos, Quaternion.identity, transform);

            Vector2Int key = new Vector2Int(x, z);
            iceBlockObjects[key] = ice;
        }
    }

    // Optional: Debug visualization
    void OnDrawGizmos()
    {
        if (gridData == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = GridToWorldPosition(x, z);

                switch (gridData[x, z])
                {
                    case EMPTY:
                        Gizmos.color = new Color(0, 1, 0, 0.2f);
                        break;
                    case ICE_BLOCK:
                        Gizmos.color = new Color(0, 0, 1, 0.5f);
                        break;
                    case WALL:
                        Gizmos.color = new Color(0.5f, 0.3f, 0, 0.8f);
                        break;
                }

                Gizmos.DrawCube(pos, Vector3.one * 0.9f);
            }
        }
    }

   
}