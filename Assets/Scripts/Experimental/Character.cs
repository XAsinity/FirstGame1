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

    private Animator animator;
    private Dictionary<AbilityData, float> cooldownTimers = new Dictionary<AbilityData, float>();

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        // If Character was placed manually in scene without init, seed runtime from SO
        if (baseStats != null && currentHealth == 0f)
            ApplyStatsFromSO();
        SetupCooldowns();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        // cooldowns
        var keys = new List<AbilityData>(cooldownTimers.Keys);
        foreach (var a in keys)
            cooldownTimers[a] = Mathf.Max(0f, cooldownTimers[a] - dt);

        // resource regen
        if (baseStats != null)
            currentResource = Mathf.Min(baseStats.resourceMax, currentResource + baseStats.healthRegen * dt);
    }

    public void InitializeFromProfile(CharacterProfile profile)
    {
        // assign references (read-only SO references are fine), then copy initial runtime values
        baseStats = profile.stats;
        abilities = new List<AbilityData>(profile.abilities);

        ApplyStatsFromSO();
        SetupCooldowns();
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
        if (!CanUseAbility(ability)) return;
        if (baseStats != null) currentResource -= ability.resourceCost;
        cooldownTimers[ability] = ability.cooldown;
        if (animator != null && !string.IsNullOrEmpty(ability.animatorTrigger))
            animator.SetTrigger(ability.animatorTrigger);
        StartCoroutine(PerformAbilityRoutine(ability));
    }

    private IEnumerator PerformAbilityRoutine(AbilityData ability)
    {
        if (ability == null) yield break;
        yield return new WaitForSeconds(0.15f);
        Vector3 center = transform.position + transform.forward * ability.range;
        if (ability.effectPrefab != null)
            Instantiate(ability.effectPrefab, center, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(center, ability.radius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                float raw = ability.damage;
                if (ability.scaleWithPhysical && baseStats != null) raw += baseStats.basePhysicalDamage * ability.scaleMultiplier;
                float final = DamageCalculator.CalculateDamage(raw, ability.damageType, enemy.armor, enemy.magicResist, baseStats != null ? baseStats.critChance : 0f, baseStats != null ? baseStats.critMultiplier : 1f);
                enemy.TakeDamage(final);
            }
        }
    }
}