using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MoveEnemiesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public NativeParallelMultiHashMap<int2, int>.ReadOnly Grid;

    [WriteOnly] public NativeArray<EnemyRuntime> NextEnemies;

    public NativeQueue<DamageEvent>.ParallelWriter DamageEvents;
    
    [ReadOnly] public float3 Target;
    [ReadOnly] public float DeltaTime;

    [ReadOnly] public float CellSize;
    [ReadOnly] public float SeparationDistance;
    [ReadOnly] public float SeparationWeight;
    [ReadOnly] public int MaxNeighbours;

    [ReadOnly] public float SwarmAttackRange;
    [ReadOnly] public int SwarmDamage;
    [ReadOnly] public float SwarmAttackCooldown;
    
    [ReadOnly] public float ChargeRange;
    [ReadOnly] public float ChargeSpeed;
    [ReadOnly] public float ChargeDuration;
    [ReadOnly] public float ChargeTelegraph;
    [ReadOnly] public float ChargeRecovery;
    [ReadOnly] public float ChargeCooldown;
    [ReadOnly] public float ChargeHitRange;
    [ReadOnly] public int ChargeDamage;

    [ReadOnly] public float BruteMoveSpeed;
    [ReadOnly] public float BruteSlamRange;
    [ReadOnly] public float BruteWindup;
    [ReadOnly] public float BruteAttackDuration;
    [ReadOnly] public float BruteRecovery;
    [ReadOnly] public float BruteCooldown;
    [ReadOnly] public int BruteDamage;

    public void Execute(int index)
    {
        EnemyRuntime enemy = Enemies[index];

        TickTimers(ref enemy);

        float3 direction;

        switch (enemy.Behaviour)
        {
            case EnemyBehaviourType.Swarm:
                direction = UpdateSwarm(index, ref enemy);
                break;
            
            case EnemyBehaviourType.Charger:
                direction = UpdateCharger(index, ref enemy);
                break;

            case EnemyBehaviourType.Brute:
                direction = UpdateBrute(index, ref enemy);
                break;

            default:
                direction = GetChaseDirection(index, enemy.Position);
                break;
        }

        float speed = enemy.Speed;

        if (enemy.Behaviour == EnemyBehaviourType.Charger &&
            enemy.State == EnemyState.Attack)
        {
            speed = ChargeSpeed;
        }
        else if (enemy.Behaviour == EnemyBehaviourType.Brute)
        {
            speed = BruteMoveSpeed;
        }

        enemy.Position += direction * speed * DeltaTime;

        NextEnemies[index] = enemy;
    }

    private float3 UpdateSwarm(int index, ref EnemyRuntime enemy)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);
        float attackRangeSq = SwarmAttackRange * SwarmAttackRange;

        if (enemy.AttackCooldown <= 0f && distanceSq <= attackRangeSq)
        {
            DamageEvents.Enqueue(new DamageEvent(index, SwarmDamage));
            enemy.AttackCooldown = SwarmAttackCooldown;
        }

        return GetChaseDirection(index, enemy.Position);
    }
    
    private float3 UpdateCharger(int index, ref EnemyRuntime enemy)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);

        switch (enemy.State)
        {
            case EnemyState.Chase:
                if (enemy.AttackCooldown <= 0f &&
                    distanceSq <= ChargeRange * ChargeRange)
                {
                    enemy.State = EnemyState.Telegraph;
                    enemy.StateTimer = ChargeTelegraph;
                    return float3.zero;
                }

                return GetChaseDirection(index, enemy.Position);

            case EnemyState.Telegraph:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.ChargeDirection = math.normalizesafe(Target - enemy.Position);
                    enemy.State = EnemyState.Attack;
                    enemy.StateTimer = ChargeDuration;
                    enemy.HasHit = 0;

                    return enemy.ChargeDirection;
                }

                return float3.zero;

            case EnemyState.Attack:
                if (enemy.HasHit == 0 &&
                    distanceSq <= ChargeHitRange * ChargeHitRange)
                {
                    DamageEvents.Enqueue(new DamageEvent(index, ChargeDamage));
                    enemy.HasHit = 1;
                }

                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Recover;
                    enemy.StateTimer = ChargeRecovery;
                    enemy.AttackCooldown = ChargeCooldown;

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

    private float3 UpdateBrute(int index, ref EnemyRuntime enemy)
    {
        float distanceSq = math.distancesq(enemy.Position, Target);
        float slamRangeSq = BruteSlamRange * BruteSlamRange;

        switch (enemy.State)
        {
            case EnemyState.Chase:
                if (enemy.AttackCooldown <= 0f && distanceSq <= slamRangeSq)
                {
                    enemy.State = EnemyState.Telegraph;
                    enemy.StateTimer = BruteWindup;
                    return float3.zero;
                }

                return GetChaseDirection(index, enemy.Position);

            case EnemyState.Telegraph:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Attack;
                    enemy.StateTimer = BruteAttackDuration;

                    if (distanceSq <= slamRangeSq)
                        DamageEvents.Enqueue(new DamageEvent(index, BruteDamage));
                }

                return float3.zero;

            case EnemyState.Attack:
                if (enemy.StateTimer <= 0f)
                {
                    enemy.State = EnemyState.Recover;
                    enemy.StateTimer = BruteRecovery;
                    enemy.AttackCooldown = BruteCooldown;
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