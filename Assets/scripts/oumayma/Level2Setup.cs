using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public class Level2Setup : MonoBehaviour
{
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "level2")
            return;

        GridManager gm = FindObjectOfType<GridManager>();
        if (gm == null) { Debug.LogError("GridManager not found!"); return; }
        if (gm.wallPrefab == null) { Debug.LogError("wallPrefab not assigned!"); return; }

        ClearAll(gm);
        BuildEmptyGridWithBorder(gm);
        BuildTShapeCentered(gm);
        gm.GenerateLevel();

        Debug.Log("Level 2: T-Shape at center (7,7) → world (-0.5, 0, -0.5)");
    }

    void ClearAll(GridManager gm)
    {
        foreach (var o in gm.fruitObjects.Values) if (o) Destroy(o);
        foreach (var o in gm.iceBlockObjects.Values) if (o) Destroy(o);
        gm.fruitObjects.Clear();
        gm.iceBlockObjects.Clear();
    }

    void BuildEmptyGridWithBorder(GridManager gm)
    {
        gm.gridData = new int[gm.gridWidth, gm.gridHeight];
        for (int x = 0; x < gm.gridWidth; x++)
            for (int z = 0; z < gm.gridHeight; z++)
                gm.gridData[x, z] = GridManager.EMPTY;

        // Border walls
        for (int x = 0; x < gm.gridWidth; x++)
        {
            gm.gridData[x, 0] = GridManager.WALL;
            gm.gridData[x, gm.gridHeight - 1] = GridManager.WALL;
        }
        for (int z = 0; z < gm.gridHeight; z++)
        {
            gm.gridData[0, z] = GridManager.WALL;
            gm.gridData[gm.gridWidth - 1, z] = GridManager.WALL;
        }
    }

    void BuildTShapeCentered(GridManager gm)
    {
        int centerX = 7;
        int centerZ = 7;

        // Crossbar: 7 tiles wide, 2 tiles above center → z = 9
        int crossTopZ = centerZ + 2;  // 9
        for (int x = centerX - 3; x <= centerX + 3; x++)
        {
            gm.gridData[x, crossTopZ] = GridManager.WALL;
        }

        // Stem: 5 tiles down from crossbar → z = 9 → 5
        for (int z = crossTopZ; z >= crossTopZ - 4; z--)
        {
            gm.gridData[centerX, z] = GridManager.WALL;
        }
    }
}