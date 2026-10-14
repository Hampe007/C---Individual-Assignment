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
    [Header("Orb Sizes")]
    [SerializeField, Min(0.1f)] private float _smallOrbScale = 0.65f;
    [SerializeField, Min(0.1f)] private float _mediumOrbScale = 1f;
    [SerializeField, Min(0.1f)] private float _largeOrbScale = 1.4f;

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
        config.SmallOrbScale = _smallOrbScale;
        config.MediumOrbScale = _mediumOrbScale;
        config.LargeOrbScale = _largeOrbScale;
        config.LeavesPoison = (byte)(Tier >= EnemyTier.Tier2 ? 1 : 0);
        return config;
    }
}
