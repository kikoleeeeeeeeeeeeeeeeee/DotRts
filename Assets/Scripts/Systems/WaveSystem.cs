using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static GridSystem;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ZombieSpawnerSystem))]
[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
partial struct WaveSystem : ISystem
{
    private int logFrameCounter;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaveState>();
        state.RequireForUpdate<GridSystem.GridSystemData>();
        logFrameCounter = 0;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        logFrameCounter++;

        var waveConfigQuery = SystemAPI.QueryBuilder().WithAll<WaveConfiguration>().Build();
        if (waveConfigQuery.CalculateEntityCount() == 0) return;
        var waveConfig = state.EntityManager.GetSharedComponent<WaveConfiguration>(waveConfigQuery.GetSingletonEntity());
        
        var waveDataQuery = SystemAPI.QueryBuilder().WithAll<WaveDataBuffer>().Build();
        if (waveDataQuery.CalculateEntityCount() == 0) return;
        var waveDataBuffer = state.EntityManager.GetBuffer<WaveDataBuffer>(waveDataQuery.GetSingletonEntity());
        
        EntityCommandBuffer entityCommandBuffer = 
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach ((StartWaveEvent startWaveEvent, Entity eventEntity) in SystemAPI.Query<StartWaveEvent>().WithEntityAccess())
        {
            foreach (RefRW<WaveState> waveState in SystemAPI.Query<RefRW<WaveState>>())
            {
                if (startWaveEvent.waveIndex < waveDataBuffer.Length)
                {
                    WaveDataBuffer waveData = waveDataBuffer[startWaveEvent.waveIndex];
                    
                    int totalZombies = waveData.baseZombieCount + (waveData.additionalZombiePerWave * startWaveEvent.waveIndex);
                    waveState.ValueRW.totalZombiesToSpawn = totalZombies;
                    waveState.ValueRW.spawnInterval = math.max(0.1f, waveData.spawnInterval - (waveConfig.spawnIntervalReductionPerWave * startWaveEvent.waveIndex));
                    waveState.ValueRW.waveTimer = waveData.waveDuration;
                    waveState.ValueRW.zombiesSpawnedInCurrentWave = 0;
                    waveState.ValueRW.spawnTimer = 0f;
                    waveState.ValueRW.isWaveActive = true;
                    
                    Debug.Log($"[WaveSystem] StartWaveEvent processed: wave {startWaveEvent.waveIndex}, zombies: {totalZombies}");
                    
                    if (WaveManager.Instance != null)
                    {
                        WaveManager.Instance.OnECSWaveStarted(startWaveEvent.waveIndex, totalZombies);
                    }
                }
            }
            
            entityCommandBuffer.DestroyEntity(eventEntity);
        }
        
        foreach ((RefRW<WaveState> waveState, Entity waveStateEntity) in SystemAPI.Query<RefRW<WaveState>>().WithEntityAccess())
        {
            if (!waveState.ValueRO.isWaveActive)
            {
                continue;
            }
            
            waveState.ValueRW.waveTimer -= SystemAPI.Time.DeltaTime;
            
            if (waveState.ValueRO.waveTimer <= 0f)
            {
                waveState.ValueRW.isWaveActive = false;
                waveState.ValueRW.currentWaveIndex++;
                waveState.ValueRW.isPreparationPhase = true;
                waveState.ValueRW.preparationTimer = waveConfig.globalPreparationTime;
                waveState.ValueRW.zombiesKilledInCurrentWave = 0;
                waveState.ValueRW.zombiesSpawnedInCurrentWave = 0;
                
                Debug.Log($"[WaveSystem] Wave ended. Next wave index: {waveState.ValueRO.currentWaveIndex}");
                
                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.EndCurrentWave();
                }
                continue;
            }
            
            waveState.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;
            
            if (waveState.ValueRO.spawnTimer <= 0f && 
                waveState.ValueRO.zombiesSpawnedInCurrentWave < waveState.ValueRO.totalZombiesToSpawn)
            {
                float3 spawnPosition = GetRandomSpawnPosition(ref state);
                
                if (!math.isnan(spawnPosition.x) && !math.isnan(spawnPosition.y) && !math.isnan(spawnPosition.z))
                {
                    var spawnRequestBuffer = state.EntityManager.GetBuffer<SpawnZombieRequestBuffer>(waveStateEntity);
                    spawnRequestBuffer.Add(new SpawnZombieRequestBuffer
                    {
                        spawnPosition = spawnPosition
                    });
                    
                    waveState.ValueRW.zombiesSpawnedInCurrentWave++;
                    
                    if (waveState.ValueRO.zombiesSpawnedInCurrentWave <= 3 || waveState.ValueRO.zombiesSpawnedInCurrentWave % 10 == 0)
                    {
                        Debug.Log($"[WaveSystem] Spawn request #{waveState.ValueRO.zombiesSpawnedInCurrentWave}/{waveState.ValueRO.totalZombiesToSpawn} at {spawnPosition}");
                    }
                }
                else
                {
                    Debug.LogError($"[WaveSystem] Spawn position is NaN! spawnPosition=({spawnPosition.x},{spawnPosition.y},{spawnPosition.z})");
                }
                
                waveState.ValueRW.spawnTimer = waveState.ValueRO.spawnInterval;
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
                if (!hqTransform.Position.Equals(float3.zero) &&
                    !math.isnan(hqTransform.Position.x) &&
                    !math.isnan(hqTransform.Position.y) &&
                    !math.isnan(hqTransform.Position.z))
                {
                    basePosition = hqTransform.Position;
                }
                else
                {
                    Debug.LogError("[WaveSystem] HQ position is invalid (zero or NaN)! Zombies will target map center.");
                }
            }
        }
        else
        {
            Debug.LogError("[WaveSystem] No BuildingHQ singleton found! Zombies will target map center (0,0,0).");
        }

        float mapWidth = 200f;
        float mapHeight = 200f;
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            mapWidth = mapConfig.mapWidth;
            mapHeight = mapConfig.mapHeight;
            if (mapWidth <= 0) mapWidth = 200f;
            if (mapHeight <= 0) mapHeight = 200f;
        }
        else
        {
            Debug.LogError("[WaveSystem] No MapConfigData singleton found! Using default map size 200x200.");
        }

        float edgeDistance = math.min(mapWidth, mapHeight) * 0.4f;

        float angleDegrees = UnityEngine.Random.Range(0f, 360f);
        float angleRadians = math.radians(angleDegrees);
        float3 spawnDirection = new float3(math.cos(angleRadians), 0f, math.sin(angleRadians));

        float3 spawnPosition = basePosition + spawnDirection * edgeDistance;

        spawnPosition.x = math.clamp(spawnPosition.x, -mapWidth / 2, mapWidth / 2);
        spawnPosition.z = math.clamp(spawnPosition.z, -mapHeight / 2, mapHeight / 2);
        spawnPosition.y = 0f;

        if (math.isnan(spawnPosition.x) || math.isnan(spawnPosition.y) || math.isnan(spawnPosition.z))
        {
            Debug.LogError("[WaveSystem] Final spawn position is NaN! Returning zero.");
            return float3.zero;
        }

        return spawnPosition;
    }
}