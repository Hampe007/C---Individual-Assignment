using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct BuildSpatialGridJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public float CellSize;

    public NativeParallelMultiHashMap<int2, int>.ParallelWriter Grid;

    public void Execute(int index)
    {
        int2 cell = SpatialGrid.GetCell(Enemies[index].Position, CellSize);
        Grid.Add(cell, index);
    }
}

[BurstCompile]
public struct MoveEnemiesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public NativeArray<EnemyConfig> EnemyConfigs;
    [ReadOnly] public NativeParallelMultiHashMap<int2, int>.ReadOnly Grid;

    [WriteOnly] public NativeArray<EnemyRuntime> NextEnemies;

    public NativeQueue<int>.ParallelWriter DamageEvents;

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
            DamageEvents.Enqueue(config.Damage);
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
                    enemy.ChargeDirection = Target - enemy.Position;
                    enemy.ChargeDirection.y = 0f;
                    enemy.ChargeDirection = math.normalizesafe(enemy.ChargeDirection);

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
                    DamageEvents.Enqueue(config.Damage);
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
                        DamageEvents.Enqueue(config.Damage);
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

        float3 chase = Target - position;
        chase.y = 0f;
        chase = math.normalizesafe(chase);

        float3 separation = GetSeparation(index, position);
        separation.y = 0f;

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

        for (int cellX = -1; cellX <= 1; cellX++)
        {
            for (int cellY = -1; cellY <= 1; cellY++)
            {
                int2 cell = center + new int2(cellX, cellY);

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

[BurstCompile]
internal struct SyncEnemyViewsJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public float3 Target;

    public void Execute(int index, TransformAccess transform)
    {
        float3 position = Enemies[index].Position;
        float3 forward = Target - position;
        forward.y = 0f;
        quaternion rotation = quaternion.identity;

        if (math.lengthsq(forward) > 0.0001f)
            rotation = quaternion.LookRotationSafe(forward, math.up());

        transform.SetPositionAndRotation(
            new Vector3(position.x, position.y, position.z),
            new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w));
    }
}

public static class SpatialGrid
{
    public static int2 GetCell(float3 position, float cellSize)
    {
        return (int2)math.floor(position.xz / cellSize);
    }
}
