using UnityEngine;

/// <summary>
/// Defines the damage type for an attack or ability.
/// Physical: mitigated by target armor.
/// Magical:  mitigated by target magic resist.
/// True:     bypasses all mitigation.
/// </summary>
public enum DamageType { Physical, Magical, True }

/// <summary>
/// ScriptableObject template that defines a single ability (cooldown, cost, damage, area, VFX).
/// Create instances via: Right-click > Create > Game > Ability.
/// Assign to a <see cref="CharacterProfile"/> to make the ability available to a character.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName = "New Ability";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Usage")]
    public float cooldown = 3f;
    public float resourceCost = 0f;

    [Header("Damage")]
    public DamageType damageType = DamageType.Physical;
    public float damage = 10f;
    public bool scaleWithPhysical = true;
    public float scaleMultiplier = 1f;

    [Header("Area / Range")]
    public float radius = 2f;
    public float range = 1f;

    [Header("VFX / Animation")]
    public string animatorTrigger; // exact trigger name in Animator
    public GameObject effectPrefab;

    [Header("Flags")]
    public bool isUltimate = false;
}