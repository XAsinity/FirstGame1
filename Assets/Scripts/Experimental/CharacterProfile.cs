using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Game/CharacterProfile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Prefab & Identity")]
    public GameObject characterPrefab; // prefab with Character component
    public CharacterStats stats;       // reference to scriptable stats

    [Header("Abilities (1..4)")]
    public AbilityData[] abilities = new AbilityData[4];

    [Header("UI / Flavor")]
    public Sprite portrait;
    public string displayName = "New Hero";
}