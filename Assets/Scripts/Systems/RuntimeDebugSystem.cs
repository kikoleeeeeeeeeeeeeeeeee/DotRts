using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RuntimeDebugSystem : ISystem
{
    private bool hasLogged;
    
    public void OnCreate(ref SystemState state)
    {
        hasLogged = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (hasLogged) return;
        
        Debug.Log("=== 运行时调试信息 ===");
        
        // 检查 ECS World
        var world = state.World;
        Debug.Log($"[RuntimeDebug] World: {world.Name}, EntityManager: {world.EntityManager != null}");
        
        // 检查 EntitiesReferences 组件
        if (SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
        {
            Debug.Log("[RuntimeDebug] EntitiesReferences 单例存在！");
            Debug.Log($"  zombiePrefabEntity: {entitiesReferences.zombiePrefabEntity}, Valid: {entitiesReferences.zombiePrefabEntity != Entity.Null}");
            Debug.Log($"  buildingHQPrefabEntity: {entitiesReferences.buildingHQPrefabEntity}, Valid: {entitiesReferences.buildingHQPrefabEntity != Entity.Null}");
            Debug.Log($"  soldierPrefabEntity: {entitiesReferences.soldierPrefabEntity}, Valid: {entitiesReferences.soldierPrefabEntity != Entity.Null}");
        }
        else
        {
            Debug.LogError("[RuntimeDebug] EntitiesReferences 单例不存在！");
        }
        
        // 检查僵尸预制体标记
        var zombiePrefabQuery = SystemAPI.QueryBuilder().WithAll<ZombiePrefabMarker, Unity.Entities.Prefab>().Build();
        int zombiePrefabCount = zombiePrefabQuery.CalculateEntityCount();
        Debug.Log($"[RuntimeDebug] 僵尸预制体数量: {zombiePrefabCount}");
        
        // 检查波次状态
        if (SystemAPI.TryGetSingleton<WaveState>(out var waveState))
        {
            Debug.Log("[RuntimeDebug] WaveState 存在！");
            Debug.Log($"  currentWaveIndex: {waveState.currentWaveIndex}");
            Debug.Log($"  isWaveActive: {waveState.isWaveActive}");
            Debug.Log($"  isPreparationPhase: {waveState.isPreparationPhase}");
            Debug.Log($"  preparationTimer: {waveState.preparationTimer}");
            Debug.Log($"  isBasePlaced: {waveState.isBasePlaced}");
        }
        else
        {
            Debug.LogError("[RuntimeDebug] WaveState 不存在！");
        }
        
        hasLogged = true;
    }
}