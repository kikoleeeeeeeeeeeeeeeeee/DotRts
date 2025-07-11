using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

partial struct VisualUnderFogOrWarSystems : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        EntityCommandBuffer entityCommandBuffer  =
         SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRW<VisualUnderFogOfWar> visualUnderFogOfWar,
            Entity entity)
        in SystemAPI.Query<RefRW<VisualUnderFogOfWar>>().WithEntityAccess())
        {

            LocalTransform parentlocalTransform = SystemAPI.GetComponent<LocalTransform>(visualUnderFogOfWar.ValueRO.parentEntity);
           
            if(!collisionWorld.SphereCast(
                parentlocalTransform.Position,
                visualUnderFogOfWar.ValueRO.sphereCastSize,
                new float3(0, 1, 0),
                100,
                new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.FOR_OF_WAR,
                    GroupIndex = 0
                }))
            {
                //战争迷雾中 ，不可见
                if (visualUnderFogOfWar.ValueRO.isVisible)
                {
                    visualUnderFogOfWar.ValueRW.isVisible = false;
                    entityCommandBuffer.AddComponent<DisableRendering>(entity);
                }

            }
            else
            {
                //课间

                if (!visualUnderFogOfWar.ValueRO.isVisible)
                {
                    visualUnderFogOfWar.ValueRW.isVisible = true;
                    entityCommandBuffer.RemoveComponent<DisableRendering>(entity);
                }
            }
        }
    }
}
