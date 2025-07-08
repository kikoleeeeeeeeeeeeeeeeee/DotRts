using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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

        foreach((RefRO<EnemyAttackHQ> enemyAttackHQ,
            RefRW<UnityMover> unitmover,
            RefRO<Target> target)
        in SystemAPI.Query<RefRO<EnemyAttackHQ>,
            RefRW<UnityMover>,
            RefRO<Target>>().WithDisabled<MoveOverride>())
        {
             if(target.ValueRO.targetEntity != Entity.Null)
            {
                continue;
            }

            unitmover.ValueRW.targetPosition = hqPosition;
        }
    }

}
