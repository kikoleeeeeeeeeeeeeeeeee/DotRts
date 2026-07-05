using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.UIElements;

partial struct MeleeAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<RaycastHit> raycastHitList = new NativeList<RaycastHit>(Allocator.Temp);

        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var healthLookup = SystemAPI.GetComponentLookup<Health>();

        foreach ((RefRO<LocalTransform> localTransform,
                  RefRW<MeleeAttack> meleeAttack,
                  RefRO<Target> target,
                  RefRW<TargetPositionPathQueued> targetPositionPathQueued,
            EnabledRefRW<TargetPositionPathQueued> targetPositionPathQueuedEnable)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<MeleeAttack>,
                RefRO<Target>,
                RefRW<TargetPositionPathQueued>,
            EnabledRefRW<TargetPositionPathQueued>>().WithDisabled<MoveOverride>().WithPresent<TargetPositionPathQueued>())
        {
            Entity targetEntity = target.ValueRO.targetEntity;
            if (targetEntity == Entity.Null || !localTransformLookup.HasComponent(targetEntity))
            {
                continue;
            }

            LocalTransform targetLocalTransform = localTransformLookup[targetEntity];
            
            // 验证位置是否有效，避免 NaN 导致 RayCast 错误
            if (math.isnan(localTransform.ValueRO.Position.x) || 
                math.isnan(localTransform.ValueRO.Position.y) || 
                math.isnan(localTransform.ValueRO.Position.z) ||
                math.isnan(targetLocalTransform.Position.x) || 
                math.isnan(targetLocalTransform.Position.y) || 
                math.isnan(targetLocalTransform.Position.z))
            {
                continue;
            }

            float meleeAttackDistanceSq = 10f;
            bool isCloseEnoughToAttack = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position) < meleeAttackDistanceSq;

            bool isTouchingTarget = false;

            if (!isCloseEnoughToAttack)
            {
                float3 toTarget = targetLocalTransform.Position - localTransform.ValueRO.Position;
                float distanceToTarget = math.length(toTarget);
                
                if (distanceToTarget < 0.001f || distanceToTarget > 1000f)
                {
                    continue;
                }
                
                float3 dirToTarget = toTarget / distanceToTarget;
                
                // 验证 meleeAttack 数据是否有效
                if (math.isnan(meleeAttack.ValueRO.colliderSize) || 
                    math.isinf(meleeAttack.ValueRO.colliderSize))
                {
                    continue;
                }
                
                float raycastDistance = math.min(meleeAttack.ValueRO.colliderSize + 0.4f, 10f);
                
                float3 rayEnd = localTransform.ValueRO.Position + dirToTarget * raycastDistance;
                
                // 验证终点是否有效
                if (math.isnan(rayEnd.x) || math.isnan(rayEnd.y) || math.isnan(rayEnd.z))
                {
                    continue;
                }

                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u<<GameAssets.UNITS_LAYER|1u<<GameAssets.BUILDINGS_LAYER,
                        GroupIndex = 0,
                    }
                };

                raycastHitList.Clear();
                if (collisionWorld.CastRay(raycastInput, ref raycastHitList))
                {
                    foreach (RaycastHit raycastHit in raycastHitList)
                    {
                        if (raycastHit.Entity == target.ValueRO.targetEntity)
                        {
                            isTouchingTarget = true;
                            break;
                        }
                    }
                }
            }

            if (!isCloseEnoughToAttack && !isTouchingTarget)
            {
                targetPositionPathQueued.ValueRW.targetPosition = targetLocalTransform.Position;
                targetPositionPathQueuedEnable.ValueRW = true;
            }
            else
            {
                targetPositionPathQueued.ValueRW.targetPosition = localTransform.ValueRO.Position;
                targetPositionPathQueuedEnable.ValueRW = true;

                meleeAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (meleeAttack.ValueRW.timer > 0)
                {
                    continue;
                }

                meleeAttack.ValueRW.timer = meleeAttack.ValueRW.timerMax;

                if (healthLookup.HasComponent(targetEntity))
                {
                    RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetEntity);
                    targetHealth.ValueRW.healthAmount -= meleeAttack.ValueRW.damageAmount;
                    targetHealth.ValueRW.onHealthChanged = true;
                    meleeAttack.ValueRW.onAttacked = true;
                }
            }
        }

        raycastHitList.Dispose(); // 
    }
}
