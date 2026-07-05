using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class CreateZombiePrefabInScene : EditorWindow
{
    [MenuItem("Tools/Create Zombie Prefab in Scene")]
    public static void ShowWindow()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[ZombieSpawner] ECS World 不存在！请确保游戏正在运行");
            return;
        }

        var entityManager = world.EntityManager;
        
        // 查找僵尸预制体
        var query = entityManager.CreateEntityQuery(
            typeof(ZombiePrefabMarker),
            typeof(Unity.Entities.Prefab)
        );

        var prefabEntities = query.ToEntityArray(Allocator.Temp);

        if (prefabEntities.Length > 0)
        {
            Debug.Log($"[ZombieSpawner] 找到 {prefabEntities.Length} 个僵尸预制体");
            foreach (var entity in prefabEntities)
            {
                Debug.Log($"  Entity Index: {entity.Index}");
            }
        }
        else
        {
            Debug.LogError("[ZombieSpawner] 未找到僵尸预制体！");
            Debug.LogError("[ZombieSpawner] 请确保:");
            Debug.LogError("  1. 在 Hierarchy 中创建一个空 GameObject");
            Debug.LogError("  2. 将 ZombieUnit 预制体拖入场景中作为子物体");
            Debug.LogError("  3. 确保该 GameObject 是激活状态");
            Debug.LogError("  4. 保存场景并重新进入 Play 模式");
        }

        prefabEntities.Dispose();
    }
}
