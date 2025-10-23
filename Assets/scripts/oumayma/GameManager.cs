using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton

    [Header("Game Stats")]
    public int currentScore = 0;
    public int fruitsCollected = 0;
    public int totalFruits = 0; // Set at level start

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;
        Debug.Log($"Score: {currentScore}");
    }

    public void CollectFruit()
    {
        fruitsCollected++;
        if (fruitsCollected >= totalFruits)
        {
            Debug.Log("Level Complete! All fruits collected.");
            // TODO: Load next level or show win screen
        }
    }

    public void ResetLevelStats()
    {
        currentScore = 0;
        fruitsCollected = 0;
        totalFruits = 0;
    }
}