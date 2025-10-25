using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText; // ← ADD THIS LINE
    public GameObject gameOverPanel;
    public GameObject winPanel;

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
        // Make sure panels are hidden at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        UpdateScore(0);
        UpdateTimer(20f);
        UpdateLives(3); // ← ADD THIS LINE
    }



    // ADD THIS METHOD
    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";

            if (lives == 1)
            {
                livesText.color = Color.red;
            }
            else if (lives == 2)
            {
                livesText.color = Color.yellow;
            }
            else
            {
                livesText.color = Color.white;
            }
        }
    }
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerText.text = $"Time: {seconds}";

            // Change color based on time remaining
            if (seconds <= 5)
            {
                timerText.color = Color.red;
            }
            else if (seconds <= 10)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void ShowWin()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }

    public void HideAllPanels()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }
}