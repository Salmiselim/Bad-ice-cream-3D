using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DestructibleBlock : MonoBehaviour
{
    [Header("Effects")]
    [Tooltip("Particle effect prefab (should contain a ParticleSystem).")]
    public GameObject destructionEffectPrefab;

    [Tooltip("Optional fragment prefab to spawn. If null the script will spawn small cube primitives.")]
    public GameObject fragmentPrefab;

    [Header("Fragments / Physics")]
    public int fragmentCount = 8;
    public float fragmentSize = 0.25f;
    public float explosionForce = 10f;
    public float explosionRadius = 1f;

    [Header("Destroy Timing")]
    public float objectDestroyDelay = 0.05f;

    bool _isDestroyed = false;
    Renderer _renderer;
    Collider _collider;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();

        if (_collider == null)
            Debug.LogWarning($"[DestructibleBlock] {name} has no Collider!");
    }

    // Public API: call this when you want the block to play its effect and destroy itself.
    public void TriggerDestruction()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        // Spawn particle effect (unparent so it survives)
        if (destructionEffectPrefab != null)
        {
            GameObject effect = Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
            effect.transform.parent = null;
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                float life = ps.main.duration;
                // try to add startLifetime safely
                try { life += ps.main.startLifetime.constantMax; } catch { life += 1f; }
                Destroy(effect, Mathf.Max(1f, life + 0.2f));
            }
            else
            {
                Destroy(effect, 5f);
            }
        }

        // disable visuals & collider immediately
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;

        // spawn fragments
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject frag = null;
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
            Quaternion rot = Random.rotation;

            if (fragmentPrefab != null)
            {
                frag = Instantiate(fragmentPrefab, spawnPos, rot);
                frag.transform.localScale = Vector3.one * fragmentSize;
            }
            else
            {
                frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frag.transform.position = spawnPos;
                frag.transform.rotation = rot;
                frag.transform.localScale = Vector3.one * fragmentSize;

                // copy material from this block if available
                if (_renderer != null)
                {
                    var fragRenderer = frag.GetComponent<Renderer>();
                    if (fragRenderer != null) fragRenderer.material = _renderer.material;
                }
            }

            Rigidbody rb = frag.GetComponent<Rigidbody>();
            if (rb == null) rb = frag.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            Destroy(frag, 3f);
        }

        // Notify GridManager in case the block was destroyed directly (idempotent)
   

        // destroy the block object after tiny delay
        Destroy(gameObject, objectDestroyDelay);
    }

    // Optional fallback if other code calls Destroy(gameObject) directly.
    // OnDestroy runs in many contexts (scene unload, editor); enable only if you want a minimal fallback.
    /*
    void OnDestroy()
    {
        if (!_isDestroyed && Application.isPlaying)
        {
            if (destructionEffectPrefab != null)
            {
                GameObject effect = Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
                Destroy(effect, 2f);
            }
        }
    }
    */
}
