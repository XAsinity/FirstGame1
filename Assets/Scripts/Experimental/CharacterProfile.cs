using UnityEngine;

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

    [Header("Capsule / Physics hints (optional)")]
    public bool overrideCapsuleSizing = false;
    public float capsuleHeight = 2f;
    public float capsuleRadius = 0.5f;
    public Vector3 capsuleCenter = new Vector3(0f, 1f, 0f);
}