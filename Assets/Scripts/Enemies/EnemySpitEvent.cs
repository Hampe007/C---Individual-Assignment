using Unity.Mathematics;

/// <summary>A job-safe request to create a Puglin spit volley on the main thread.</summary>
public struct EnemySpitEvent
{
    public float3 Origin;
    public float3 Target;
    public int Damage;
    public byte Count;
    public float Speed;
    public float Lifetime;
    public float Spread;
}
