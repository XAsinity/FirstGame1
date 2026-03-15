using UnityEngine;

/// Runtime helper to test ability hit detection.
/// - Attach alongside AbilityRangeGizmo (or anywhere).
/// - Press the configured debugKey to run Physics.OverlapSphere at the currently selected ability.
/// - Visually draws Debug.DrawLine/spheres for a few seconds.
public class AbilityRangeDebugger : MonoBehaviour
{
    [Tooltip("Key to run a debug overlap test")]
    public KeyCode debugKey = KeyCode.G;
    [Tooltip("Which ability index to test (0..3)")]
    public int abilityIndex = 0;
    [Tooltip("How long debug visuals persist (seconds)")]
    public float debugDuration = 1.5f;
    [Tooltip("Layer mask to test against (use enemies layer)")]
    public LayerMask hitLayers = ~0;

    void Update()
    {
        if (Input.GetKeyDown(debugKey))
            RunDebugTest();
    }

    void RunDebugTest()
    {
        var character = GetComponent<Character>();
        if (character == null || character.abilities == null || abilityIndex < 0 || abilityIndex >= character.abilities.Count)
        {
            Debug.LogWarning("AbilityRangeDebugger: missing character or invalid ability index.");
            return;
        }

        var a = character.abilities[abilityIndex];
        if (a == null)
        {
            Debug.LogWarning("AbilityRangeDebugger: ability is null.");
            return;
        }

        // Match Character.cs scaling: multiply offset, range and radius by average lossyScale.
        float visualScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
        float meleeOffset = character.meleeVerticalOffset;
        Vector3 center = transform.position
            + Vector3.up * (meleeOffset * visualScale)
            + transform.forward * (a.range * visualScale);
        float scaledRadius = a.radius * visualScale;

        Debug.Log($"Ability debug: {a.abilityName} center={center} radius={scaledRadius:F2}");

        // Visual debug: wireframe sphere via multiple Debug.DrawLine calls
        DrawDebugSphere(center, scaledRadius, debugDuration, Color.red);

        // Actual overlap test (same as ability logic)
        Collider[] hits = Physics.OverlapSphere(center, scaledRadius, hitLayers);
        foreach (var c in hits)
        {
            // draw a green sphere at each hit collider's position
            Debug.DrawLine(center, c.transform.position, Color.green, debugDuration);
            Debug.DrawRay(c.transform.position, Vector3.up * 0.5f, Color.green, debugDuration);
            Debug.Log($"Hit: {c.name} (root: {c.transform.root.name})");
        }
    }

    void DrawDebugSphere(Vector3 center, float radius, float duration, Color col)
    {
        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 p0 = center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
            Debug.DrawLine(p0, p1, col, duration);
        }
        // vertical circle
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 p0 = center + new Vector3(0f, Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3(0f, Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            Debug.DrawLine(p0, p1, col, duration);
        }
    }
}