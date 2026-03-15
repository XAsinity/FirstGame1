using UnityEngine;

/// <summary>
/// Defines all damage categories in the game.
/// Physical: mitigated by target armor.
/// Magical:  mitigated by target magic resist.
/// True:     bypasses all mitigation.
/// Add new types here to extend the system.
/// </summary>
public enum DamageType { Physical, Magical, True }

/// <summary>
/// Carries all the data needed to describe a single damage hit.
/// Construct one from an ability or attack definition and pass it to
/// <see cref="DamageSystem.CalculateDamage"/>.
/// </summary>
public struct DamageInfo
{
    /// <summary>The category of damage (determines which defensive stat mitigates it).</summary>
    public DamageType type;

    /// <summary>Flat base damage before any scaling or stat contributions.</summary>
    public float baseDamage;

    /// <summary>
    /// When true the attacker's physical damage stats (plus the global physical offset)
    /// are added to <see cref="baseDamage"/>, scaled by <see cref="scaleMultiplier"/>.
    /// </summary>
    public bool scaleWithPhysical;

    /// <summary>Multiplier applied to the physical/magic stat contribution when scaling.</summary>
    public float scaleMultiplier;

    // ── future fields can be added here without breaking callers ──
    // public float critChanceOverride;
    // public bool  ignoreArmor;
    // public float lifestealPercent;
}

/// <summary>
/// Central, static damage system for the game.
/// All damage calculations (primary attacks, abilities, DoTs, etc.) should go through here.
///
/// Global base damage offsets (<see cref="GlobalPhysicalDamage"/>, <see cref="GlobalMagicDamage"/>)
/// are added on top of every relevant attack. They can be overridden at runtime via
/// <see cref="GlobalCombatSettings.Apply"/>.
///
/// Formula:
///   raw  = DamageInfo.baseDamage
///          + (scaleWithPhysical  ? (GlobalPhysicalDamage + attackerPhysicalBase) * scaleMultiplier : 0)
///          + (type==Magical && !scaleWithPhysical ? GlobalMagicDamage + attackerMagicBase : 0)
///   crit = roll(attackerCritChance) → raw *= attackerCritMultiplier
///   final = mitigation formula per DamageType
/// </summary>
public static class DamageSystem
{
    /// <summary>Global flat physical damage added to every physical attack/ability.</summary>
    public static float GlobalPhysicalDamage = 5f;

    /// <summary>Global flat magic damage added to every magical ability.</summary>
    public static float GlobalMagicDamage = 5f;

    /// <summary>
    /// Calculates final damage after stat scaling, crit rolls, and target mitigation.
    /// </summary>
    /// <param name="info">Describes the hit: base damage, type, scaling flags.</param>
    /// <param name="attackerPhysicalBase">The attacker's base physical damage stat.</param>
    /// <param name="attackerMagicBase">The attacker's base magic damage stat.</param>
    /// <param name="attackerCritChance">Crit chance as a percentage (0–100).</param>
    /// <param name="attackerCritMultiplier">Damage multiplier applied on a critical hit.</param>
    /// <param name="targetArmor">Target's armor value (reduces Physical damage).</param>
    /// <param name="targetMagicResist">Target's magic resist value (reduces Magical damage).</param>
    /// <returns>Final damage value after all modifiers.</returns>
    public static float CalculateDamage(
        DamageInfo info,
        float attackerPhysicalBase,
        float attackerMagicBase,
        float attackerCritChance,
        float attackerCritMultiplier,
        float targetArmor,
        float targetMagicResist)
    {
        // 1. Accumulate raw damage
        float raw = info.baseDamage;

        if (info.scaleWithPhysical)
        {
            raw += (GlobalPhysicalDamage + attackerPhysicalBase) * info.scaleMultiplier;
        }
        else if (info.type == DamageType.Magical)
        {
            raw += GlobalMagicDamage + attackerMagicBase;
        }

        // 2. Apply crit
        bool isCrit = Random.value * 100f <= attackerCritChance;
        float dmg = raw * (isCrit ? attackerCritMultiplier : 1f);

        // 3. Apply target mitigation
        switch (info.type)
        {
            case DamageType.Physical:
                return dmg * (100f / (100f + Mathf.Max(0f, targetArmor)));
            case DamageType.Magical:
                return dmg * (100f / (100f + Mathf.Max(0f, targetMagicResist)));
            case DamageType.True:
                return dmg;
            default:
                return dmg;
        }
    }
}
