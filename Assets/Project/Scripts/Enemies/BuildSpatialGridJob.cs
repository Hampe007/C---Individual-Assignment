using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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