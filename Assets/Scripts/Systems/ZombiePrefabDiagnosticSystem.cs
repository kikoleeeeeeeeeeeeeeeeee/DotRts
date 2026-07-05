using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
partial struct ZombiePrefabDiagnosticSystem : ISystem
{
    private bool hasLoggedDiagnostic;
    
    public void OnCreate(ref SystemState state)
    {
        hasLoggedDiagnostic = false;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if (hasLoggedDiagnostic)
        {
            state.Enabled = false;
            return;
        }
        
        // 检查 EntitiesReferences
        if (SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
        {
            Debug.Log("=== Zombie Prefab Diagnostic ===");
            Debug.Log($"EntitiesReferences.zombiePrefabEntity: {entitiesReferences.zombiePrefabEntity}");
            
            if (entitiesReferences.zombiePrefabEntity != Entity.Null)
            {
                bool exists = state.EntityManager.Exists(entitiesReferences.zombiePrefabEntity);
                Debug.Log($"Entity Exists: {exists}");
                
                if (exists)
                {
                    bool hasPrefab = state.EntityManager.HasComponent<Unity.Entities.Prefab>(entitiesReferences.zombiePrefabEntity);
                    bool hasMarker = state.EntityManager.HasComponent<ZombiePrefabMarker>(entitiesReferences.zombiePrefabEntity);
                    Debug.Log($"Has Prefab component: {hasPrefab}");
                    Debug.Log($"Has ZombiePrefabMarker: {hasMarker}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[ZombiePrefabDiagnostic] EntitiesReferences singleton not found!");
        }
        
        // 查找所有带 ZombiePrefabMarker 的实体
        var markerQuery = state.EntityManager.CreateEntityQuery(typeof(ZombiePrefabMarker));
        Debug.Log($"Total entities with ZombiePrefabMarker: {markerQuery.CalculateEntityCount()}");
        
        // 查找所有带 Prefab 标签的实体
        var prefabQuery = state.EntityManager.CreateEntityQuery(typeof(Unity.Entities.Prefab));
        Debug.Log($"Total Prefab entities: {prefabQuery.CalculateEntityCount()}");
        
        // 查找同时有 ZombiePrefabMarker 和 Prefab 的实体
        var zombiePrefabQuery = state.EntityManager.CreateEntityQuery(
            typeof(ZombiePrefabMarker),
            typeof(Unity.Entities.Prefab)
        );
        
        var entities = zombiePrefabQuery.ToEntityArray(Allocator.Temp);
        Debug.Log($"Zombie Prefab entities (with both Marker + Prefab): {entities.Length}");
        
        foreach (var entity in entities)
        {
            Debug.Log($"  - Entity({entity.Index}:{entity.Version})");
        }
        
        entities.Dispose();
        
        Debug.Log("=== End Diagnostic ===");
        hasLoggedDiagnostic = true;
        state.Enabled = false;
    }
}
