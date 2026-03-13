using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Character : MonoBehaviour
{
    [Header("Assigned at edit-time (optional)")]
    public CharacterStats baseStats; // reference to SO (template). We'll copy runtime values from this.
    public List<AbilityData> abilities = new List<AbilityData>();

    [Header("Runtime (do not edit the SOs directly!)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentResource;

    [Header("Primary attack (melee) settings")]
    public LayerMask meleeHitLayers = ~0; // configure to only hit enemies
    public float meleeVerticalOffset = 1.0f; // raise ray origin so it hits chest/head height

    private Animator animator;
    private Dictionary<AbilityData, float> cooldownTimers = new Dictionary<AbilityData, float>();

    // Pending ability fired by UseAbility; consumed by animation event OnAbilityHit or DoPendingAbility immediate fallback
    private AbilityData pendingAbility;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
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

        // consume resource and set cooldown
        if (baseStats != null) currentResource -= ability.resourceCost;
        cooldownTimers[ability] = ability.cooldown;

        // set the pending ability so animation event knows what to apply
        pendingAbility = ability;

        // trigger the animation
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
        // wait one frame to allow any animation states to start (optional)
        yield return null;
        DoPendingAbility();
    }

    // Animation Event should call this on the hit frame: name: OnAbilityHit
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

    // Primary attack (M1) — does an immediate forward raycast (useful for melee) and applies basePhysicalDamage
    public void PrimaryAttack()
    {
        if (baseStats == null)
        {
            Debug.Log("[Character] PrimaryAttack aborted: baseStats missing.");
            return;
        }

        float range = baseStats.attackRange;
        float damage = baseStats.basePhysicalDamage;

        // trigger attack animation if present
        if (animator != null)
        {
            animator.SetTrigger("PrimaryAttack");
        }

        // Ray origin slightly above ground to better hit colliders
        Vector3 origin = transform.position + Vector3.up * meleeVerticalOffset;
        Vector3 dir = transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, range, meleeHitLayers))
        {
            Debug.DrawLine(origin, hit.point, Color.green, 1.0f);
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                float final = DamageCalculator.CalculateDamage(damage, DamageType.Physical, enemy.armor, enemy.magicResist,
                    baseStats != null ? baseStats.critChance : 0f,
                    baseStats != null ? baseStats.critMultiplier : 1f);
                enemy.TakeDamage(final);
                Debug.Log($"[Character] PrimaryAttack hit {enemy.name} -> {final:F1} damage.");
            }
            else
            {
                Debug.Log($"[Character] PrimaryAttack ray hit '{hit.collider.name}' but no EnemyHealth component found.");
            }
        }
        else
        {
            Debug.DrawRay(origin, dir * range, Color.red, 0.7f);
            Debug.Log("[Character] PrimaryAttack: no hit.");
        }
    }

    // Debug visualization for ability ranges
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

        // draw primary attack ray
        if (baseStats != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 o = transform.position + Vector3.up * meleeVerticalOffset;
            Gizmos.DrawLine(o, o + transform.forward * baseStats.attackRange);
        }
    }

    // Optional: get cooldown remaining for UI
    public float GetCooldownRemaining(AbilityData ability)
    {
        if (ability == null || !cooldownTimers.ContainsKey(ability)) return 0f;
        return cooldownTimers[ability];
    }
}