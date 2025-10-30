using UnityEngine;
using System.Collections;

public class FruitCollection : MonoBehaviour
{
    [Header("Collection")]
    public float collectDuration = 0.25f;
    public int points = 10;

    private Collider col;
    private Renderer rend;
    private Material mat;
    private bool isCollecting = false;
    private GridManager gridManager;

    void Start()
    {
        col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = true;
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null) mat = new Material(rend.material);

        if (rend != null) rend.material = mat;

        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) Debug.LogError("FruitCollection: GridManager not found.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollecting) return;
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    public void Collect()
    {
        if (isCollecting) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive) return;

        isCollecting = true;
        if (col != null) col.enabled = false;

        // give points
        if (GameManager.Instance != null) GameManager.Instance.AddScore(points);

        // notify grid manager to remove/clean up
        if (gridManager != null)
        {
            Vector2Int gridPos = gridManager.WorldToGridPosition(transform.position);
            gridManager.CollectFruit(gridPos.x, gridPos.y);
        }

        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < collectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectDuration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (mat != null)
            {
                Color c = mat.HasProperty("_Color") ? mat.color : Color.white;
                c.a = Mathf.Lerp(1f, 0f, t);
                if (mat.HasProperty("_Color")) mat.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
