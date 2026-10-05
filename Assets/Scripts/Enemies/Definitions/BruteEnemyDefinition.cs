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
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = _slamRange;
        config.AttackCooldown = _attackCooldown;
        config.TelegraphDuration = _windupDuration;
        config.AttackDuration = _attackDuration;
        config.RecoveryDuration = _recoveryDuration;
        config.HitRange = _slamRange;
        return config;
    }
}
