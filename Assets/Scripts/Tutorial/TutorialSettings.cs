using System;
using UnityEngine;

public enum TutorialGoal
{
    ReachMarker,
    DefeatEnemies,
    ReachLevel,
    ChooseUpgrade,
    Survive
}

[Serializable]
public sealed class TutorialLesson
{
    public string Title;
    [TextArea(2, 4)] public string Instruction;
    public TutorialGoal Goal;
    [Min(0)] public int MarkerIndex;
    [Min(1)] public int TargetCount = 1;
    [Min(1f)] public float Duration = 20f;
}

[CreateAssetMenu(fileName = "Tutorial-Settings", menuName = "Game/Tutorial/Settings")]
public sealed class TutorialSettings : ScriptableObject
{
    public string MenuScene = "MainMenu";
    public EnemyDefinition PracticeEnemy;
    public TutorialLesson[] Lessons;
}
