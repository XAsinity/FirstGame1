using System.Collections;
using UnityEngine;

/// <summary>
/// VFX script that simulates a ground rupture wave spreading outward in a cone shape.
/// Spawns rows of placeholder cubes as the wave front advances, driven every frame by
/// Character.cs via SetWaveFront(). This keeps VFX and damage perfectly in sync — one
/// clock, one source of truth.
///
/// Wave dimensions (distance, cone angle, chunk scale) are set automatically by
/// Character.cs via InitFromAbility() using values from AbilityData. Wave speed is
/// not stored here — the wave front is fed in each frame by the damage coroutine.
/// Only visual tuning knobs (rows, lifetime, pop height, jitter, etc.) remain
/// editable on the prefab Inspector.
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
    // -----------------------------------------------------------------------
    // Set at runtime by Character.cs → InitFromAbility(). Never expose in
    // Inspector — they'd conflict with the driven model.
    // -----------------------------------------------------------------------
    private Vector3 waveOrigin;
    private Vector3 waveForward;
    private float waveDistance;
    private float coneHalfAngle;
    private float chunkScale;

    private int nextRow;
    private float[] rowDistances;
    private bool initialized;
    private bool waveComplete;

    [Header("Chunk Settings")]
    [Tooltip("Number of rows of chunks spawned as the wave travels outward.")]
    [Range(4, 20)]
    public int waveRows = 8;

    [Tooltip("Number of chunks per row (spread across the cone width at that distance).")]
    [Range(2, 10)]
    public int chunksPerRow = 4;

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

    /// <summary>
    /// Called by Character.cs after spawning this prefab to configure wave dimensions.
    /// Pre-computes per-row distance thresholds used by SetWaveFront().
    /// Wave speed is not needed here — the wave front is driven each frame by the damage coroutine.
    /// </summary>
    public void InitFromAbility(float abilityRange, float abilityConeHalfAngle)
    {
        waveOrigin = transform.position;
        waveForward = transform.forward;
        waveDistance = abilityRange;
        coneHalfAngle = abilityConeHalfAngle;
        chunkScale = Mathf.Max(0.2f, 0.5f * (abilityRange / 8f));

        rowDistances = new float[waveRows];
        for (int i = 0; i < waveRows; i++)
            rowDistances[i] = (float)(i + 1) / waveRows * waveDistance;

        nextRow = 0;
        initialized = true;
        waveComplete = false;
    }

    /// <summary>
    /// Called every frame by Character.cs's ConeWaveDamage coroutine with the current
    /// wave front distance. Spawns any rows whose distance threshold has been reached.
    /// This is the ONLY thing that advances the VFX — no independent timer.
    /// </summary>
    public void SetWaveFront(float currentDistance)
    {
        if (!initialized) return;

        while (nextRow < waveRows && currentDistance >= rowDistances[nextRow])
        {
            SpawnRow(nextRow);
            nextRow++;
        }
    }

    /// <summary>
    /// Called by Character.cs when the damage wave finishes. Starts the self-destruct
    /// timer so the VFX GameObject is cleaned up after the last chunks fade out.
    /// </summary>
    public void OnWaveComplete()
    {
        if (waveComplete) return;
        waveComplete = true;
        StartCoroutine(SelfDestructAfterDelay(chunkLifetime + 0.3f));
    }

    private IEnumerator SelfDestructAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void SpawnRow(int row)
    {
        float t = (float)(row + 1) / waveRows;
        float rowDist = rowDistances[row];

        for (int c = 0; c < chunksPerRow; c++)
        {
            float lateralT = chunksPerRow > 1 ? (float)c / (chunksPerRow - 1) : 0.5f;
            float angleOffset = Mathf.Lerp(-coneHalfAngle, coneHalfAngle, lateralT);
            angleOffset += Random.Range(-angleJitter, angleJitter);
            float distJitter = Random.Range(-distanceJitter, distanceJitter);

            Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * waveForward;

            Vector3 targetPos = waveOrigin + dir * (rowDist + distJitter);
            targetPos.y = waveOrigin.y;

            Vector3 startPos = waveOrigin + dir * Mathf.Max(0f, rowDist - slideDistance + distJitter);
            startPos.y = waveOrigin.y;

            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = $"GroundChunk_r{row}_c{c}";

            var col = chunk.GetComponent<Collider>();
            if (col != null) Destroy(col);

            chunk.transform.position = startPos;
            float scale = chunkScale + Random.Range(-0.15f, 0.15f);
            scale *= Mathf.Lerp(0.7f, 1.3f, t);
            chunk.transform.localScale = new Vector3(scale, scale * 0.5f, scale);
            chunk.transform.rotation = Quaternion.Euler(
                Random.Range(-15f, 15f),
                Random.Range(0f, 360f),
                Random.Range(-15f, 15f)
            );

            var eruption = chunk.AddComponent<GroundChunkEruption>();
            eruption.startPos = startPos;
            eruption.targetPos = targetPos;
            eruption.popHeight = chunkPopHeight + Random.Range(-0.1f, 0.2f);
            eruption.lifetime = chunkLifetime + Random.Range(-0.1f, 0.2f);
            eruption.slideDurationFraction = slideDurationFraction;
        }
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

            float slideFraction = Mathf.Max(slideDurationFraction, 0.01f);
            float slideT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / slideFraction), 2f);
            Vector3 basePos = Vector3.Lerp(startPos, targetPos, slideT);

            float popT;
            if (t < 0.15f)
                popT = t / 0.15f;
            else if (t < 0.45f)
                popT = 1f - ((t - 0.15f) / 0.3f);
            else
                popT = 0f;

            transform.position = basePos + Vector3.up * (popHeight * popT);

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