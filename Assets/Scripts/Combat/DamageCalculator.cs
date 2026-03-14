using UnityEngine;

/// <summary>
/// Static utility class that performs all damage calculations for the game.
/// Contains global base damage offsets (<see cref="GlobalPhysicalDamage"/> and <see cref="GlobalMagicDamage"/>)
/// that are added to every attack, on top of per-character stats.
/// The damage formula is: <c>(GlobalBase + CharacterBase) * Multipliers</c>, then mitigated by armor or magic resist.
/// For a data-driven alternative at edit-time, see <see cref="GlobalCombatSettings"/>.
/// Physical/Magical damage uses the mitigation formula: <c>final = raw * (100 / (100 + defense))</c>.
/// True damage bypasses all mitigation.
/// </summary>
public static class DamageCalculator
{
    /// <summary>Global flat physical damage added to every physical attack and ability in the game.</summary>
    public static float GlobalPhysicalDamage = 5f;

    /// <summary>Global flat magic damage added to every magical ability in the game.</summary>
    public static float GlobalMagicDamage = 5f;

    // Physical/Magical use simple mitigation formula: final = raw * (100 / (100 + defense))
    public static float CalculateDamage(float rawDamage, DamageType type, float targetArmor, float targetMagicResist, float critChancePercent, float critMultiplier)
    {
        bool isCrit = UnityEngine.Random.value * 100f <= critChancePercent;
        float dmg = rawDamage * (isCrit ? critMultiplier : 1f);

        switch (type)
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