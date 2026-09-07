using Unity.Mathematics;

public struct EnemyRuntime
{
    public float3 Position;
    public float Speed;

    public EnemyBehaviourType Behaviour;
    public EnemyState State;

    public float StateTimer;
    public float AttackCooldown;
    public float3 ChargeDirection;
    
    public EnemyRuntime(
        float3 position,
        float speed,
        EnemyBehaviourType behaviour)
    {
        Position = position;
        Speed = speed;

        Behaviour = behaviour;
        State = EnemyState.Chase;

        StateTimer = 0f;
        AttackCooldown = 0f;
        
        ChargeDirection = float3.zero;
    }
}