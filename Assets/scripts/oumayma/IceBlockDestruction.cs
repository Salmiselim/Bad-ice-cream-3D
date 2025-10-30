using UnityEngine;
using System.Collections;

public class IceBlockDestruction : MonoBehaviour
{
    [Header("Destruction Settings")]
    public float shrinkDuration = 0.25f;
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Effects")]
    public bool addRotation = true;
    public float rotationSpeed = 180f;

    private bool hasNotified = false;

    public void Shatter()
    {
        Debug.Log("Shatter() called on " + gameObject.name);
        StartCoroutine(ShatterEffect());
    }

    private IEnumerator ShatterEffect()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;

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
            float curveValue = shrinkCurve.Evaluate(t);

            transform.localScale = startScale * curveValue;

            if (addRotation)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            if (mat != null && mat.HasProperty("_Color"))
            {
                Color newColor = startColor;
                newColor.a = curveValue;
                mat.color = newColor;
            }

            yield return null;
        }

        // --- Notify GridManager once ---
        if (!hasNotified && GridManager.Instance != null)
        {
            hasNotified = true;

            Vector3 pos = transform.position;
            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(pos);

            // mark this block destroyed in grid
            GridManager.Instance.NotifyIceBlockDestroyed(gridPos.x, gridPos.y);

            // try to spawn Phase 2 fruit if prefab exists
            if (GridManager.Instance.fruitPrefabPhase2 != null)
            {
                GameObject fruit = Object.Instantiate(
                    GridManager.Instance.fruitPrefabPhase2,
                    GridManager.Instance.GridToWorldPosition(gridPos.x, gridPos.y),
                    Quaternion.identity,
                    GridManager.Instance.transform
                );
                GridManager.Instance.AddFruit(gridPos.x, gridPos.y, fruit);
            }

            Debug.Log($"Ice block at {gridPos} destroyed — spawning Phase 2 fruit.");
        }

        Destroy(gameObject);
    }
}
