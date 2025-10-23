using UnityEngine;

public class FruitSpin : MonoBehaviour
{
    public float spinSpeed = 90f; // Degrees per second

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }
}