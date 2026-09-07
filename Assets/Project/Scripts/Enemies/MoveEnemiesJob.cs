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

    [ReadOnly] public float3 Target;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float CellSize;
    [ReadOnly] public float SeparationDistance;
    [ReadOnly] public float SeparationWeight;
    [ReadOnly] public int MaxNeighbours;
    
    public void Execute(int index)
    {
        EnemyRuntime enemy = Enemies[index];

        float3 chase = math.normalizesafe(Target - enemy.Position);
        float3 separation = GetSeparation(index, enemy.Position);

        float3 direction = math.normalizesafe(
            chase + math.normalizesafe(separation) * SeparationWeight);

        enemy.Position += direction * enemy.Speed * DeltaTime;

        NextEnemies[index] = enemy;
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

                    if (sqrDistance <= 0.0001f || sqrDistance >= distanceSq)
                        continue;

                    separation += offset * math.rsqrt(sqrDistance);

                    if (++neighbours >= MaxNeighbours)
                        return separation;
                }
                while (Grid.TryGetNextValue(out otherIndex, ref iterator));
            }
        }

        return separation;
    }
}