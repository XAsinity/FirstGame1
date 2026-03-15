using UnityEngine;

/// <summary>
/// Defines the type of damage an attack or ability deals.
/// Physical: mitigated by target armor.
/// Magical:  mitigated by target magic resist.
/// True:     bypasses all mitigation.
/// Add new values here to extend the damage type system for future damage categories.
/// </summary>
public enum DamageType { Physical, Magical, True }

/// <summary>
/// Carries all parameters needed to compute a single instance of damage.
/// Build one of these for each attack or ability hit, then pass it to
/// <see cref="DamageSystem.CalculateDamage"/>.
/// </summary>
public struct DamageInfo
{
    /// <summary>The type of damage (determines which defense stat mitigates it).</summary>
    public DamageType type;

    /// <summary>Flat base damage from the ability or attack definition, before scaling.</summary>
    public float baseDamage;

    /// <summary>When true, the attacker's physical stats are added as a scaled contribution.</summary>
    public bool scaleWithPhysical;

    /// <summary>Multiplier applied to the attacker's physical (or magic) stat contribution.</summary>
    public float scaleMultiplier;

    /// <summary>Whether this attack/ability can critically strike.</summary>
    public bool allowCrit;

    /// <summary>
    /// Ability-specific crit damage multiplier. Combined multiplicatively with
    /// <c>attackerCritMultiplier</c> when a crit is applied.
    /// Set to 0 to apply no ability-specific bonus (only <c>attackerCritMultiplier</c> is used).
    /// Positive non-zero values add an extra factor (e.g. 1.5 = 50% more crit damage than normal).
    /// </summary>
    public float critMultiplier;
}

/// <summary>
/// Centralized, deterministic damage calculation system.
/// All damage in the game should flow through <see cref="CalculateDamage"/>.
///
/// Design principles:
/// - Deterministic: no randomness inside this class. The caller resolves crit chance and
///   passes the result via the <c>forceCrit</c> parameter, keeping this method unit-testable.
/// - Extensible: add new <see cref="DamageType"/> values and handle them in the switch block.
/// - Global bonuses: <see cref="GlobalPhysicalDamage"/> and <see cref="GlobalMagicDamage"/> are
///   applied on top of per-character stats in every calculation. Modify them at runtime (e.g.
///   from <see cref="GlobalCombatSettings"/>) to apply game-wide damage buffs.
/// </summary>
public static class DamageSystem
{
    /// <summary>Global flat physical damage added to every physical attack and ability.</summary>
    public static float GlobalPhysicalDamage = 5f;

    /// <summary>Global flat magic damage added to every magical ability.</summary>
    public static float GlobalMagicDamage = 5f;

    /// <summary>
    /// Computes the final damage for a single hit.
    ///
    /// Formula:
    ///   raw = info.baseDamage
    ///       + (scaleWithPhysical ? (GlobalPhysicalDamage + attackerPhysicalBase) * info.scaleMultiplier : 0)
    ///       + (!scaleWithPhysical and type==Magical ? GlobalMagicDamage + attackerMagicBase : 0)
    ///
    ///   crit_raw = (forceCrit and allowCrit)
    ///              ? raw * attackerCritMultiplier * max(info.critMultiplier, 1)
    ///              : raw
    ///
    ///   final = crit_raw * mitigation(type, targetArmor, targetMagicResist)
    ///
    /// Mitigation: Physical/Magical → damage * 100 / (100 + defense); True → no reduction.
    /// </summary>
    /// <param name="info">Attack/ability parameters including damage type, base damage, and crit settings.</param>
    /// <param name="attackerPhysicalBase">Attacker's per-character physical damage stat.</param>
    /// <param name="attackerMagicBase">Attacker's per-character magic damage stat.</param>
    /// <param name="attackerCritMultiplier">Attacker's crit damage multiplier (e.g. 2.0 = double damage on crit).</param>
    /// <param name="targetArmor">Target's armor value.</param>
    /// <param name="targetMagicResist">Target's magic resistance value.</param>
    /// <param name="forceCrit">
    /// Pass true when the caller has already determined this hit is a critical strike.
    /// Randomness must be resolved outside this method to keep the calculation deterministic.
    /// </param>
    /// <returns>Final damage value after scaling, crit, and mitigation.</returns>
    public static float CalculateDamage(
        DamageInfo info,
        float attackerPhysicalBase,
        float attackerMagicBase,
        float attackerCritMultiplier,
        float targetArmor,
        float targetMagicResist,
        bool forceCrit = false)
    {
        float raw = info.baseDamage;

        if (info.scaleWithPhysical)
            raw += (GlobalPhysicalDamage + attackerPhysicalBase) * info.scaleMultiplier;
        else if (info.type == DamageType.Magical)
            raw += GlobalMagicDamage + attackerMagicBase;

        if (info.allowCrit && forceCrit)
        {
            // Combine the attacker's crit multiplier with any ability-specific override.
            // A critMultiplier of 0 in DamageInfo means "no override — use attacker's only".
            float abilityCritMult = info.critMultiplier > 0f ? info.critMultiplier : 1f;
            raw *= attackerCritMultiplier * abilityCritMult;
        }

        switch (info.type)
        {
            case DamageType.Physical:
                return raw * (100f / (100f + Mathf.Max(0f, targetArmor)));
            case DamageType.Magical:
                return raw * (100f / (100f + Mathf.Max(0f, targetMagicResist)));
            case DamageType.True:
                return raw;
            default:
                return raw;
        }
    }
}
