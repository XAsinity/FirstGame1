using UnityEngine;

public static class DamageCalculator
{
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