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
    public void OnCreate( ref SystemState state)
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
        //NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        localTransformComponentLookup.Update(ref state);
        factionComponentLookup.Update(ref state);
        entityStorageInfoLookup.Update(ref state);
        FindTargetJob findTargetJob = new FindTargetJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            collisionWorld = collisionWorld,
            entityStorageInfoLookup = entityStorageInfoLookup,
            factionComponentLookup  = factionComponentLookup,
            localTransformcomponentLookup = localTransformComponentLookup 
        };

        findTargetJob.ScheduleParallel();


        /*
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<FindTarget> findTarget,
            RefRW<Target> target,
            RefRO<TargetOverride> targetOverride)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<FindTarget>,
                RefRW<Target>,
                RefRO<TargetOverride>>())
        {
            findTarget.ValueRW.timer -= SystemAPI.Time.DeltaTime;

            if (findTarget.ValueRO.timer > 0f)
            {
                continue;
            }

            findTarget.ValueRW.timer = findTarget.ValueRO.timeMax;

            // ����ʹ�� TargetOverride
            if (targetOverride.ValueRO.targetEntity != Entity.Null)
            {
                target.ValueRW.targetEntity = targetOverride.ValueRO.targetEntity;
                continue;
            }

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

            // ����ǰĿ�����
            if (target.ValueRO.targetEntity != Entity.Null &&
                SystemAPI.HasComponent<LocalTransform>(target.ValueRO.targetEntity))
            {
                closeTargetEntity = target.ValueRO.targetEntity;
                var targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
                closeTargetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                currentTargetDistanceOffset = 2f;
            }

            // ��������Ŀ��
            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, findTarget.ValueRO.range, ref distanceHitList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (!SystemAPI.Exists(distanceHit.Entity) || !SystemAPI.HasComponent<Faction>(distanceHit.Entity))
                    {
                        continue;
                    }

                    Faction targetFaction = SystemAPI.GetComponent<Faction>(distanceHit.Entity);

                    if (targetFaction.factionType == findTarget.ValueRO.targetFaction)
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
                        break; // �ҵ�һ�����˳�
                    }
                }
            }

            if (closeTargetEntity != Entity.Null)
            {
                target.ValueRW.targetEntity = closeTargetEntity;
            }
        }*/
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

            // ����ʹ�� TargetOverride
            if (targetOverride.targetEntity != Entity.Null)
            {
                target.targetEntity = targetOverride.targetEntity;
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

            // ����ǰĿ�����
            if (target.targetEntity!=Entity.Null)
            {
                closeTargetEntity = target.targetEntity;
                LocalTransform targetLocalTransform = localTransformcomponentLookup[target.targetEntity]    ;
                closeTargetDistance = math.distance(localTransform.Position, targetLocalTransform.Position);
                currentTargetDistanceOffset = 2f;
            }

            // ��������Ŀ��
            if (collisionWorld.OverlapSphere(localTransform.Position, findTarget.range, ref distanceHitList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (!entityStorageInfoLookup.Exists(distanceHit.Entity) || !factionComponentLookup.HasComponent(distanceHit.Entity))
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
                        break; // �ҵ�һ�����˳�
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
