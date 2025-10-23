using UnityEngine;

public class IceBlockAnimation : MonoBehaviour
{
    public float popInDuration = 0.2f;

    void Start()
    {
        // Start small and grow
        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    System.Collections.IEnumerator PopIn()
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < popInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popInDuration;

            // Ease out effect
            t = 1 - Mathf.Pow(1 - t, 3);

            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }
}