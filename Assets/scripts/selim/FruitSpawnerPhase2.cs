using UnityEngine;

public class FruitSpawnerPhase2 : MonoBehaviour
{
    private GridManager grid;

    [Header("Spawn Settings")]
    public int fruitsPerPhase1 = 3; // initial amount (you had 3 in your earlier grid)
    public int fruitsPerPhase2 = 5; // phase 2 goal (match GameManager.phase2FruitGoal)

    void Awake()
    {
        grid = FindObjectOfType<GridManager>();
        if (grid == null) Debug.LogError("FruitSpawnerPhase2: GridManager not found in scene.");
    }

    // call with phase = 1 or 2
    public void SpawnPhaseFruits(int phase)
    {
        if (grid == null) return;

        // CLEAR any existing fruit objects and grid FRUIT flags
        foreach (var kvp in grid.fruitObjects)
            if (kvp.Value != null) Destroy(kvp.Value);
        grid.fruitObjects.Clear();

        for (int x = 0; x < grid.gridWidth; x++)
            for (int z = 0; z < grid.gridHeight; z++)
                if (grid.gridData[x, z] == GridManager.FRUIT)
                    grid.gridData[x, z] = GridManager.EMPTY;

        // pick prefab and count
        GameObject prefabToSpawn;
        int spawnCount;
        if (phase == 1)
        {
            prefabToSpawn = grid.fruitPrefabPhase1;
            spawnCount = fruitsPerPhase1;
        }
        else
        {
            prefabToSpawn = grid.fruitPrefabPhase2;
            spawnCount = fruitsPerPhase2;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("FruitSpawnerPhase2: prefabToSpawn is null for phase " + phase);
            return;
        }

        // spawn on random empty tiles (tries limit)
        int spawned = 0;
        int attempts = 0;
        while (spawned < spawnCount && attempts < spawnCount * 30)
        {
            attempts++;
            int x = Random.Range(1, grid.gridWidth - 1);
            int z = Random.Range(1, grid.gridHeight - 1);

            if (!grid.IsValidPosition(x, z)) continue;
            if (grid.gridData[x, z] != GridManager.EMPTY) continue;

            Vector3 pos = grid.GridToWorldPosition(x, z);
            GameObject fruit = Instantiate(prefabToSpawn, pos, Quaternion.identity, grid.transform);

            grid.AddFruit(x, z, fruit);
            spawned++;
        }

        // update game manager total
        if (GameManager.Instance != null)
        {
            GameManager.Instance.totalFruits = grid.fruitObjects.Count;
            Debug.Log($"SpawnPhaseFruits: Spawned {grid.fruitObjects.Count} fruits for phase {phase}");
        }
    }
}
