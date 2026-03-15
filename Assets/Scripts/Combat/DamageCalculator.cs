using UnityEngine;

/// <summary>
/// Legacy damage calculation utility.
/// All new code should use <see cref="DamageSystem"/> and <see cref="DamageInfo"/> directly.
/// This class is kept for backwards compatibility and forwards all calls to <see cref="DamageSystem"/>.
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Global flat physical damage. Reading or writing this proxies to <see cref="DamageSystem.GlobalPhysicalDamage"/>.
    /// </summary>
    public static float GlobalPhysicalDamage
    {
        get => DamageSystem.GlobalPhysicalDamage;
        set => DamageSystem.GlobalPhysicalDamage = value;
    }

    /// <summary>
    /// Global flat magic damage. Reading or writing this proxies to <see cref="DamageSystem.GlobalMagicDamage"/>.
    /// </summary>
    public static float GlobalMagicDamage
    {
        get => DamageSystem.GlobalMagicDamage;
        set => DamageSystem.GlobalMagicDamage = value;
    }

    /// <summary>
    /// Legacy overload. Resolves a crit roll internally, then delegates to
    /// <see cref="DamageSystem.CalculateDamage"/>.
    /// Prefer building a <see cref="DamageInfo"/> and calling <see cref="DamageSystem.CalculateDamage"/> directly.
    /// </summary>
    public static float CalculateDamage(float rawDamage, DamageType type, float targetArmor, float targetMagicResist, float critChancePercent, float critMultiplier)
    {
        bool isCrit = UnityEngine.Random.value * 100f <= critChancePercent;
        var info = new DamageInfo
        {
            type = type,
            baseDamage = rawDamage,
            scaleWithPhysical = false,
            scaleMultiplier = 1f,
            allowCrit = true,
            critMultiplier = 1f
        };
        return DamageSystem.CalculateDamage(info, 0f, 0f, critMultiplier, targetArmor, targetMagicResist, isCrit);
    }
}