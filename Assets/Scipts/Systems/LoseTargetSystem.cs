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

            // Ŀ��ʵ��Ϊ�գ�����
            if (targetEntity == Entity.Null)
                continue;

            // ��Ŀ�긲��ʵ�壬����
            if (targetOverride.ValueRO.targetEntity != Entity.Null)
                continue;

            // Ŀ��ʵ�岻���ڣ�����
            if (!SystemAPI.Exists(targetEntity))
                continue;

            // Ŀ��ʵ��û�� LocalTransform ���������
            if (!SystemAPI.HasComponent<LocalTransform>(targetEntity))
                continue;

            // ��ȡĿ��λ��
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetEntity);

            float targetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);

            if (targetDistance > loseTarget.ValueRO.loseTargetDistance)
            {
                // Ŀ�����̫Զ������Ŀ��ʵ��Ϊ��
                target.ValueRW.targetEntity = Entity.Null;
            }
        }
    }
}
