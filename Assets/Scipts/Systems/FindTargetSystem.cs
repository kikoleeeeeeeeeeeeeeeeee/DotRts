using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

partial struct FindTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

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

            // 优先使用 TargetOverride
            if (targetOverride.ValueRO.targetEntity != Entity.Null)
            {
                target.ValueRW.targetEntity = targetOverride.ValueRO.targetEntity;
                continue;
            }

            distanceHitList.Clear();
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u<<GameAssets.BUILDINGS_LAYER ,
                GroupIndex = 0,
            };

            Entity closeTargetEntity = Entity.Null;
            float closeTargetDistance = float.MaxValue;
            float currentTargetDistanceOffset = 0f;

            // 处理当前目标距离
            if (target.ValueRO.targetEntity != Entity.Null &&
                SystemAPI.HasComponent<LocalTransform>(target.ValueRO.targetEntity))
            {
                closeTargetEntity = target.ValueRO.targetEntity;
                var targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
                closeTargetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                currentTargetDistanceOffset = 2f;
            }

            // 搜索附近目标
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
                        break; // 找到一个就退出
                    }
                }
            }

            if (closeTargetEntity != Entity.Null)
            {
                target.ValueRW.targetEntity = closeTargetEntity;
            }
        }
    }
}
