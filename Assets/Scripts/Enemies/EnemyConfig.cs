/// <summary>Immutable job-friendly stats copied from an EnemyDefinition during HordeManager initialization.</summary>
public struct EnemyConfig
{
    public EnemyBehaviourType Behaviour;

    public int Health;
    public float MoveSpeed;
    public int Damage;

    public int XPReward;
    public int ScoreReward;

    public float AttackRange;
    public float AttackCooldown;

    public float SpecialSpeed;
    public float TelegraphDuration;
    public float AttackDuration;
    public float RecoveryDuration;
    public float HitRange;
    public float PreferredRange;
    public float ProjectileSpeed;
    public float ProjectileLifetime;
    public float ProjectileSpread;
    public byte ProjectileCount;
    public float SmallOrbScale;
    public float MediumOrbScale;
    public float LargeOrbScale;
    public byte LeavesPoison;
}

public enum EnemyBehaviourType : byte
{
    Swarm,
    Charger,
    Brute,
    Spitter
}
