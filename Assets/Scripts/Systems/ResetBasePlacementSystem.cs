using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 重置基地放置系统 - 在每波开始时重置放置标志
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct ResetBasePlacementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameSceneTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 使用 EntityCommandBuffer 进行结构性修改
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        // 检查是否有波次开始事件
        foreach (var startWaveEvent in SystemAPI.Query<StartWaveEvent>().WithEntityAccess())
        {
            // 重置基地放置标志
            if (BasePlacementSystem.Instance != null)
            {
                BasePlacementSystem.Instance.ResetPlacementFlag();
            }
            
            // 使用 ECB 销毁事件
            ecb.DestroyEntity(startWaveEvent.Item2);
        }
    }
}
