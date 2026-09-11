using System;
using UnityEngine;

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