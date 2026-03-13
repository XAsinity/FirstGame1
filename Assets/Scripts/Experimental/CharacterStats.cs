using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Game/CharacterStats")]
public class CharacterStats : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "New Character";
    public Sprite portrait;

    [Header("Core")]
    public float moveSpeed = 8f;
    public float maxHealth = 100f;
    public float healthRegen = 0f;

    [Header("Combat")]
    public float basePhysicalDamage = 10f;
    public float baseMagicDamage = 0f;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f;

    [Header("Defenses")]
    public float armor = 0f;
    public float magicResist = 0f;

    [Header("Crit")]
    [Range(0f, 100f)]
    public float critChance = 0f;
    public float critMultiplier = 1.5f;

    [Header("Resources")]
    public float resourceMax = 100f;
    public bool resourceIsRage = false;

    [Header("Misc")]
    public int level = 1;
}