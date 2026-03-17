using UnityEngine;

/// <summary>
/// Marker component added to every obstacle spawned by <see cref="ArenaGenerator"/>.
/// Allows other systems (AI, destructibles, etc.) to identify auto-generated obstacles
/// vs. hand-placed designer objects.
/// </summary>
public class ObstacleData : MonoBehaviour
{
    /// <summary>
    /// True when this obstacle was created from a placeholder primitive
    /// (i.e., no prefab was assigned to <see cref="ArenaGenerator.obstaclePrefabs"/>).
    /// False when spawned from a real designer prefab.
    /// </summary>
    public bool isPlaceholder;
}
