using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private bool isAlive = true; // Tracks if the player is alive

    public void KillPlayer()
    {
        if (isAlive)
        {
            isAlive = false;
            // Disable player movement (assumes PlayerController script)
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
            // Disable Rigidbody physics to stop movement
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true; // Stops physics interactions
            }
            // Placeholder for game over logic
            GameOver();
        }
    }

    private void GameOver()
    {
        // Placeholder for game over system (to be implemented later)
        Debug.Log("Game Over! Player has been killed.");
        // Add your game over logic here later (e.g., show UI, restart scene)
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}
