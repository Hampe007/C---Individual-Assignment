using UnityEngine;

[CreateAssetMenu(fileName = "NewChargerEnemy", menuName = "Enemies/Charger Enemy")]
public sealed class ChargerEnemyDefinition : EnemyDefinition
{
    [Header("Charger")]
    [SerializeField, Min(0f)] private float _chargeRange = 10f;
    [SerializeField, Min(0f)] private float _chargeSpeed = 12f;
    [SerializeField, Min(0f)] private float _chargeDuration = 0.55f;
    [SerializeField, Min(0f)] private float _telegraphDuration = 0.65f;
    [SerializeField, Min(0f)] private float _recoveryDuration = 0.8f;
    [SerializeField, Min(0f)] private float _chargeCooldown = 5f;
    [SerializeField, Min(0f)] private float _hitRange = 1.5f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Charger;

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
            
            AttackRange = _chargeRange,
            AttackCooldown = _chargeCooldown,

            SpecialSpeed = _chargeSpeed,
            TelegraphDuration = _telegraphDuration,
            AttackDuration = _chargeDuration,
            RecoveryDuration = _recoveryDuration,
            HitRange = _hitRange
        };
    }
}