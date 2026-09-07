using Unity.Mathematics;

public static class SpatialGrid
{
    public static int2 GetCell(float3 position, float cellSize)
    {
        return (int2)math.floor(position.xz / cellSize);
    }
}