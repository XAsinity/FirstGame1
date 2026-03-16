using System.Collections;
using UnityEngine;

/// <summary>
/// VFX script that simulates a ground rupture wave spreading outward in a cone shape.
/// Spawns rows of placeholder cubes that appear progressively further from the origin,
/// like the ground cracking/erupting outward (Breach ult / anime ground slam style).
///
/// The wave speed here should roughly match AbilityData.waveSpeed so the VFX and damage
/// are visually synced. Tweak waveSpeed and waveRows to taste.
///
/// PLACEHOLDER: The cubes are stand-in visuals. When real VFX are ready, replace this
/// script on the prefab — the Character.cs spawning code doesn't need to change.
///
/// SETUP:
/// 1. Create an empty GameObject in Unity
/// 2. Add this GroundSlamWaveVFX component
/// 3. Save as prefab, assign to GroundSlam ability's effectPrefab field
/// </summary>
public class GroundSlamWaveVFX : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("How fast the wave front travels outward (should roughly match AbilityData.waveSpeed).")]
    public float waveSpeed = 12f;

    [Tooltip("Total distance the wave travels.")]
    public float waveDistance = 8f;

    [Tooltip("Half-angle of the cone in degrees (should match AbilityData.coneHalfAngle).")]
    public float coneHalfAngle = 40f;

    [Header("Chunk Settings")]
    [Tooltip("Number of rows of chunks spawned as the wave travels outward.")]
    [Range(4, 20)]
    public int waveRows = 8;

    [Tooltip("Number of chunks per row (spread across the cone width at that distance).")]
    [Range(2, 10)]
    public int chunksPerRow = 4;

    [Tooltip("Base scale of each chunk cube.")]
    public float chunkScale = 0.5f;

    [Tooltip("How long each chunk stays visible before fading/destroying.")]
    public float chunkLifetime = 0.8f;

    [Tooltip("How high chunks pop up on spawn (simulates ground erupting).")]
    public float chunkPopHeight = 0.6f;

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        // Time between each row spawning, based on wave speed and distance
        const float minWaveSpeed = 0.1f; // guard against division by zero or near-zero speed
        float totalTime = waveDistance / Mathf.Max(waveSpeed, minWaveSpeed);
        float rowDelay = totalTime / Mathf.Max(waveRows, 1);

        for (int row = 0; row < waveRows; row++)
        {
            // This row's distance from the caster (0 = at feet, waveDistance = max range)
            float t = (float)(row + 1) / waveRows;
            float rowDist = t * waveDistance;

            for (int c = 0; c < chunksPerRow; c++)
            {
                // Spread chunks across the cone width at this distance
                float lateralT = chunksPerRow > 1 ? (float)c / (chunksPerRow - 1) : 0.5f;
                float angleOffset = Mathf.Lerp(-coneHalfAngle, coneHalfAngle, lateralT);

                // Add small random jitter
                angleOffset += Random.Range(-4f, 4f);
                float distJitter = Random.Range(-0.3f, 0.3f);

                Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * transform.forward;
                Vector3 spawnPos = transform.position + dir * (rowDist + distJitter);
                spawnPos.y = transform.position.y; // ground level

                // Create a cube chunk
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"GroundChunk_r{row}_c{c}";

                // Remove collider immediately
                var col = chunk.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Position and random scale
                chunk.transform.position = spawnPos;
                float scale = chunkScale + Random.Range(-0.15f, 0.15f);
                // Chunks further away are slightly larger (perspective of spreading)
                scale *= Mathf.Lerp(0.7f, 1.3f, t);
                chunk.transform.localScale = new Vector3(scale, scale * 0.5f, scale); // flat-ish
                chunk.transform.rotation = Quaternion.Euler(
                    Random.Range(-15f, 15f),
                    Random.Range(0f, 360f),
                    Random.Range(-15f, 15f)
                );

                // Add the eruption behavior
                var eruption = chunk.AddComponent<GroundChunkEruption>();
                eruption.popHeight = chunkPopHeight + Random.Range(-0.1f, 0.2f);
                eruption.lifetime = chunkLifetime + Random.Range(-0.1f, 0.2f);
            }

            yield return new WaitForSeconds(rowDelay);
        }

        // Wait for last chunks to finish, then self-destruct
        yield return new WaitForSeconds(chunkLifetime + 0.3f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Makes a single ground chunk pop up from the ground then settle/shrink away.
    /// Simulates a piece of ground erupting upward then falling back.
    /// </summary>
    private class GroundChunkEruption : MonoBehaviour
    {
        public float popHeight = 0.6f;
        public float lifetime = 0.8f;

        private Vector3 startPos;
        private Vector3 originalScale;
        private float elapsed;

        void Start()
        {
            startPos = transform.position;
            originalScale = transform.localScale;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            // Pop up quickly then settle back down (like ground erupting)
            // Peak at t=0.2, back to ground by t=0.6, then just sit there shrinking
            float popT;
            if (t < 0.2f)
                popT = t / 0.2f; // rise
            else if (t < 0.6f)
                popT = 1f - ((t - 0.2f) / 0.4f); // fall back
            else
                popT = 0f; // on ground

            transform.position = startPos + Vector3.up * (popHeight * popT);

            // Shrink away in the last 40% of lifetime
            if (t > 0.6f)
            {
                float shrinkT = (t - 0.6f) / 0.4f;
                transform.localScale = originalScale * Mathf.Lerp(1f, 0f, shrinkT);
            }

            if (elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}

