using Unity.Mathematics;

public struct EnemyRuntime
{
    public float3 Position;
    public float3 ChargeDirection;

    public float Speed;
    public float StateTimer;
    public float AttackCooldown;

    public byte HasHit;
    
    public EnemyBehaviourType Behaviour;
    public EnemyState State;
    public EnemyVisualType Visual;

    public EnemyRuntime(float3 position, float speed, EnemyBehaviourType behaviour, EnemyVisualType visual)
    {
        Position = position;
        ChargeDirection = float3.zero;
        Speed = speed;
        StateTimer = 0f;
        AttackCooldown = 0f;
        HasHit = 0;
        Behaviour = behaviour;
        Visual = visual;
        State = EnemyState.Chase;
    }
}