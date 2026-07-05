using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DebugEntitiesReferencesSystem : ISystem
{
    private bool hasLogged;
    
    public void OnCreate(ref SystemState state)
    {
        hasLogged = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (hasLogged) return;
        
        // 检查是否存在 EntitiesReferences 单例
        if (SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
        {
            Debug.Log("[DebugEntitiesReferencesSystem] EntitiesReferences singleton found!");
            Debug.Log($"  zombiePrefabEntity: {entitiesReferences.zombiePrefabEntity}, Valid: {entitiesReferences.zombiePrefabEntity != Entity.Null}");
            Debug.Log($"  buildingHQPrefabEntity: {entitiesReferences.buildingHQPrefabEntity}, Valid: {entitiesReferences.buildingHQPrefabEntity != Entity.Null}");
            Debug.Log($"  soldierPrefabEntity: {entitiesReferences.soldierPrefabEntity}, Valid: {entitiesReferences.soldierPrefabEntity != Entity.Null}");
        }
        else
        {
            Debug.LogError("[DebugEntitiesReferencesSystem] EntitiesReferences singleton NOT found!");
        }
        
        // 检查是否有 EntitiesReferenceAuthoring 对应的实体
        foreach (var (entitiesRefs, entity) in SystemAPI.Query<EntitiesReferences>().WithEntityAccess())
        {
            Debug.Log($"[DebugEntitiesReferencesSystem] Found EntitiesReferences component on entity: {entity}");
            Debug.Log($"  zombiePrefabEntity: {entitiesRefs.zombiePrefabEntity}, Valid: {entitiesRefs.zombiePrefabEntity != Entity.Null}");
        }
        
        hasLogged = true;
    }
}