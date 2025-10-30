using UnityEngine;

public class FruitSpin : MonoBehaviour
{
    public float spinSpeed = 90f; // Degrees per second

    void Update()
    {
        transform.Rotate(Vector3.up * 20f * Time.deltaTime, Space.World);
    }
}