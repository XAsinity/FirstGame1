using UnityEngine;

/// <summary>
/// Static utility class that performs all damage calculations for the game.
/// This class is now a thin wrapper around <see cref="DamageSystem"/> to maintain backward compatibility.
/// New code should use <see cref="DamageSystem"/> directly.
/// Global base damage offsets are kept in sync with <see cref="DamageSystem.GlobalPhysicalDamage"/>
/// and <see cref="DamageSystem.GlobalMagicDamage"/>.
/// </summary>
public static class DamageCalculator
{
    /// <summary>Global flat physical damage — delegates to <see cref="DamageSystem.GlobalPhysicalDamage"/>.</summary>
    public static float GlobalPhysicalDamage
    {
        get => DamageSystem.GlobalPhysicalDamage;
        set => DamageSystem.GlobalPhysicalDamage = value;
    }

    /// <summary>Global flat magic damage — delegates to <see cref="DamageSystem.GlobalMagicDamage"/>.</summary>
    public static float GlobalMagicDamage
    {
        get => DamageSystem.GlobalMagicDamage;
        set => DamageSystem.GlobalMagicDamage = value;
    }

    /// <summary>
    /// Legacy damage calculation entry-point. Delegates to <see cref="DamageSystem.CalculateDamage"/>.
    /// Crit is rolled here for backward compatibility; new code should use <see cref="DamageSystem.RollCrit"/>
    /// and build a <see cref="DamageInfo"/> for a fully deterministic path.
    /// </summary>
    public static float CalculateDamage(float rawDamage, DamageType type, float targetArmor, float targetMagicResist, float critChancePercent, float critMultiplier)
    {
        bool isCrit = DamageSystem.RollCrit(critChancePercent);
        var info = new DamageInfo
        {
            type              = type,
            baseDamage        = rawDamage,
            scaleWithPhysical = false,
            scaleMultiplier   = 1f,
            allowCrit         = true,
            isCrit            = isCrit,
            critMultiplier    = critMultiplier,
        };
        // Pass 0 for attacker bases because rawDamage already includes global + character offsets
        return DamageSystem.CalculateDamage(info, 0f, 0f, targetArmor, targetMagicResist);
    }
}