using UnityEngine;

public class IceBlockDestruction : MonoBehaviour
{
    [Header("Destruction Settings")]
    public float shrinkDuration = 0.25f;
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Effects")]
    public bool addRotation = true;
    public float rotationSpeed = 180f;

    public void Shatter()
    {
        StartCoroutine(ShatterEffect());
    }

    System.Collections.IEnumerator ShatterEffect()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;

        // Optional: Get material for fade effect
        Renderer renderer = GetComponent<Renderer>();
        Material mat = null;
        Color startColor = Color.white;

        if (renderer != null)
        {
            mat = renderer.material;
            if (mat.HasProperty("_Color"))
            {
                startColor = mat.color;
            }
        }

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;

            // Use animation curve for smoother shrinking
            float curveValue = shrinkCurve.Evaluate(t);

            // Shrink
            transform.localScale = startScale * curveValue;

            // Optional: Rotate while shrinking
            if (addRotation)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            // Optional: Fade out
            if (mat != null && mat.HasProperty("_Color"))
            {
                Color newColor = startColor;
                newColor.a = curveValue;
                mat.color = newColor;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}