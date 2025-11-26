using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSignOutManager : MonoBehaviour
{
    [Header("UI References")]
    public Button signOutButton;
    public TextMeshProUGUI usernameText; // Display the username
    public TextMeshProUGUI statusText; // Optional: for displaying additional messages

    [Header("Scene Settings")]
    public string authSceneName = "Auth_selim";

    void Start()
    {
        // Setup button listener
        if (signOutButton != null)
        {
            signOutButton.onClick.AddListener(OnSignOutClicked);
        }
        else
        {
            Debug.LogError("Sign Out Button is not assigned in Inspector!");
        }

        // Check if user is signed in
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            DisplayUserInfo();
        }
        else
        {
            Debug.LogWarning("Not signed in! Redirecting to auth scene...");
            SceneManager.LoadScene(authSceneName);
        }
    }

    void DisplayUserInfo()
    {
        string playerName = AuthenticationService.Instance.PlayerName;
        string playerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Game scene loaded. Signed in as: {playerName} (ID: {playerId})");

        // Display username
        if (usernameText != null)
        {
            usernameText.text = $"Welcome, {playerName}!";
        }
        else
        {
            Debug.LogWarning("Username Text is not assigned in Inspector!");
        }

        // Optional: Display additional status
        if (statusText != null)
        {
            statusText.text = $"Player ID: {playerId}";
        }
    }

    public void OnSignOutClicked()
    {
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            string playerName = AuthenticationService.Instance.PlayerName;

            // Sign out
            AuthenticationService.Instance.SignOut();

            Debug.Log($"✓ Signed out {playerName}");

            if (usernameText != null)
            {
                usernameText.text = "Signing out...";
            }
        }

        // Return to auth scene
        Debug.Log($"Returning to {authSceneName}");
        SceneManager.LoadScene(authSceneName);
    }

    // Optional: Add keyboard shortcut for testing
    void Update()
    {
        // Press ESC to sign out (optional - remove if you don't want this)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC pressed - Signing out");
            OnSignOutClicked();
        }
    }
}