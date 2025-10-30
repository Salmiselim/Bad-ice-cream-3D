using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public AudioClip playerHitSFX;     // Drag your "damage" sound here
    public AudioClip backgroundMusic;

    [Header("Game Stats")]
    public int currentScore = 0;
    public int fruitsCollected = 0;
    public int totalFruits = 0;

    [Header("Timer Settings")]
    public float levelTime = 60f;
    private float timeRemaining;
    private bool isGameActive = true;

    [Header("Player Lives")]
    public int playerLives = 3;
    private int currentLives;

    [Header("Phase Settings")]
    public int currentPhase = 1; // 1 or 2
    public int phase2FruitGoal = 5; // how many fruits to spawn/collect in phase 2

    public bool IsGameActive => isGameActive;
    public bool IsPhase2() => currentPhase == 2;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        timeRemaining = levelTime;
        isGameActive = true;
        currentLives = playerLives;
        currentPhase = 1;
        AudioManager.instance.PlayMusic(backgroundMusic);

        // spawn initial fruits for phase 1
        FruitSpawnerPhase2 spawner = FindObjectOfType<FruitSpawnerPhase2>();
        if (spawner != null)
        {
            spawner.SpawnPhaseFruits(currentPhase);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLives(currentLives);
    }

    void Update()
    {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTimer(timeRemaining);

        if (timeRemaining <= 0f)
            GameOver();

        if (!isGameActive && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(currentScore);
    }

    // Called by GridManager when a fruit is collected
    public void CollectFruit(int x, int z)
    {
        fruitsCollected++;
        Debug.Log($"Fruits Collected: {fruitsCollected}/{totalFruits}");

        if (currentPhase == 1 && fruitsCollected >= totalFruits)
        {
            // switch to phase 2 and spawn second-prefab fruits
            currentPhase = 2;
            fruitsCollected = 0;
            totalFruits = phase2FruitGoal;

            Debug.Log("Switching to Phase 2 - spawning second prefab fruits");
            FruitSpawnerPhase2 spawner = FindObjectOfType<FruitSpawnerPhase2>();
            if (spawner != null) spawner.SpawnPhaseFruits(currentPhase);
        }
        else if (currentPhase == 2 && fruitsCollected >= totalFruits)
        {
            LevelComplete();
        }
    }

    public void PlayerDied()
    {
        if (!isGameActive) return;
        currentLives--;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);

        // ADD THIS: Play hit sound
        if (playerHitSFX != null)
            AudioManager.instance.PlaySFX(playerHitSFX);

        if (currentLives <= 0)
        {
           /* // ADD THIS: Play game over sound
            if (gameOverSFX != null)
                AudioManager.instance.PlaySFX(gameOverSFX);
           */
            GameOver();
        }
        else
        {
            Invoke(nameof(RespawnPlayer), 0f);
        }
    }

    void RespawnPlayer()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.gridManager != null)
        {
            Vector3 spawnPos = player.gridManager.GridToWorldPosition(player.spawnGridPosition.x, player.spawnGridPosition.y);
            spawnPos.y = player.transform.position.y;
            player.transform.position = spawnPos;
            player.targetPosition = spawnPos;
            player.currentGridPos = player.spawnGridPosition;
        }
    }

    void LevelComplete()
    {
        isGameActive = false;
        if (UIManager.Instance != null) UIManager.Instance.ShowWin();
        Debug.Log("Level Complete!");
    }

    void GameOver()
    {
        isGameActive = false;
        if (UIManager.Instance != null) UIManager.Instance.ShowGameOver();
        Debug.Log("Game Over");
    }
}
