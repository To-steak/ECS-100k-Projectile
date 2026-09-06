using System.Runtime.CompilerServices;
using Unity.Mathematics;

public struct GridManager
{
    public const float CELL_SIZE = 10f;
    public const float COVERAGE = 2000f;
    public const float ENEMY_RADIUS = 0.5f;

    public const int GRID_SIZE = (int)(COVERAGE / CELL_SIZE);
    public const int GRID_OFFSET = GRID_SIZE / 2;
    public const int TOTAL_CELLS = GRID_SIZE * GRID_SIZE;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 ToCell(float3 position) => new int2((int)math.floor(position.x / CELL_SIZE), (int)math.floor(position.z / CELL_SIZE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToLinearIndex(int2 cell)
    {
        int x = cell.x + GRID_OFFSET;
        int z = cell.y + GRID_OFFSET;
        if (x < 0 || x >= GRID_SIZE || z < 0 || z >= GRID_SIZE) return -1;
        return z * GRID_SIZE + x;
    }
}