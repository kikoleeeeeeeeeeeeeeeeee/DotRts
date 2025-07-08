using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct MoveOverrideSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRO<LocalTransform> localTransform,
            RefRO<MoveOverride> moveOverride,
            EnabledRefRW < MoveOverride > moveOverrideEnable,
            RefRW <UnityMover> unityMover )
        in SystemAPI.Query<
            RefRO<LocalTransform>,
            RefRO<MoveOverride>,
            EnabledRefRW<MoveOverride>,
            RefRW<UnityMover>>()) {

           if( math.distancesq(localTransform.ValueRO.Position, moveOverride.ValueRO.targetPosition)
                > UnityMoveSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
            {
                unityMover.ValueRW.targetPosition = moveOverride.ValueRO.targetPosition;
            }
            else
            {
                moveOverrideEnable.ValueRW = false;
            }
        }
    }

}
