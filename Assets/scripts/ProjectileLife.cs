using Unity.Netcode;
using UnityEngine;

public class ProjectileLife : NetworkBehaviour
{
    public float lifetime = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) // Only server handles destruction
        {
            StartCoroutine(DestroyAfterTime(lifetime));
        }
    }

    private System.Collections.IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        if (IsSpawned)
        {
            // Despawn and destroy the object
            NetworkObject.Despawn();
            Destroy(gameObject);
        }
    }
}
