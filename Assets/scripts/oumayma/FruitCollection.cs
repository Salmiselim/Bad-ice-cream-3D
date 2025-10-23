using UnityEngine;

public class FruitCollection : MonoBehaviour
{
    [Header("Collection Settings")]
    public float collectDuration = 0.3f;
    public int points = 10; // Score value

    private Renderer fruitRenderer;
    private bool isCollecting = false;

    void Start()
    {
        fruitRenderer = GetComponentInChildren<Renderer>(); // Get the mesh renderer
    }

    public void Collect()
    {
        if (isCollecting) return;
        isCollecting = true;
        StartCoroutine(CollectAnimation());
    }

    System.Collections.IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Color startColor = fruitRenderer.material.color;

        while (elapsed < collectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectDuration;

            // Scale down with ease-in
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Fade out
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            fruitRenderer.material.color = newColor;

            yield return null;
        }

        Destroy(gameObject);
    }
}