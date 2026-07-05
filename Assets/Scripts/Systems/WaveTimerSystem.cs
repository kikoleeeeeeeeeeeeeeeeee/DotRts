using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
partial struct WaveTimerSystem : ISystem
{
    private int logFrameCounter;
    private bool hasLoggedWaitingForBase;
    private bool hasLoggedWaveStarted;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaveState>();
        logFrameCounter = 0;
        hasLoggedWaitingForBase = false;
        hasLoggedWaveStarted = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        logFrameCounter++;

        var waveConfigQuery = SystemAPI.QueryBuilder().WithAll<WaveConfiguration>().Build();
        if (waveConfigQuery.CalculateEntityCount() == 0)
        {
            if (logFrameCounter % 300 == 1)
                Debug.LogError("[WaveTimer] WaveConfiguration not found! No wave data entity exists.");
            return;
        }
        var waveConfig = state.EntityManager.GetSharedComponent<WaveConfiguration>(waveConfigQuery.GetSingletonEntity());
        
        var waveDataQuery = SystemAPI.QueryBuilder().WithAll<WaveDataBuffer>().Build();
        if (waveDataQuery.CalculateEntityCount() == 0)
        {
            if (logFrameCounter % 300 == 1)
                Debug.LogError("[WaveTimer] WaveDataBuffer not found! No wave data entity exists.");
            return;
        }
        var waveDataBuffer = state.EntityManager.GetBuffer<WaveDataBuffer>(waveDataQuery.GetSingletonEntity());
        
        foreach (RefRW<WaveState> waveState in SystemAPI.Query<RefRW<WaveState>>())
        {
            if (waveState.ValueRO.isWaveActive)
            {
                if (!hasLoggedWaveStarted)
                {
                    Debug.Log($"[WaveTimer] Wave {waveState.ValueRO.currentWaveIndex} is active. Zombies spawned: {waveState.ValueRO.zombiesSpawnedInCurrentWave}/{waveState.ValueRO.totalZombiesToSpawn}, timer: {waveState.ValueRO.waveTimer:F1}s");
                    hasLoggedWaveStarted = true;
                }
                continue;
            }
            
            if (WaveManager.Instance != null)
            {
                bool isBasePlaced = WaveManager.Instance.IsBasePlaced;
                float preparationTimer = WaveManager.Instance.PreparationTimer;
                
                waveState.ValueRW.isBasePlaced = isBasePlaced;
                waveState.ValueRW.preparationTimer = preparationTimer;
                
                if (!isBasePlaced)
                {
                    if (!hasLoggedWaitingForBase)
                    {
                        Debug.LogError("[WaveTimer] Base not placed! Waiting for HQ placement before starting wave.");
                        hasLoggedWaitingForBase = true;
                    }
                    continue;
                }

                hasLoggedWaitingForBase = false;
                
                if (preparationTimer <= 0f && waveState.ValueRO.isPreparationPhase)
                {
                    int waveIndex = waveState.ValueRO.currentWaveIndex;
                    
                    if (waveIndex < waveDataBuffer.Length)
                    {
                        WaveDataBuffer waveData = waveDataBuffer[waveIndex];
                        
                        int totalZombies = waveData.baseZombieCount + (waveData.additionalZombiePerWave * waveIndex);
                        waveState.ValueRW.totalZombiesToSpawn = totalZombies;
                        waveState.ValueRW.spawnInterval = math.max(0.1f, waveData.spawnInterval - (waveConfig.spawnIntervalReductionPerWave * waveIndex));
                        waveState.ValueRW.waveTimer = waveData.waveDuration;
                        waveState.ValueRW.zombiesSpawnedInCurrentWave = 0;
                        waveState.ValueRW.spawnTimer = waveState.ValueRW.spawnInterval;
                        waveState.ValueRW.isPreparationPhase = false;
                        waveState.ValueRW.isWaveActive = true;
                        
                        Debug.Log($"[WaveTimer] Wave {waveIndex} started! Total zombies: {totalZombies}, spawn interval: {waveState.ValueRW.spawnInterval:F2}s, wave duration: {waveData.waveDuration:F1}s");
                        
                        hasLoggedWaveStarted = false;
                        WaveManager.Instance.OnECSWaveStarted(waveIndex, totalZombies);
                    }
                    else
                    {
                        Debug.LogError($"[WaveTimer] Wave index {waveIndex} out of range (max {waveDataBuffer.Length}). All waves completed?");
                    }
                }
            }
            else
            {
                if (logFrameCounter % 300 == 1)
                {
                    Debug.LogError("[WaveTimer] WaveManager.Instance is null! Cannot read wave state from MonoBehaviour.");
                }

                if (waveState.ValueRO.isPreparationPhase && waveState.ValueRO.isBasePlaced)
                {
                    waveState.ValueRW.preparationTimer -= SystemAPI.Time.DeltaTime;
                    
                    if (waveState.ValueRO.preparationTimer <= 0f)
                    {
                        int waveIndex = waveState.ValueRO.currentWaveIndex;
                        
                        if (waveIndex < waveDataBuffer.Length)
                        {
                            WaveDataBuffer waveData = waveDataBuffer[waveIndex];
                            
                            int totalZombies = waveData.baseZombieCount + (waveData.additionalZombiePerWave * waveIndex);
                            waveState.ValueRW.totalZombiesToSpawn = totalZombies;
                            waveState.ValueRW.spawnInterval = math.max(0.1f, waveData.spawnInterval - (waveConfig.spawnIntervalReductionPerWave * waveIndex));
                            waveState.ValueRW.waveTimer = waveData.waveDuration;
                            waveState.ValueRW.zombiesSpawnedInCurrentWave = 0;
                            waveState.ValueRW.spawnTimer = waveState.ValueRW.spawnInterval;
                            waveState.ValueRW.isPreparationPhase = false;
                            waveState.ValueRW.isWaveActive = true;
                            
                            Debug.Log($"[WaveTimer] Wave {waveIndex} started (ECS mode)! Total zombies: {totalZombies}");
                        }
                    }
                }
            }
        }
    }
}