using UnityEngine;

public struct EnemyDeathData
{
    public Vector3 Position;
    public int DefinitionIndex;
    public int XPReward;
    public int ScoreReward;

    public EnemyDeathData(Vector3 position, int definitionIndex, int xpReward, int scoreReward)
    {
        Position = position;
        DefinitionIndex = definitionIndex;
        XPReward = xpReward;
        ScoreReward = scoreReward;
    }
}