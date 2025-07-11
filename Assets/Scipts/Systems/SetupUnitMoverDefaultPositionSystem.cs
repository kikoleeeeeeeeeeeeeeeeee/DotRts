using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct SetupUnitMoverDefaultPositionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach((RefRO<LocalTransform> localtransofm,
            RefRW<UnityMover> unityMover,
            RefRO<SetupUnitMoverDefaultPosition> setupUnitMoverDefaultPosition,
            Entity entity) 
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<UnityMover>,
                RefRO<SetupUnitMoverDefaultPosition>>().WithEntityAccess()){
            
            unityMover.ValueRW.targetPosition = localtransofm.ValueRO.Position;
            entityCommandBuffer.RemoveComponent<SetupUnitMoverDefaultPosition>(entity);


        }
    }

}
