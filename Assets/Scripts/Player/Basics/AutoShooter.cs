using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1f; // Fires 1 time per second
    public float attackRange = 10f; // How close enemies need to be to shoot

    private float fireTimer;

    void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            ShootClosestEnemy();
        }
    }

    void ShootClosestEnemy()
    {
        // Find every object in the scene tagged "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject closestEnemy = null;
        float shortestDistance = attackRange;

        // Loop through them to find the closest one
        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                closestEnemy = enemy;
            }
        }

        // If we found an enemy in range, shoot at it!
        if (closestEnemy != null)
        {
            fireTimer = 0f; // Reset the cooldown timer

            // Spawn the bullet
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            // Aim the bullet at the enemy's center (adding 1 to Y so it doesn't aim at their feet)
            Vector3 aimTarget = new Vector3(closestEnemy.transform.position.x, closestEnemy.transform.position.y + 1f, closestEnemy.transform.position.z);
            bullet.transform.LookAt(aimTarget);
        }
    }
}