using System.Collections;
using UnityEngine;

/// <summary>
/// VFX script that simulates a ground rupture wave spreading outward in a cone shape.
/// Spawns rows of placeholder cubes that appear progressively further from the origin,
/// like the ground cracking/erupting outward (Breach ult / anime ground slam style).
///
/// Each chunk spawns slightly BEHIND its target position and slides outward to it,
/// giving the impression of a shockwave pushing debris forward.
///
/// The wave speed here should roughly match AbilityData.waveSpeed so the VFX and damage
/// are visually synced. Tweak waveSpeed and waveRows to taste.
///
/// PLACEHOLDER: The cubes are stand-in visuals. When real VFX are ready, replace this
/// prefab — the Character.cs spawning code doesn't need to change.
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

    [Tooltip("How long each chunk stays visible before shrinking and destroying.")]
    public float chunkLifetime = 0.8f;

    [Tooltip("How high chunks pop up on spawn (simulates ground erupting).")]
    public float chunkPopHeight = 0.6f;

    [Tooltip("How far behind its target position each chunk starts (slides forward from here).")]
    public float slideDistance = 1.5f;

    [Header("Jitter / Randomness")]
    [Tooltip("Random angle spread added to each chunk direction (degrees). Adds organic variation.")]
    public float angleJitter = 4f;

    [Tooltip("Random distance spread added to each chunk position (units). Adds organic variation.")]
    public float distanceJitter = 0.3f;

    [Tooltip("Fraction of a chunk's lifetime spent sliding to its target position (0–1). Lower = snappier slide.")]
    [Range(0.1f, 0.9f)]
    public float slideDurationFraction = 0.4f;

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        Vector3 waveOrigin = transform.position;
        Vector3 waveForward = transform.forward;

        // Time between each row spawning, based on wave speed and distance
        const float minWaveSpeed = 0.1f;
        float totalTime = waveDistance / Mathf.Max(waveSpeed, minWaveSpeed);
        float rowDelay = totalTime / Mathf.Max(waveRows, 1);

        for (int row = 0; row < waveRows; row++)
        {
            float t = (float)(row + 1) / waveRows;
            float rowDist = t * waveDistance;

            for (int c = 0; c < chunksPerRow; c++)
            {
                // Spread chunks across the cone width at this distance
                float lateralT = chunksPerRow > 1 ? (float)c / (chunksPerRow - 1) : 0.5f;
                float angleOffset = Mathf.Lerp(-coneHalfAngle, coneHalfAngle, lateralT);
                angleOffset += Random.Range(-angleJitter, angleJitter);
                float distJitter = Random.Range(-distanceJitter, distanceJitter);

                Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * waveForward;

                // Target position: where the chunk should end up
                Vector3 targetPos = waveOrigin + dir * (rowDist + distJitter);
                targetPos.y = waveOrigin.y;

                // Start position: slightly behind the target (chunk slides outward)
                Vector3 startPos = waveOrigin + dir * Mathf.Max(0f, rowDist - slideDistance + distJitter);
                startPos.y = waveOrigin.y;

                // Create a cube chunk
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"GroundChunk_r{row}_c{c}";

                // Remove collider immediately
                var col = chunk.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Position at start, random scale
                chunk.transform.position = startPos;
                float scale = chunkScale + Random.Range(-0.15f, 0.15f);
                scale *= Mathf.Lerp(0.7f, 1.3f, t); // further chunks slightly larger
                chunk.transform.localScale = new Vector3(scale, scale * 0.5f, scale);
                chunk.transform.rotation = Quaternion.Euler(
                    Random.Range(-15f, 15f),
                    Random.Range(0f, 360f),
                    Random.Range(-15f, 15f)
                );

                // Add the eruption + slide behavior
                var eruption = chunk.AddComponent<GroundChunkEruption>();
                eruption.startPos = startPos;
                eruption.targetPos = targetPos;
                eruption.popHeight = chunkPopHeight + Random.Range(-0.1f, 0.2f);
                eruption.lifetime = chunkLifetime + Random.Range(-0.1f, 0.2f);
                eruption.slideDurationFraction = slideDurationFraction;
            }

            yield return new WaitForSeconds(rowDelay);
        }

        // Wait for last chunks to finish, then self-destruct
        yield return new WaitForSeconds(chunkLifetime + 0.3f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Makes a single ground chunk slide outward from start to target while popping up
    /// from the ground, then settling and shrinking away.
    /// Simulates a piece of ground being pushed outward by a shockwave.
    /// </summary>
    public class GroundChunkEruption : MonoBehaviour
    {
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 targetPos;
        public float popHeight = 0.6f;
        public float lifetime = 0.8f;
        [HideInInspector] public float slideDurationFraction = 0.4f;

        private Vector3 originalScale;
        private float elapsed;

        void Start()
        {
            originalScale = transform.localScale;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            // Slide outward from start to target (fast at first, easing out)
            float slideFraction = Mathf.Max(slideDurationFraction, 0.01f);
            float slideT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / slideFraction), 2f); // ease-out
            Vector3 basePos = Vector3.Lerp(startPos, targetPos, slideT);

            // Pop up quickly then settle back down
            float popT;
            if (t < 0.15f)
                popT = t / 0.15f; // rise fast
            else if (t < 0.45f)
                popT = 1f - ((t - 0.15f) / 0.3f); // fall back
            else
                popT = 0f; // on ground

            transform.position = basePos + Vector3.up * (popHeight * popT);

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

