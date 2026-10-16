using UnityEngine;

[CreateAssetMenu(fileName = "NewChargerEnemy", menuName = "Enemies/Charger Enemy")]
public sealed class ChargerEnemyDefinition : EnemyDefinition
{
    [Header("Charger")]
    [SerializeField, Min(0f)] private float chargeRange = 10f;
    [SerializeField, Min(0f)] private float chargeSpeed = 12f;
    [SerializeField, Min(0f)] private float chargeDuration = 0.55f;
    [SerializeField, Min(0f)] private float telegraphDuration = 0.65f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.8f;
    [SerializeField, Min(0f)] private float chargeCooldown = 5f;
    [SerializeField, Min(0f)] private float hitRange = 1.5f;

    public override EnemyBehaviourType Behaviour => EnemyBehaviourType.Charger;

    public override EnemyConfig CreateConfig()
    {
        EnemyConfig config = CreateBaseConfig();
        config.AttackRange = chargeRange;
        config.AttackCooldown = chargeCooldown;
        config.SpecialSpeed = chargeSpeed;
        config.TelegraphDuration = telegraphDuration;
        config.AttackDuration = chargeDuration;
        config.RecoveryDuration = recoveryDuration;
        config.HitRange = hitRange;
        return config;
    }
}
