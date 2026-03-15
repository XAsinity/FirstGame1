using UnityEngine;

/// <summary>
/// ScriptableObject that stores global combat settings shared across all characters.
/// These values are applied on top of per-character stats in every damage calculation.
/// Create an instance via: Right-click > Create > Game > GlobalCombatSettings.
/// Assign the asset to <see cref="DamageSystem"/> at runtime (e.g. from a GameManager or
/// initialization script) to override the static defaults in <see cref="DamageSystem"/>.
/// </summary>
[CreateAssetMenu(fileName = "GlobalCombatSettings", menuName = "Game/GlobalCombatSettings")]
public class GlobalCombatSettings : ScriptableObject
{
    [Header("Global Base Damage")]
    [Tooltip("Flat physical damage added to every physical attack and ability, on top of character stats.")]
    public float globalBasePhysicalDamage = 5f;

    [Tooltip("Flat magic damage added to every magical ability, on top of character stats.")]
    public float globalBaseMagicDamage = 5f;

    /// <summary>
    /// Applies these settings to <see cref="DamageSystem"/> so all damage calls in the game
    /// reflect the values set in this asset. Call this once at game start (e.g. from a
    /// GameManager or PlayerManager).
    /// </summary>
    public void Apply()
    {
        DamageSystem.GlobalPhysicalDamage = globalBasePhysicalDamage;
        DamageSystem.GlobalMagicDamage    = globalBaseMagicDamage;
    }
}
