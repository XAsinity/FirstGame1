using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy AI that chases the player.
///
/// When a <see cref="NavMeshAgent"/> component is attached AND the <see cref="ArenaGenerator"/>
/// has successfully baked a NavMesh, the agent navigates around obstacles automatically.
///
/// Falls back to direct <see cref="Vector3.MoveTowards"/> movement when no NavMesh is
/// available, so the script works even without ArenaGenerator in the scene.
///
/// SETUP:
/// - Attach this script to an enemy prefab.
/// - For NavMesh navigation: also add a NavMeshAgent component to the same prefab
///   and ensure ArenaGenerator (with bakeNavMesh=true) runs first in the scene.
/// - Without NavMeshAgent: enemies will still chase the player via direct movement.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeed = 3.5f;

    [Tooltip("How close the enemy can get to the player before it stops moving (units). " +
             "Used by the NavMeshAgent; direct-movement mode always stops at melee contact.")]
    public float stoppingDistance = 1.2f;

    [Tooltip("How fast the NavMeshAgent rotates to face its target (degrees per second).")]
    public float agentAngularSpeed = 360f;

    [Tooltip("How quickly the NavMeshAgent reaches its target speed (units per second squared).")]
    public float agentAcceleration = 12f;

    private Transform player;
    private NavMeshAgent _agent;

    void Start()
    {
        // Grab NavMeshAgent if one is present on this prefab.
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.speed            = moveSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.angularSpeed     = agentAngularSpeed;
            _agent.acceleration     = agentAcceleration;
        }

        // Automatically find the object tagged "Player" when the enemy spawns.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[EnemyAI] Enemy couldn't find the Player! Did you remember to set the Player tag?");
        }
    }

    void Update()
    {
        if (player == null) return;

        if (_agent != null && _agent.isOnNavMesh)
        {
            // NavMeshAgent path — the agent steers around ArenaGenerator obstacles automatically.
            _agent.SetDestination(player.position);
        }
        else
        {
            // Fallback: direct movement used when no NavMesh has been baked yet
            // (e.g. ArenaGenerator not in scene, or bakeNavMesh=false).
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(targetPosition);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }
}