using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMP = TMPro.TextMeshProUGUI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject hostPanel;
    public GameObject joinPanel;
    public Transform lobbyListParent;
    public GameObject lobbyListItemPrefab;
    public TMP lobbyNameText;
    public Button startButton;
    public Button leaveButton;

    private Lobby currentLobby;
    private bool isHost = false;
    private bool servicesInitialized = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private async void Start()
    {
        await EnsureServicesInitialized();
    }

    private async Task EnsureServicesInitialized()
    {
        if (servicesInitialized) return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        servicesInitialized = true;
    }

    public async void HostLobby(string lobbyName = "MyLobby", int maxPlayers = 4)
    {
        await EnsureServicesInitialized();

        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                    { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId) }
                }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        isHost = true;

        Debug.Log($"Lobby hosted: {currentLobby.Id} | {currentLobby.Name} | Host: {currentLobby.HostId}");

        SetupLobbyUI();
        InvokeRepeating(nameof(Heartbeat), 15f, 15f);
    }

    public async void SearchLobbies()
    {
        foreach (Transform child in lobbyListParent)
            Destroy(child.gameObject);

        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = 50,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (response.Results.Count == 0)
            {
                Debug.Log("No public lobbies with slots — trying unfiltered...");
                response = await LobbyService.Instance.QueryLobbiesAsync();
            }

            foreach (var lobby in response.Results)
            {
                var item = Instantiate(lobbyListItemPrefab, lobbyListParent);
                var listItem = item.GetComponent<LobbyListItem>();

                TMP text = item.GetComponentInChildren<TMP>();
                if (text != null)
                    text.text = $"{lobby.Name} ({lobby.AvailableSlots} slots left)";

                Button joinBtn = listItem != null && listItem.joinButton != null ? listItem.joinButton : item.GetComponentInChildren<Button>();

                if (joinBtn != null)
                {
                    string lobbyId = lobby.Id; // CRITICAL: capture in local variable
                    joinBtn.onClick.RemoveAllListeners();
                    joinBtn.onClick.AddListener(() => JoinLobby(lobbyId)); // Now works 100% of the time
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Search failed: " + e.Message);
        }
    }

    public async void JoinLobby(string lobbyId)
    {
        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                        { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId) }
                    }
                }
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            isHost = false;

            SetupLobbyUI();

            Debug.Log($"Successfully joined lobby: {lobbyId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    private void SetupLobbyUI()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(false);
        if (lobbyNameText != null)
            lobbyNameText.text = currentLobby.Name;

        // Subscribe to lobby events (player joined/left, data changed, etc.)
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChangedCallback;
        LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, callbacks);

        // Leave button
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => StartCoroutine(LeaveLobbyCoroutine()));
        }

        StartCoroutine(EnableHostPanelNextFrame());
    }

    private void OnLobbyChangedCallback(ILobbyChanges changes)
    {
        changes.ApplyToLobby(currentLobby);
        OnLobbyChanged(currentLobby);
    }

    private void OnLobbyChanged(Lobby lobby)
    {
        currentLobby = lobby;
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        if (startButton == null || currentLobby == null) return;

        startButton.gameObject.SetActive(isHost);

        bool allReady = true;
        foreach (var player in currentLobby.Players)
        {
            if (player.Data.TryGetValue("ready", out var readyVal) && readyVal.Value == "0")
            {
                allReady = false;
                break;
            }
        }

        startButton.interactable = allReady && currentLobby.Players.Count >= 2; // or == MaxPlayers
    }

    public async void ToggleReady()
    {
        if (currentLobby == null) return;

        string current = currentLobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId)
            ?.Data["ready"].Value ?? "0";

        string newValue = current == "1" ? "0" : "1";

        var options = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newValue) }
            }
        };

        await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, options);
    }

    // THIS IS THE FIX FOR "ONLY HOST LOADS THE GAME SCENE"
    public async void StartGame()
    {
        if (!isHost) return;

        // Tell EVERY player in the lobby to load the game scene
        var lobbyData = new Dictionary<string, DataObject>
        {
            { "startGame", new DataObject(DataObject.VisibilityOptions.Member, "true") }
        };

        await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
        {
            Data = lobbyData
        });

        // Host loads immediately
        LoadGameScene();
    }

    // Call this from LobbyEventCallbacks or poll
    private void Update()
    {
        if (currentLobby != null && !isHost)
        {
            if (currentLobby.Data != null && currentLobby.Data.TryGetValue("startGame", out var data) && data.Value == "true")
            {
                // Prevent loading multiple times
                if (SceneManager.GetActiveScene().name != "GameScene")
                {
                    LoadGameScene();
                }
            }
        }
    }

    private void LoadGameScene()
    {
        CancelInvoke(nameof(Heartbeat));
        SceneManager.LoadScene("GameScene");
    }

    private IEnumerator EnableHostPanelNextFrame()
    {
        yield return null;
        hostPanel.SetActive(true);
    }

    private IEnumerator LeaveLobbyCoroutine()
    {
        yield return LeaveLobby();
        hostPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        joinPanel.SetActive(false);
    }

    private async Task LeaveLobby()
    {
        try
        {
            if (currentLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);

                if (isHost && currentLobby.Players.Count <= 1)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Leave failed: " + e);
        }
        finally
        {
            currentLobby = null;
            isHost = false;
            CancelInvoke(nameof(Heartbeat));
        }
    }

    private async void Heartbeat()
    {
        if (currentLobby != null && isHost)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
    }

    // Public wrappers for buttons
    public void HostLobbyButton() => HostLobby();
    public void LeaveLobbyButton() => StartCoroutine(LeaveLobbyCoroutine());
}