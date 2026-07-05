using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 调试工具：手动生成僵尸进行测试
/// </summary>
public class DebugSpawnZombie : MonoBehaviour
{
    public static DebugSpawnZombie Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    [ContextMenu("Spawn 1 Zombie at HQ")]
    public void SpawnZombieAtHQ()
    {
        SpawnZombie(1);
    }
    
    [ContextMenu("Spawn 5 Zombies at HQ")]
    public void Spawn5ZombiesAtHQ()
    {
        SpawnZombie(5);
    }
    
    [ContextMenu("Spawn 10 Zombies at HQ")]
    public void Spawn10ZombiesAtHQ()
    {
        SpawnZombie(10);
    }
    
    public void SpawnZombie(int amount)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[DebugSpawnZombie] ECS World 不存在！");
            return;
        }
        
        var entityManager = world.EntityManager;
        
        // 获取僵尸预制体
        Entity zombiePrefabEntity = GetZombiePrefabEntity(entityManager);
        if (zombiePrefabEntity == Entity.Null)
        {
            Debug.LogError("[DebugSpawnZombie] 未找到僵尸预制体！");
            return;
        }
        
        // 获取 HQ 位置
        float3 hqPosition = GetHQPosition(entityManager);
        
        // 生成僵尸
        for (int i = 0; i < amount; i++)
        {
            Entity zombieEntity = entityManager.Instantiate(zombiePrefabEntity);
            entityManager.AddComponent<RuntimeEntity>(zombieEntity);
            
            // 计算生成位置（HQ 周围随机位置）
            float angle = (float)i / amount * math.PI * 2;
            float radius = 10f;
            float3 spawnPosition = hqPosition + new float3(math.cos(angle) * radius, 0, math.sin(angle) * radius);
            
            // 设置位置
            entityManager.SetComponentData(zombieEntity, new LocalTransform
            {
                Position = spawnPosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            
            Debug.Log($"[DebugSpawnZombie] 生成僵尸 #{i+1}, 位置：{spawnPosition}, Entity: {zombieEntity}");
        }
        
        Debug.Log($"[DebugSpawnZombie] 成功生成 {amount} 个僵尸");
    }
    
    private Entity GetZombiePrefabEntity(EntityManager entityManager)
    {
        // 查找带有 ZombieSpawnSource 标记的实体
        var query = entityManager.CreateEntityQuery(
            typeof(ZombieSpawnSource),
            typeof(ZombiePrefabMarker),
            typeof(Zombie)
        );
        
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            Entity result = entities[0];
            entities.Dispose();
            return result;
        }
        entities.Dispose();
        
        Debug.LogError("[DebugSpawnZombie] 无法找到僵尸预制体！请确保场景中有僵尸单位。");
        return Entity.Null;
    }
    
    private float3 GetHQPosition(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingHQ));
        if (query.CalculateEntityCount() > 0)
        {
            Entity hqEntity = query.GetSingletonEntity();
            if (entityManager.HasComponent<LocalTransform>(hqEntity))
            {
                return entityManager.GetComponentData<LocalTransform>(hqEntity).Position;
            }
        }
        
        Debug.LogWarning("[DebugSpawnZombie] 未找到 HQ，使用地图中心 (0,0,0)");
        return float3.zero;
    }
}
