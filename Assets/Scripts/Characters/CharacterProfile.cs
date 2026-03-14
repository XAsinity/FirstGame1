using UnityEngine;

/// <summary>
/// ScriptableObject that bundles together everything needed to represent a playable character:
/// a visual prefab, a <see cref="CharacterStats"/> asset, and an array of <see cref="AbilityData"/> assets.
/// <see cref="PlayerManager"/> reads this profile to spawn and initialise the character at runtime.
/// Visual placement overrides (scale, offset, rotation) allow artists to fine-tune without code changes.
/// Capsule sizing values are always applied at spawn so the physics capsule matches each character's proportions.
/// Create instances via: Right-click > Create > Game > CharacterProfile.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Game/CharacterProfile")]
public class CharacterProfile : ScriptableObject
{
    public string displayName;
    public GameObject characterPrefab;
    public CharacterStats stats;
    public AbilityData[] abilities;

    [Header("Visual / Runtime placement")]
    public Vector3 visualScale = Vector3.one; // scale to apply to the visual prefab (localScale)
    public Vector3 visualLocalOffset = Vector3.zero;
    public Vector3 visualLocalEulerOffset = Vector3.zero;

    [Header("Capsule / Physics hints")]
    [Tooltip("Deprecated — capsule sizing is now always applied. This field is kept for backwards compatibility.")]
    public bool overrideCapsuleSizing = false;
    public float capsuleHeight = 2f;
    public float capsuleRadius = 0.5f;
    public Vector3 capsuleCenter = new Vector3(0f, 1f, 0f);
}