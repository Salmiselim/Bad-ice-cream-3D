using UnityEngine;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class GoogleSignIn : MonoBehaviour
{
    public async void SignInWithGoogle(string idToken)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);

            Debug.Log("Signed in with Google successfully!");
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException e)
        {
            Debug.LogError("Sign-in failed: " + e.Message);
        }
    }
}
