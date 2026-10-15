using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeProgression : MonoBehaviour
{
    public event System.Action UpgradePurchased;

    [SerializeField] private SkillTreeDefinition _skillTree;
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private SwordWeapon _swordWeapon;
    [SerializeField] private PlayerLantern _playerLantern;
    [SerializeField] private SoulBoltWeapon _soulBoltWeapon;

    private readonly Dictionary<string, SkillTreeNode> _nodes = new();
    private readonly Dictionary<UpgradeDefinition, int> _upgradeLevels = new();

    private void Awake()
    {
        if (_skillTree == null || _playerProgression == null ||
            _playerHealth == null || _playerMovement == null || _swordWeapon == null)
        {
            Debug.LogError("SkillTreeProgression is missing required references.");
            enabled = false;
            return;
        }

        for (int nodeIndex = 0; nodeIndex < _skillTree.NodeCount; nodeIndex++)
        {
            SkillTreeNode node = _skillTree.GetNode(nodeIndex);

            if (node == null || string.IsNullOrEmpty(node.Id) || _nodes.ContainsKey(node.Id))
            {
                Debug.LogError("Skill tree nodes require unique, non-empty IDs.");
                _nodes.Clear();
                enabled = false;
                return;
            }

            _nodes.Add(node.Id, node);
        }
    }

    public SkillTreeNode GetNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return null;

        _nodes.TryGetValue(nodeId, out SkillTreeNode node);
        return node;
    }

    public bool IsPurchased(string nodeId)
    {
        return GetLevel(nodeId) > 0;
    }

    public int GetLevel(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        return node != null && node.Upgrade != null &&
               _upgradeLevels.TryGetValue(node.Upgrade, out int level) ? level : 0;
    }

    public bool IsMaxed(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        return node != null && node.Upgrade != null && GetLevel(nodeId) >= node.Upgrade.MaxLevel;
    }

    public bool HasPrerequisites(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null)
            return false;

        if (node.PrerequisiteIds != null)
        {
            foreach (string prerequisite in node.PrerequisiteIds)
                if (!IsPurchased(prerequisite))
                    return false;
        }

        return true;
    }

    public IReadOnlyList<string> GetUnmetRequirements(string nodeId)
    {
        var requirements = new List<string>();
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null)
            return requirements;

        if (node.PrerequisiteIds != null)
        {
            foreach (string prerequisiteId in node.PrerequisiteIds)
            {
                if (IsPurchased(prerequisiteId))
                    continue;

                SkillTreeNode prerequisite = GetNode(prerequisiteId);
                requirements.Add(prerequisite != null && prerequisite.Upgrade != null
                    ? $"Unlock {prerequisite.Upgrade.DisplayName}"
                    : $"Unlock {prerequisiteId}");
            }
        }

        // A missing prerequisite already explains why its dependent weapon is unavailable.
        if (requirements.Count > 0)
            return requirements;

        switch (node.Upgrade.Effect)
        {
            case UpgradeEffectType.LanternRange when _playerLantern == null || !_playerLantern.IsReady:
                requirements.Add("Unlock and activate the Lantern");
                break;
            case UpgradeEffectType.LanternUnlock when _playerLantern == null || !_playerLantern.CanUnlock:
                requirements.Add("Lantern must be available");
                break;
            case UpgradeEffectType.SoulBoltUnlock when _soulBoltWeapon == null || _soulBoltWeapon.IsUnlocked:
                requirements.Add("Soul Bolt must be available");
                break;
            case UpgradeEffectType.SoulBoltDamage or UpgradeEffectType.SoulBoltCount or UpgradeEffectType.SoulBoltTracking
                when _soulBoltWeapon == null || !_soulBoltWeapon.IsUnlocked:
                requirements.Add("Unlock Soul Bolt");
                break;
            case UpgradeEffectType.SoulBoltCount when _soulBoltWeapon.BoltCount >= 5:
                requirements.Add("Soul Bolt count limit reached");
                break;
            case UpgradeEffectType.SoulBoltTracking when _soulBoltWeapon.TrackingTier >= 3:
                requirements.Add("Soul Bolt tracking limit reached");
                break;
        }

        return requirements;
    }

    public bool CanPurchase(string nodeId)
    {
        return CanPurchase(GetNode(nodeId));
    }

    private bool CanPurchase(SkillTreeNode node)
    {
        if (!isActiveAndEnabled || node == null || node.Upgrade == null ||
            _playerHealth.CurrentHealth <= 0 ||
            _playerProgression.SkillPoints <= 0)
            return false;

        return !IsLocked(node.Id) && !IsMaxed(node.Id);
    }

    public bool IsLocked(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null || !HasPrerequisites(nodeId))
            return true;

        if (node.Upgrade.Effect == UpgradeEffectType.LanternRange && (_playerLantern == null || !_playerLantern.IsReady))
            return true;

        if (node.Upgrade.Effect == UpgradeEffectType.LanternUnlock && (_playerLantern == null || !_playerLantern.CanUnlock))
            return true;

        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltUnlock && (_soulBoltWeapon == null || _soulBoltWeapon.IsUnlocked))
            return true;

        if ((node.Upgrade.Effect == UpgradeEffectType.SoulBoltDamage ||
             node.Upgrade.Effect == UpgradeEffectType.SoulBoltCount ||
             node.Upgrade.Effect == UpgradeEffectType.SoulBoltTracking) &&
            (_soulBoltWeapon == null || !_soulBoltWeapon.IsUnlocked))
            return true;

        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltCount && _soulBoltWeapon.BoltCount >= 5)
            return true;
        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltTracking && _soulBoltWeapon.TrackingTier >= 3)
            return true;

        return false;
    }

    public bool TryPurchase(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);

        if (!CanPurchase(node) || !ApplyUpgrade(node.Upgrade))
            return false;

        _upgradeLevels.TryGetValue(node.Upgrade, out int level);
        _upgradeLevels[node.Upgrade] = level + 1;
        _playerProgression.SpendSkillPoint();
        UpgradePurchased?.Invoke();
        return true;
    }

    public bool HasAvailableNode()
    {
        foreach (SkillTreeNode node in _nodes.Values)
        {
            if (CanPurchase(node))
                return true;
        }

        return false;
    }

    private bool ApplyUpgrade(UpgradeDefinition upgrade)
    {
        switch (upgrade.Effect)
        {
            case UpgradeEffectType.MoveSpeed:
                _playerMovement.AddMoveSpeed(upgrade.Value);
                break;
            case UpgradeEffectType.SwordDamage:
                _swordWeapon.AddDamage(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.SwordCooldown:
                _swordWeapon.ReduceCooldown(upgrade.Value);
                break;
            case UpgradeEffectType.Cleave:
                _swordWeapon.IncreaseTargetsPerSwing();
                break;
            case UpgradeEffectType.MaxHealth:
                _playerHealth.IncreaseMaxHealth(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.LanternRange:
                if (_playerLantern == null || !_playerLantern.IsReady)
                    return false;
                _playerLantern.IncreaseRange(upgrade.Value);
                break;
            case UpgradeEffectType.LanternUnlock:
                return _playerLantern != null && _playerLantern.TryUnlock();
            case UpgradeEffectType.SoulBoltUnlock:
                if (_soulBoltWeapon == null || _soulBoltWeapon.IsUnlocked) return false;
                _soulBoltWeapon.Unlock();
                break;
            case UpgradeEffectType.SoulBoltDamage:
                _soulBoltWeapon.AddDamage(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.SoulBoltCount:
                if (_soulBoltWeapon == null || !_soulBoltWeapon.IsUnlocked || _soulBoltWeapon.BoltCount >= 5)
                    return false;
                _soulBoltWeapon.AddBolt();
                break;
            case UpgradeEffectType.SoulBoltTracking:
                if (_soulBoltWeapon == null || !_soulBoltWeapon.IsUnlocked || _soulBoltWeapon.TrackingTier >= 3)
                    return false;
                _soulBoltWeapon.AddTrackingTier();
                break;
            default:
                return false;
        }

        return true;
    }
}
