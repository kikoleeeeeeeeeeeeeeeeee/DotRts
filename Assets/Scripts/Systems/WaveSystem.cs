using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static GridSystem;

/// <summary>
/// ECS 侧波次系统：只负责"按请求配置生成参数 → 生成僵尸"与"按请求清理场上敌人"。
///
/// 计时与索引推进全部由 Mono 侧 <see cref="WaveManager"/> 单点负责，
/// 本系统不再递减 waveTimer、不再修改 currentWaveIndex、不再调用 EndCurrentWave。
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ZombieSpawnerSystem))]
partial struct WaveSystem : ISystem
{
    /// <summary>虫群波：僵尸数量倍率。</summary>
    private const float swarmZombieCountMultiplier = 2f;

    /// <summary>虫群波：投放间隔缩放（小于 1 表示更密集）。</summary>
    private const float swarmSpawnIntervalScale = 0.6f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaveState>();
        state.RequireForUpdate<GridSystemData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var waveDataQuery = SystemAPI.QueryBuilder().WithAll<WaveDataBuffer>().Build();
        if (waveDataQuery.CalculateEntityCount() == 0) return;
        DynamicBuffer<WaveDataBuffer> waveDataBuffer =
            state.EntityManager.GetBuffer<WaveDataBuffer>(waveDataQuery.GetSingletonEntity());

        var waveConfigQuery = SystemAPI.QueryBuilder().WithAll<WaveConfiguration>().Build();
        if (waveConfigQuery.CalculateEntityCount() == 0) return;
        WaveConfiguration waveConfig =
            state.EntityManager.GetSharedComponent<WaveConfiguration>(waveConfigQuery.GetSingletonEntity());

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        HandleStartWaveRequests(ref state, waveDataBuffer, waveConfig, ecb);
        HandleClearFieldRequests(ref state, ecb);
        SpawnZombies(ref state);
    }

    /// <summary>处理 Mono 发来的开波请求：只写入本波生成参数，不参与计时。</summary>
    private void HandleStartWaveRequests(ref SystemState state,
        DynamicBuffer<WaveDataBuffer> waveDataBuffer,
        WaveConfiguration waveConfig,
        EntityCommandBuffer ecb)
    {
        foreach ((StartWaveEvent startWaveEvent, Entity eventEntity) in
                 SystemAPI.Query<StartWaveEvent>().WithEntityAccess())
        {
            int waveIndex = startWaveEvent.waveIndex;
            if (waveIndex < 0 || waveIndex >= waveDataBuffer.Length)
            {
                Debug.LogError($"[WaveSystem] 收到越界的开波请求 waveIndex={waveIndex}（共 {waveDataBuffer.Length} 波）。");
                ecb.DestroyEntity(eventEntity);
                continue;
            }

            WaveDataBuffer waveData = waveDataBuffer[waveIndex];
            int totalZombies = waveData.baseZombieCount + waveData.additionalZombiePerWave * waveIndex;
            float spawnInterval = math.max(0.1f, waveData.spawnInterval - waveConfig.spawnIntervalReductionPerWave * waveIndex);

            // 负值或 0 视为未配置，回退到 1 倍，避免整波僵尸血量/伤害归零。
            float healthMultiplier = waveData.zombieHealthMultiplier > 0f ? waveData.zombieHealthMultiplier : 1f;
            float damageMultiplier = waveData.zombieDamageMultiplier > 0f ? waveData.zombieDamageMultiplier : 1f;
            bool isBossWave = waveData.isSpecialWave && waveData.specialWaveType == SpecialWaveType.Boss;
            bool isSwarmWave = waveData.isSpecialWave && waveData.specialWaveType == SpecialWaveType.Swarm;

            // 虫群波：数量更多、投放更密集，且不生成 Boss。
            if (isSwarmWave)
            {
                totalZombies = math.max(1, (int)math.round(totalZombies * swarmZombieCountMultiplier));
                spawnInterval = math.max(0.1f, spawnInterval * swarmSpawnIntervalScale);
            }

            foreach (RefRW<WaveState> waveState in SystemAPI.Query<RefRW<WaveState>>())
            {
                waveState.ValueRW.currentWaveIndex = waveIndex;
                waveState.ValueRW.totalZombiesToSpawn = math.max(0, totalZombies);
                waveState.ValueRW.spawnInterval = spawnInterval;
                waveState.ValueRW.spawnTimer = 0f;
                waveState.ValueRW.zombiesSpawnedInCurrentWave = 0;
                waveState.ValueRW.zombiesKilledInCurrentWave = 0;
                waveState.ValueRW.zombieHealthMultiplier = healthMultiplier;
                waveState.ValueRW.zombieDamageMultiplier = damageMultiplier;
                waveState.ValueRW.isBossWave = isBossWave;
                waveState.ValueRW.bossSpawned = false;
                waveState.ValueRW.bossSpawnTimer = isBossWave ? math.max(0f, waveData.bossWaveConfig.bossSpawnDelay) : 0f;
                waveState.ValueRW.bossHealth = waveData.bossWaveConfig.bossHealth;
                waveState.ValueRW.bossDamage = waveData.bossWaveConfig.bossDamage;
                waveState.ValueRW.isWaveActive = true;
            }

            Debug.Log($"[WaveSystem] 第 {waveIndex + 1} 波配置：{totalZombies} 只，间隔 {spawnInterval:F2}s，血量 x{healthMultiplier:F2}，伤害 x{damageMultiplier:F2}" +
                      (isBossWave ? $"，Boss 波（延迟 {waveData.bossWaveConfig.bossSpawnDelay:F1}s）" : "") +
                      (isSwarmWave ? "，虫群波（数量翻倍、投放更密集）" : ""));
            ecb.DestroyEntity(eventEntity);
        }
    }

    /// <summary>处理波次结束的清理请求：销毁场上所有进攻 HQ 的敌人。</summary>
    private void HandleClearFieldRequests(ref SystemState state, EntityCommandBuffer ecb)
    {
        var clearEventQuery = SystemAPI.QueryBuilder().WithAll<ClearFieldEnemiesEvent>().Build();
        if (clearEventQuery.CalculateEntityCount() == 0) return;

        var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyAttackHQ, RuntimeEntity>().Build();
        int enemyCount = enemyQuery.CalculateEntityCount();
        if (enemyCount > 0)
        {
            NativeArray<Entity> enemies = enemyQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < enemies.Length; i++)
            {
                ecb.DestroyEntity(enemies[i]);
            }
            enemies.Dispose();
            Debug.Log($"[WaveSystem] 波次结束清理：销毁场上残留敌人 {enemyCount} 只。");
        }

        NativeArray<Entity> events = clearEventQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < events.Length; i++)
        {
            ecb.DestroyEntity(events[i]);
        }
        events.Dispose();
    }

    /// <summary>按已配置的生成参数持续投放僵尸。</summary>
    private void SpawnZombies(ref SystemState state)
    {
        foreach ((RefRW<WaveState> waveState, Entity waveStateEntity) in
                 SystemAPI.Query<RefRW<WaveState>>().WithEntityAccess())
        {
            if (!waveState.ValueRO.isWaveActive) continue;

            DynamicBuffer<SpawnZombieRequestBuffer> spawnRequestBuffer =
                state.EntityManager.GetBuffer<SpawnZombieRequestBuffer>(waveStateEntity);

            // Boss 波：延迟生成一只 Boss（不占用小怪数量配额）
            if (waveState.ValueRO.isBossWave && !waveState.ValueRO.bossSpawned)
            {
                waveState.ValueRW.bossSpawnTimer -= SystemAPI.Time.DeltaTime;
                if (waveState.ValueRO.bossSpawnTimer <= 0f)
                {
                    float3 bossPosition = GetRandomSpawnPosition(ref state);
                    if (!math.isnan(bossPosition.x) && !math.isnan(bossPosition.z))
                    {
                        spawnRequestBuffer.Add(new SpawnZombieRequestBuffer
                        {
                            spawnPosition = bossPosition,
                            isBoss = true
                        });
                        waveState.ValueRW.bossSpawned = true;
                        Debug.Log($"[WaveSystem] Boss 已生成 at {bossPosition}");
                    }
                }
            }

            if (waveState.ValueRO.zombiesSpawnedInCurrentWave >= waveState.ValueRO.totalZombiesToSpawn) continue;

            waveState.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;
            if (waveState.ValueRO.spawnTimer > 0f) continue;

            float3 spawnPosition = GetRandomSpawnPosition(ref state);
            if (math.isnan(spawnPosition.x) || math.isnan(spawnPosition.y) || math.isnan(spawnPosition.z))
            {
                Debug.LogError("[WaveSystem] 计算出的刷怪点无效，本帧跳过。");
                waveState.ValueRW.spawnTimer = 1f;
                continue;
            }

            spawnRequestBuffer.Add(new SpawnZombieRequestBuffer { spawnPosition = spawnPosition });

            waveState.ValueRW.zombiesSpawnedInCurrentWave++;
            waveState.ValueRW.spawnTimer = math.max(0.05f, waveState.ValueRO.spawnInterval);

            int spawned = waveState.ValueRO.zombiesSpawnedInCurrentWave;
            if (spawned <= 3 || spawned % 10 == 0)
            {
                Debug.Log($"[WaveSystem] 刷怪 {spawned}/{waveState.ValueRO.totalZombiesToSpawn} at {spawnPosition}");
            }
        }
    }

    private float3 GetRandomSpawnPosition(ref SystemState state)
    {
        float3 basePosition = float3.zero;

        if (SystemAPI.HasSingleton<BuildingHQ>())
        {
            Entity hqEntity = SystemAPI.GetSingletonEntity<BuildingHQ>();
            if (state.EntityManager.HasComponent<LocalTransform>(hqEntity))
            {
                LocalTransform hqTransform = state.EntityManager.GetComponentData<LocalTransform>(hqEntity);
                if (!math.isnan(hqTransform.Position.x) &&
                    !math.isnan(hqTransform.Position.y) &&
                    !math.isnan(hqTransform.Position.z))
                {
                    basePosition = hqTransform.Position;
                }
            }
        }

        float mapWidth = 200f;
        float mapHeight = 200f;
        float spawnAreaRadius = 0f;
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            MapConfigData mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            if (mapConfig.mapWidth > 0) mapWidth = mapConfig.mapWidth;
            if (mapConfig.mapHeight > 0) mapHeight = mapConfig.mapHeight;
            spawnAreaRadius = mapConfig.spawnAreaRadius;
        }

        // 优先消费地图配置里的刷怪半径；未配置时退回「地图短边的 40%」。
        float edgeDistance = spawnAreaRadius > 0f
            ? spawnAreaRadius
            : math.min(mapWidth, mapHeight) * 0.4f;

        // 留 2 单位内边距，避免刷怪点被 clamp 到地图边界之外。
        float maxEdgeDistance = math.max(1f, math.min(mapWidth, mapHeight) * 0.5f - 2f);
        edgeDistance = math.clamp(edgeDistance, 1f, maxEdgeDistance);

        float angleRadians = math.radians(UnityEngine.Random.Range(0f, 360f));
        float3 spawnDirection = new float3(math.cos(angleRadians), 0f, math.sin(angleRadians));

        float3 spawnPosition = basePosition + spawnDirection * edgeDistance;
        spawnPosition.x = math.clamp(spawnPosition.x, -mapWidth / 2, mapWidth / 2);
        spawnPosition.z = math.clamp(spawnPosition.z, -mapHeight / 2, mapHeight / 2);
        spawnPosition.y = 0f;

        return spawnPosition;
    }
}
