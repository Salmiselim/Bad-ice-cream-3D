using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f; // Speed of the enemy (set in NavMeshAgent)
    [SerializeField] private float stoppingDistance = 1.5f; // Distance at which the enemy stops moving toward the player
    private NavMeshAgent navAgent; // Reference to the NavMeshAgent component
    private Transform player; // Reference to the player's transform

    void Start()
    {
        // Get the NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogWarning("NavMeshAgent not found on enemy: " + gameObject.name);
            enabled = false; // Disable the script to prevent errors
            return;
        }

        // Set NavMeshAgent properties
        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;

        // Find the player by tag (ensure the player GameObject has the "Player" tag)
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player == null)
        {
            Debug.LogWarning("Player not found for enemy: " + gameObject.name);
            enabled = false; // Disable the script to prevent errors
        }
    }

    void Update()
    {
        if (player == null || navAgent == null) return;

        // Set the NavMeshAgent destination to the player's position
        navAgent.SetDestination(player.position);

        // Optional: Make the enemy face the player (only rotates on Y-axis for natural look)
        if (navAgent.remainingDistance > stoppingDistance)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Keep rotation on Y-axis only to avoid tilting
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * navAgent.angularSpeed);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is the player
        if (other.CompareTag("Player"))
        {
            // Get the PlayerHealth component and kill the player
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.KillPlayer();
            }
        }
    }
}