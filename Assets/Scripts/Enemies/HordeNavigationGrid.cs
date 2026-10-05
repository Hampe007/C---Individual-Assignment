using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct HordeNavigationGrid
{
    [ReadOnly] public NativeArray<byte> Blocked;
    [ReadOnly] public NativeArray<int> Distance;
    public float2 Origin;
    public int2 Size;
    public float CellSize;

    public bool Enabled => Size.x > 0 && Size.y > 0;

    public int2 Cell(float2 position) => (int2)math.floor((position - Origin) / CellSize);

    public bool IsOpen(int2 cell)
    {
        return math.all(cell >= 0) && math.all(cell < Size) && Blocked[cell.y * Size.x + cell.x] == 0;
    }

    public bool IsReachable(float2 position)
    {
        if (!Enabled) return true;
        int2 cell = Cell(position);
        return IsOpen(cell) && Distance[cell.y * Size.x + cell.x] != int.MaxValue;
    }

    public bool CanTravel(float2 start, float2 end)
    {
        if (!Enabled) return true;
        int2 previous = Cell(start);
        if (!IsOpen(previous)) return false;
        int steps = math.max(1, (int)math.ceil(math.distance(start, end) / (CellSize * 0.4f)));
        for (int step = 1; step <= steps; step++)
        {
            int2 cell = Cell(math.lerp(start, end, step / (float)steps));
            if (!IsOpen(cell)) return false;
            if (cell.x != previous.x && cell.y != previous.y &&
                (!IsOpen(new int2(cell.x, previous.y)) || !IsOpen(new int2(previous.x, cell.y)))) return false;
            previous = cell;
        }
        return true;
    }

    public float2 Direction(float2 position, float2 target)
    {
        float2 direct = math.normalizesafe(target - position);
        if (!Enabled) return direct;
        int2 cell = Cell(position);
        if (!IsOpen(cell)) return float2.zero;
        int cost = Distance[cell.y * Size.x + cell.x];
        if (cost == int.MaxValue) return float2.zero;
        if (cost <= 2 && CanTravel(position, target)) return direct;
        int bestCost = cost;
        float bestAlignment = -2;
        float2 destination = position;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                int2 next = cell + new int2(offsetX, offsetY);
                if (!IsOpen(next)) continue;
                if (offsetX != 0 && offsetY != 0 &&
                    (!IsOpen(cell + new int2(offsetX, 0)) || !IsOpen(cell + new int2(0, offsetY)))) continue;
                int nextCost = Distance[next.y * Size.x + next.x];
                float2 centre = Origin + ((float2)next + 0.5f) * CellSize;
                float alignment = math.dot(math.normalizesafe(centre - position), direct);
                if (nextCost >= cost || nextCost > bestCost || nextCost == bestCost && alignment <= bestAlignment) continue;
                bestCost = nextCost;
                bestAlignment = alignment;
                destination = centre;
            }
        }
        return math.normalizesafe(destination - position);
    }

    public float2 Move(float2 start, float2 displacement)
    {
        float2 end = start + displacement;
        if (CanTravel(start, end)) return end;
        float2 horizontal = start + new float2(displacement.x, 0);
        float2 vertical = start + new float2(0, displacement.y);
        bool canHorizontal = CanTravel(start, horizontal);
        bool canVertical = CanTravel(start, vertical);
        if (canHorizontal && (!canVertical || math.abs(displacement.x) >= math.abs(displacement.y))) return horizontal;
        return canVertical ? vertical : start;
    }
}

[BurstCompile]
public struct BuildHordeFlowFieldJob : IJob
{
    [ReadOnly] public NativeArray<byte> Blocked;
    public NativeArray<int> Distance;
    public NativeArray<int> Queue;
    public int2 Size;
    public int TargetIndex;

    public void Execute()
    {
        for (int index = 0; index < Distance.Length; index++) Distance[index] = int.MaxValue;
        if (TargetIndex < 0 || Blocked[TargetIndex] != 0) return;
        int head = 0, tail = 1;
        Queue[0] = TargetIndex;
        Distance[TargetIndex] = 0;
        while (head < tail)
        {
            int index = Queue[head++];
            int column = index % Size.x;
            int row = index / Size.x;
            int distance = Distance[index] + 1;
            if (column > 0) Visit(index - 1, distance, ref tail);
            if (column + 1 < Size.x) Visit(index + 1, distance, ref tail);
            if (row > 0) Visit(index - Size.x, distance, ref tail);
            if (row + 1 < Size.y) Visit(index + Size.x, distance, ref tail);
        }
    }

    private void Visit(int index, int distance, ref int tail)
    {
        if (Blocked[index] != 0 || Distance[index] != int.MaxValue) return;
        Distance[index] = distance;
        Queue[tail++] = index;
    }
}
