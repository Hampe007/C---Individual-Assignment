using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillTree", menuName = "Upgrades/Skill Tree")]
public sealed class SkillTreeDefinition : ScriptableObject
{
    [SerializeField] private SkillTreeNode[] _nodes;

    public int NodeCount => _nodes != null ? _nodes.Length : 0;

    public SkillTreeNode GetNode(int index)
    {
        if (_nodes == null || index < 0 || index >= _nodes.Length)
            return null;

        return _nodes[index];
    }

    public SkillTreeNode GetNode(string id)
    {
        if (_nodes == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i] != null && _nodes[i].Id == id)
                return _nodes[i];
        }

        return null;
    }
}

[Serializable]
public sealed class SkillTreeNode
{
    [SerializeField] private string _id;
    [SerializeField] private UpgradeDefinition _upgrade;
    [SerializeField] private string[] _prerequisiteIds;

    public string Id => _id;
    public UpgradeDefinition Upgrade => _upgrade;
    public string[] PrerequisiteIds => _prerequisiteIds;
}
