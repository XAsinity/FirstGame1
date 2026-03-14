// name=Assets/Scripts/Character.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NOTE: Removed RequireComponent(typeof(Animator)) so Animator can live on a child model.
// Character will find an Animator on the same GameObject or in children (recommended pattern for model-child animators).
public class Character : MonoBehaviour
{
    [Header("Assigned at edit-time (optional)")]
    public CharacterStats baseStats; // reference to SO (template). We'll copy runtime values from this.
    public List<AbilityData> abilities = new List<AbilityData>();

    [Header("Animator (optional) — will automatically find Animator in children if not assigned)")]
    [Tooltip("If left empty, Character will search GetComponentInChildren<Animator>()")]
    [SerializeField] private Animator animator;

    [Header("Runtime (do not edit the SOs directly!)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentResource;

    [Header("Primary attack (melee) settings")]
    public LayerMask meleeHitLayers = ~0;
    public float meleeVerticalOffset = 1.0f;
    public float meleeRadius = 0.5f;

    private Dictionary<AbilityData, float> cooldownTimers = new Dictionary<AbilityData, float>();
    private AbilityData pendingAbility;
    private readonly RaycastHit[] _meleeSphereBuffer = new RaycastHit[16];

    void Awake()
    {
        // If the animator wasn't explicitly assigned in the Inspector, find it in children
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
                Debug.LogWarning($"Character ({name}) could not find an Animator on itself or children. Add an Animator to the visual child or assign one in Inspector.");
        }

        if (baseStats != null && currentHealth == 0f)
            ApplyStatsFromSO();
        SetupCooldowns();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var keys = new List<AbilityData>(cooldownTimers.Keys);
        foreach (var a in keys)
            cooldownTimers[a] = Mathf.Max(0f, cooldownTimers[a] - dt);

        if (baseStats != null)
            currentResource = Mathf.Min(baseStats.resourceMax, currentResource + baseStats.healthRegen * dt);
    }

    public void InitializeFromProfile(CharacterProfile profile)
    {
        baseStats = profile.stats;
        abilities = new List<AbilityData>(profile.abilities);
        ApplyStatsFromSO();
        SetupCooldowns();
        Debug.Log($"[Character] Initialized from profile '{(profile != null ? profile.displayName : "null")}'");
    }

    private void ApplyStatsFromSO()
    {
        if (baseStats == null) return;
        currentHealth = baseStats.maxHealth;
        currentResource = baseStats.resourceMax;
    }

    private void SetupCooldowns()
    {
        cooldownTimers.Clear();
        foreach (var a in abilities)
            if (a != null) cooldownTimers[a] = 0f;
    }

    public bool CanUseAbility(AbilityData ability)
    {
        if (ability == null) return false;
        if (!cooldownTimers.ContainsKey(ability)) return false;
        if (cooldownTimers[ability] > 0f) return false;
        if (baseStats != null && ability.resourceCost > currentResource) return false;
        return true;
    }

    public void UseAbility(AbilityData ability)
    {
        if (!CanUseAbility(ability))
        {
            Debug.Log($"[Character] Attempted to use ability '{(ability != null ? ability.abilityName : "null")}', but CanUseAbility returned false.");
            return;
        }

        if (baseStats != null) currentResource -= ability.resourceCost;
        cooldownTimers[ability] = ability.cooldown;
        pendingAbility = ability;

        if (animator != null && !string.IsNullOrEmpty(ability.animatorTrigger))
        {
            animator.SetTrigger(ability.animatorTrigger);
            Debug.Log($"[Character] Animator trigger '{ability.animatorTrigger}' sent for ability '{ability.abilityName}'.");
        }
        else
        {
            Debug.Log($"[Character] No animator trigger for ability '{ability.abilityName}', applying immediately.");
            StartCoroutine(ApplyPendingAbilityImmediate());
        }
    }

    private IEnumerator ApplyPendingAbilityImmediate()
    {
        yield return null;
        DoPendingAbility();
    }

    public void OnAbilityHit()
    {
        Debug.Log("[Character] OnAbilityHit animation event received.");
        DoPendingAbility();
    }

    private void DoPendingAbility()
    {
        if (pendingAbility == null)
        {
            Debug.Log("[Character] DoPendingAbility called but no pending ability.");
            return;
        }

        Vector3 center = transform.position + transform.forward * pendingAbility.range;
        if (pendingAbility.effectPrefab != null)
            Instantiate(pendingAbility.effectPrefab, center, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(center, pendingAbility.radius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                float raw = pendingAbility.damage;
                if (pendingAbility.scaleWithPhysical && baseStats != null) raw += baseStats.basePhysicalDamage * pendingAbility.scaleMultiplier;

                float final = DamageCalculator.CalculateDamage(
                    raw,
                    pendingAbility.damageType,
                    enemy.armor,
                    enemy.magicResist,
                    baseStats != null ? baseStats.critChance : 0f,
                    baseStats != null ? baseStats.critMultiplier : 1f
                );

                enemy.TakeDamage(final);
                Debug.Log($"[Character] Ability '{pendingAbility.abilityName}' hit {enemy.name} for {final:F1} damage.");
            }
        }

        pendingAbility = null;
    }

    public void PrimaryAttack()
    {
        if (baseStats == null)
        {
            Debug.Log("[Character] PrimaryAttack aborted: baseStats missing.");
            return;
        }

        float range = baseStats.attackRange;
        float damage = baseStats.basePhysicalDamage;

        if (animator != null)
            animator.SetTrigger("PrimaryAttack");

        Vector3 origin = transform.position + Vector3.up * meleeVerticalOffset;
        Vector3 dir = transform.forward;

        RaycastHit[] hits = _meleeSphereBuffer;
        int hitCount = Physics.SphereCastNonAlloc(origin, meleeRadius, dir, hits, range, meleeHitLayers);
        bool hitEnemy = false;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            if (hit.collider.transform.root == transform.root)
            {
                Debug.Log($"[Character] PrimaryAttack SphereCast hit own collider '{hit.collider.name}' — ignoring.");
                continue;
            }

            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                Debug.DrawLine(origin, hit.point, Color.green, 1.0f);
                float final = DamageCalculator.CalculateDamage(damage, DamageType.Physical, enemy.armor, enemy.magicResist,
                    baseStats != null ? baseStats.critChance : 0f,
                    baseStats != null ? baseStats.critMultiplier : 1f);
                enemy.TakeDamage(final);
                Debug.Log($"[Character] PrimaryAttack hit {enemy.name} -> {final:F1} damage.");
                hitEnemy = true;
                break;
            }
        }

        if (!hitEnemy)
        {
            Debug.DrawRay(origin, dir * range, Color.red, 0.7f);
            Debug.Log("[Character] PrimaryAttack: no enemy hit.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (abilities == null) return;
        Gizmos.color = Color.red;
        foreach (var a in abilities)
        {
            if (a == null) continue;
            Vector3 center = transform.position + transform.forward * a.range;
            Gizmos.DrawWireSphere(center, a.radius);
        }

        if (baseStats != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 o = transform.position + Vector3.up * meleeVerticalOffset;
            Gizmos.DrawWireSphere(o, meleeRadius);
            Gizmos.DrawWireSphere(o + transform.forward * baseStats.attackRange, meleeRadius);
            Gizmos.DrawLine(o, o + transform.forward * baseStats.attackRange);
        }
    }

    public float GetCooldownRemaining(AbilityData ability)
    {
        if (ability == null || !cooldownTimers.ContainsKey(ability)) return 0f;
        return cooldownTimers[ability];
    }

    // Expose animator assignment in case PlayerManager wants to set it explicitly
    public void AssignAnimator(Animator a)
    {
        animator = a;
    }
}