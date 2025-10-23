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
        UIManager.Instance.UpdateScore(currentScore);
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

    void LevelComplete()
    {
        Debug.Log("=== LEVEL COMPLETE ===");
        isGameActive = false;

        // Show win panel
        UIManager.Instance.ShowWin();
    }

    void GameOver()
    {
        Debug.Log("=== GAME OVER - TIME'S UP ===");
        isGameActive = false;

        // Show game over panel
        UIManager.Instance.ShowGameOver();
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
        isGameActive = true;
    }
}