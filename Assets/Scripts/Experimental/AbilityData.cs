using UnityEngine;

public enum DamageType { Physical, Magical, True }

[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName = "New Ability";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Usage")]
    public float cooldown = 3f;
    public float resourceCost = 0f;

    [Header("Damage")]
    public DamageType damageType = DamageType.Physical;
    public float damage = 10f;
    public bool scaleWithPhysical = true;
    public float scaleMultiplier = 1f;

    [Header("Area / Range")]
    public float radius = 2f;
    public float range = 1f;

    [Header("VFX / Animation")]
    public string animatorTrigger; // exact trigger name in Animator
    public GameObject effectPrefab;

    [Header("Flags")]
    public bool isUltimate = false;
}