using UnityEngine;

/// <summary>
/// Manages an enemy's health, armor, and magic resistance.
/// Call <see cref="TakeDamage"/> to deal damage; the enemy is destroyed when health reaches zero.
/// Armor and magic resist values are read by <see cref="DamageCalculator"/> to reduce incoming damage.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 10f;
    public float health;
    public float armor = 0f;
    public float magicResist = 0f;

    void Awake()
    {
        health = maxHealth;
    }

    // Accept float damage
    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"{gameObject.name} took {amount:F1} damage. Remaining: {health:F1}");
        if (health <= 0f)
            Die();
    }

    void Die()
    {
        // TODO: VFX, pooling, drop loot, etc.
        Destroy(gameObject);
    }
}