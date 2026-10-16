using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeProgression : MonoBehaviour
{
    public event System.Action UpgradePurchased;
    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private PlayerLantern playerLantern;
    [SerializeField] private SoulBoltWeapon soulBoltWeapon;
    private readonly Dictionary<string, SkillTreeNode> nodes = new();
    private readonly Dictionary<UpgradeDefinition, int> upgradeLevels = new();
    private void Awake()
    {
        if (skillTree == null || playerProgression == null || playerHealth == null || playerMovement == null || swordWeapon == null)
        {
            Debug.LogError("SkillTreeProgression is missing required references.");
            enabled = false;
            return;
        }

        for (int nodeIndex = 0; nodeIndex < skillTree.NodeCount; nodeIndex++)
        {
            SkillTreeNode node = skillTree.GetNode(nodeIndex);
            if (node == null || string.IsNullOrEmpty(node.Id) || nodes.ContainsKey(node.Id))
            {
                Debug.LogError("Skill tree nodes require unique, non-empty IDs.");
                nodes.Clear();
                enabled = false;
                return;
            }

            nodes.Add(node.Id, node);
        }
    }

    public SkillTreeNode GetNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        nodes.TryGetValue(nodeId, out SkillTreeNode node);
        return node;
    }

    public bool IsPurchased(string nodeId)
    {
        return GetLevel(nodeId) > 0;
    }

    public int GetLevel(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        return node != null && node.Upgrade != null && upgradeLevels.TryGetValue(node.Upgrade, out int level) ? level : 0;
    }

    public bool IsMaxed(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        return node != null && node.Upgrade != null && !node.Upgrade.IsRepeatable && GetLevel(nodeId) >= node.Upgrade.MaxLevel;
    }

    public bool HasPrerequisites(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null)
        {
            return false;
        }

        if (node.PrerequisiteIds != null)
        {
            foreach (string prerequisite in node.PrerequisiteIds)
            {
                if (!IsPurchased(prerequisite))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public IReadOnlyList<string> GetUnmetRequirements(string nodeId)
    {
        var requirements = new List<string>();
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null)
        {
            return requirements;
        }

        if (node.PrerequisiteIds != null)
        {
            foreach (string prerequisiteId in node.PrerequisiteIds)
            {
                if (IsPurchased(prerequisiteId))
                {
                    continue;
                }

                SkillTreeNode prerequisite = GetNode(prerequisiteId);
                requirements.Add(prerequisite != null && prerequisite.Upgrade != null ? $"Unlock {prerequisite.Upgrade.DisplayName}" : $"Unlock {prerequisiteId}");
            }
        }

        // A missing prerequisite already explains why its dependent weapon is unavailable.
        if (requirements.Count > 0)
        {
            return requirements;
        }

        switch (node.Upgrade.Effect)
        {
            case UpgradeEffectType.RestoreHealth when playerHealth.CurrentHealth >= playerHealth.MaxHealth:
                requirements.Add("Health is already full");
                break;
            case UpgradeEffectType.LanternRange when playerLantern == null || !playerLantern.IsReady:
                requirements.Add("Unlock and activate the Lantern");
                break;
            case UpgradeEffectType.LanternUnlock when playerLantern == null || !playerLantern.CanUnlock:
                requirements.Add("Lantern must be available");
                break;
            case UpgradeEffectType.SoulBoltUnlock when soulBoltWeapon == null || soulBoltWeapon.IsUnlocked:
                requirements.Add("Soul Bolt must be available");
                break;
            case UpgradeEffectType.SoulBoltDamage or UpgradeEffectType.SoulBoltCount or UpgradeEffectType.SoulBoltTracking when soulBoltWeapon == null || !soulBoltWeapon.IsUnlocked:
                requirements.Add("Unlock Soul Bolt");
                break;
            case UpgradeEffectType.SoulBoltCount when soulBoltWeapon.BoltCount >= 5:
                requirements.Add("Soul Bolt count limit reached");
                break;
            case UpgradeEffectType.SoulBoltTracking when soulBoltWeapon.TrackingTier >= 3:
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
        if (!isActiveAndEnabled || node == null || node.Upgrade == null || playerHealth.CurrentHealth <= 0 || playerProgression.SkillPoints <= 0)
        {
            return false;
        }

        return !IsLocked(node.Id) && !IsMaxed(node.Id);
    }

    public bool IsLocked(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        if (node == null || node.Upgrade == null || !HasPrerequisites(nodeId))
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.RestoreHealth && playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.LanternRange && (playerLantern == null || !playerLantern.IsReady))
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.LanternUnlock && (playerLantern == null || !playerLantern.CanUnlock))
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltUnlock && (soulBoltWeapon == null || soulBoltWeapon.IsUnlocked))
        {
            return true;
        }

        if ((node.Upgrade.Effect == UpgradeEffectType.SoulBoltDamage || node.Upgrade.Effect == UpgradeEffectType.SoulBoltCount || node.Upgrade.Effect == UpgradeEffectType.SoulBoltTracking) && (soulBoltWeapon == null || !soulBoltWeapon.IsUnlocked))
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltCount && soulBoltWeapon.BoltCount >= 5)
        {
            return true;
        }

        if (node.Upgrade.Effect == UpgradeEffectType.SoulBoltTracking && soulBoltWeapon.TrackingTier >= 3)
        {
            return true;
        }

        return false;
    }

    public bool TryPurchase(string nodeId)
    {
        SkillTreeNode node = GetNode(nodeId);
        if (!CanPurchase(node) || !ApplyUpgrade(node.Upgrade))
        {
            return false;
        }

        upgradeLevels.TryGetValue(node.Upgrade, out int level);
        upgradeLevels[node.Upgrade] = level + 1;
        playerProgression.SpendSkillPoint();
        UpgradePurchased?.Invoke();
        return true;
    }

    public bool HasAvailableNode()
    {
        foreach (SkillTreeNode node in nodes.Values)
        {
            if (CanPurchase(node))
            {
                return true;
            }
        }

        return false;
    }

    private bool ApplyUpgrade(UpgradeDefinition upgrade)
    {
        switch (upgrade.Effect)
        {
            case UpgradeEffectType.RestoreHealth:
                return playerHealth.TryHeal(Mathf.CeilToInt(playerHealth.MaxHealth * upgrade.Value));
            case UpgradeEffectType.MoveSpeed:
                playerMovement.AddMoveSpeed(upgrade.Value);
                break;
            case UpgradeEffectType.SwordDamage:
                swordWeapon.AddDamage(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.SwordCooldown:
                swordWeapon.ReduceCooldown(upgrade.Value);
                break;
            case UpgradeEffectType.Cleave:
                swordWeapon.IncreaseTargetsPerSwing();
                break;
            case UpgradeEffectType.MaxHealth:
                playerHealth.IncreaseMaxHealth(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.LanternRange:
                if (playerLantern == null || !playerLantern.IsReady)
                {
                    return false;
                }

                playerLantern.IncreaseRange(upgrade.Value);
                break;
            case UpgradeEffectType.LanternUnlock:
                return playerLantern != null && playerLantern.TryUnlock();
            case UpgradeEffectType.SoulBoltUnlock:
                if (soulBoltWeapon == null || soulBoltWeapon.IsUnlocked)
                {
                    return false;
                }

                soulBoltWeapon.Unlock();
                break;
            case UpgradeEffectType.SoulBoltDamage:
                soulBoltWeapon.AddDamage(Mathf.RoundToInt(upgrade.Value));
                break;
            case UpgradeEffectType.SoulBoltCount:
                if (soulBoltWeapon == null || !soulBoltWeapon.IsUnlocked || soulBoltWeapon.BoltCount >= 5)
                {
                    return false;
                }

                soulBoltWeapon.AddBolt();
                break;
            case UpgradeEffectType.SoulBoltTracking:
                if (soulBoltWeapon == null || !soulBoltWeapon.IsUnlocked || soulBoltWeapon.TrackingTier >= 3)
                {
                    return false;
                }

                soulBoltWeapon.AddTrackingTier();
                break;
            default:
                return false;
        }

        return true;
    }
}