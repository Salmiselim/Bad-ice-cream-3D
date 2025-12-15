using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Button hostButton;   // ← Assign these in Inspector
    public Button joinButton;   // ← So we can disable them while initializing

    private Lobby currentLobby;
    private bool isHost = false;
    private bool isInitialized = false;  // ← This is the key

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private async void Start()
    {
        // Disable buttons until fully ready
        if (hostButton) hostButton.interactable = false;
        if (joinButton) joinButton.interactable = false;

        Debug.Log("Initializing Unity Services + Authentication...");
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously: {AuthenticationService.Instance.PlayerId}");
            }

            isInitialized = true;
            Debug.Log("Lobby system READY!");

            // Re-enable buttons
            if (hostButton) hostButton.interactable = true;
            if (joinButton) joinButton.interactable = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("INIT FAILED: " + e.Message);
        }
    }

    // BLOCK ALL ACTIONS UNTIL INITIALIZED
    private bool WaitForInit()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Lobby not ready yet — please wait...");
            return false;
        }
        return true;
    }

    // ==================== BUTTONS ====================
    public void HostLobbyButton() => HostLobby();
    public void SearchLobbiesButton() => SearchLobbies();
    public void LeaveLobbyButton() => LeaveLobby();  // Now fire-and-forget

    // ==================== HOST ====================
    public async void HostLobby(string lobbyName = "My Lobby", int maxPlayers = 4)
    {
        if (!WaitForInit()) return;

        try
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                        { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId.Substring(0, 8)) }
                    }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            isHost = true;
            Debug.Log($"Lobby Created: {currentLobby.Id} | Code: {currentLobby.LobbyCode}");

            SetupLobbyUI();
            InvokeRepeating(nameof(Heartbeat), 15f, 15f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Host failed: " + e.Message);
        }
    }

    // ==================== SEARCH ====================
    public async void SearchLobbies()
    {
        if (!WaitForInit()) return;

        foreach (Transform child in lobbyListParent) Destroy(child.gameObject);

        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (response.Results.Count == 0)
            {
                Debug.Log("No lobbies found with open slots.");
                return;
            }

            foreach (var lobby in response.Results)
            {
                var item = Instantiate(lobbyListItemPrefab, lobbyListParent);
                var text = item.GetComponentInChildren<TMP>();
                if (text) text.text = $"{lobby.Name} [{lobby.Players.Count}/{lobby.MaxPlayers}]";

                var btn = item.GetComponentInChildren<Button>();
                if (btn)
                {
                    string lobbyId = lobby.Id;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => JoinLobby(lobbyId));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Search failed: " + e.Message);
        }
    }

    // ==================== JOIN ====================
    public async void JoinLobby(string lobbyId)
    {
        if (!WaitForInit()) return;

        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                        { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId.Substring(0, 8)) }
                    }
                }
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            isHost = false;
            Debug.Log("Joined lobby!");
            SetupLobbyUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Join failed: " + e.Message);
        }
    }

    // ==================== REST OF THE CODE (unchanged) ====================
    private void SetupLobbyUI()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(false);
        hostPanel.SetActive(true);
        if (lobbyNameText) lobbyNameText.text = $"Lobby: {currentLobby.Name}";

        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, callbacks);

        UpdateStartButton();
        UpdatePlayerList();

        if (leaveButton)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(LeaveLobbyButton);
        }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        changes.ApplyToLobby(currentLobby);
        UpdateStartButton();
        UpdatePlayerList();

        if (!isHost && currentLobby.Data != null && currentLobby.Data.TryGetValue("startGame", out var val) && val.Value == "true")
        {
            LoadGameScene();
        }
    }

    private void UpdateStartButton()
    {
        if (startButton == null || currentLobby == null) return;
        startButton.gameObject.SetActive(isHost);
        bool allReady = currentLobby.Players.All(p => p.Data != null && p.Data.ContainsKey("ready") && p.Data["ready"].Value == "1");
        startButton.interactable = allReady && currentLobby.Players.Count >= 2;
    }

    public async void ToggleReady()
    {
        if (currentLobby == null || !WaitForInit()) return;

        var myPlayer = currentLobby.Players.FirstOrDefault(p => p.Id == AuthenticationService.Instance.PlayerId);
        string newVal = (myPlayer?.Data["ready"].Value == "1") ? "0" : "1";

        var options = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newVal) }
            }
        };

        await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, options);
    }

    public async void StartGame()
    {
        if (!isHost || currentLobby == null || !WaitForInit()) return;

        var latest = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        if (latest.Players.All(p => p.Data["ready"].Value == "1"))
        {
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "startGame", new DataObject(DataObject.VisibilityOptions.Member, "true") }
                }
            });
            LoadGameScene();
        }
    }

    private void LoadGameScene()
    {
        CancelInvoke(nameof(Heartbeat));
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        SceneManager.LoadScene("GameScene");
    }

    private async void LeaveLobby()
    {
        if (currentLobby == null) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            if (isHost) await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
        catch { }
        finally
        {
            currentLobby = null;
            isHost = false;
            CancelInvoke(nameof(Heartbeat));
            hostPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }

    private async void Heartbeat()
    {
        if (currentLobby != null && isHost)
            try { await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id); } catch { }
    }

    [Header("Player List UI")]
    public Transform playerListContent;     // Drag the Content of your ScrollView here
    public GameObject playerItemPrefab;     // Drag your PlayerItemPrefab here
    private Dictionary<string, GameObject> playerListItems = new Dictionary<string, GameObject>();
    private void UpdatePlayerList()
    {
        if (currentLobby == null) return;

        // Clear old entries (except those still in lobby)
        var currentPlayerIds = new HashSet<string>(currentLobby.Players.Select(p => p.Id));
        var toRemove = new List<string>();
        foreach (var id in playerListItems.Keys)
        {
            if (!currentPlayerIds.Contains(id))
                toRemove.Add(id);
        }
        foreach (var id in toRemove)
        {
            Destroy(playerListItems[id]);
            playerListItems.Remove(id);
        }

        // Add or update players
        foreach (var player in currentLobby.Players)
        {
            string playerId = player.Id;
            string playerName = player.Data?.ContainsKey("name") == true
                ? player.Data["name"].Value
                : playerId.Substring(0, 8);  // fallback

            string readyStatus = player.Data?.ContainsKey("ready") == true && player.Data["ready"].Value == "1"
                ? "✓ Ready"
                : "✗ Not Ready";

            if (playerListItems.TryGetValue(playerId, out GameObject item))
            {
                // Update existing
                item.GetComponent<TMP>().text = $"{playerName} - {readyStatus}";
            }
            else
            {
                // Create new
                var newItem = Instantiate(playerItemPrefab, playerListContent);
                newItem.GetComponent<TMP>().text = $"{playerName} - {readyStatus}";
                playerListItems[playerId] = newItem;
            }
        }
    }
}