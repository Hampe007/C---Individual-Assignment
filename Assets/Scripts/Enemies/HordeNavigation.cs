using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class HordeNavigation : IDisposable
{
    private NativeArray<byte> _blocked;
    private NativeArray<int> _distance;
    private NativeArray<int> _queue;
    private int _targetIndex = -1;
    private int _requestedIndex = -1;
    private bool _initialized;

    public HordeNavigationGrid Grid { get; private set; }

    public HordeNavigation(Transform obstacles, Vector2 centre, Vector2 area, float cellSize, float clearance)
    {
        cellSize = Mathf.Max(0.25f, cellSize);
        int2 size = obstacles == null ? int2.zero : (int2)math.ceil(new float2(area.x, area.y) / cellSize);
        int count = math.max(1, size.x * size.y);
        _blocked = new NativeArray<byte>(count, Allocator.Persistent);
        _distance = new NativeArray<int>(count, Allocator.Persistent);
        _queue = new NativeArray<int>(count, Allocator.Persistent);
        Grid = new HordeNavigationGrid
        {
            Blocked = _blocked, Distance = _distance, Size = size, CellSize = cellSize,
            Origin = new float2(centre.x, centre.y) - new float2(area.x, area.y) * 0.5f
        };
        if (obstacles == null) return;
        Physics.SyncTransforms();
        foreach (var collider in obstacles.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger) continue;
            Bounds bounds = collider.bounds;
            if (bounds.max.y < 0.1f || bounds.min.y > 2f) continue;
            int2 minimum = math.max(int2.zero, Grid.Cell(new float2(bounds.min.x, bounds.min.z) - clearance));
            int2 maximum = math.min(size - 1, Grid.Cell(new float2(bounds.max.x, bounds.max.z) + clearance));
            for (int row = minimum.y; row <= maximum.y; row++)
                for (int column = minimum.x; column <= maximum.x; column++) _blocked[row * size.x + column] = 1;
        }
        Schedule(new float3(centre.x, 0, centre.y)).Complete();
        for (int index = 0; index < _distance.Length; index++)
            if (_distance[index] == int.MaxValue) _blocked[index] = 1;
    }

    public JobHandle Schedule(float3 target, JobHandle dependency = default)
    {
        if (!Grid.Enabled) return dependency;
        int2 cell = math.clamp(Grid.Cell(target.xz), int2.zero, Grid.Size - 1);
        int targetIndex = cell.y * Grid.Size.x + cell.x;
        if (_initialized && targetIndex == _requestedIndex) return dependency;
        _requestedIndex = targetIndex;
        if (_blocked[targetIndex] != 0)
        {
            float bestDistance = float.MaxValue;
            targetIndex = -1;
            for (int index = 0; index < _blocked.Length; index++)
            {
                if (_blocked[index] != 0) continue;
                float2 centre = Grid.Origin + new float2(index % Grid.Size.x + 0.5f, index / Grid.Size.x + 0.5f) * Grid.CellSize;
                float distance = math.distancesq(centre, target.xz);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                targetIndex = index;
            }
        }
        if (_initialized && targetIndex == _targetIndex) return dependency;
        _initialized = true;
        _targetIndex = targetIndex;
        return new BuildHordeFlowFieldJob
        {
            Blocked = _blocked, Distance = _distance, Queue = _queue, Size = Grid.Size, TargetIndex = targetIndex
        }.Schedule(dependency);
    }

    public void Dispose()
    {
        if (_blocked.IsCreated) _blocked.Dispose();
        if (_distance.IsCreated) _distance.Dispose();
        if (_queue.IsCreated) _queue.Dispose();
    }
}
