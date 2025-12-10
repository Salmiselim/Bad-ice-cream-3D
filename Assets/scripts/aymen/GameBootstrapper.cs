// File: GameBootstrapper.cs
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("GameBootstrapper");
        Instance = go.AddComponent<GameBootstrapper>();
        DontDestroyOnLoad(go);
    }

    private async void Start()
    {
        Debug.Log("Bootstrapper: Initializing Unity Services...");
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Bootstrapper: Signed in as {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Bootstrapper failed: " + e);
        }
    }

    // Call these from anywhere
    public static void GoToLobby() => SceneManager.LoadScene("LobbyScene");
    public static void GoToGame() => SceneManager.LoadScene("GameScene");
}