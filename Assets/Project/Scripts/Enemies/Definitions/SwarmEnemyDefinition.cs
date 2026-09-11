using UnityEngine;

[CreateAssetMenu(fileName = "NewSwarmEnemy", menuName = "Enemies/Swarm Enemy")]
public sealed class SwarmEnemyDefinition : EnemyDefinition
{
    [Header("Swarm")]
    [SerializeField, Min(0f)] private float _attackRange = 1.3f;
    [SerializeField, Min(0f)] private float _attackCooldown = 1f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Swarm;

    public override EnemyConfig CreateConfig()
    {
        return new EnemyConfig
        {
            Behaviour = Behaviour,
            Health = Health,
            MoveSpeed = MoveSpeed,
            Damage = Damage,

            XPReward = XPReward,
            ScoreReward = ScoreReward,
            
            AttackRange = _attackRange,
            AttackCooldown = _attackCooldown
        };
    }
}