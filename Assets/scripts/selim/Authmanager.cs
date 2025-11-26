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

    [Header("Scene Transition")]
    public string gameSceneName = "GameScene";
    public float transitionDelay = 1.5f;

    [Header("Testing")]
    public bool forceSignOutOnStart = true;
    public Button debugButton;

    async void Start()
    {
        statusText.text = "Initializing...";

        await UnityServices.InitializeAsync();

        // FOR TESTING: Force sign out if checkbox is enabled
        if (forceSignOutOnStart && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Forced sign out for testing");
        }

        // Check if already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            ShowSignedInState();
            // Automatically transition to game scene
            await TransitionToGameScene();
        }
        else
        {
            statusText.text = "Ready! Please sign up or sign in.";
            ShowSignUpPanel();
            EnableAllButtons();
        }

        // Setup button listeners
        signUpButton.onClick.AddListener(OnSignUpClicked);
        signInButton.onClick.AddListener(OnSignInClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        switchToSignInButton.onClick.AddListener(ShowSignInPanel);
        switchToSignUpButton.onClick.AddListener(ShowSignUpPanel);

        if (emergencySignOutButton != null)
        {
            emergencySignOutButton.onClick.AddListener(OnSignOutClicked);
            emergencySignOutButton.gameObject.SetActive(true);
        }

        if (debugButton != null)
        {
            debugButton.onClick.AddListener(ShowDebugInfo);
        }
    }

    void Update()
    {
        // Press ESC key to sign out (for testing)
        if (Input.GetKeyDown(KeyCode.Escape) && AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("ESC pressed - Signing out");
            OnSignOutClicked();
        }
    }

    // ==================== SCENE TRANSITION ====================

    async Task TransitionToGameScene()
    {
        statusText.text = $"Loading {gameSceneName}...";
        Debug.Log($"Transitioning to {gameSceneName} in {transitionDelay} seconds");

        await Task.Delay((int)(transitionDelay * 1000));

        SceneManager.LoadScene(gameSceneName);
    }

    // ==================== SIGN UP ====================

    public async void OnSignUpClicked()
    {
        string username = signUpUsernameInput.text;
        string password = signUpPasswordInput.text;
        string confirmPassword = signUpConfirmPasswordInput.text;

        if (AuthenticationService.Instance.IsSignedIn)
        {
            string currentUsername = AuthenticationService.Instance.PlayerName;
            statusText.text = $"Already signed in as: {currentUsername}\nPlease sign out first.";
            ShowSignedInState();
            return;
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            statusText.text = "Please fill in all fields";
            return;
        }

        if (password != confirmPassword)
        {
            statusText.text = "Passwords do not match!";
            return;
        }

        if (password.Length < 8)
        {
            statusText.text = "Password must be at least 8 characters";
            return;
        }

        if (password.Length > 30)
        {
            statusText.text = "Password must be less than 30 characters";
            return;
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSymbol = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            if (char.IsLower(c)) hasLower = true;
            if (char.IsDigit(c)) hasDigit = true;
            if (!char.IsLetterOrDigit(c)) hasSymbol = true;
        }

        if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
        {
            statusText.text = "Password must contain:\n- Uppercase (A-Z)\n- Lowercase (a-z)\n- Number (0-9)\n- Symbol (!@#$%...)";
            return;
        }

        statusText.text = "Creating account...";
        DisableAllButtons();

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            Debug.Log("✓ Sign up successful!");
            statusText.text = "Account created! Setting up profile...";

            // IMPORTANT: Update the player name to the username
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
                Debug.Log($"✓ Player name set to: {username}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to set player name: {ex.Message}");
            }

            statusText.text = "✓ Signed in successfully!";

            // Transition to game scene
            await TransitionToGameScene();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Sign up failed: " + ex.Message);

            if (ex.Message.Contains("already exists") || ex.Message.Contains("already in use") || ex.Message.Contains("ENTITY_EXISTS"))
            {
                statusText.text = "❌ Username already exists!\nTry a different username or sign in.";
            }
            else if (ex.Message.Contains("invalid") || ex.Message.Contains("parameter"))
            {
                statusText.text = "❌ Invalid username or password format";
            }
            else
            {
                statusText.text = "❌ Sign up failed: " + ex.Message;
            }

            EnableAllButtons();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError("Request failed: " + ex.Message);
            statusText.text = "Connection error. Please try again.";
            EnableAllButtons();
        }
    }

    // ==================== SIGN IN ====================

    public async void OnSignInClicked()
    {
        string username = signInUsernameInput.text;
        string password = signInPasswordInput.text;

        if (AuthenticationService.Instance.IsSignedIn)
        {
            string currentUsername = AuthenticationService.Instance.PlayerName;
            statusText.text = $"Already signed in as: {currentUsername}\nPlease sign out first.";
            ShowSignedInState();
            return;
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            return;
        }

        statusText.text = "Signing in...";
        DisableAllButtons();

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            Debug.Log("✓ Sign in successful!");
            statusText.text = "✓ Signed in successfully!";

            // Transition to game scene
            await TransitionToGameScene();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Sign in failed: " + ex.Message);

            if (ex.Message.Contains("not found") || ex.Message.Contains("does not exist"))
            {
                statusText.text = "Account not found. Please sign up first.";
            }
            else if (ex.Message.Contains("password") || ex.Message.Contains("credentials") || ex.Message.Contains("invalid"))
            {
                statusText.text = "Wrong username or password";
            }
            else
            {
                statusText.text = "Sign in failed: " + ex.Message;
            }

            EnableAllButtons();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError("Request failed: " + ex.Message);
            statusText.text = "Connection error. Please try again.";
            EnableAllButtons();
        }
    }

    // ==================== SIGN OUT ====================

    public void OnSignOutClicked()
    {
        AuthenticationService.Instance.SignOut();

        Debug.Log("✓ Signed out");
        statusText.text = "Signed out";

        signUpUsernameInput.text = "";
        signUpPasswordInput.text = "";
        signUpConfirmPasswordInput.text = "";
        signInUsernameInput.text = "";
        signInPasswordInput.text = "";

        ShowSignUpPanel();
        EnableAllButtons();
    }

    // ==================== DEBUG ====================

    public async void ShowDebugInfo()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Not signed in!");
            return;
        }

        Debug.Log("=== PLAYER DEBUG INFO ===");
        Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        Debug.Log($"Username: {AuthenticationService.Instance.PlayerName}");
        Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken.Substring(0, 50)}...");

        try
        {
            var playerInfo = await AuthenticationService.Instance.GetPlayerInfoAsync();
            Debug.Log($"Created At: {playerInfo.CreatedAt}");
            Debug.Log($"Identity Providers: {playerInfo.Identities?.Count ?? 0}");

            if (playerInfo.Identities != null)
            {
                foreach (var identity in playerInfo.Identities)
                {
                    Debug.Log($"  - Provider: {identity.TypeId}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get player info: {e.Message}");
        }

        Debug.Log("========================");
    }

    // ==================== UI MANAGEMENT ====================

    void ShowSignUpPanel()
    {
        signUpPanel.SetActive(true);
        signInPanel.SetActive(false);
        signOutButton.gameObject.SetActive(false);
        switchToSignInButton.gameObject.SetActive(true);
        switchToSignUpButton.gameObject.SetActive(false);
        EnableAllButtons();
    }

    void ShowSignInPanel()
    {
        signUpPanel.SetActive(false);
        signInPanel.SetActive(true);
        signOutButton.gameObject.SetActive(false);
        switchToSignInButton.gameObject.SetActive(false);
        switchToSignUpButton.gameObject.SetActive(true);
        EnableAllButtons();
    }

    void ShowSignedInState()
    {
        signUpPanel.SetActive(false);
        signInPanel.SetActive(false);
        switchToSignInButton.gameObject.SetActive(false);
        switchToSignUpButton.gameObject.SetActive(false);

        if (signOutButton != null)
        {
            signOutButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Sign Out Button is not assigned in Inspector!");
        }

        string playerId = AuthenticationService.Instance.PlayerId;
        string playerName = AuthenticationService.Instance.PlayerName;

        statusText.text = $"✓ Connected!\n\nUsername: {playerName}\n\nPlayer ID:\n{playerId}";

        Debug.Log($"Player ID: {playerId}");
        Debug.Log($"Username: {playerName}");
    }

    void DisableAllButtons()
    {
        signUpButton.interactable = false;
        signInButton.interactable = false;
        switchToSignInButton.interactable = false;
        switchToSignUpButton.interactable = false;
    }

    void EnableAllButtons()
    {
        signUpButton.interactable = true;
        signInButton.interactable = true;
        switchToSignInButton.interactable = true;
        switchToSignUpButton.interactable = true;
    }
}