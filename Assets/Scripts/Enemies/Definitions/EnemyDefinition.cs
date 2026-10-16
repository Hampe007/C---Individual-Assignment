using UnityEngine;

/// <summary>
/// Managed, inspector-authored enemy data. CreateConfig converts it into a value type that can be stored in a
/// NativeArray and safely read by Burst-compiled jobs; the prefab is used by the separate view/pooling system.
/// </summary>
public abstract class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string displayName;
    [SerializeField] private EnemyTier tier = EnemyTier.Tier1;
    [SerializeField] private GameObject prefab;

    [Header("Base Stats")]
    [SerializeField, Min(1)] private int health = 30;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(1)] private int damage = 10;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int xpReward = 1;
    [SerializeField, Min(0)] private int scoreReward = 10;

    public string DisplayName => displayName;
    public EnemyTier Tier => tier;
    public GameObject Prefab => prefab;

    public int XPReward => xpReward;
    public int ScoreReward => scoreReward;

    public abstract EnemyBehaviourType Behaviour { get; }
    public abstract EnemyConfig CreateConfig();

    protected EnemyConfig CreateBaseConfig()
    {
        return new EnemyConfig
        {
            Behaviour = Behaviour,
            Health = health,
            MoveSpeed = moveSpeed,
            Damage = damage,
            XPReward = xpReward,
            ScoreReward = scoreReward
        };
    }
}

public enum EnemyTier
{
    Tier1,
    Tier2,
    Tier3
}
