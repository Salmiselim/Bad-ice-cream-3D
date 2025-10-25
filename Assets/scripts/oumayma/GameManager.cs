using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Stats")]
    public int currentScore = 0;
    public int fruitsCollected = 0;
    public int totalFruits = 0;

    [Header("Timer Settings")]
    public float levelTime = 20f; // 20 seconds
    private float timeRemaining;
    private bool isGameActive = true;

    [Header("Player Lives")] // ADD THIS SECTION
    public int playerLives = 3;
    private int currentLives;


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


    void Start()
    {
        timeRemaining = levelTime;
        isGameActive = true;

        currentLives = playerLives; // ADD THIS

        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives); // ADD THIS
        }
    }





    void ShowLevelComplete()
    {
        Debug.Log("=== LEVEL COMPLETE ===");
        // TODO: Load next level or show win screen
    }





    void Update()
    {
        if (isGameActive)
        {
            // Countdown timer
            timeRemaining -= Time.deltaTime;

            // Update UI
            UIManager.Instance.UpdateTimer(timeRemaining);

            // Check if time is up
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                GameOver();
            }
        }

        // Restart with R key
        if (!isGameActive && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;
        Debug.Log($"Score: {currentScore}");

        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(currentScore);
        }
    }

    public void CollectFruit(int x, int z)
    {
        fruitsCollected++;
        Debug.Log($"Fruits Collected: {fruitsCollected}/{totalFruits} at position ({x}, {z})");

        if (fruitsCollected >= totalFruits)
        {
            LevelComplete();
        }
    }


    public void PlayerDied()
    {
        if (!isGameActive) return;

        currentLives--;
        Debug.Log($"Player died! Lives remaining: {currentLives}");

        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
        }

        if (currentLives <= 0)
        {
            GameOver();
        }
        else
        {
            // Respawn player with brief delay ( i put no delay 0f you can modify it later hihihihi)
            Invoke("RespawnPlayer", 0f);
        }
    }



    // ADD THIS METHOD - Respawn player at spawn point
    void RespawnPlayer()
    {
        Debug.Log("Respawning player...");

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Reset position to spawn
            player.currentGridPos = player.spawnGridPosition;
            Vector3 spawnPos = player.gridManager.GridToWorldPosition(
                player.spawnGridPosition.x,
                player.spawnGridPosition.y
            );
            spawnPos.y = player.transform.position.y;
            player.transform.position = spawnPos;
            player.targetPosition = spawnPos;

            Debug.Log($"Player respawned at {player.spawnGridPosition}");
        }
    }

    void LevelComplete()
    {
        Debug.Log("=== LEVEL COMPLETE ===");
        isGameActive = false;

        // Show win panel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWin();
        }
    }

    void GameOver()
    {
        Debug.Log("=== GAME OVER - TIME'S UP ===");
        isGameActive = false;

        // Show game over panel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }

    void RestartLevel()
    {
        Debug.Log("Restarting level...");

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetLevelStats()
    {
        currentScore = 0;
        fruitsCollected = 0;
        totalFruits = 0;
        timeRemaining = levelTime;
        currentLives = playerLives;
        isGameActive = true;
    }
}