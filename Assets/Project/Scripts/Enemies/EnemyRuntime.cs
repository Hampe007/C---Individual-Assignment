using Unity.Mathematics;

public struct EnemyRuntime
{
    public float3 Position;
    public float3 ChargeDirection;
    
    public float StateTimer;
    public float AttackCooldown;
    
    public int Health;
    public byte HasHit;
    public int DefinitionIndex;
    
    public EnemyState State;
    
    public EnemyRuntime(float3 position, int health, int definitionIndex)
    {
        Position = position;
        ChargeDirection = float3.zero;
        StateTimer = 0f;
        AttackCooldown = 0f;
        Health = health;
        HasHit = 0;
        DefinitionIndex = definitionIndex;
        State = EnemyState.Chase;
    }
}
