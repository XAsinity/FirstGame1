using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 3f;

    void Start()
    {
        // Destroy the bullet after 3 seconds so it doesn't fly forever
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Fly straight forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // This triggers when the bullet's collider hits another collider
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Find the health script and deal damage!
            EnemyHealth enemyStats = other.GetComponent<EnemyHealth>();
            if (enemyStats != null)
            {
                enemyStats.TakeDamage(damage);
            }

            // Destroy the bullet upon impact
            Destroy(gameObject);
        }
    }
}