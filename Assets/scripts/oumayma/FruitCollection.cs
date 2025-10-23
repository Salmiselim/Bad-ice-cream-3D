using UnityEngine;

public class FruitCollection : MonoBehaviour
{
    [Header("Collection Settings")]
    public float collectDuration = 0.3f;
    public int points = 10;

    private Renderer fruitRenderer;
    private bool isCollecting = false;
    private Collider fruitCollider;
    private Material fruitMaterial;

    void Start()
    {
        // Try multiple ways to find the renderer
        fruitRenderer = GetComponent<Renderer>();
        if (fruitRenderer == null)
        {
            fruitRenderer = GetComponentInChildren<Renderer>();
        }

        // Get collider to disable it when collecting
        fruitCollider = GetComponent<Collider>();
        if (fruitCollider == null)
        {
            fruitCollider = GetComponentInChildren<Collider>();
        }

        // Create a copy of the material so we don't affect the original
        if (fruitRenderer != null)
        {
            fruitMaterial = fruitRenderer.material;
        }

        // Debug to see if everything is found
        if (fruitRenderer == null)
        {
            Debug.LogError($"No Renderer found on fruit: {gameObject.name}");
        }
        if (fruitCollider == null)
        {
            Debug.LogWarning($"No Collider found on fruit: {gameObject.name}");
        }
    }

    public void Collect()
    {
        if (isCollecting)
        {
            Debug.Log("Fruit already collecting, ignoring duplicate call");
            return;
        }

        isCollecting = true;

        // IMMEDIATELY disable collider to prevent double-collection
        if (fruitCollider != null)
        {
            fruitCollider.enabled = false;
            Debug.Log($"Disabled collider on fruit: {gameObject.name}");
        }

        StartCoroutine(CollectAnimation());
    }

    System.Collections.IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        // Check if material supports color changes
        bool canFade = false;
        Color startColor = Color.white;

        if (fruitMaterial != null)
        {
            // Try different common color properties
            if (fruitMaterial.HasProperty("_Color"))
            {
                startColor = fruitMaterial.color;
                canFade = true;
            }
            else if (fruitMaterial.HasProperty("_BaseColor"))
            {
                startColor = fruitMaterial.GetColor("_BaseColor");
                canFade = true;
            }
        }

        while (elapsed < collectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectDuration;

            // Scale down with ease-in
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Fade out (only if material supports it)
            if (canFade && fruitMaterial != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, t);

                if (fruitMaterial.HasProperty("_Color"))
                {
                    fruitMaterial.color = newColor;
                }
                else if (fruitMaterial.HasProperty("_BaseColor"))
                {
                    fruitMaterial.SetColor("_BaseColor", newColor);
                }
            }

            yield return null;
        }

        Debug.Log($"Destroying fruit: {gameObject.name}");
        Destroy(gameObject);
    }
}