using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
partial struct WaveSystemBootstrap : ISystem
{
    private bool hasInitialized;
    
    public void OnCreate(ref SystemState state)
    {
        hasInitialized = false;
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
            hasInitialized = true;
            state.Enabled = false;
            return;
        }
        
        var gameSceneTagQuery = state.EntityManager.CreateEntityQuery(typeof(GameSceneTag));
        if (gameSceneTagQuery.CalculateEntityCount() == 0)
        {
            UnityEngine.Debug.Log("[WaveSystemBootstrap] 等待 GameSceneTag 加载...");
            return;
        }
        
        try
        {
            UnityEngine.Debug.Log("[WaveSystemBootstrap] 检测到 GameSceneTag，开始初始化波次系统...");
            
            WaveDataSo waveDataSo = Resources.Load<WaveDataSo>("WaveData");
            if (waveDataSo == null)
            {
                UnityEngine.Debug.LogError("[WaveSystemBootstrap] Failed to load WaveDataSo from Resources! Using default config. Put WaveDataSo in a Resources folder named 'WaveData'.");
                waveDataSo = CreateDefaultWaveData();
            }
            else
            {
                UnityEngine.Debug.Log($"[WaveSystemBootstrap] Loaded WaveDataSo from Resources: {waveDataSo.waves.Length} waves, preparationTime={waveDataSo.globalPreparationTime}s, baseZombieCount={waveDataSo.waves[0].baseZombieCount}");
            }
            
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
                isPreparationPhase = true,
                isBasePlaced = false,
                zombiesKilledInCurrentWave = 0,
                waveTimer = 0f,
                preparationTimer = waveDataSo.globalPreparationTime,
                spawnTimer = 0f,
                zombiesSpawnedInCurrentWave = 0,
                totalZombiesToSpawn = 0,
                spawnInterval = waveDataSo.baseSpawnInterval
            });
            
            // 添加生成请求 Buffer（类似兵营的 SpawnUnitTypeBuffer）
            state.EntityManager.AddBuffer<SpawnZombieRequestBuffer>(waveStateEntity);
            
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
            waveDuration = Mathf.Infinity,
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
