using UnityEngine;

[CreateAssetMenu(fileName = "NewSpitterEnemy", menuName = "Enemies/Spitter Enemy")]
public sealed class SpitterEnemyDefinition : EnemyDefinition
{
    [SerializeField, Min(1f)] private float _attackRange = 12f;
    [SerializeField, Min(1f)] private float _preferredRange = 8f;
    [SerializeField, Min(0.1f)] private float _windupDuration = 0.75f;
    [SerializeField, Min(0.1f)] private float _recoveryDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float _attackCooldown = 3f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 9f;
    [SerializeField, Min(0.1f)] private float _projectileLifetime = 2f;
    [SerializeField, Range(0f, 45f)] private float _projectileSpread = 16f;
    [SerializeField, Range(1, 5)] private int _projectileCount = 1;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Spitter;

    public override EnemyConfig CreateConfig()
    {
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = _attackRange;
        config.PreferredRange = _preferredRange;
        config.TelegraphDuration = _windupDuration;
        config.RecoveryDuration = _recoveryDuration;
        config.AttackCooldown = _attackCooldown;
        config.ProjectileSpeed = _projectileSpeed;
        config.ProjectileLifetime = _projectileLifetime;
        config.ProjectileSpread = _projectileSpread;
        config.ProjectileCount = (byte)_projectileCount;
        return config;
    }
}
