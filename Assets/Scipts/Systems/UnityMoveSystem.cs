using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
partial struct UnityMoveSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridSystem.GridSystemData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        GridSystem.GridSystemData gridSystemData = SystemAPI.GetSingleton<GridSystem.GridSystemData>();

        PhysicsWorldSingleton physicsWorldSingleton  = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld  = physicsWorldSingleton.CollisionWorld;


        foreach ((
           RefRO<LocalTransform> localTransform,
           RefRW<TargetPositionPathQueued> targetPositionPathQueued,
           EnabledRefRW<TargetPositionPathQueued> targetPositionPathQueuedEnableRW,
           RefRW<FlowFieldPathRequest> flowFieldPathRequest,
           EnabledRefRW<FlowFieldPathRequest> flowFieldPathRequestEnableRW,
           EnabledRefRW < FlowFieldFollow > flowFieldFollowEnable,
           RefRW <UnityMover> unitMover,
           Entity entity)
           in SystemAPI.Query<RefRO<LocalTransform>,
           RefRW<TargetPositionPathQueued>,
           EnabledRefRW<TargetPositionPathQueued>,
           RefRW<FlowFieldPathRequest>,
           EnabledRefRW<FlowFieldPathRequest>,
           EnabledRefRW<FlowFieldFollow>,
           RefRW <UnityMover>>().WithPresent<FlowFieldPathRequest,FlowFieldFollow>().WithEntityAccess())
        {
            RaycastInput raycastInput = new RaycastInput
            {
                Start = localTransform.ValueRO.Position,
                End = targetPositionPathQueued.ValueRO.targetPosition,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALL,
                    GroupIndex = 0
                }
            };

            if (!collisionWorld.CastRay(raycastInput))
            {

                unitMover.ValueRW.targetPosition = targetPositionPathQueued.ValueRO.targetPosition;
                flowFieldPathRequestEnableRW.ValueRW = false;
                flowFieldFollowEnable.ValueRW = false;
            }
            else
            {
                if (SystemAPI.HasComponent<MoveOverride>(entity))
                {
                    SystemAPI.SetComponentEnabled<MoveOverride>(entity, false);
                }
                if (GridSystem.IsValidWalkableGridPosition(targetPositionPathQueued.ValueRO.targetPosition, gridSystemData))
                {
                    flowFieldPathRequest.ValueRW.targetPosition = targetPositionPathQueued.ValueRO.targetPosition;
                    flowFieldPathRequestEnableRW.ValueRW = true;
                }
                else
                {
                    unitMover.ValueRW.targetPosition =localTransform.ValueRO.Position;
                    flowFieldPathRequestEnableRW.ValueRW = false;
                    flowFieldFollowEnable.ValueRW = false;
                }


            }
            targetPositionPathQueuedEnableRW.ValueRW = false;
        }
        foreach ((
        RefRO<LocalTransform> localTransform,
        RefRW<FlowFieldFollow> flowFieldFollow,
        EnabledRefRW<FlowFieldFollow> flowFieldFollowEnableRW,
        RefRW<UnityMover> unitMover)
        in SystemAPI.Query<RefRO<LocalTransform>,
        RefRW<FlowFieldFollow>,
        EnabledRefRW<FlowFieldFollow>,
        RefRW<UnityMover>>())
        {
            int2 gridPosition = GridSystem.GetGridPosition(localTransform.ValueRO.Position, gridSystemData.gridNodeSize);

            int index = GridSystem.CalculateIndex(gridPosition, gridSystemData.width);

            Entity gridNodeEntity = gridSystemData.gridMapArray[flowFieldFollow.ValueRO.gridIndex].gridEntityArray[index];



            GridSystem.GridNode gridNode = SystemAPI.GetComponent<GridSystem.GridNode>(gridNodeEntity);

            float3 gridNodeMoveVecotr = GridSystem.GetWorldMovementVector(gridNode.vector);

            if (GridSystem.IsWall(gridNode))
            {
                gridNodeMoveVecotr = flowFieldFollow.ValueRO.lastMoveVector;
            }
            else
            {
                flowFieldFollow.ValueRW.lastMoveVector = gridNodeMoveVecotr;
            }

            unitMover.ValueRW.targetPosition =
                GridSystem.GetWorldCenterPosition(gridPosition.x, gridPosition.y, gridSystemData.gridNodeSize)
                + gridNodeMoveVecotr
                * (gridSystemData.gridNodeSize * 2f);

            if (math.distance(localTransform.ValueRO.Position, flowFieldFollow.ValueRO.targetPosition) < gridSystemData.gridNodeSize)
            {
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                flowFieldFollowEnableRW.ValueRW = false;
            }
            RaycastInput raycastInput = new RaycastInput
            {
                Start = localTransform.ValueRO.Position,
                End = flowFieldFollow.ValueRO.targetPosition,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALL,
                    GroupIndex = 0
                }
            };

            if (!collisionWorld.CastRay(raycastInput))
            {

                unitMover.ValueRW.targetPosition = flowFieldFollow.ValueRO.targetPosition;
                flowFieldFollowEnableRW.ValueRW = false;
            }
        }


        UnitMoverJob unitMoverJob = new UnitMoverJob
        {
            detalTime = SystemAPI.Time.DeltaTime
        };
        unitMoverJob.ScheduleParallel();

    }
}

[BurstCompile]
public partial struct UnitMoverJob : IJobEntity
{
    public float detalTime;
    public void Execute(ref LocalTransform localTransform, ref UnityMover unityMover, ref PhysicsVelocity physicsVelocity)
    {
        float3 moveDirection = unityMover.targetPosition - localTransform.Position;

        float reachedTargetDistanceSq = UnityMoveSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;

        if (math.lengthsq(moveDirection) <= reachedTargetDistanceSq)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            unityMover.isMoving = false;
            return;
        }
        unityMover.isMoving = true;

        moveDirection = math.normalize(moveDirection);


        localTransform.Rotation =
            math.slerp(localTransform.Rotation,
            quaternion.LookRotation(moveDirection, math.up()),
            detalTime * unityMover.rotationSpeed);

        physicsVelocity.Linear = moveDirection * unityMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;

    }
}

