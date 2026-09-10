using UnityEngine;

public abstract class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _displayName;
    [SerializeField] private GameObject _prefab;

    [Header("Base Stats")]
    [SerializeField, Min(1)] private int _health = 30;
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;
    [SerializeField, Min(1)] private int _damage = 10;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int _xpReward = 1;
    [SerializeField, Min(0)] private int _scoreReward = 10;

    public string DisplayName => _displayName;
    public GameObject Prefab => _prefab;
    public int XPReward => _xpReward;
    public int ScoreReward => _scoreReward;

    protected int Health => _health;
    protected float MoveSpeed => _moveSpeed;
    protected int Damage => _damage;

    public abstract EnemyBehaviourType Behaviour { get; }
    public abstract EnemyConfig CreateConfig();
}