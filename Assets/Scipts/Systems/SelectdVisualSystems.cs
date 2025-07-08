using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(ResetEventsSystem ))]

partial struct SelectdVisualSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
        foreach (RefRO<Selected> selectd in SystemAPI.Query<RefRO<Selected>>().WithPresent <Selected>())
        {
            if (selectd.ValueRO.onSelected)
            {
                RefRW<LocalTransform> visualLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selectd.ValueRO.visualEntity);
                visualLocalTransform.ValueRW.Scale = selectd.ValueRO.showScale;
            }
            if (selectd.ValueRO.onDeselected)
            {
                RefRW<LocalTransform> visualLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selectd.ValueRO.visualEntity);
                visualLocalTransform.ValueRW.Scale = 0f;
            }
        }
    }
}
