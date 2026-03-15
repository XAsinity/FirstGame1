using UnityEngine;

// DamageType enum is defined in DamageSystem.cs and shared across all combat scripts.

/// <summary>
/// ScriptableObject template that defines a single ability (cooldown, cost, damage, area, VFX).
/// Damage type and scaling reference <see cref="DamageType"/> from <see cref="DamageSystem"/>.
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
    public float baseDamage = 10f;
    public bool scaleWithPhysical = true;
    public float scaleMultiplier = 1f;

    [Header("Crit")]
    public bool allowCrit = true;
    public float critMultiplier = 1.5f;

    [Header("Area / Range")]
    public float radius = 2f;
    public float range = 1f;

    [Header("VFX / Animation")]
    public string animatorTrigger; // exact trigger name in Animator
    public GameObject effectPrefab;

    [Header("Flags")]
    public bool isUltimate = false;

    // ---------------------------------------------------------------------------
    // Legacy property: old code referenced "damage"; redirect to baseDamage.
    // Remove once all call-sites are updated to baseDamage.
    // ---------------------------------------------------------------------------
    [System.Obsolete("Use baseDamage instead. This compatibility shim will be removed in a future update.")]
    public float damage => baseDamage;
}