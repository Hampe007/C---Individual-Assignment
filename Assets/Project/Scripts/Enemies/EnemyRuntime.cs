using Unity.Mathematics;

public struct EnemyRuntime
{
    public float3 Position;
    public float3 ChargeDirection;

    public float Speed;
    public float StateTimer;
    public float AttackCooldown;

    public EnemyBehaviourType Behaviour;
    public EnemyState State;

    public EnemyRuntime(float3 position, float speed, EnemyBehaviourType behaviour)
    {
        Position = position;
        ChargeDirection = float3.zero;
        Speed = speed;
        StateTimer = 0f;
        AttackCooldown = 0f;
        Behaviour = behaviour;
        State = EnemyState.Chase;
    }
}