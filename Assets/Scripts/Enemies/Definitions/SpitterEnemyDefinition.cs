using UnityEngine;

[CreateAssetMenu(fileName = "NewSpitterEnemy", menuName = "Enemies/Spitter Enemy")]
public sealed class SpitterEnemyDefinition : EnemyDefinition
{
    [SerializeField, Min(1f)] private float attackRange = 12f;
    [SerializeField, Min(1f)] private float preferredRange = 8f;
    [SerializeField, Min(0.1f)] private float windupDuration = 0.75f;
    [SerializeField, Min(0.1f)] private float recoveryDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 3f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 9f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 2f;
    [SerializeField, Range(0f, 45f)] private float projectileSpread = 16f;
    [SerializeField, Range(1, 5)] private int projectileCount = 1;
    [Header("Orb Sizes")]
    [SerializeField, Min(0.1f)] private float smallOrbScale = 0.65f;
    [SerializeField, Min(0.1f)] private float mediumOrbScale = 1f;
    [SerializeField, Min(0.1f)] private float largeOrbScale = 1.4f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Spitter;

    public override EnemyConfig CreateConfig()
    {
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = attackRange;
        config.PreferredRange = preferredRange;
        config.TelegraphDuration = windupDuration;
        config.RecoveryDuration = recoveryDuration;
        config.AttackCooldown = attackCooldown;
        config.ProjectileSpeed = projectileSpeed;
        config.ProjectileLifetime = projectileLifetime;
        config.ProjectileSpread = projectileSpread;
        config.ProjectileCount = (byte)projectileCount;
        config.SmallOrbScale = smallOrbScale;
        config.MediumOrbScale = mediumOrbScale;
        config.LargeOrbScale = largeOrbScale;
        config.LeavesPoison = (byte)(Tier >= EnemyTier.Tier2 ? 1 : 0);
        return config;
    }
}
