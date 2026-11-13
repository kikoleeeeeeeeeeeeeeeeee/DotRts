using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 基地放置系统 - 在子场景内处理基地放置逻辑
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
partial struct BasePlacementECS : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameSceneTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 检查是否已经放置过基地
        var hqQuery = SystemAPI.QueryBuilder().WithAll<BuildingHQ>().Build();
        if (hqQuery.CalculateEntityCount() > 0)
        {
            // 已经放置过，不再处理
            return;
        }

        // 使用 EntityCommandBuffer 进行结构性修改
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        // 检查是否有放置请求
        foreach (var (placeRequest, entity) in SystemAPI.Query<RefRO<PlaceBaseRequest>>().WithEntityAccess())
        {
            // 使用预制体 Entity 进行实例化（继承所有烘焙的组件）
            Entity hqEntity = ecb.Instantiate(placeRequest.ValueRO.hqPrefabEntity);
            ecb.AddComponent<RuntimeEntity>(hqEntity);
            ecb.AddComponent<PendingGlobalBuff>(hqEntity);

            // 设置位置
            ecb.SetComponent(hqEntity, new LocalTransform
            {
                Position = placeRequest.ValueRO.position,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            
            // 销毁放置请求
            ecb.DestroyEntity(entity);
            
            Debug.Log($"[BasePlacementECS] 基地已放置在位置 {placeRequest.ValueRO.position}");
        }
    }
}

/// <summary>
/// 放置基地请求组件
/// </summary>
public struct PlaceBaseRequest : IComponentData
{
    public float3 position;
    public Entity hqPrefabEntity; // 添加 HQ 预制体 Entity
}
