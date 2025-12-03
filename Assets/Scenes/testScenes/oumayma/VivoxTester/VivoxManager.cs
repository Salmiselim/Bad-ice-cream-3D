using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System;
using System.Collections.Generic;
using System.Linq;

public class VivoxManager : MonoBehaviour
{
    [Header("Vivox Configuration")]
    [Tooltip("The channel name players will join (e.g., 'GameLobby', 'Team1')")]
    public string channelName = "GameLobby";

    [Tooltip("Is this a 3D positional channel?")]
    public bool usePositionalAudio = false;

    [Header("3D Audio Settings (if positional)")]
    [Tooltip("Distance at which audio starts to fade")]
    public int audioFadeDistance = 32;

    [Tooltip("Maximum distance for audio")]
    public int audioMaxDistance = 60;

    [Tooltip("Audio fade model")]
    public AudioFadeModel audioFadeModel = AudioFadeModel.InverseByDistance;

    [Header("Status (Read-Only)")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private bool isLoggedIn = false;
    [SerializeField] private bool isInChannel = false;

    // Current channel name
    private string _currentChannelName;

    #region Unity Lifecycle

    private void Awake()
    {
        // Make this persistent across scenes if needed
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        try
        {
            // Wait for Unity Services to be initialized by Authmanager or here if not initialized.
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                // If Authmanager already called InitializeAsync, this will be fast/instant.
                await UnityServices.InitializeAsync();
            }

            // Wait for AuthenticationService to be signed in (Authmanager should sign in then transition).
            // This avoids race conditions if VivoxManager scene loads before auth completes.
            float waitTimeout = 10f; // seconds
            float waited = 0f;
            while (!AuthenticationService.Instance.IsSignedIn && waited < waitTimeout)
            {
                await System.Threading.Tasks.Task.Delay(200);
                waited += 0.2f;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[Vivox] Authentication not signed in after wait. Proceeding with anonymous sign-in (dev).");
                await AuthenticatePlayer(); // fallback to anonymous if needed
            }

            Debug.Log("[Vivox] Unity Services ready; proceeding with Vivox initialization");

            // Initialize Vivox Service
            await VivoxService.Instance.InitializeAsync();

            Debug.Log("[Vivox] Vivox service initialized successfully");
            isInitialized = true;

            // Login to Vivox
            await LoginToVivox();

            // Subscribe to events
            SubscribeToVivoxEvents();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Initialization failed: {ex.Message}\n{ex.StackTrace}");
        }
    }


    private void OnApplicationQuit()
    {
        // Clean disconnect when application closes
        Cleanup();
    }

    #endregion

    #region Authentication

    private async System.Threading.Tasks.Task AuthenticatePlayer()
    {
        // Check if already authenticated by your colleague's system
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log($"[Vivox] Already authenticated as: {AuthenticationService.Instance.PlayerId}");
            return;
        }

        try
        {
            // Sign in anonymously (or use your colleague's auth method)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Vivox] Authenticated with Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Authentication failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Vivox Login

    private async System.Threading.Tasks.Task LoginToVivox()
    {
        if (!isInitialized)
        {
            Debug.LogError("[Vivox] Cannot login - Vivox not initialized");
            return;
        }

        try
        {
            var loginOptions = new LoginOptions
            {
                DisplayName = AuthenticationService.Instance.PlayerId,
                EnableTTS = false // Text-to-speech (optionnel)
            };

            await VivoxService.Instance.LoginAsync(loginOptions);

            Debug.Log($"[Vivox] Logged in successfully");
            isLoggedIn = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Login failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Event Subscription

    private void SubscribeToVivoxEvents()
    {
        // Subscribe to channel events
        VivoxService.Instance.ChannelJoined += OnChannelJoined;
        VivoxService.Instance.ChannelLeft += OnChannelLeft;

        // Subscribe to participant events
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

        // Subscribe to text message events
        VivoxService.Instance.ChannelMessageReceived += OnTextMessageReceived;

        Debug.Log("[Vivox] Subscribed to Vivox events");
    }

    private void UnsubscribeFromVivoxEvents()
    {
        if (VivoxService.Instance == null) return;

        VivoxService.Instance.ChannelJoined -= OnChannelJoined;
        VivoxService.Instance.ChannelLeft -= OnChannelLeft;
        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;
        VivoxService.Instance.ChannelMessageReceived -= OnTextMessageReceived;
    }

    #endregion

    #region Channel Management

    /// <summary>
    /// Join a voice channel. Call this when player enters game/lobby.
    /// </summary>
    public async void JoinChannel(string customChannelName = null)
    {
        if (!isLoggedIn)
        {
            Debug.LogError("[Vivox] Cannot join channel - not logged in");
            return;
        }

        if (isInChannel)
        {
            Debug.LogWarning("[Vivox] Already in a channel. Leave current channel first.");
            return;
        }

        string targetChannel = customChannelName ?? channelName;
        _currentChannelName = targetChannel;

        try
        {
            if (usePositionalAudio)
            {
                // Join a positional (3D audio) channel
                Channel3DProperties properties = new Channel3DProperties(
                    audioFadeDistance,
                    audioMaxDistance,
                    1.0f, // conversational distance (usually 1.0)
                    audioFadeModel
                );

                await VivoxService.Instance.JoinPositionalChannelAsync(
                    targetChannel,
                    ChatCapability.TextAndAudio,
                    properties
                );

                Debug.Log($"[Vivox] Joined positional channel: {targetChannel}");
            }
            else
            {
                // Join a non-positional (normal) channel
                await VivoxService.Instance.JoinGroupChannelAsync(
                    targetChannel,
                    ChatCapability.TextAndAudio
                );

                Debug.Log($"[Vivox] Joined group channel: {targetChannel}");
            }

            isInChannel = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to join channel: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Join an echo channel for testing (you hear yourself)
    /// </summary>
    public async void JoinEchoChannel(string testChannelName = "EchoTest")
    {
        if (!isLoggedIn)
        {
            Debug.LogError("[Vivox] Cannot join channel - not logged in");
            return;
        }

        try
        {
            await VivoxService.Instance.JoinEchoChannelAsync(
                testChannelName,
                ChatCapability.AudioOnly
            );

            Debug.Log($"[Vivox] Joined echo channel: {testChannelName}");
            _currentChannelName = testChannelName;
            isInChannel = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to join echo channel: {ex.Message}");
        }
    }

    /// <summary>
    /// Leave the current voice channel.
    /// </summary>
    public async void LeaveChannel()
    {
        if (!isInChannel || string.IsNullOrEmpty(_currentChannelName))
        {
            Debug.LogWarning("[Vivox] Not in a channel");
            return;
        }

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(_currentChannelName);

            Debug.Log($"[Vivox] Left channel: {_currentChannelName}");
            isInChannel = false;
            _currentChannelName = null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Error leaving channel: {ex.Message}");
        }
    }

    /// <summary>
    /// Leave all channels
    /// </summary>
    public async void LeaveAllChannels()
    {
        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
            Debug.Log("[Vivox] Left all channels");
            isInChannel = false;
            _currentChannelName = null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Error leaving all channels: {ex.Message}");
        }
    }

    /// <summary>
    /// Update 3D position for positional audio (call this in Update() for moving players)
    /// </summary>
    public void Update3DPosition(Vector3 speakerPosition, Vector3 listenerPosition, Vector3 listenerForward, Vector3 listenerUp)
    {
        if (!isInChannel || !usePositionalAudio)
            return;

        try
        {
            VivoxService.Instance.Set3DPosition(
                speakerPosition, // Position du speaker (le joueur qui parle)
                listenerPosition, // Position du listener (le joueur local)
                listenerForward, // Direction avant du listener
                listenerUp, // Direction haut du listener
                _currentChannelName // Le nom du canal
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to update 3D position: {ex.Message}");
        }
    }

    #endregion

    #region Channel Events

    private void OnChannelJoined(string channelName)
    {
        Debug.Log($"[Vivox] Successfully joined channel: {channelName}");
        _currentChannelName = channelName;
        isInChannel = true;
    }

    private void OnChannelLeft(string channelName)
    {
        Debug.Log($"[Vivox] Left channel: {channelName}");

        if (_currentChannelName == channelName)
        {
            _currentChannelName = null;
            isInChannel = false;
        }
    }

    #endregion

    #region Participant Events

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        Debug.Log($"[Vivox] Player joined: {participant.DisplayName} (ID: {participant.PlayerId})");

        // You can trigger UI updates here
        // Example: UpdatePlayerList();
    }

    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        Debug.Log($"[Vivox] Player left: {participant.DisplayName}");

        // Update UI here
    }

    #endregion

    #region Audio Controls

    /// <summary>
    /// Mute or unmute local microphone.
    /// </summary>
    public void SetMicrophoneMuted(bool muted)
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("[Vivox] Cannot toggle mic - not logged in");
            return;
        }

        try
        {
            if (muted)
            {
                VivoxService.Instance.MuteInputDevice();
            }
            else
            {
                VivoxService.Instance.UnmuteInputDevice();
            }

            Debug.Log($"[Vivox] Microphone {(muted ? "muted" : "unmuted")}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to toggle microphone: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if microphone is currently muted.
    /// </summary>
    public bool IsMicrophoneMuted()
    {
        try
        {
            return VivoxService.Instance.IsInputDeviceMuted;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Set input device (microphone) volume (-50 to 50, default 0).
    /// </summary>
    public void SetInputVolume(int volume)
    {
        try
        {
            volume = Mathf.Clamp(volume, -50, 50);
            VivoxService.Instance.SetInputDeviceVolume(volume);
            Debug.Log($"[Vivox] Input volume set to: {volume}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to set input volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Set output device (speaker) volume (-50 to 50, default 0).
    /// </summary>
    public void SetOutputVolume(int volume)
    {
        try
        {
            volume = Mathf.Clamp(volume, -50, 50);
            VivoxService.Instance.SetOutputDeviceVolume(volume);
            Debug.Log($"[Vivox] Output volume set to: {volume}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to set output volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current input volume (-50 to 50).
    /// </summary>
    public int GetInputVolume()
    {
        try
        {
            return VivoxService.Instance.InputDeviceVolume;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get current output volume (-50 to 50).
    /// </summary>
    public int GetOutputVolume()
    {
        try
        {
            return VivoxService.Instance.OutputDeviceVolume;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Mute a specific participant locally (you won't hear them).
    /// </summary>
    public void MuteParticipant(VivoxParticipant participant, bool muted)
    {
        if (!isInChannel)
        {
            Debug.LogWarning($"[Vivox] Cannot mute participant - not in channel");
            return;
        }

        try
        {
            if (muted)
            {
                participant.MutePlayerLocally();
            }
            else
            {
                participant.UnmutePlayerLocally();
            }

            Debug.Log($"[Vivox] Participant {participant.DisplayName} {(muted ? "muted" : "unmuted")} locally");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to mute participant: {ex.Message}");
        }
    }

    /// <summary>
    /// Block a player (they can't hear you and you can't hear them in any channel)
    /// </summary>
    public async void BlockPlayer(string playerId)
    {
        try
        {
            await VivoxService.Instance.BlockPlayerAsync(playerId);
            Debug.Log($"[Vivox] Player {playerId} blocked");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to block player: {ex.Message}");
        }
    }

    /// <summary>
    /// Unblock a player
    /// </summary>
    public async void UnblockPlayer(string playerId)
    {
        try
        {
            await VivoxService.Instance.UnblockPlayerAsync(playerId);
            Debug.Log($"[Vivox] Player {playerId} unblocked");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to unblock player: {ex.Message}");
        }
    }

    #endregion

    #region Text Chat

    private void OnTextMessageReceived(VivoxMessage message)
    {
        Debug.Log($"[Vivox] [{message.ChannelName}] {message.SenderDisplayName}: {message.MessageText}");

        // Handle text message in UI
        // Example: DisplayChatMessage(message.SenderDisplayName, message.MessageText);
    }

    /// <summary>
    /// Send a text message to the channel.
    /// </summary>
    public async void SendTextMessage(string message)
    {
        if (!isInChannel || string.IsNullOrEmpty(_currentChannelName))
        {
            Debug.LogWarning("[Vivox] Cannot send message - not in channel");
            return;
        }

        try
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(_currentChannelName, message);
            Debug.Log($"[Vivox] Message sent: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to send message: {ex.Message}");
        }
    }

    #endregion

    #region Cleanup

    private void Cleanup()
    {
        try
        {
            UnsubscribeFromVivoxEvents();

            if (isInChannel)
            {
                LeaveAllChannels();
            }

            if (VivoxService.Instance != null && isLoggedIn)
            {
                VivoxService.Instance.LogoutAsync();
            }

            Debug.Log("[Vivox] Cleanup completed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Error during cleanup: {ex.Message}");
        }
    }

    #endregion

    #region Public Getters

    public bool IsInitialized => isInitialized;
    public bool IsLoggedIn => isLoggedIn;
    public bool IsInChannel => isInChannel;
    public string CurrentChannelName => _currentChannelName ?? "None";

    /// <summary>
    /// Get list of all participants in current channel.
    /// </summary>
    public List<VivoxParticipant> GetParticipants()
    {
        var participants = new List<VivoxParticipant>();

        if (!isInChannel || string.IsNullOrEmpty(_currentChannelName))
            return participants;

        try
        {
            if (VivoxService.Instance.ActiveChannels.ContainsKey(_currentChannelName))
            {
                var channelParticipants = VivoxService.Instance.ActiveChannels[_currentChannelName];
                participants.AddRange(channelParticipants);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to get participants: {ex.Message}");
        }

        return participants;
    }

    /// <summary>
    /// Get participant names as strings
    /// </summary>
    public List<string> GetParticipantNames()
    {
        return GetParticipants().Select(p => p.DisplayName).ToList();
    }

    /// <summary>
    /// Get number of participants in channel
    /// </summary>
    public int GetParticipantCount()
    {
        return GetParticipants().Count;
    }


    /// <summary>
    /// Ancienne méthode compat : règle le volume global incoming (pour tout ce qu'on entend).
    /// usage attendu : SetIncomingVolume(0) ou SetIncomingVolume(25)  => -50..50
    /// </summary>
    public void SetIncomingVolume(int volume)
    {
        try
        {
            volume = Mathf.Clamp(volume, -50, 50);
            // Ton script utilise SetOutputDeviceVolume(int) pour la sortie => on garde la même logique
            VivoxService.Instance.SetOutputDeviceVolume(volume);
            Debug.Log($"[Vivox] Global incoming/output volume set to {volume}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to set global incoming volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Règle localement le volume d'un participant (ne modifie que ce que le client local entend de ce participant).
    /// Retourne true si la modification a réussi, false sinon.
    /// volume : -50..50 (0 = par défaut)
    /// </summary>
    public bool SetIncomingVolume(string participantPlayerId, int volume)
    {
        if (!isInChannel || string.IsNullOrEmpty(_currentChannelName))
        {
            Debug.LogWarning("[Vivox] Cannot set participant volume - not in a channel");
            return false;
        }

        try
        {
            volume = Mathf.Clamp(volume, -50, 50);

            if (!VivoxService.Instance.ActiveChannels.TryGetValue(_currentChannelName, out var participants))
            {
                Debug.LogWarning($"[Vivox] Channel not found: {_currentChannelName}");
                return false;
            }

            foreach (var p in participants)
            {
                // PlayerId est l'identifiant que Vivox expose (tu utilises PlayerId dans tes logs)
                if (p.PlayerId == participantPlayerId)
                {
                    p.SetLocalVolume(volume); // API v16.x : int -50..50
                    Debug.Log($"[Vivox] Set local volume {volume} for participant {participantPlayerId}");
                    return true;
                }
            }

            Debug.LogWarning($"[Vivox] Participant {participantPlayerId} not found in {_currentChannelName}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox] Failed to set participant local volume: {ex.Message}");
            return false;
        }
    }


    #endregion
}