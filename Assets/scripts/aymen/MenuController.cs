using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;          // for Button (optional)
using TMPro;                  // if using TextMeshPro

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Scenes")]
    public string gameSceneName = "GameScene";   // change to your gameplay scene
    public string optionsSceneName = "Options";  // optional

    private void Start()
    {
        // Hook up listeners (you can also do this in the Inspector)
        playButton.onClick.AddListener(PlayGame);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(QuitGame);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        // If you have a separate Options scene:
        // SceneManager.LoadScene(optionsSceneName);

        // Or just toggle an options panel in the same scene:
        Debug.Log("Options opened (implement panel toggle)");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}