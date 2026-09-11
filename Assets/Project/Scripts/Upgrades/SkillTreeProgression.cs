using UnityEngine;

public sealed class SkillTreeProgression : MonoBehaviour
{
    [SerializeField] private SkillTreeDefinition _skillTree;
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private UpgradeSystem _upgradeSystem;

    private bool[] _purchasedNodes;

    private void Awake()
    {
        if (_skillTree == null ||
            _playerProgression == null ||
            _playerHealth == null ||
            _upgradeSystem == null)
        {
            Debug.LogError("SkillTreeProgression is missing required references.");
            enabled = false;
            return;
        }

        _purchasedNodes = new bool[_skillTree.NodeCount];
    }

    public bool IsPurchased(string nodeId)
    {
        int index = GetNodeIndex(nodeId);

        if (index < 0)
            return false;

        return _purchasedNodes[index];
    }

    public SkillTreeNode GetNode(string nodeId)
    {
        int index = GetNodeIndex(nodeId);

        if (index < 0)
            return null;

        return _skillTree.GetNode(index);
    }
    
    public bool CanPurchase(string nodeId)
    {
        int index = GetNodeIndex(nodeId);

        if (index < 0)
            return false;

        if (_purchasedNodes[index])
            return false;

        if (_playerHealth.CurrentHealth <= 0)
            return false;

        if (_playerProgression.SkillPoints <= 0)
            return false;

        SkillTreeNode node = _skillTree.GetNode(index);

        if (node.Upgrade == null)
            return false;

        if (_upgradeSystem.IsMaxLevel(node.Upgrade))
            return false;

        return HasPrerequisites(node);
    }

    public bool TryPurchase(string nodeId)
    {
        if (!CanPurchase(nodeId))
            return false;

        int index = GetNodeIndex(nodeId);
        SkillTreeNode node = _skillTree.GetNode(index);

        if (!_upgradeSystem.ApplyUpgrade(node.Upgrade))
            return false;

        _purchasedNodes[index] = true;
        _playerProgression.SpendSkillPoint();

        return true;
    }

    private bool HasPrerequisites(SkillTreeNode node)
    {
        string[] prerequisites = node.PrerequisiteIds;

        if (prerequisites == null || prerequisites.Length == 0)
            return true;

        for (int i = 0; i < prerequisites.Length; i++)
        {
            if (!IsPurchased(prerequisites[i]))
                return false;
        }

        return true;
    }

    private int GetNodeIndex(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return -1;

        for (int i = 0; i < _skillTree.NodeCount; i++)
        {
            SkillTreeNode node = _skillTree.GetNode(i);

            if (node != null && node.Id == nodeId)
                return i;
        }

        return -1;
    }
    
    public bool HasAvailableNode()
    {
        for (int i = 0; i < _skillTree.NodeCount; i++)
        {
            SkillTreeNode node = _skillTree.GetNode(i);

            if (node != null && CanPurchase(node.Id))
                return true;
        }

        return false;
    }

    public int GetNodeCount()
    {
        return _skillTree.NodeCount;
    }
}