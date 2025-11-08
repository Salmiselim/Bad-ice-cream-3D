using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    private PlayerController localController;

    private void Awake()
    {
        // Get the existing PlayerController
        localController = GetComponent<PlayerController>();

        // Disable it by default so only the owner uses it
        if (localController != null)
            localController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Enable player control ONLY for the local owner
        if (IsOwner && localController != null)
        {
            localController.enabled = true;
        }
    }
}
