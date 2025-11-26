using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class Authmanager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusText;

    [Header("Sign Up Panel")]
    public GameObject signUpPanel;
    public TMP_InputField signUpUsernameInput;
    public TMP_InputField signUpPasswordInput;
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
    public Button emergencySignOutButton; // Always visible emergency sign out

    [Header("Testing")]
    public bool forceSignOutOnStart = true; // ENABLED by default for testing
    public Button debugButton; // Optional: Add a debug button to show player info

    async void Start()
    {
        // Show loading
        statusText.text = "Initializing...";

        // Initialize Unity Services
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
        }
        else
        {
            statusText.text = "Ready! Please sign up or sign in.";
            ShowSignUpPanel();
        }

        // Setup button listeners
        signUpButton.onClick.AddListener(OnSignUpClicked);
        signInButton.onClick.AddListener(OnSignInClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        switchToSignInButton.onClick.AddListener(ShowSignInPanel);
        switchToSignUpButton.onClick.AddListener(ShowSignUpPanel);

        // Emergency sign out (always available)
        if (emergencySignOutButton != null)
        {
            emergencySignOutButton.onClick.AddListener(OnSignOutClicked);
            emergencySignOutButton.gameObject.SetActive(true); // Always visible
        }

        // Optional debug button
        if (debugButton != null)
        {
            debugButton.onClick.AddListener(ShowDebugInfo);
        }
    }

    // Debug: Show all available player info
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

        // Get additional player info
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

    // ==================== SIGN UP ====================

    public async void OnSignUpClicked()
    {
        string username = signUpUsernameInput.text;
        string password = signUpPasswordInput.text;

        // Check if already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            string currentUsername = AuthenticationService.Instance.PlayerName;
            statusText.text = $"Already signed in as: {currentUsername}\nPlease sign out first.";
            ShowSignedInState();
            return;
        }

        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            return;
        }

        // Validate password requirements
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
            // Sign up with username and password
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            Debug.Log("✓ Sign up successful!");
            statusText.text = "Account created! Signing in...";

          
            Debug.Log("✓ Sign in successful!");
            ShowSignedInState();
        }
        catch (AuthenticationException ex)
        {
            // Handle specific errors
            Debug.LogError("Sign up failed: " + ex.Message);

            // Check error message for specific errors
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

        // Check if already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            string currentUsername = AuthenticationService.Instance.PlayerName;
            statusText.text = $"Already signed in as: {currentUsername}\nPlease sign out first.";
            ShowSignedInState();
            return;
        }

        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            return;
        }

        statusText.text = "Signing in...";
        DisableAllButtons();

        try
        {
            // Sign in with username and password
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            Debug.Log("✓ Sign in successful!");
            ShowSignedInState();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Sign in failed: " + ex.Message);

            // Check error message for specific errors
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

        // Clear input fields
        signUpUsernameInput.text = "";
        signUpPasswordInput.text = "";
        signInUsernameInput.text = "";
        signInPasswordInput.text = "";

        ShowSignUpPanel();
    }

    // ==================== UPDATE PASSWORD ====================

    public async void OnUpdatePasswordClicked(string oldPassword, string newPassword)
    {
        statusText.text = "Updating password...";

        try
        {
            await AuthenticationService.Instance.UpdatePasswordAsync(oldPassword, newPassword);

            Debug.Log("✓ Password updated!");
            statusText.text = "Password updated successfully!";
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Password update failed: " + ex.Message);
            statusText.text = "Failed to update password: " + ex.Message;
        }
    }

    // ==================== UI MANAGEMENT ====================

    void ShowSignUpPanel()
    {
        signUpPanel.SetActive(true);
        signInPanel.SetActive(false);
        signOutButton.gameObject.SetActive(false);
        switchToSignInButton.gameObject.SetActive(true);
        switchToSignUpButton.gameObject.SetActive(false);
    }

    void ShowSignInPanel()
    {
        signUpPanel.SetActive(false);
        signInPanel.SetActive(true);
        signOutButton.gameObject.SetActive(false);
        switchToSignInButton.gameObject.SetActive(false);
        switchToSignUpButton.gameObject.SetActive(true);
    }

    void ShowSignedInState()
    {
        // Hide sign up/in panels
        signUpPanel.SetActive(false);
        signInPanel.SetActive(false);

        // Hide switch buttons
        switchToSignInButton.gameObject.SetActive(false);
        switchToSignUpButton.gameObject.SetActive(false);

        // SHOW sign out button
        if (signOutButton != null)
        {
            signOutButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Sign Out Button is not assigned in Inspector!");
        }

        // Display player info
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