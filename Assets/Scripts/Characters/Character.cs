using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core runtime character controller that manages abilities, primary attack, health, and resources.
/// Initialized via <see cref="InitializeFromProfile"/> (called by PlayerManager) from a <see cref="CharacterProfile"/>.
/// Abilities are triggered by <see cref="AbilityInput"/> and resolved through Unity animation events
/// or immediately when no animation trigger is configured.
/// Primary attack uses a SphereCast in the forward direction and respects a per-character cooldown
/// defined in <see cref="CharacterStats.primaryAttackCooldown"/>.
/// Damage calculations are delegated to <see cref="DamageSystem.CalculateDamage"/> via
/// <see cref="DamageInfo"/>, which applies global base damage offsets, stat scaling,
/// armor/magic-resist mitigation, and crit rolls.
/// SphereCast/OverlapSphere origins, radii and ranges are multiplied by the character's visual
/// scale (average of transform.lossyScale) so hit detection matches the scaled model.
/// </summary>
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
    private float primaryAttackTimer;

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

        if (primaryAttackTimer > 0f)
            primaryAttackTimer -= dt;

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

        // Scale origin and radius by the visual model's world-space scale so hit
        // detection matches the rendered character size.
        float visualScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
        Vector3 center = transform.position
            + Vector3.up * (meleeVerticalOffset * visualScale)
            + transform.forward * (pendingAbility.range * visualScale);
        float scaledRadius = pendingAbility.radius * visualScale;

        if (pendingAbility.effectPrefab != null)
            Instantiate(pendingAbility.effectPrefab, center, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(center, scaledRadius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                DamageInfo info = pendingAbility.ToDamageInfo();
                float final = DamageSystem.CalculateDamage(
                    info,
                    baseStats != null ? baseStats.basePhysicalDamage : 0f,
                    baseStats != null ? baseStats.baseMagicDamage    : 0f,
                    baseStats != null ? baseStats.critChance         : 0f,
                    baseStats != null ? baseStats.critMultiplier     : 1f,
                    enemy.armor,
                    enemy.magicResist
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

        if (primaryAttackTimer > 0f)
        {
            Debug.Log($"[Character] PrimaryAttack on cooldown ({primaryAttackTimer:F2}s remaining).");
            return;
        }

        float range = baseStats.attackRange;

        primaryAttackTimer = baseStats.primaryAttackCooldown;

        if (animator != null)
            animator.SetTrigger("PrimaryAttack");

        // Scale origin, radius and range by the visual model's world-space scale so the
        // SphereCast matches the height and size of the rendered character.
        float visualScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
        Vector3 origin = transform.position + Vector3.up * (meleeVerticalOffset * visualScale);
        Vector3 dir = transform.forward;

        RaycastHit[] hits = _meleeSphereBuffer;
        int hitCount = Physics.SphereCastNonAlloc(origin, meleeRadius * visualScale, dir, hits, range * visualScale, meleeHitLayers);
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

                var info = new DamageInfo
                {
                    type              = DamageType.Physical,
                    baseDamage        = 0f,
                    scaleWithPhysical = true,
                    scaleMultiplier   = 1f
                };
                float final = DamageSystem.CalculateDamage(
                    info,
                    baseStats != null ? baseStats.basePhysicalDamage : 0f,
                    baseStats != null ? baseStats.baseMagicDamage    : 0f,
                    baseStats != null ? baseStats.critChance         : 0f,
                    baseStats != null ? baseStats.critMultiplier     : 1f,
                    enemy.armor,
                    enemy.magicResist
                );

                enemy.TakeDamage(final);
                Debug.Log($"[Character] PrimaryAttack hit {enemy.name} -> {final:F1} damage.");
                hitEnemy = true;
                break;
            }
        }

        if (!hitEnemy)
        {
            Debug.DrawRay(origin, dir * (range * visualScale), Color.red, 0.7f);
            Debug.Log("[Character] PrimaryAttack: no enemy hit.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (abilities == null) return;

        // Use average lossyScale so gizmos respect model scaling in the Scene view.
        float visualScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;

        Gizmos.color = Color.red;
        foreach (var a in abilities)
        {
            if (a == null) continue;
            Vector3 center = transform.position
                + Vector3.up * (meleeVerticalOffset * visualScale)
                + transform.forward * (a.range * visualScale);
            Gizmos.DrawWireSphere(center, a.radius * visualScale);
        }

        if (baseStats != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 o = transform.position + Vector3.up * (meleeVerticalOffset * visualScale);
            Gizmos.DrawWireSphere(o, meleeRadius * visualScale);
            Gizmos.DrawWireSphere(o + transform.forward * (baseStats.attackRange * visualScale), meleeRadius * visualScale);
            Gizmos.DrawLine(o, o + transform.forward * (baseStats.attackRange * visualScale));
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