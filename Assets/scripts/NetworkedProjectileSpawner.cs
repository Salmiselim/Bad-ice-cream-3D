using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedProjectileSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public GameObject ballPrefab;
    public Transform spawnPoint;  

    [Header("Ball Properties")]
    public float ballForce = 10f;

 
    private void Update()
    {
        if (!IsOwner) return; 

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            SpawnBallServerRpc();
        }
    }

    [ServerRpc]
    private void SpawnBallServerRpc()
    {
       
        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject ballNetworkObject = ball.GetComponent<NetworkObject>();
        ballNetworkObject.Spawn();
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.AddForce(spawnPoint.forward * ballForce, ForceMode.Impulse);
        }
       // InitializeBallClientRpc(ballNetworkObject.NetworkObjectId);
    }

    [ClientRpc]
    private void InitializeBallClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject ballNetworkObject))
        {
            
        }
    }
}
