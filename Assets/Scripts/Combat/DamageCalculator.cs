/// <summary>
/// Legacy compatibility wrapper — all logic now lives in <see cref="DamageSystem"/>.
/// Prefer calling <see cref="DamageSystem.CalculateDamage"/> directly in new code.
/// This class is kept so that existing callers (e.g. <see cref="GlobalCombatSettings"/>)
/// continue to compile without modification.
/// </summary>
public static class DamageCalculator
{
    /// <summary>Delegates to <see cref="DamageSystem.GlobalPhysicalDamage"/>.</summary>
    public static float GlobalPhysicalDamage
    {
        get => DamageSystem.GlobalPhysicalDamage;
        set => DamageSystem.GlobalPhysicalDamage = value;
    }

    /// <summary>Delegates to <see cref="DamageSystem.GlobalMagicDamage"/>.</summary>
    public static float GlobalMagicDamage
    {
        get => DamageSystem.GlobalMagicDamage;
        set => DamageSystem.GlobalMagicDamage = value;
    }

    /// <summary>
    /// Legacy overload — wraps the call in a <see cref="DamageInfo"/> and delegates to
    /// <see cref="DamageSystem.CalculateDamage"/>. <paramref name="rawDamage"/> is treated as
    /// a fully pre-computed base value (no additional stat scaling is applied).
    /// </summary>
    public static float CalculateDamage(float rawDamage, DamageType type, float targetArmor, float targetMagicResist, float critChancePercent, float critMultiplier)
    {
        var info = new DamageInfo
        {
            type              = type,
            baseDamage        = rawDamage,
            scaleWithPhysical = false,
            scaleMultiplier   = 1f
        };
        // Pass 0 for attacker stat bases because rawDamage is already the full pre-computed value.
        return DamageSystem.CalculateDamage(info, 0f, 0f, critChancePercent, critMultiplier, targetArmor, targetMagicResist);
    }
}