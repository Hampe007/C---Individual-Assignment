using UnityEngine;

[CreateAssetMenu(fileName = "NewBruteEnemy", menuName = "Enemies/Brute Enemy")]
public sealed class BruteEnemyDefinition : EnemyDefinition
{
    [Header("Brute")]
    [SerializeField, Min(0f)] private float _slamRange = 3f;
    [SerializeField, Min(0f)] private float _windupDuration = 1.2f;
    [SerializeField, Min(0f)] private float _attackDuration = 0.25f;
    [SerializeField, Min(0f)] private float _recoveryDuration = 1.5f;
    [SerializeField, Min(0f)] private float _attackCooldown = 2.5f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Brute;

    public override EnemyConfig CreateConfig()
    {
        return new EnemyConfig
        {
            Behaviour = Behaviour,
            Health = Health,
            MoveSpeed = MoveSpeed,
            Damage = Damage,

            AttackRange = _slamRange,
            AttackCooldown = _attackCooldown,

            TelegraphDuration = _windupDuration,
            AttackDuration = _attackDuration,
            RecoveryDuration = _recoveryDuration,
            HitRange = _slamRange
        };
    }
}