using UnityEngine;

// DamageType enum is defined in DamageSystem.cs and shared by all combat scripts.

/// <summary>
/// ScriptableObject template that defines a single ability (cooldown, cost, damage, area, VFX).
/// Create instances via: Right-click > Create > Game > Ability.
/// Assign to a <see cref="CharacterProfile"/> to make the ability available to a character.
/// Damage type and scaling fields map directly to <see cref="DamageInfo"/> via
/// <see cref="ToDamageInfo"/> for use with <see cref="DamageSystem.CalculateDamage"/>.
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

    /// <summary>
    /// Builds a <see cref="DamageInfo"/> from this ability's inspector-configured fields,
    /// ready to pass to <see cref="DamageSystem.CalculateDamage"/>.
    /// </summary>
    public DamageInfo ToDamageInfo() => new DamageInfo
    {
        type             = damageType,
        baseDamage       = damage,
        scaleWithPhysical = scaleWithPhysical,
        scaleMultiplier  = scaleMultiplier
    };

    [Header("Area / Range")]
    public float radius = 2f;
    public float range = 1f;

    [Header("VFX / Animation")]
    public string animatorTrigger; // exact trigger name in Animator
    public GameObject effectPrefab;

    [Header("Flags")]
    public bool isUltimate = false;
}