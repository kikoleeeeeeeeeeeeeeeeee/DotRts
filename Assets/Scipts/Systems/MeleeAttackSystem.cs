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
                continue; // 目标为空或目标没有 LocalTransform，跳过
            }

            LocalTransform targetLocalTransform = localTransformLookup[targetEntity];

            float meleeAttackDistanceSq = 10f;
            bool isCloseEnoughToAttack = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position) < meleeAttackDistanceSq;

            bool isTouchingTarget = false;

            if (!isCloseEnoughToAttack)
            {
                float3 dirToTarget = math.normalize(targetLocalTransform.Position - localTransform.ValueRO.Position);
                float distanceExtraToTestRaycast = .4f;

                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = localTransform.ValueRO.Position + dirToTarget * (meleeAttack.ValueRO.colliderSize + distanceExtraToTestRaycast),
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
