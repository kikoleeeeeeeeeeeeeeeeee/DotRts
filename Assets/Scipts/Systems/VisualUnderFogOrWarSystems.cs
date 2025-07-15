using System.ComponentModel;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using static UnityEngine.EventSystems.EventTrigger;

partial struct VisualUnderFogOrWarSystems : ISystem
{

    private ComponentLookup<LocalTransform> localTransformComponentLookup;



    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameSceneTag>();

        localTransformComponentLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        EntityCommandBuffer entityCommandBuffer  =
         SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


        localTransformComponentLookup.Update(ref state);

        VisualUnderFogOfWarJob visualUnderFogOfWarJob = new VisualUnderFogOfWarJob
        {
            collisionWorld = collisionWorld,
            entityCommandBuffer = entityCommandBuffer.AsParallelWriter(),
            localTransformComponentLookup = localTransformComponentLookup,
            deltaTime =SystemAPI.Time.DeltaTime,
        };
        visualUnderFogOfWarJob.ScheduleParallel();


        /*
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
                //ս�������� �����ɼ�
                if (visualUnderFogOfWar.ValueRO.isVisible)
                {
                    visualUnderFogOfWar.ValueRW.isVisible = false;
                    entityCommandBuffer.AddComponent<DisableRendering>(entity);
                }

            }
            else
            {
                //�μ�

                if (!visualUnderFogOfWar.ValueRO.isVisible)
                {
                    visualUnderFogOfWar.ValueRW.isVisible = true;
                    entityCommandBuffer.RemoveComponent<DisableRendering>(entity);
                }
            }
        }*/
    }


    [BurstCompile]
    public partial struct VisualUnderFogOfWarJob : IJobEntity
    {

        [Unity.Collections.ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
        [Unity.Collections.ReadOnly] public CollisionWorld collisionWorld;

        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;
        public float deltaTime;

        public void Execute(ref VisualUnderFogOfWar visualUnderFogOfWar,[ChunkIndexInQuery]int chunkIndexInQuery,Entity entity)
        {
            visualUnderFogOfWar.timer -= deltaTime;
            if (visualUnderFogOfWar.timer > 0)
            {
                return;
            }

            visualUnderFogOfWar.timer += visualUnderFogOfWar.timerMax;

            LocalTransform parentlocalTransform = localTransformComponentLookup[visualUnderFogOfWar.parentEntity];

            if (!collisionWorld.SphereCast(
                parentlocalTransform.Position,
                visualUnderFogOfWar.sphereCastSize,
                new float3(0, 1, 0),
                100,
                new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.FOR_OF_WAR,
                    GroupIndex = 0
                }))
            {
                //ս�������� �����ɼ�
                if (visualUnderFogOfWar.isVisible)
                {
                    visualUnderFogOfWar.isVisible = false;
                    entityCommandBuffer.AddComponent<DisableRendering>(chunkIndexInQuery, entity);
                }

            }
            else
            {
                //�μ�

                if (!visualUnderFogOfWar.isVisible)
                {
                    visualUnderFogOfWar.isVisible = true;
                    entityCommandBuffer.RemoveComponent<DisableRendering>(chunkIndexInQuery, entity);
                }
            }
        }
    }
}

