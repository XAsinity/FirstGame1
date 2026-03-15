using System.Collections;
using UnityEngine;

/// <summary>
/// Simple cone wave VFX using white cube primitives to simulate ground chunks shattering forward.
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in Unity (GameObject > Create Empty).
/// 2. Add the GroundSlamWaveVFX component to it.
/// 3. Save as prefab at: Assets/CharacterData/Barbarian/Abilities/GroundSlamWavePrefab.prefab
/// 4. Assign the prefab to the GroundSlam ability's "Effect Prefab" field in the Inspector.
///
/// The script spawns ~8–12 small white cubes in a fan/cone pattern from the spawn point.
/// Cubes fly outward and upward, spread apart in a cone shape, then self-destroy after ~1 second.
/// The parent GameObject also destroys itself once all cubes are gone.
/// </summary>
public class GroundSlamWaveVFX : MonoBehaviour
{
    [Tooltip("Number of cube chunks to spawn in the cone fan.")]
    [Range(6, 16)]
    public int chunkCount = 10;

    [Tooltip("Half-angle of the fan cone in degrees.")]
    [Range(10f, 90f)]
    public float coneHalfAngle = 40f;

    [Tooltip("How far the chunks travel outward.")]
    public float travelDistance = 8f;

    [Tooltip("How long each chunk lives before destroying itself (seconds).")]
    public float chunkLifetime = 0.9f;

    [Tooltip("Speed at which chunks move outward.")]
    public float chunkSpeed = 10f;

    [Tooltip("Scale of each cube chunk.")]
    public float chunkScale = 0.4f;

    void Start()
    {
        StartCoroutine(SpawnChunks());
    }

    private IEnumerator SpawnChunks()
    {
        for (int i = 0; i < chunkCount; i++)
        {
            // Distribute chunks evenly across the cone angle
            float t = chunkCount > 1 ? (float)i / (chunkCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-coneHalfAngle, coneHalfAngle, t);

            // Small random jitter so chunks don't look too uniform
            angle += Random.Range(-3f, 3f);

            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            // Slight random Y offset to look like ground chunks being thrown upward
            float yOffset = Random.Range(0f, 0.3f);
            Vector3 spawnPos = transform.position + new Vector3(0f, yOffset, 0f);

            // Create a small white cube
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.transform.position = spawnPos;
            chunk.transform.localScale = Vector3.one * (chunkScale + Random.Range(-0.1f, 0.1f));
            chunk.transform.rotation = Random.rotation;

            // Remove the collider so chunks don't interfere with gameplay
            Destroy(chunk.GetComponent<Collider>());

            // Drive the chunk with a simple mover
            ChunkMover mover = chunk.AddComponent<ChunkMover>();
            mover.direction = (dir + Vector3.up * Random.Range(0.1f, 0.4f)).normalized;
            mover.speed = chunkSpeed + Random.Range(-2f, 2f);
            mover.lifetime = chunkLifetime;

            // Stagger chunk spawns slightly for a wave feel
            yield return new WaitForSeconds(0.03f);
        }

        // Wait for the longest-lived chunk then destroy this VFX object
        yield return new WaitForSeconds(chunkLifetime + 0.2f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Internal component that moves a single chunk outward and destroys it after its lifetime.
    /// </summary>
    private class ChunkMover : MonoBehaviour
    {
        public Vector3 direction;
        public float speed;
        public float lifetime;

        private float _elapsed;

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Slow down over time for a natural deceleration feel
            float t = _elapsed / lifetime;
            float currentSpeed = Mathf.Lerp(speed, 0f, t);
            transform.position += direction * currentSpeed * Time.deltaTime;

            // Slight tumble rotation for extra visual flair
            transform.Rotate(Vector3.one * 180f * Time.deltaTime, Space.Self);
        }
    }
}
