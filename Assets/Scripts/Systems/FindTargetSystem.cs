using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

partial struct FindTargetSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformComponentLookup;
    private ComponentLookup<Faction> factionComponentLookup;
    public EntityStorageInfoLookup entityStorageInfoLookup;


    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        localTransformComponentLookup = state.GetComponentLookup<LocalTransform>(true);
        factionComponentLookup = state.GetComponentLookup<Faction>(true);
        entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        localTransformComponentLookup.Update(ref state);
        factionComponentLookup.Update(ref state);
        entityStorageInfoLookup.Update(ref state);
        
        FindTargetJob findTargetJob = new FindTargetJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            collisionWorld = collisionWorld,
            entityStorageInfoLookup = entityStorageInfoLookup,
            factionComponentLookup = factionComponentLookup,
            localTransformcomponentLookup = localTransformComponentLookup
        };

        findTargetJob.ScheduleParallel();
    }


    [BurstCompile]
    public partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> localTransformcomponentLookup;
        [ReadOnly] public ComponentLookup<Faction> factionComponentLookup;
        [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;
        [ReadOnly] public CollisionWorld collisionWorld;
        public float deltaTime;

        public void Execute(in LocalTransform localTransform,
            ref FindTarget findTarget,
            ref Target target,
            in TargetOverride targetOverride)
        {
            findTarget.timer -= deltaTime;

            if (findTarget.timer > 0f)
            {
                return;
            }

            findTarget.timer = findTarget.timeMax;

            if (targetOverride.targetEntity != Entity.Null)
            {
                if (entityStorageInfoLookup.Exists(targetOverride.targetEntity) &&
                    localTransformcomponentLookup.HasComponent(targetOverride.targetEntity))
                {
                    target.targetEntity = targetOverride.targetEntity;
                }
                return;
            }

            NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

            distanceHitList.Clear();
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0,
            };

            Entity closeTargetEntity = Entity.Null;
            float closeTargetDistance = float.MaxValue;
            float currentTargetDistanceOffset = 0f;

            if (target.targetEntity != Entity.Null)
            {
                if (entityStorageInfoLookup.Exists(target.targetEntity) &&
                    localTransformcomponentLookup.HasComponent(target.targetEntity))
                {
                    closeTargetEntity = target.targetEntity;
                    LocalTransform targetLocalTransform = localTransformcomponentLookup[target.targetEntity];
                    closeTargetDistance = math.distance(localTransform.Position, targetLocalTransform.Position);
                    currentTargetDistanceOffset = 2f;
                }
                else
                {
                    target.targetEntity = Entity.Null;
                }
            }

            if (collisionWorld.OverlapSphere(localTransform.Position, findTarget.range, ref distanceHitList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (!entityStorageInfoLookup.Exists(distanceHit.Entity) ||
                        !factionComponentLookup.HasComponent(distanceHit.Entity) ||
                        !localTransformcomponentLookup.HasComponent(distanceHit.Entity))
                    {
                        continue;
                    }

                    Faction targetFaction = factionComponentLookup[distanceHit.Entity];

                    if (targetFaction.factionType == findTarget.targetFaction)
                    {
                        if (closeTargetEntity == Entity.Null)
                        {
                            closeTargetEntity = distanceHit.Entity;
                            closeTargetDistance = distanceHit.Distance;
                        }
                        else
                        {
                            if (distanceHit.Distance + currentTargetDistanceOffset < closeTargetDistance)
                            {
                                closeTargetEntity = distanceHit.Entity;
                                closeTargetDistance = distanceHit.Distance;
                            }
                        }
                        break;
                    }
                }
            }

            if (closeTargetEntity != Entity.Null)
            {
                target.targetEntity = closeTargetEntity;
            }
            distanceHitList.Dispose();
        }
    }
}
