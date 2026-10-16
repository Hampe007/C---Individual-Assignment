using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillTree", menuName = "Upgrades/Skill Tree")]
public sealed class SkillTreeDefinition : ScriptableObject
{
    [SerializeField] private SkillTreeNode[] nodes;
    public int NodeCount => nodes != null ? nodes.Length : 0;

    public SkillTreeNode GetNode(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length)
        {
            return null;
        }

        return nodes[index];
    }

    public SkillTreeNode GetNode(string id)
    {
        if (nodes == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].Id == id)
            {
                return nodes[i];
            }
        }

        return null;
    }
}

[Serializable]
public sealed class SkillTreeNode
{
    [SerializeField] private string id;
    [SerializeField] private UpgradeDefinition upgrade;
    [SerializeField] private string[] prerequisiteIds;
    public string Id => id;
    public UpgradeDefinition Upgrade => upgrade;
    public string[] PrerequisiteIds => prerequisiteIds;
}