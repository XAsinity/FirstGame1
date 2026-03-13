using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 3; // Takes 3 hits to destroy

    public void TakeDamage(int damage)
    {
        health -= damage;

        // Make the enemy flash or shrink here later!

        if (health <= 0)
        {
            Destroy(gameObject); // Poof! Enemy is dead.
        }
    }
}