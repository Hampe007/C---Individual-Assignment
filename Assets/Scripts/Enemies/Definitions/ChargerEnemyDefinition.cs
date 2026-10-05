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
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = _chargeRange;
        config.AttackCooldown = _chargeCooldown;
        config.SpecialSpeed = _chargeSpeed;
        config.TelegraphDuration = _telegraphDuration;
        config.AttackDuration = _chargeDuration;
        config.RecoveryDuration = _recoveryDuration;
        config.HitRange = _hitRange;
        return config;
    }
}
