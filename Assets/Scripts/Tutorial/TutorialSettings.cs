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
    public string title;
    [TextArea(2, 4)] public string instruction;
    public TutorialGoal goal;
    [Min(0)] public int markerIndex;
    [Min(1)] public int targetCount = 1;
    [Min(1f)] public float duration = 20f;
}

[CreateAssetMenu(fileName = "Tutorial-Settings", menuName = "Game/Tutorial/Settings")]
public sealed class TutorialSettings : ScriptableObject
{
    public string menuScene = "MainMenu";
    public EnemyDefinition practiceEnemy;
    public TutorialLesson[] lessons;
}
