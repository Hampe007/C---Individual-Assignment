using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeProgression : MonoBehaviour
{
    [SerializeField] private SkillTreeDefinition _skillTree;
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private SwordWeapon _swordWeapon;

    private readonly Dictionary<string, SkillTreeNode> _nodes = new();
    private readonly HashSet<string> _purchasedNodes = new();
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
        return _purchasedNodes.Contains(nodeId);
    }

    public bool CanPurchase(string nodeId)
    {
        return CanPurchase(GetNode(nodeId));
    }

    private bool CanPurchase(SkillTreeNode node)
    {
        if (!isActiveAndEnabled || node == null || node.Upgrade == null ||
            IsPurchased(node.Id) || _playerHealth.CurrentHealth <= 0 ||
            _playerProgression.SkillPoints <= 0)
            return false;

        _upgradeLevels.TryGetValue(node.Upgrade, out int level);

        if (level >= node.Upgrade.MaxLevel)
            return false;

        if (node.PrerequisiteIds != null)
        {
            foreach (string prerequisite in node.PrerequisiteIds)
            {
                if (!IsPurchased(prerequisite))
                    return false;
            }
        }

        return true;
    }

    public bool TryPurchase(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);

        if (!CanPurchase(node) || !ApplyUpgrade(node.Upgrade))
            return false;

        _upgradeLevels.TryGetValue(node.Upgrade, out int level);
        _upgradeLevels[node.Upgrade] = level + 1;
        _purchasedNodes.Add(node.Id);
        _playerProgression.SpendSkillPoint();
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
            case UpgradeEffectType.SwordCount:
                _swordWeapon.AddSword();
                break;
            case UpgradeEffectType.MaxHealth:
                _playerHealth.IncreaseMaxHealth(Mathf.RoundToInt(upgrade.Value));
                break;
            default:
                return false;
        }

        return true;
    }
}
