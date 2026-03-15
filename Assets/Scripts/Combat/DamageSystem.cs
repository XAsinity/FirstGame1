using UnityEngine;

/// <summary>
/// Defines the type of damage dealt. Extend this enum to add new damage categories (e.g. Fire, Ice)
/// without changing existing damage logic — add a new case to <see cref="DamageSystem.ApplyMitigation"/>.
/// Physical: mitigated by target armor.
/// Magical:  mitigated by target magic resist.
/// True:     bypasses all mitigation.
/// </summary>
public enum DamageType { Physical, Magical, True }

/// <summary>
/// Carries all parameters needed to compute a single damage instance.
/// Build this struct at the call-site (Character, projectile, etc.) and pass it to
/// <see cref="DamageSystem.CalculateDamage"/>.
/// </summary>
public struct DamageInfo
{
    /// <summary>Damage category — determines which mitigation stat is applied.</summary>
    public DamageType type;

    /// <summary>Flat damage value before global offsets and scaling are added.</summary>
    public float baseDamage;

    /// <summary>When true the global physical damage offset and attacker's physical base are added (scaled by <see cref="scaleMultiplier"/>).</summary>
    public bool scaleWithPhysical;

    /// <summary>Multiplier applied to the physical scaling contribution. Ignored when <see cref="scaleWithPhysical"/> is false.</summary>
    public float scaleMultiplier;

    /// <summary>Whether this instance is allowed to critically strike.</summary>
    public bool allowCrit;

    /// <summary>
    /// Pre-resolved crit flag. Set this from a <see cref="DamageSystem.RollCrit"/> call before building the struct
    /// so that <see cref="DamageSystem.CalculateDamage"/> remains deterministic and unit-testable.
    /// </summary>
    public bool isCrit;

    /// <summary>Damage multiplier applied when <see cref="allowCrit"/> and <see cref="isCrit"/> are both true.</summary>
    public float critMultiplier;
}

/// <summary>
/// Centralized, deterministic damage calculation system.
/// All damage math flows through <see cref="CalculateDamage"/>; no randomness is resolved inside —
/// callers must supply the <see cref="DamageInfo.isCrit"/> flag (use <see cref="RollCrit"/> before building <see cref="DamageInfo"/>).
///
/// Global combat offsets (<see cref="GlobalPhysicalDamage"/>, <see cref="GlobalMagicDamage"/>) apply to
/// every attack and can be updated at runtime via <see cref="GlobalCombatSettings.Apply"/>.
///
/// Mitigation formula: <c>final = raw * (100 / (100 + defense))</c>.
/// </summary>
public static class DamageSystem
{
    /// <summary>Global flat physical damage added to every physical attack and ability.</summary>
    public static float GlobalPhysicalDamage = 5f;

    /// <summary>Global flat magic damage added to every magical ability.</summary>
    public static float GlobalMagicDamage = 5f;

    /// <summary>
    /// Compute final damage from a <see cref="DamageInfo"/>, attacker base stats, and target defenses.
    /// This method is fully deterministic — crit randomness must be resolved by the caller via <see cref="RollCrit"/>.
    /// </summary>
    /// <param name="info">Damage parameters built by the caller.</param>
    /// <param name="attackerPhysicalBase">Attacker's per-character base physical damage (from CharacterStats).</param>
    /// <param name="attackerMagicBase">Attacker's per-character base magic damage (from CharacterStats).</param>
    /// <param name="targetArmor">Target's armor value.</param>
    /// <param name="targetMagicResist">Target's magic resistance value.</param>
    /// <returns>Final damage after scaling, crits, and mitigation.</returns>
    public static float CalculateDamage(
        DamageInfo info,
        float attackerPhysicalBase,
        float attackerMagicBase,
        float targetArmor,
        float targetMagicResist)
    {
        float raw = info.baseDamage;

        // Add global + attacker scaling on top of the base damage
        if (info.scaleWithPhysical)
            raw += (GlobalPhysicalDamage + attackerPhysicalBase) * info.scaleMultiplier;
        else if (info.type == DamageType.Magical)
            raw += GlobalMagicDamage + attackerMagicBase;

        // Apply crit multiplier — deterministic because isCrit was resolved by the caller
        if (info.allowCrit && info.isCrit)
            raw *= info.critMultiplier;

        return ApplyMitigation(raw, info.type, targetArmor, targetMagicResist);
    }

    /// <summary>
    /// Apply armor/magic resist mitigation to a raw damage value.
    /// Extend this switch when new <see cref="DamageType"/> values are added.
    /// </summary>
    public static float ApplyMitigation(float rawDamage, DamageType type, float targetArmor, float targetMagicResist)
    {
        switch (type)
        {
            case DamageType.Physical:
                return rawDamage * (100f / (100f + Mathf.Max(0f, targetArmor)));
            case DamageType.Magical:
                return rawDamage * (100f / (100f + Mathf.Max(0f, targetMagicResist)));
            case DamageType.True:
                return rawDamage;
            default:
                return rawDamage;
        }
    }

    /// <summary>
    /// Roll a random crit given a percent chance (0–100).
    /// Call this before building a <see cref="DamageInfo"/> and assign the result to
    /// <see cref="DamageInfo.isCrit"/> so that <see cref="CalculateDamage"/> stays deterministic.
    /// </summary>
    /// <param name="critChancePercent">Crit chance as a percentage (e.g. 25 = 25%).</param>
    public static bool RollCrit(float critChancePercent)
    {
        return Random.value * 100f <= critChancePercent;
    }
}
