using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

partial struct BitCollisionSystem : ISystem
{
    private const bool USE_BIT_FILTER = false;

    static readonly ProfilerMarker s_AllocMarker = new ProfilerMarker("BitCollision.Alloc");
    static readonly ProfilerMarker s_FillMarker = new ProfilerMarker("BitCollision.Fill");
    static readonly ProfilerMarker s_SortMarker = new ProfilerMarker("BitCollision.Sort");
    static readonly ProfilerMarker s_BuildGridMarker = new ProfilerMarker("BitCollision.BuildGrid");
    static readonly ProfilerMarker s_QueryMarker = new ProfilerMarker("BitCollision.Query");
    static readonly ProfilerMarker s_SyncMarker = new ProfilerMarker("BitCollision.Sync");

    private EntityQuery _enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _enemyQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<Enemy, LocalTransform>().Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        s_SyncMarker.Begin();
        state.CompleteDependency();
        s_SyncMarker.End();

        int enemyCount = _enemyQuery.CalculateEntityCount();
        if (enemyCount == 0) return;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var enemyDataArray = new NativeArray<EnemyGridData>(enemyCount, Allocator.Temp);

        s_AllocMarker.Begin();
        var occupancyBits = new NativeBitArray(
            USE_BIT_FILTER ? GridManager.TOTAL_CELLS : 1,
            Allocator.Temp, NativeArrayOptions.ClearMemory);
        var gridOffsets = new NativeArray<int>(GridManager.TOTAL_CELLS, Allocator.Temp);
        var gridCounts = new NativeArray<int>(GridManager.TOTAL_CELLS, Allocator.Temp);
        s_AllocMarker.End();

        s_FillMarker.Begin();
        for (int i = 0; i < GridManager.TOTAL_CELLS; i++)
        {
            gridOffsets[i] = -1;
            gridCounts[i] = 0;
        }
        s_FillMarker.End();

        int index = 0;
        foreach (var (enemyTransform, enemy, enemyEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 position = enemyTransform.ValueRO.Position;
            int linearIndex = GridManager.ToLinearIndex(GridManager.ToCell(position));

            if (USE_BIT_FILTER && linearIndex != -1)
            {
                occupancyBits.Set(linearIndex, true); // 해당 셀에 적이 있음을 비트로 표시
            }

            enemyDataArray[index++] = new EnemyGridData
            {
                CellIndex = linearIndex,   // 범위 밖이면 -1. BuildGrid에서 걸러진다.
                Entity = enemyEntity,
                Position = position
            };
        }

        s_SortMarker.Begin();
        enemyDataArray.Sort();
        s_SortMarker.End();

        s_BuildGridMarker.Begin();
        for (int i = 0; i < enemyDataArray.Length; i++)
        {
            int cellIndex = enemyDataArray[i].CellIndex;
            if (cellIndex == -1) continue;

            if (gridOffsets[cellIndex] == -1)
            {
                gridOffsets[cellIndex] = i;
            }
            gridCounts[cellIndex]++;
        }
        s_BuildGridMarker.End();

        s_QueryMarker.Begin();
        foreach (var (bulletTransform, bullet, bulletEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            float3 position = bulletTransform.ValueRO.Position;
            float radius = bullet.ValueRO.Radius + GridManager.ENEMY_RADIUS;
            int2 cellCoord = GridManager.ToCell(position);
            bool hit = false;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int linearIndex = GridManager.ToLinearIndex(cellCoord + new int2(i, j));
                    if (linearIndex == -1) continue;

                    // 1차 검문: 비트가 0이면 빈 격자이므로 배열 조회를 건너뛴다
                    if (USE_BIT_FILTER && !occupancyBits.IsSet(linearIndex)) continue;

                    // 2차 탐색: 격자별 연속 구간만 순회
                    int offset = gridOffsets[linearIndex];
                    if (offset != -1)
                    {
                        int endIdx = offset + gridCounts[linearIndex];

                        for (int k = offset; k < endIdx; k++)
                        {
                            var enemyData = enemyDataArray[k];
                            float distanceSQ = math.distancesq(position, enemyData.Position);

                            if (distanceSQ <= radius * radius)
                            {
                                ecb.DestroyEntity(enemyData.Entity);
                                ecb.DestroyEntity(bulletEntity);
                                hit = true;
                                break;
                            }
                        }
                    }
                    if (hit) break;
                }
                if (hit) break;
            }
        }
        s_QueryMarker.End();

        occupancyBits.Dispose();
        enemyDataArray.Dispose();
        gridOffsets.Dispose();
        gridCounts.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}