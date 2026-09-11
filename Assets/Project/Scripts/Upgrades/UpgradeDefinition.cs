using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/Upgrade")]
public sealed class UpgradeDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string _displayName;

    [SerializeField, TextArea]
    private string _description;

    [Header("Effect")]
    [SerializeField] private UpgradeEffectType _effect;
    [SerializeField] private float _value = 1f;
    [SerializeField, Min(1)] private int _maxLevel = 5;

    public string DisplayName => _displayName;
    public string Description => _description;

    public UpgradeConfig CreateConfig()
    {
        return new UpgradeConfig
        {
            Effect = _effect,
            Value = _value,
            MaxLevel = _maxLevel
        };
    }
}