using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform player;

    [Tooltip("How many seconds between each spawn")]
    public float spawnRate = 1.5f;

    [Tooltip("How far away from the player enemies spawn (keep this high enough to be off-screen)")]
    public float spawnRadius = 20f;

    private float timer;

    void Update()
    {
        // If the player is missing (or dead), stop spawning
        if (player == null) return;

        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            SpawnEnemy();
            timer = 0f; // Reset the timer
        }
    }

    void SpawnEnemy()
    {
        // Pick a random point on the edge of an invisible circle
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;

        // Convert that 2D point into 3D space around the player's current position
        Vector3 spawnPosition = new Vector3(
            player.position.x + randomCircle.x,
            player.position.y,
            player.position.z + randomCircle.y
        );

        // Spawn the enemy!
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}