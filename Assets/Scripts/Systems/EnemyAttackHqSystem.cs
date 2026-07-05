using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
partial struct EnemyAttackHqSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BuildingHQ>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity hqentity = SystemAPI.GetSingletonEntity<BuildingHQ>();
        float3 hqPosition = SystemAPI.GetComponent<LocalTransform>(hqentity).Position;

        foreach ((RefRO<EnemyAttackHQ> enemyAttackHQ,
            RefRW<TargetPositionPathQueued> targetPositionPathQueued,
            EnabledRefRW < TargetPositionPathQueued > targetPositionPathQueuedEnable,
            RefRW<Target> target)
        in SystemAPI.Query<RefRO<EnemyAttackHQ>,
            RefRW<TargetPositionPathQueued>,
            EnabledRefRW<TargetPositionPathQueued>,
            RefRW<Target>>().WithDisabled<MoveOverride>().WithPresent<TargetPositionPathQueued>())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                target.ValueRW.targetEntity = hqentity;
                targetPositionPathQueued.ValueRW.targetPosition = hqPosition;
                targetPositionPathQueuedEnable.ValueRW = true;
            }
        }
    }

}
