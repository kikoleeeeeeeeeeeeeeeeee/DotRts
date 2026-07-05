using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class ZombiePrefabValidator : EditorWindow
{
    [MenuItem("Tools/Validate Zombie Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ZombiePrefabValidator>("Zombie Prefab Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Zombie Prefab 验证工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("查找僵尸预制体"))
        {
            FindZombiePrefab();
        }
        
        if (GUILayout.Button("检查烘焙状态"))
        {
            CheckBakeStatus();
        }
    }

    private void FindZombiePrefab()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[ZombieValidator] ECS World 不存在！请确保游戏正在运行");
            return;
        }

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(
            typeof(ZombiePrefabMarker),
            typeof(Unity.Entities.Prefab)
        );

        var prefabEntities = query.ToEntityArray(Allocator.Temp);

        if (prefabEntities.Length > 0)
        {
            Debug.Log($"[ZombieValidator] 找到 {prefabEntities.Length} 个僵尸预制体:");
            foreach (var entity in prefabEntities)
            {
                Debug.Log($"  - Entity Index: {entity.Index}, Version: {entity.Version}");
            }
            Debug.Log("[ZombieValidator] 僵尸预制体查找成功！现在可以运行游戏测试了。");
        }
        else
        {
            Debug.LogError("[ZombieValidator] 未找到僵尸预制体！");
            Debug.LogError("[ZombieValidator] 请确保:");
            Debug.LogError("  1. 僵尸预制体上有 ZombieAuthoring 组件");
            Debug.LogError("  2. 预制体在场景中是激活状态 (SetActive(true))");
            Debug.LogError("  3. 场景已经正确烘焙");
        }

        prefabEntities.Dispose();
    }

    private void CheckBakeStatus()
    {
        Debug.Log("=== 检查 ECS 烘焙状态 ===");
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[ZombieValidator] ECS World 不存在！");
            return;
        }

        var entityManager = world.EntityManager;
        
        // 检查所有 Prefab
        var prefabQuery = entityManager.CreateEntityQuery(typeof(Unity.Entities.Prefab));
        Debug.Log($"[ZombieValidator] 总 Prefab 数量：{prefabQuery.CalculateEntityCount()}");
        
        // 检查所有 ZombiePrefabMarker
        var markerQuery = entityManager.CreateEntityQuery(typeof(ZombiePrefabMarker));
        Debug.Log($"[ZombieValidator] 带有 ZombiePrefabMarker 的实体数量：{markerQuery.CalculateEntityCount()}");
        
        // 检查同时有 Prefab 和 ZombiePrefabMarker 的实体
        var zombiePrefabQuery = entityManager.CreateEntityQuery(
            typeof(Unity.Entities.Prefab),
            typeof(ZombiePrefabMarker)
        );
        Debug.Log($"[ZombieValidator] 僵尸预制体数量：{zombiePrefabQuery.CalculateEntityCount()}");
    }
}
