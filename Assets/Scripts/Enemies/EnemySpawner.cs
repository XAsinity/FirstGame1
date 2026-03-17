using UnityEngine;

/// <summary>
/// Spawns enemy prefabs in a circle around the player at a configurable rate and radius.
/// Enemies are placed on the ground plane at the player's Y position using <see cref="Random.insideUnitCircle"/>.
/// Stops spawning automatically if the player reference is lost.
///
/// When <see cref="arenaGenerator"/> is assigned the spawner automatically clamps spawn
/// positions inside the arena boundaries so enemies never appear outside the walls created
/// by <see cref="ArenaGenerator"/>.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform player;

    [Tooltip("How many seconds between each spawn")]
    public float spawnRate = 1.5f;

    [Tooltip("How far away from the player enemies spawn (keep this high enough to be off-screen)")]
    public float spawnRadius = 20f;

    [Header("Arena Integration (optional)")]
    [Tooltip("Assign the ArenaGenerator in the scene so spawn positions are clamped inside the arena walls. " +
             "Leave null if no ArenaGenerator is used.")]
    public ArenaGenerator arenaGenerator;

    [Tooltip("How far inside the arena boundary (from the wall) enemies are allowed to spawn (units).")]
    public float arenaSpawnMargin = 1.5f;

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

        // Clamp to arena bounds when ArenaGenerator is assigned, so enemies don't
        // appear outside the boundary walls.
        if (arenaGenerator != null)
        {
            float hw = arenaGenerator.arenaWidth  * 0.5f - arenaSpawnMargin;
            float hl = arenaGenerator.arenaLength * 0.5f - arenaSpawnMargin;
            spawnPosition.x = Mathf.Clamp(spawnPosition.x, -hw, hw);
            spawnPosition.z = Mathf.Clamp(spawnPosition.z, -hl, hl);
        }

        // Spawn the enemy!
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}