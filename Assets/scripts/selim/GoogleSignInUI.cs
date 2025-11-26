using UnityEngine;
using UnityEngine.UI;

public class GoogleSignInUI : MonoBehaviour
{
    public GoogleSignIn googleSignInScript;
    public InputField tokenInput; 
    public Text logText;

    public void OnClickSignIn()
    {
        string token = tokenInput.text;
        if (!string.IsNullOrEmpty(token))
        {
            googleSignInScript.SignInWithGoogle(token);
            logText.text = "Signing in...";
        }
        else
        {
            logText.text = "ID Token is empty!";
        }
    }
}
