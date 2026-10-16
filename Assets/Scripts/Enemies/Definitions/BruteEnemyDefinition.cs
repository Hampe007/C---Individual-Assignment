using UnityEngine;

[CreateAssetMenu(fileName = "NewBruteEnemy", menuName = "Enemies/Brute Enemy")]
public sealed class BruteEnemyDefinition : EnemyDefinition
{
    [Header("Brute")]
    [SerializeField, Min(0f)] private float slamRange = 3f;
    [SerializeField, Min(0f)] private float windupDuration = 1.2f;
    [SerializeField, Min(0f)] private float attackDuration = 0.25f;
    [SerializeField, Min(0f)] private float recoveryDuration = 1.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 2.5f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Brute;

    public override EnemyConfig CreateConfig()
    {
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = slamRange;
        config.AttackCooldown = attackCooldown;
        config.TelegraphDuration = windupDuration;
        config.AttackDuration = attackDuration;
        config.RecoveryDuration = recoveryDuration;
        config.HitRange = slamRange;
        return config;
    }
}
