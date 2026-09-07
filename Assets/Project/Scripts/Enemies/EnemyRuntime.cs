using Unity.Mathematics;

public struct EnemyRuntime
{
    public float3 Position;
    public float Speed;

    public EnemyRuntime(float3 position, float speed)
    {
        Position = position;
        Speed = speed;
    }
}