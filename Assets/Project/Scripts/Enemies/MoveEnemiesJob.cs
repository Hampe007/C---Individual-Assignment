using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MoveEnemiesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public NativeArray<EnemyConfig> EnemyConfigs;
    [ReadOnly] public NativeParallelMultiHashMap<int2, int>.ReadOnly Grid;

    [WriteOnly] public NativeArray<EnemyRuntime> NextEnemies;

    public NativeQueue<DamageEvent>.ParallelWriter DamageEvents;
    
    [ReadOnly] public float3 Target;
    [ReadOnly] public float DeltaTime;

    [ReadOnly] public float CellSize;
    [ReadOnly] public float SeparationDistance;
    [ReadOnly] public float SeparationWeight;
    [ReadOnly] public int MaxNeighbours;

    public void Execute(int index)
    {
        EnemyRuntime enemy = Enemies[index];
        EnemyConfig config = EnemyConfigs[enemy.DefinitionIndex];

        TickTimers(ref enemy);

        float3 direction;

        switch (config.Behaviour)
        {
            case EnemyBehaviourType.Swarm:
                direction = UpdateSwarm(index, ref enemy, config);
                break;

            case EnemyBehaviourType.Charger:
                direction = UpdateCharger(index, ref enemy, config);
                break;

            case EnemyBehaviourType.Brute:
                direction = UpdateBrute(index, ref enemy, config);
                break;

            default:
                direction = GetChaseDirection(index, enemy.Position);
                break;
        }

        float speed = config.MoveSpeed;

        if (config.Behaviour == EnemyBehaviourType.Charger && enemy.State == EnemyState.Attack)
            speed = config.SpecialSpeed;

        enemy.Position += direction * speed * DeltaTime;

        NextEnemies[index] = enemy;
    }

    private float3 UpdateSwarm(int index, ref EnemyRuntime enemy, EnemyConfig config)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);
        float attackRangeSq = config.AttackRange * config.AttackRange;

        if (enemy.AttackCooldown <= 0f && distanceSq <= attackRangeSq)
        {
            DamageEvents.Enqueue(new DamageEvent(index, config.Damage));
            enemy.AttackCooldown = config.AttackCooldown;
        }

        return GetChaseDirection(index, enemy.Position);
    }
    
    private float3 UpdateCharger(int index, ref EnemyRuntime enemy, EnemyConfig config)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);

        switch (enemy.State)
        {
            case EnemyState.Chase:
                if (enemy.AttackCooldown <= 0f &&
                    distanceSq <= config.AttackRange * config.AttackRange)
                {
                    enemy.State = EnemyState.Telegraph;
                    enemy.StateTimer = config.TelegraphDuration;
                    return float3.zero;
                }

                return GetChaseDirection(index, enemy.Position);

            case EnemyState.Telegraph:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.ChargeDirection = math.normalizesafe(Target - enemy.Position);
                    enemy.State = EnemyState.Attack;
                    enemy.StateTimer = config.AttackDuration;
                    enemy.HasHit = 0;

                    return enemy.ChargeDirection;
                }

                return float3.zero;

            case EnemyState.Attack:
                if (enemy.HasHit == 0 &&
                    distanceSq <= config.HitRange * config.HitRange)
                {
                    DamageEvents.Enqueue(new DamageEvent(index, config.Damage));
                    enemy.HasHit = 1;
                }

                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Recover;
                    enemy.StateTimer = config.RecoveryDuration;
                    enemy.AttackCooldown = config.AttackCooldown;

                    return float3.zero;
                }

                return enemy.ChargeDirection;

            case EnemyState.Recover:
                if (enemy.StateTimer <= 0f)
                    enemy.State = EnemyState.Chase;

                return float3.zero;

            default:
                enemy.State = EnemyState.Chase;
                return GetChaseDirection(index, enemy.Position);
        }
    }

    private float3 UpdateBrute(int index, ref EnemyRuntime enemy, EnemyConfig config)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);
        float attackRangeSq = config.AttackRange * config.AttackRange;

        switch (enemy.State)
        {
            case EnemyState.Chase:
                if (enemy.AttackCooldown <= 0f && distanceSq <= attackRangeSq)
                {
                    enemy.State = EnemyState.Telegraph;
                    enemy.StateTimer = config.TelegraphDuration;
                    return float3.zero;
                }

                return GetChaseDirection(index, enemy.Position);

            case EnemyState.Telegraph:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Attack;
                    enemy.StateTimer = config.AttackDuration;

                    if (distanceSq <= attackRangeSq)
                        DamageEvents.Enqueue(new DamageEvent(index, config.Damage));
                }

                return float3.zero;

            case EnemyState.Attack:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Recover;
                    enemy.StateTimer = config.RecoveryDuration;
                    enemy.AttackCooldown = config.AttackCooldown;
                }

                return float3.zero;

            case EnemyState.Recover:
                if (enemy.StateTimer <= 0f)
                    enemy.State = EnemyState.Chase;

                return float3.zero;

            default:
                enemy.State = EnemyState.Chase;
                return GetChaseDirection(index, enemy.Position);
        }
    }

    private float3 GetChaseDirection(int index, float3 position)
    {
        float3 chase = math.normalizesafe(Target - position);
        float3 separation = GetSeparation(index, position);

        return math.normalizesafe(
            chase + math.normalizesafe(separation) * SeparationWeight);
    }

    private void TickTimers(ref EnemyRuntime enemy)
    {
        enemy.StateTimer = math.max(0f, enemy.StateTimer - DeltaTime);
        enemy.AttackCooldown = math.max(0f, enemy.AttackCooldown - DeltaTime);
    }

    private float3 GetSeparation(int index, float3 position)
    {
        float3 separation = float3.zero;
        float distanceSq = SeparationDistance * SeparationDistance;
        int neighbours = 0;

        int2 center = SpatialGrid.GetCell(position, CellSize);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int2 cell = center + new int2(x, y);

                if (!Grid.TryGetFirstValue(cell, out int otherIndex, out var iterator))
                    continue;

                do
                {
                    if (otherIndex == index)
                        continue;

                    float3 offset = position - Enemies[otherIndex].Position;
                    float sqrDistance = math.lengthsq(offset);

                    if (sqrDistance <= 0.0001f ||
                        sqrDistance >= distanceSq)
                        continue;

                    separation += offset * math.rsqrt(sqrDistance);

                    neighbours++;

                    if (neighbours >= MaxNeighbours)
                        return separation;
                }
                while (Grid.TryGetNextValue(out otherIndex, ref iterator));
            }
        }

        return separation;
    }
}