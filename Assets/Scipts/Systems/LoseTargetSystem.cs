using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct LoseTargetSystem : ISystem
{


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        foreach (
            (RefRO<LocalTransform> localtransform,
            RefRW<Target> target,
            RefRO<LoseTarget> loseTarget,
            RefRO<TargetOverride> targetOverride)
        in SystemAPI.Query<
            RefRO<LocalTransform>, 
            RefRW<Target>,
            RefRO<LoseTarget>,
            RefRO<TargetOverride>>()){

            if(target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }
            
            if(targetOverride.ValueRO.targetEntity != Entity.Null)
            {
                continue;
            }

            if (!localTransformLookup.HasComponent(targetOverride.ValueRO.targetEntity))
                continue;

            LocalTransform targetlocaltransform =  SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            float targetDistance = math.distance(localtransform.ValueRO.Position, targetlocaltransform.Position);
            if(targetDistance> loseTarget.ValueRO.loseTargetDistance)
            {
                target.ValueRW.targetEntity = Entity.Null;
            }

        }
    }

}
