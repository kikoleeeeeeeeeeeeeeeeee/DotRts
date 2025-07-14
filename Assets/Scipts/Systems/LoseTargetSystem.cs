using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct LoseTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Target> target,
            RefRO<LoseTarget> loseTarget,
            RefRO<TargetOverride> targetOverride)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Target>,
                RefRO<LoseTarget>,
                RefRO<TargetOverride>>())
        {
            Entity targetEntity = target.ValueRO.targetEntity;

            // 目标实体为空，跳过
            if (targetEntity == Entity.Null)
                continue;

            // 有目标覆盖实体，跳过
            if (targetOverride.ValueRO.targetEntity != Entity.Null)
                continue;

            // 目标实体不存在，跳过
            if (!SystemAPI.Exists(targetEntity))
                continue;

            // 目标实体没有 LocalTransform 组件，跳过
            if (!SystemAPI.HasComponent<LocalTransform>(targetEntity))
                continue;

            // 读取目标位置
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetEntity);

            float targetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);

            if (targetDistance > loseTarget.ValueRO.loseTargetDistance)
            {
                // 目标距离太远，重置目标实体为空
                target.ValueRW.targetEntity = Entity.Null;
            }
        }
    }
}
