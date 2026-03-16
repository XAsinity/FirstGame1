using UnityEngine;

/// <summary>
/// Attach to any ability VFX prefab to auto-destroy it after a configurable lifetime.
/// This is the only script needed on a VFX placeholder or final VFX prefab.
/// 
/// Setup for placeholder VFX:
/// 1. Create any visual in Unity (Cube, Quad, Particle System, etc.)
/// 2. Remove any Collider components (so it doesn't block gameplay)
/// 3. Add this AbilityVFXLifetime component
/// 4. Set lifetime (default 1 second)
/// 5. Save as prefab, assign to AbilityData.effectPrefab
/// 6. When real VFX is ready, just swap the prefab reference — one click
///
/// Optional features:
/// - Scale pulse: grows slightly then shrinks for a quick "impact" feel
/// - Fade: (not implemented yet — add when you have transparent materials)
/// </summary>
public class AbilityVFXLifetime : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("How long (seconds) before this VFX object is destroyed.")]
    public float lifetime = 1.0f;

    [Header("Scale Pulse (optional)")]
    [Tooltip("If true, the object scales up quickly then back down over its lifetime.")]
    public bool scalePulse = true;

    [Tooltip("Maximum scale multiplier at the peak of the pulse.")]
    public float pulseMaxScale = 1.5f;

    [Tooltip("How quickly the pulse reaches max scale (0-1 range, 0.3 = peak at 30% of lifetime).")]
    [Range(0.05f, 0.5f)]
    public float pulsePeakTime = 0.2f;

    private Vector3 originalScale;
    private float timer;

    void Start()
    {
        originalScale = transform.localScale;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!scalePulse) return;

        timer += Time.deltaTime;
        if (timer >= lifetime) return;
        float t = timer / lifetime;

        float scaleMultiplier;
        if (t < pulsePeakTime)
        {
            // Growing phase: 1 -> pulseMaxScale
            scaleMultiplier = Mathf.Lerp(1f, pulseMaxScale, t / pulsePeakTime);
        }
        else
        {
            // Shrinking phase: pulseMaxScale -> 0
            float shrinkT = (t - pulsePeakTime) / (1f - pulsePeakTime);
            scaleMultiplier = Mathf.Lerp(pulseMaxScale, 0f, shrinkT);
        }

        transform.localScale = originalScale * scaleMultiplier;
    }
}
