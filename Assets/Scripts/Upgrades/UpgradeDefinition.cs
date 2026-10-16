using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/Upgrade")]
public sealed class UpgradeDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName;

    [SerializeField, TextArea] private string description;

    [Header("Effect")]
    [SerializeField] private UpgradeEffectType effect;
    [SerializeField] private float value = 1f;
    [SerializeField, Min(1)] private int maxLevel = 5;
    [SerializeField] private bool repeatable;

    public string DisplayName => displayName;
    public string Description => description;

    public UpgradeEffectType Effect => effect;
    public float Value => value;
    public int MaxLevel => maxLevel;
    public bool IsRepeatable => repeatable;
}

public enum UpgradeEffectType
{
    MoveSpeed,
    SwordDamage,
    SwordCooldown,
    Cleave,
    MaxHealth,
    LanternRange,
    LanternUnlock,
    SoulBoltUnlock,
    SoulBoltDamage,
    SoulBoltCount,
    SoulBoltTracking,
    RestoreHealth
}
