using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Authmanager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusText;

    [Header("Sign Up Panel")]
    public GameObject signUpPanel;
    public TMP_InputField signUpUsernameInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField signUpConfirmPasswordInput;
    public Button signUpButton;

    [Header("Sign In Panel")]
    public GameObject signInPanel;
    public TMP_InputField signInUsernameInput;
    public TMP_InputField signInPasswordInput;
    public Button signInButton;

    [Header("Other Buttons")]
    public Button signOutButton;
    public Button switchToSignInButton;
    public Button switchToSignUpButton;
    public Button emergencySignOutButton;

    [Header("Scene Names")]
    public string lobbySceneName = "LobbyScene";   // ← CHANGE IF YOUR LOBBY SCENE HAS DIFFERENT NAME
    public string gameSceneName = "GameScene";

    [Header("Testing")]
    public bool forceSignOutOnStart = false;
    public Button debugButton;

    private async void Start()
    {
        statusText.text = "Initializing Unity Services...";

        // Wait for GameBootstrapper to finish initialization and anonymous sign-in
        while (UnityServices.State != ServicesInitializationState.Initialized)
            await Task.Delay(100);

        while (!AuthenticationService.Instance.IsSignedIn)
            await Task.Delay(100);

        // Optional: Force sign out for testing (username/password flow)
        if (forceSignOutOnStart && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Forced sign out for testing");
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            ShowSignedInState();
            GoToLobbyScene();
        }
        else
        {
            statusText.text = "Ready! Please sign up or sign in.";
            ShowSignUpPanel();
            EnableAllButtons();
        }

        // Button listeners
        signUpButton.onClick.AddListener(OnSignUpClicked);
        signInButton.onClick.AddListener(OnSignInClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        switchToSignInButton.onClick.AddListener(ShowSignInPanel);
        switchToSignUpButton.onClick.AddListener(ShowSignUpPanel);

        if (emergencySignOutButton != null)
            emergencySignOutButton.onClick.AddListener(OnSignOutClicked);

        if (debugButton != null)
            debugButton.onClick.AddListener(ShowDebugInfo);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && AuthenticationService.Instance.IsSignedIn)
            OnSignOutClicked();
    }

    // ==================== SIGN UP ====================
    public async void OnSignUpClicked()
    {
        string username = signUpUsernameInput.text.Trim();
        string password = signUpPasswordInput.text;
        string confirm = signUpConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            statusText.text = "Please fill all fields";
            return;
        }
        if (password != confirm) { statusText.text = "Passwords don't match!"; return; }
        if (password.Length < 8) { statusText.text = "Password too short (8+ chars)"; return; }

        statusText.text = "Creating account...";
        DisableAllButtons();

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
            Debug.Log("Sign up successful: " + username);
            GoToLobbyScene();
        }
        catch (AuthenticationException ex)
        {
            if (ex.ErrorCode == 10103) // Username already exists
                statusText.text = "Username already taken!";
            else
                statusText.text = "Sign up failed: " + ex.Message;
            EnableAllButtons();
        }
        catch (Exception ex)
        {
            statusText.text = "Error: " + ex.Message;
            EnableAllButtons();
        }
    }

    // ==================== SIGN IN ====================
    public async void OnSignInClicked()
    {
        string username = signInUsernameInput.text.Trim();
        string password = signInPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Enter username & password";
            return;
        }

        statusText.text = "Signing in...";
        DisableAllButtons();

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Signed in as: " + username);
            GoToLobbyScene();
        }
        catch (AuthenticationException ex)
        {
            if (ex.ErrorCode == 10104)
                statusText.text = "Wrong username or password";
            else if (ex.ErrorCode == 10102)
                statusText.text = "Account not found. Sign up first.";
            else
                statusText.text = "Sign in failed: " + ex.Message;
            EnableAllButtons();
        }
        catch (Exception ex)
        {
            statusText.text = "Connection error";
            EnableAllButtons();
        }
    }

    // ==================== SIGN OUT ====================
    public void OnSignOutClicked()
    {
        AuthenticationService.Instance.SignOut();
        statusText.text = "Signed out";
        signUpUsernameInput.text = "";
        signUpPasswordInput.text = "";
        signUpConfirmPasswordInput.text = "";
        signInUsernameInput.text = "";
        signInPasswordInput.text = "";
        ShowSignUpPanel();
        EnableAllButtons();
    }

    // ==================== GO TO LOBBY ====================
    private void GoToLobbyScene()
    {
        statusText.text = "Welcome! Loading lobby...";
        SceneManager.LoadScene(lobbySceneName);
    }

    // ==================== DEBUG ====================
    public void ShowDebugInfo()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        Debug.Log($"Name: {AuthenticationService.Instance.PlayerName}");
    }

    // ==================== UI HELPERS ====================
    private void ShowSignUpPanel()
    {
        signUpPanel.SetActive(true);
        signInPanel.SetActive(false);
        signOutButton.gameObject.SetActive(false);
    }

    private void ShowSignInPanel()
    {
        signUpPanel.SetActive(false);
        signInPanel.SetActive(true);
        signOutButton.gameObject.SetActive(false);
    }

    private void ShowSignedInState()
    {
        signUpPanel.SetActive(false);
        signInPanel.SetActive(false);
        signOutButton.gameObject.SetActive(true);
        statusText.text = $"Signed in as {AuthenticationService.Instance.PlayerName}";
    }

    private void DisableAllButtons()
    {
        signUpButton.interactable = false;
        signInButton.interactable = false;
        switchToSignInButton.interactable = false;
        switchToSignUpButton.interactable = false;
    }

    private void EnableAllButtons()
    {
        signUpButton.interactable = true;
        signInButton.interactable = true;
        switchToSignInButton.interactable = true;
        switchToSignUpButton.interactable = true;
    }
}