using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;
    public GameObject gameOverPanel;
    public GameObject winPanel;

    [Header("Retry Settings")]
    public KeyCode retryKey = KeyCode.R; // Change if needed
    public float retryDelay = 0.5f;      // Prevents instant retry spam

    private bool canRetry = false;
    private float retryTimer = 0f;

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
        HideAllPanels();
        UpdateScore(0);
        UpdateTimer(20f);
        UpdateLives(3);
    }

    void Update()
    {
        // Handle retry input only when a panel is active
        if (canRetry && Time.time >= retryTimer)
        {
            if (Input.GetKeyDown(retryKey))
            {
                RetryLevel();
            }
        }
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
            livesText.color = lives switch
            {
                1 => Color.red,
                2 => Color.yellow,
                _ => Color.white
            };
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerText.text = $"Time: {seconds}";
            timerText.color = seconds switch
            {
                <= 5 => Color.red,
                <= 10 => Color.yellow,
                _ => Color.white
            };
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            EnableRetry();
        }
    }

    public void ShowWin()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            EnableRetry();
        }
    }

    public void HideAllPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        DisableRetry();
    }

    // === RETRY LOGIC ===
    private void EnableRetry()
    {
        canRetry = true;
        retryTimer = Time.time + retryDelay;
    }

    private void DisableRetry()
    {
        canRetry = false;
    }

    public void RetryLevel()
    {
        DisableRetry();
        HideAllPanels();

        // Optional: Add fade-to-black here (see bonus below)

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Optional: Call this from a UI Button too!
    public void OnRetryButtonClicked()
    {
        RetryLevel();
    }
}