using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
partial struct WaveSystemBootstrap : ISystem
{
    private bool hasInitialized;
    private int waitFrames;
    
    public void OnCreate(ref SystemState state)
    {
        hasInitialized = false;
        waitFrames = 0;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (hasInitialized)
        {
            state.Enabled = false;
            return;
        }
        
        var query = state.EntityManager.CreateEntityQuery(typeof(WaveState));
        if (query.CalculateEntityCount() > 0)
        {
            query.Dispose();
            hasInitialized = true;
            state.Enabled = false;
            return;
        }
        query.Dispose();
        
        var gameSceneTagQuery = state.EntityManager.CreateEntityQuery(typeof(GameSceneTag));
        if (gameSceneTagQuery.CalculateEntityCount() == 0)
        {
            return;
        }
        gameSceneTagQuery.Dispose();

        // 单一数据来源：优先使用场景内 WaveManager 上配置的 WaveDataSo，
        // 避免 Mono 与 ECS 各自读取不同配置导致波次内容不一致。
        WaveDataSo waveDataSo = null;
        if (WaveManager.Instance != null)
        {
            waveDataSo = WaveManager.Instance.WaveDataSo;
        }

        if (waveDataSo == null)
        {
            waitFrames++;
            if (waitFrames < 120)
            {
                return; // 等待 Mono 侧 Awake/Start 完成注入
            }

            waveDataSo = Resources.Load<WaveDataSo>("WaveData");
            if (waveDataSo != null)
            {
                UnityEngine.Debug.LogWarning("[WaveSystemBootstrap] WaveManager 未提供 WaveDataSo，已退回 Resources/WaveData。");
            }
        }

        if (waveDataSo == null || waveDataSo.waves == null || waveDataSo.waves.Length == 0)
        {
            UnityEngine.Debug.LogError("[WaveSystemBootstrap] 无法获取有效的 WaveDataSo（场景 WaveManager 未配置且 Resources/WaveData 也不存在），ECS 侧将使用临时兜底配置。");
            waveDataSo = CreateDefaultWaveData();
        }

        try
        {
            UnityEngine.Debug.Log($"[WaveSystemBootstrap] 初始化波次数据：{waveDataSo.waves.Length} 波，准备 {waveDataSo.globalPreparationTime}s");
            
            Entity waveConfigEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddSharedComponent(waveConfigEntity, new WaveConfiguration
            {
                globalPreparationTime = waveDataSo.globalPreparationTime,
                baseSpawnInterval = waveDataSo.baseSpawnInterval,
                spawnIntervalReductionPerWave = waveDataSo.spawnIntervalReductionPerWave
            });
            
            Entity waveDataEntity = state.EntityManager.CreateEntity();
            var waveDataBuffer = state.EntityManager.AddBuffer<WaveDataBuffer>(waveDataEntity);
            for (int i = 0; i < waveDataSo.waves.Length; i++)
            {
                waveDataBuffer.Add(new WaveDataBuffer
                {
                    waveNumber = waveDataSo.waves[i].waveNumber,
                    waveDuration = waveDataSo.waves[i].waveDuration,
                    preparationTime = waveDataSo.waves[i].preparationTime,
                    baseZombieCount = waveDataSo.waves[i].baseZombieCount,
                    additionalZombiePerWave = waveDataSo.waves[i].additionalZombiePerWave,
                    spawnInterval = waveDataSo.waves[i].spawnInterval,
                    zombieHealthMultiplier = waveDataSo.waves[i].zombieHealthMultiplier,
                    zombieDamageMultiplier = waveDataSo.waves[i].zombieDamageMultiplier,
                    isSpecialWave = waveDataSo.waves[i].isSpecialWave,
                    specialWaveType = waveDataSo.waves[i].specialWaveType,
                    bossWaveConfig = new BossWaveConfig
                    {
                        bossHealth = waveDataSo.waves[i].bossWaveConfig.bossHealth,
                        bossDamage = waveDataSo.waves[i].bossWaveConfig.bossDamage,
                        bossSpawnDelay = waveDataSo.waves[i].bossWaveConfig.bossSpawnDelay,
                        minionsCount = waveDataSo.waves[i].bossWaveConfig.minionsCount
                    }
                });
            }
            
            Entity waveStateEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(waveStateEntity, new WaveState
            {
                currentWaveIndex = 0,
                isWaveActive = false,
                isPreparationPhase = false,
                isBasePlaced = false,
                zombiesKilledInCurrentWave = 0,
                waveTimer = 0f,
                preparationTimer = 0f,
                spawnTimer = 0f,
                zombiesSpawnedInCurrentWave = 0,
                totalZombiesToSpawn = 0,
                spawnInterval = waveDataSo.baseSpawnInterval
            });
            
            // 添加生成请求 Buffer（类似兵营的 SpawnUnitTypeBuffer）
            state.EntityManager.AddBuffer<SpawnZombieRequestBuffer>(waveStateEntity);

            // 全局 Buff 乘区单例：跨波保留，新单位/建筑出生时由 GlobalBuffApplySystem 应用。
            if (!SystemAPI.HasSingleton<GlobalBuff>())
            {
                Entity globalBuffEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(globalBuffEntity, new GlobalBuff
                {
                    modifiers = new Unity.Collections.FixedList512Bytes<BuffModifier>()
                });
            }
            
            UnityEngine.Debug.Log($"[WaveSystemBootstrap] 波次系统初始化完成！创建了 {waveDataSo.waves.Length} 个波次配置");
            
            hasInitialized = true;
            state.Enabled = false;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[WaveSystemBootstrap] 创建实体时出错：{e.Message}\n{e.StackTrace}");
        }
    }
    
    private WaveDataSo CreateDefaultWaveData()
    {
        WaveDataSo waveDataSo = ScriptableObject.CreateInstance<WaveDataSo>();
        waveDataSo.globalPreparationTime = 5f;
        waveDataSo.baseSpawnInterval = 1f;
        waveDataSo.spawnIntervalReductionPerWave = 0.05f;
        waveDataSo.waves = new WaveData[1];
        waveDataSo.waves[0] = new WaveData
        {
            waveNumber = 1,
            waveDuration = 300f,
            preparationTime = 5f,
            baseZombieCount = 10,
            additionalZombiePerWave = 5,
            spawnInterval = 1f,
            zombieHealthMultiplier = 1f,
            zombieDamageMultiplier = 1f,
            isSpecialWave = false,
            specialWaveType = SpecialWaveType.Normal,
            bossWaveConfig = new BossWaveConfig
            {
                bossHealth = 100,
                bossDamage = 10,
                bossSpawnDelay = 5f,
                minionsCount = 0
            }
        };
        
        return waveDataSo;
    }
}
