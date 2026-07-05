using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.GraphicsBuffer;


[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct ResetTargetSystem : ISystem
{
    private ComponentLookup<LocalTransform> localtransformComponentLookup;
    private EntityStorageInfoLookup entityStorageInfoLookup; 
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        localtransformComponentLookup = state.GetComponentLookup<LocalTransform>(true);
        entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        localtransformComponentLookup.Update(ref state);
        entityStorageInfoLookup.Update(ref state);
        ResetTargetJob resetTargetJob = new ResetTargetJob {
            localTransformComponentLookup = localtransformComponentLookup,
            entityStorageInfoLookup = entityStorageInfoLookup
        };

        resetTargetJob.ScheduleParallel();

        ResetTargetOverrideJob resetTargetOverrideJob = new ResetTargetOverrideJob
        {
            localTransformComponentLookup = localtransformComponentLookup,
            entityStorageInfoLookup = entityStorageInfoLookup
        };
        resetTargetOverrideJob.ScheduleParallel();
        /*
        foreach (RefRW<Target> targetOverride in SystemAPI.Query<RefRW<Target>>())
        {
            if (targetOverride.ValueRW.targetEntity != Entity.Null)
            {
                if (!SystemAPI.Exists(targetOverride.ValueRO.targetEntity) || !SystemAPI.HasComponent<LocalTransform>(targetOverride.ValueRO.targetEntity))
                {
                    targetOverride.ValueRW.targetEntity = Entity.Null;
                }
            }
        }
        foreach (RefRW<TargetOverride> targetOverride in SystemAPI.Query<RefRW<TargetOverride>>())
        {
            if (targetOverride.ValueRW.targetEntity != Entity.Null)
            {
                if (!SystemAPI.Exists(targetOverride.ValueRO.targetEntity) || !SystemAPI.HasComponent<LocalTransform>(targetOverride.ValueRO.targetEntity))
                {
                    targetOverride.ValueRW.targetEntity = Entity.Null;
                }
            }
        }*/
    }
}

[BurstCompile]
public partial struct ResetTargetJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public  EntityStorageInfoLookup entityStorageInfoLookup;
    public void Execute(ref Target target)
    {
        
        if (target.targetEntity != Entity.Null)
        {
            if (!entityStorageInfoLookup.Exists(target.targetEntity) || !localTransformComponentLookup.HasComponent(target.targetEntity))
            {
                target.targetEntity = Entity.Null;
            }
        }
    }
}


[BurstCompile]
public partial struct ResetTargetOverrideJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;
    public void Execute(ref TargetOverride targetOverride)
    {

        if (targetOverride.targetEntity != Entity.Null)
        {
            if (!entityStorageInfoLookup.Exists(targetOverride.targetEntity) || !localTransformComponentLookup.HasComponent(targetOverride.targetEntity))
            {
                targetOverride.targetEntity = Entity.Null;
            }
        }
    }
}