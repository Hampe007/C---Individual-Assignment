using UnityEngine;

public sealed class UpgradeSystem : MonoBehaviour
{
    [SerializeField] private UpgradeDefinition[] _upgrades;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private SwordWeapon _swordWeapon;
    [SerializeField] private PlayerHealth _playerHealth;
    
    private int[] _upgradeLevels;

    private void Awake()
    {
        if (_playerMovement == null || _swordWeapon == null || _playerHealth == null || _upgrades == null || _upgrades.Length == 0)
        {
            Debug.LogError("UpgradeSystem is missing required references.");
            enabled = false;
            return;
        }

        _upgradeLevels = new int[_upgrades.Length];
    }

    public bool ApplyUpgrade(UpgradeDefinition upgrade)
    {
        int index = GetUpgradeIndex(upgrade);

        if (index < 0)
            return false;

        UpgradeConfig config = upgrade.CreateConfig();

        if (_upgradeLevels[index] >= config.MaxLevel)
            return false;

        switch (config.Effect)
        {
            case UpgradeEffectType.MoveSpeed:
                _playerMovement.AddMoveSpeed(config.Value);
                break;

            case UpgradeEffectType.SwordDamage:
                _swordWeapon.AddDamage(Mathf.RoundToInt(config.Value));
                break;
            
            case UpgradeEffectType.SwordCooldown:
                _swordWeapon.ReduceCooldown(config.Value);
                break;

            case UpgradeEffectType.SwordCount:
                _swordWeapon.AddSword();
                break;

            case UpgradeEffectType.MaxHealth:
                _playerHealth.IncreaseMaxHealth(Mathf.RoundToInt(config.Value));
                break;
            
            default:
                return false;
        }

        _upgradeLevels[index]++;
        return true;
    }

    public int GetLevel(UpgradeDefinition upgrade)
    {
        int index = GetUpgradeIndex(upgrade);

        if (index < 0)
            return 0;

        return _upgradeLevels[index];
    }

    public bool IsMaxLevel(UpgradeDefinition upgrade)
    {
        int index = GetUpgradeIndex(upgrade);

        if (index < 0)
            return true;

        UpgradeConfig config = upgrade.CreateConfig();
        return _upgradeLevels[index] >= config.MaxLevel;
    }
    
    private int GetUpgradeIndex(UpgradeDefinition upgrade)
    {
        for (int i = 0; i < _upgrades.Length; i++)
        {
            if (_upgrades[i] == upgrade)
                return i;
        }

        return -1;
    }
}