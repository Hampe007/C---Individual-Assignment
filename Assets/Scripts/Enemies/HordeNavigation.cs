using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class HordeNavigation : IDisposable
{
    private NativeArray<byte> blocked;
    private NativeArray<int> distance;
    private NativeArray<int> queue;
    private int targetIndex = -1;
    private int requestedIndex = -1;
    private bool initialized;
    public HordeNavigationGrid Grid { get; private set; }

    public HordeNavigation(Transform obstacles, Vector2 centre, Vector2 area, float cellSize, float clearance)
    {
        cellSize = Mathf.Max(0.25f, cellSize);
        int2 size = obstacles == null ? int2.zero : (int2)math.ceil(new float2(area.x, area.y) / cellSize);
        int count = math.max(1, size.x * size.y);
        blocked = new NativeArray<byte>(count, Allocator.Persistent);
        distance = new NativeArray<int>(count, Allocator.Persistent);
        queue = new NativeArray<int>(count, Allocator.Persistent);
        Grid = new HordeNavigationGrid
        {
            Blocked = blocked,
            Distance = distance,
            Size = size,
            CellSize = cellSize,
            Origin = new float2(centre.x, centre.y) - new float2(area.x, area.y) * 0.5f
        };
        if (obstacles == null)
        {
            return;
        }

        Physics.SyncTransforms();
        foreach (var collider in obstacles.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            if (bounds.max.y < 0.1f || bounds.min.y > 2f)
            {
                continue;
            }

            int2 minimum = math.max(int2.zero, Grid.Cell(new float2(bounds.min.x, bounds.min.z) - clearance));
            int2 maximum = math.min(size - 1, Grid.Cell(new float2(bounds.max.x, bounds.max.z) + clearance));
            for (int row = minimum.y; row <= maximum.y; row++)
            {
                for (int column = minimum.x; column <= maximum.x; column++)
                {
                    blocked[row * size.x + column] = 1;
                }
            }
        }

        Schedule(new float3(centre.x, 0, centre.y)).Complete();
        for (int index = 0; index < distance.Length; index++)
        {
            if (distance[index] == int.MaxValue)
            {
                blocked[index] = 1;
            }
        }
    }

    public JobHandle Schedule(float3 target, JobHandle dependency = default)
    {
        if (!Grid.Enabled)
        {
            return dependency;
        }

        int2 cell = math.clamp(Grid.Cell(target.xz), int2.zero, Grid.Size - 1);
        int targetIndex = cell.y * Grid.Size.x + cell.x;
        if (initialized && targetIndex == requestedIndex)
        {
            return dependency;
        }

        requestedIndex = targetIndex;
        if (blocked[targetIndex] != 0)
        {
            float bestDistance = float.MaxValue;
            targetIndex = -1;
            for (int index = 0; index < blocked.Length; index++)
            {
                if (blocked[index] != 0)
                {
                    continue;
                }

                float2 centre = Grid.Origin + new float2(index % Grid.Size.x + 0.5f, index / Grid.Size.x + 0.5f) * Grid.CellSize;
                float distance = math.distancesq(centre, target.xz);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                targetIndex = index;
            }
        }

        if (initialized && targetIndex == this.targetIndex)
        {
            return dependency;
        }

        initialized = true;
        this.targetIndex = targetIndex;
        return new BuildHordeFlowFieldJob
        {
            Blocked = blocked,
            Distance = distance,
            Queue = queue,
            Size = Grid.Size,
            TargetIndex = targetIndex
        }.Schedule(dependency);
    }

    public void Dispose()
    {
        if (blocked.IsCreated)
        {
            blocked.Dispose();
        }

        if (distance.IsCreated)
        {
            distance.Dispose();
        }

        if (queue.IsCreated)
        {
            queue.Dispose();
        }
    }
}