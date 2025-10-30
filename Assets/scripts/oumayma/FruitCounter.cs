using UnityEngine;

public class FruitCounter : MonoBehaviour
{
    void Start()
    {
        FruitCollection[] fruits = FindObjectsOfType<FruitCollection>();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.totalFruits = fruits.Length;
            Debug.Log($"Total Fruits set to: {fruits.Length}");
        }
    }
}
