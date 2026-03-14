using UnityEngine;

/// <summary>
/// Simple enemy AI that finds the player by tag on spawn and moves toward them each frame.
/// Uses <see cref="Vector3.MoveTowards"/> for movement and <see cref="Transform.LookAt"/> for rotation.
/// Attach to any enemy prefab that should chase the player.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeed = 3.5f;

    private Transform player;

    void Start()
    {
        // Automatically find the object tagged "Player" when the enemy spawns
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Enemy couldn't find the Player! Did you remember to set the Player tag?");
        }
    }

    void Update()
    {
        if (player != null)
        {
            // 1. Figure out where to look (keep the Y position flat so the enemy doesn't tilt up/down)
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);

            // 2. Face the player
            transform.LookAt(targetPosition);

            // 3. Move towards the player
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }
}