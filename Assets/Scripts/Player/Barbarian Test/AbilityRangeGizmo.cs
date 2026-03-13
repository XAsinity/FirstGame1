using UnityEngine;

/// Draws ability range gizmos for a Character in the Scene view.
/// - Attach to the root GameObject that has your Character component (or the Character prefab root).
/// - It will draw each ability's AOE center (forward * range) and radius as a wire-sphere.
/// - Configure which ability index to show, or set showAll = true to draw them all.
[ExecuteAlways]
public class AbilityRangeGizmo : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    public bool showAll = true;
    [Tooltip("If not showing all, which ability index (0..3) to visualize")]
    public int abilityIndex = 0;
    public bool showInPlayMode = true; // set false if you only want editor gizmos

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || showInPlayMode)
            DrawRanges();
    }

    void DrawRanges()
    {
        var character = GetComponent<Character>();
        if (character == null || character.abilities == null) return;

        Gizmos.color = gizmoColor;

        if (showAll)
        {
            for (int i = 0; i < character.abilities.Count; i++)
                DrawAbilityRange(character, i);
        }
        else
        {
            DrawAbilityRange(character, abilityIndex);
        }
    }

    void DrawAbilityRange(Character character, int index)
    {
        if (index < 0 || index >= character.abilities.Count) return;
        var a = character.abilities[index];
        if (a == null) return;

        Vector3 center = transform.position + transform.forward * a.range;
        Gizmos.DrawWireSphere(center, a.radius);

        // draw a small indicator for the center
        Gizmos.DrawSphere(center, 0.05f);

        // label with name & radius if available (Editor only)
#if UNITY_EDITOR
        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(center + Vector3.up * 0.2f, $"{(string.IsNullOrEmpty(a.abilityName) ? "Ability" : a.abilityName)} ({a.radius:F1})");
#endif
    }
}