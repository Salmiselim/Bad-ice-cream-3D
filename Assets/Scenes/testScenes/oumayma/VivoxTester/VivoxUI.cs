using UnityEngine;
using UnityEngine.UI;

public class VivoxUI : MonoBehaviour
{
    public VivoxManager vivox;
    public InputField channelInput;

    public Button joinButton;
    public Button leaveButton;
    public Button muteButton;

    private bool isMuted = false;

    void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        muteButton.onClick.AddListener(OnMuteClicked);
    }

    void OnJoinClicked()
    {
        // Si ton JoinChannel attend un string (ton VivoxManager le fait), on envoie channelInput.text
        vivox.JoinChannel(channelInput.text);
    }

    void OnLeaveClicked()
    {
        // Ton VivoxManager a LeaveChannel() sans param — appelle-le ainsi
        vivox.LeaveChannel();
    }

    void OnMuteClicked()
    {
        isMuted = !isMuted;
        // Ton VivoxManager expose SetMicrophoneMuted(bool)
        vivox.SetMicrophoneMuted(isMuted);
    }
}
