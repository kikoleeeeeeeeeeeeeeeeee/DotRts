using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthBarSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformComponentLookup;
    private ComponentLookup<Health> healthComponentLookup;
    private ComponentLookup<PostTransformMatrix> postTransformMatrixComponentLookup;

    public void OnCreate(ref SystemState state)
    {
        localTransformComponentLookup = state.GetComponentLookup<LocalTransform>(false);
        healthComponentLookup = state.GetComponentLookup<Health>(true);
        postTransformMatrixComponentLookup = state.GetComponentLookup<PostTransformMatrix>(false);
    }

    public void OnUpdate(ref SystemState state)
    {
        // 更新 ComponentLookup 引用
        localTransformComponentLookup.Update(ref state);
        healthComponentLookup.Update(ref state);
        postTransformMatrixComponentLookup.Update(ref state);

        // 获取相机方向
        UnityEngine.Vector3 cameraForward = UnityEngine.Vector3.zero;
        if (Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
        }

        // 创建并调度 Job
        var healthBarJob = new HealthBarJob
        {
            cameraForward = cameraForward,
            localtransformComponentLookup = localTransformComponentLookup,
            healthComponentLookup = healthComponentLookup,
            postTransformMatrixheaComponentLookup = postTransformMatrixComponentLookup
        };

        healthBarJob.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct HealthBarJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> localtransformComponentLookup;
    [ReadOnly] public ComponentLookup<Health> healthComponentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PostTransformMatrix> postTransformMatrixheaComponentLookup;

    public float3 cameraForward;

    public void Execute(in HealthBar healthBar, Entity entity)
    {
        RefRW<LocalTransform> localTransform = localtransformComponentLookup.GetRefRW(entity);
        LocalTransform parentLocalTransform = localtransformComponentLookup[healthBar.healthEntity];

        if (localTransform.ValueRO.Scale == 1f)
        {
            localTransform.ValueRW.Rotation = parentLocalTransform.InverseTransformRotation(
                quaternion.LookRotation(cameraForward, math.up()));
        }

        Health health = healthComponentLookup[healthBar.healthEntity];

        if (!health.onHealthChanged)
            return;

        float healthNormalized = (float)health.healthAmount / health.healthAmountMax;

        localTransform.ValueRW.Scale = healthNormalized == 1.0f ? 0f : 1f;

        RefRW<PostTransformMatrix> barVisualPostTransformMatrix =
            postTransformMatrixheaComponentLookup.GetRefRW(healthBar.barVisualEntity);

        barVisualPostTransformMatrix.ValueRW.Value = float4x4.Scale(healthNormalized, 1, 1);
    }
}
