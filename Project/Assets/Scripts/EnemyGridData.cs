using Unity.Entities;
using Unity.Mathematics;
using System;

struct EnemyGridData : IComparable<EnemyGridData>
{
    public int CellIndex;
    public Entity Entity;
    public float3 Position;

    public int CompareTo(EnemyGridData other)
    {
        return CellIndex.CompareTo(other.CellIndex);
    }
}