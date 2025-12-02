using UnityEngine;
using Unity.Services.Core;           // UnityServices
using Unity.Services.Authentication; // AuthenticationService
using System;

public class AuthTester : MonoBehaviour
{
    private async void Start()
    {
        Debug.Log("<color=orange>=== AUTH TEST STARTED ===</color>");

        // CORRECT WAY in 2024-2025
        if (UnityServices.State != ServicesInitializationState.Initialized)  // ← ServicesInitializationState (static)
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
        }
        Debug.Log("UnityServices initialized");

        // Already signed in?
        if (AuthenticationService.Instance.IsSignedIn)
        {
            PrintSuccess();
            return;
        }

        // Subscribe to events (new 2.0+ signatures)
        AuthenticationService.Instance.SignedIn += OnSignedIn;           // no parameters
        AuthenticationService.Instance.SignInFailed += OnSignInFailed;   // RequestFailedException
        AuthenticationService.Instance.SignedOut += OnSignedOut;

        Debug.Log("Waiting for sign-in from your friend's scripts...");
    }

    private void OnSignedIn()  // ← no parameters!
    {
        AuthenticationService.Instance.SignedIn -= OnSignedIn;
        PrintSuccess();
    }

    private void OnSignInFailed(RequestFailedException exception)
    {
        Debug.LogError($"SIGN-IN FAILED: {exception.Message} (Code: {exception.ErrorCode})");
    }

    private void OnSignedOut()
    {
        Debug.Log("Player signed out");
    }

    private void PrintSuccess()
    {
        Debug.Log("<color=green>=== AUTH WORKS PERFECTLY ===</color>");
        Debug.Log($"PlayerId : {AuthenticationService.Instance.PlayerId}");
        Debug.Log($"AccessToken preview : {AuthenticationService.Instance.AccessToken.Substring(0, Math.Min(50, AuthenticationService.Instance.AccessToken.Length))}...");
    }

    private void OnDestroy()
    {
        // Clean unsubscribe
        AuthenticationService.Instance.SignedIn -= OnSignedIn;
        AuthenticationService.Instance.SignInFailed -= OnSignInFailed;
        AuthenticationService.Instance.SignedOut -= OnSignedOut;
    }

    // BONUS: Button to force anonymous login (very useful for testing)
#if UNITY_EDITOR
    private async void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 280, 70), "<color=cyan><b>FORCE ANONYMOUS SIGN-IN</b></color>"))
        {
            Debug.Log("Forcing anonymous sign-in...");
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("Force sign-in failed: " + e.Message);
            }
        }
    }
#endif
}