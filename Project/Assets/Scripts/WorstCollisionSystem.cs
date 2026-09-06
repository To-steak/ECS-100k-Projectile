using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

partial struct WorstCollisionSystem : ISystem
{
    static readonly ProfilerMarker s_SyncMarker = new ProfilerMarker("WorstCollision.Sync");

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        s_SyncMarker.Begin();
        state.CompleteDependency();
        s_SyncMarker.End();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (bulletTransform, bullet, bulletEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
        {
            foreach (var (enemyTransform, enemy, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Enemy>>().WithEntityAccess())
            {
                float3 bulletPos = bulletTransform.ValueRO.Position;
                float3 enemyPos = enemyTransform.ValueRO.Position;

                float distanceSQ = math.distancesq(bulletPos, enemyPos);
                float radius = bullet.ValueRO.Radius + GridManager.ENEMY_RADIUS;

                if (distanceSQ <= radius * radius)
                {
                    ecb.DestroyEntity(enemyEntity);
                    ecb.DestroyEntity(bulletEntity);
                    break;
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
