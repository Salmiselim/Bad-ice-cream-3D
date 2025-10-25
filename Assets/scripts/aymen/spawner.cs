using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab; // Assign the enemy prefab in the Inspector

    void Start()
    {
        if (enemyPrefab != null)
        {
            // Instantiate the enemy at the spawn point's position and rotation
            Instantiate(enemyPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Enemy prefab not assigned at spawn point: " + gameObject.name);
        }
    }
}
