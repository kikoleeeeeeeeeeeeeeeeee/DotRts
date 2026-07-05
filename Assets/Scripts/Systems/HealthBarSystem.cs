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
        localTransformComponentLookup.Update(ref state);
        healthComponentLookup.Update(ref state);
        postTransformMatrixComponentLookup.Update(ref state);

        UnityEngine.Vector3 cameraForward = UnityEngine.Vector3.forward;
        if (Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
        }

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
        if (!localtransformComponentLookup.HasComponent(healthBar.healthEntity) ||
            !healthComponentLookup.HasComponent(healthBar.healthEntity))
        {
            return;
        }

        RefRW<LocalTransform> localTransform = localtransformComponentLookup.GetRefRW(entity);
        LocalTransform parentLocalTransform = localtransformComponentLookup[healthBar.healthEntity];

        if (math.lengthsq(cameraForward) > 0.001f && localTransform.ValueRO.Scale == 1f)
        {
            localTransform.ValueRW.Rotation = parentLocalTransform.InverseTransformRotation(
                quaternion.LookRotation(cameraForward, math.up()));
        }

        Health health = healthComponentLookup[healthBar.healthEntity];

        if (!health.onHealthChanged)
            return;

        float healthNormalized = health.healthAmountMax > 0
            ? (float)health.healthAmount / health.healthAmountMax
            : 0f;

        healthNormalized = math.clamp(healthNormalized, 0f, 1f);

        localTransform.ValueRW.Scale = healthNormalized >= 1.0f ? 0.001f : 1f;

        if (!postTransformMatrixheaComponentLookup.HasComponent(healthBar.barVisualEntity))
        {
            return;
        }

        RefRW<PostTransformMatrix> barVisualPostTransformMatrix =
            postTransformMatrixheaComponentLookup.GetRefRW(healthBar.barVisualEntity);

        barVisualPostTransformMatrix.ValueRW.Value = float4x4.Scale(math.max(healthNormalized, 0.001f), 1, 1);
    }
}
