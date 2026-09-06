using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

partial struct BestCollisionSystem : ISystem
{
    static readonly ProfilerMarker s_AllocMarker = new ProfilerMarker("BestCollision.Alloc");
    static readonly ProfilerMarker s_BuildGridMarker = new ProfilerMarker("BestCollision.BuildGrid");
    static readonly ProfilerMarker s_QueryMarker = new ProfilerMarker("BestCollision.Query");
    static readonly ProfilerMarker s_SyncMarker = new ProfilerMarker("BestCollision.Sync");

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

        s_AllocMarker.Begin();
        var enemyGrid = new NativeParallelMultiHashMap<int2, EnemyCellData>(enemyCount, Allocator.Temp);
        s_AllocMarker.End();

        s_BuildGridMarker.Begin();
        foreach (var (enemyTransform, enemy, enemyEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
        {
            float3 position = enemyTransform.ValueRO.Position;
            int2 cellCoord = GridManager.ToCell(position);

            enemyGrid.Add(cellCoord, new EnemyCellData
            {
                Entity = enemyEntity,
                Position = position
            });
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
                    int2 checkCell = cellCoord + new int2(i, j);

                    if (enemyGrid.TryGetFirstValue(checkCell, out var enemyData, out var iterator))
                    {
                        do
                        {
                            float distanceSQ = math.distancesq(position, enemyData.Position);

                            if (distanceSQ <= radius * radius)
                            {
                                ecb.DestroyEntity(enemyData.Entity);
                                ecb.DestroyEntity(bulletEntity);
                                hit = true;
                                break;
                            }
                        }
                        while (enemyGrid.TryGetNextValue(out enemyData, ref iterator));
                    }
                    if (hit) break;
                }
                if (hit) break;
            }
        }
        s_QueryMarker.End();

        enemyGrid.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}