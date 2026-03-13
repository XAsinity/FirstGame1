using UnityEngine;

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