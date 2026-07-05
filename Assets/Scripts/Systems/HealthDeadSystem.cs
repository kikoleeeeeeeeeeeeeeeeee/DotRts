using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthDeadSystem : ISystem
{
    // 存储本帧死亡的僵尸实体列表
    private NativeList<Entity> deadZombiesList;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 创建持久化的 NativeList，用于在帧之间存储数据
        deadZombiesList = new NativeList<Entity>(Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // 系统销毁时释放 NativeList 内存
        if (deadZombiesList.IsCreated)
        {
            deadZombiesList.Dispose();
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 获取实体命令缓冲区，用于延迟销毁实体
        EntityCommandBuffer entityCommandBuffer = 
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        // 清空上一帧的死亡僵尸列表
        deadZombiesList.Clear();

        // 遍历所有具有 Health 组件的实体
        foreach((
            RefRW<Health> health,
            Entity entity ) 
            in SystemAPI.Query
                <RefRW<Health>>().WithEntityAccess())
        {
            // 检查生命值是否小于等于 0
            if(health.ValueRO.healthAmount <= 0)
            {
                // 标记实体为死亡状态
                health.ValueRW.onDead = true;
                // 先禁用渲染，避免死亡实体在销毁前仍被渲染导致 IsFinite 断言
                if (SystemAPI.HasComponent<RenderMeshUnmanaged>(entity))
                {
                    entityCommandBuffer.AddComponent<DisableRendering>(entity);
                }
                // 使用命令缓冲区销毁实体（延迟到帧末执行）
                entityCommandBuffer.DestroyEntity(entity);

                // 检查死亡实体是否是僵尸
                if (SystemAPI.HasComponent<Zombie>(entity))
                {
                    // 将死亡的僵尸实体添加到列表中
                    deadZombiesList.Add(entity);
                }

                // 如果是建筑建造中，需要额外销毁视觉实体
                if (SystemAPI.HasComponent<BuildignConstruction>(entity))
                {
                    BuildignConstruction buildignConstruction = SystemAPI.GetComponent<BuildignConstruction>(entity);

                    entityCommandBuffer.DestroyEntity(buildignConstruction.visualEntity);
                }
            }
        }

        // 如果有僵尸死亡，通知事件管理器
        // WaveKillListener 会监听此事件并通知 WaveManager 更新击杀计数
        if (deadZombiesList.Length > 0)
        {
            DotsEventsManager.Instance.TriggerOnZombieDead(deadZombiesList);
        }
    }
}
