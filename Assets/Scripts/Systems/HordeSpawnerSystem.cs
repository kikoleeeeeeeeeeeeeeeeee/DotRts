using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

partial struct HordeSpawnerSystem : ISystem
{
    /// <summary>Horde 从第几波开始活动（0 基索引：1 表示第二波）。</summary>
    private const int hordeFirstActiveWaveIndex = 1;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        // 门控 1：Horde 只在波次进行中活动，避免准备阶段开局就刷出一批僵尸。
        if (!SystemAPI.HasSingleton<WaveState>()) return;
        WaveState waveState = SystemAPI.GetSingleton<WaveState>();
        if (!waveState.isWaveActive) return;

        // 门控 2：Horde 从第二波起才活动。第一波同时叠加波次刷怪会把刚放下的 HQ 直接围死，
        // 这里延后一波，保证开局至少有喘息时间。
        if (waveState.currentWaveIndex < hordeFirstActiveWaveIndex) return;

        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRO<LocalTransform> localTransform,
            RefRW<Horde> horde,
            Entity entity)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Horde>>().WithEntityAccess())
        {
            if (!horde.ValueRO.isSetup)
            {
                horde.ValueRW.isSetup = true;
                entityCommandBuffer.AddComponent<DisableRendering>(horde.ValueRO.minimapIconEntity);
            }

            float beforeStartTimer = horde.ValueRO.startTimer;

            horde.ValueRW.startTimer -= SystemAPI.Time.DeltaTime;

            float startSpawningSoonTime = 3f;
            if (beforeStartTimer > startSpawningSoonTime && horde.ValueRO.startTimer <= startSpawningSoonTime)
            {
                horde.ValueRW.onStartSpawningSoon = true;
                entityCommandBuffer.RemoveComponent<DisableRendering>(horde.ValueRO.minimapIconEntity);
            }

            if (horde.ValueRO.startTimer > 0)
            {
                continue;
            }

            if (beforeStartTimer > 0)
            {
                horde.ValueRW.onStartSpawning = true;
            }

            if (horde.ValueRO.zombieAmountToSpawn <= 0)
            {
                entityCommandBuffer.DestroyEntity(entity);
                continue;
            }

            horde.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;
            if (horde.ValueRO.spawnTimer <= 0)
            {
                // spawnTimerMax 配置为 0 时不能每帧刷一只，否则几十只会在一瞬间涌出。
                horde.ValueRW.spawnTimer = math.max(0.1f, horde.ValueRO.spawnTimerMax);

                Entity zombieEntity = entityCommandBuffer.Instantiate(entitiesReferences.zombiePrefabEntity);
                entityCommandBuffer.AddComponent<RuntimeEntity>(zombieEntity);

                Random random = horde.ValueRO.random;
                float3 spawnPosition = localTransform.ValueRO.Position;
                spawnPosition.x += random.NextFloat(-horde.ValueRO.spawnAreaWidth, +horde.ValueRO.spawnAreaWidth);
                spawnPosition.z += random.NextFloat(-horde.ValueRO.spawnAreaHeight, +horde.ValueRO.spawnAreaHeight);
                horde.ValueRW.random = random;

                entityCommandBuffer.SetComponent(zombieEntity, LocalTransform.FromPosition(spawnPosition));
                entityCommandBuffer.AddComponent<EnemyAttackHQ>(zombieEntity);

                horde.ValueRW.zombieAmountToSpawn--;
            }
        }
    }
}