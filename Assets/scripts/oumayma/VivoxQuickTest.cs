using UnityEngine;
using Unity.Services.Vivox;
using System.Threading.Tasks;

public class VivoxQuickTest : MonoBehaviour
{
    private bool loggedIn = false;

    private async void Start()
    {
        Debug.Log("<color=cyan>=== VIVOX TEST MODE ===</color>");

        try
        {
            await VivoxService.Instance.InitializeAsync();
            Debug.Log("<color=lime>Vivox initialized</color>");

            var options = new LoginOptions { DisplayName = "Player_" + Random.Range(100, 999) };
            await VivoxService.Instance.LoginAsync(options);
            loggedIn = true;
            Debug.Log("<color=lime>Vivox login OK !</color>");

            // Test micro immédiat
            await VivoxService.Instance.JoinEchoChannelAsync("test", ChatCapability.AudioOnly);
            Debug.Log("<color=yellow>PARLE DANS TON MICRO → TU DOIS T’ENTENDRE !</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vivox failed: " + e);
        }
    }

    private async void OnDestroy()
    {
        if (loggedIn)
            await VivoxService.Instance.LogoutAsync();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 50), "<color=orange><b>Si tu ne t’entends pas → Redémarre Unity après avoir activé Test Mode !</b></color>");
        if (GUI.Button(new Rect(10, 70, 300, 60), "<b>Rejoindre canal groupé 'Lobby42'</b>"))
        {
            VivoxService.Instance.JoinGroupChannelAsync("Lobby42", ChatCapability.TextAndAudio);
        }
    }
#endif
}