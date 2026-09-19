using UnityEngine;

public abstract class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _displayName;
    [SerializeField] private EnemyTier _tier = EnemyTier.Tier1;
    [SerializeField] private GameObject _prefab;

    [Header("Base Stats")]
    [SerializeField, Min(1)] private int _health = 30;
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;
    [SerializeField, Min(1)] private int _damage = 10;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int _xpReward = 1;
    [SerializeField, Min(0)] private int _scoreReward = 10;

    public string DisplayName => _displayName;
    public EnemyTier Tier => _tier;
    public GameObject Prefab => _prefab;

    public int XPReward => _xpReward;
    public int ScoreReward => _scoreReward;

    public abstract EnemyBehaviourType Behaviour { get; }
    public abstract EnemyConfig CreateConfig();

    protected EnemyConfig CreateBaseConfig()
    {
        return new EnemyConfig
        {
            Behaviour = Behaviour,
            Health = _health,
            MoveSpeed = _moveSpeed,
            Damage = _damage,
            XPReward = _xpReward,
            ScoreReward = _scoreReward
        };
    }
}

public enum EnemyTier
{
    Tier1,
    Tier2,
    Tier3
}
