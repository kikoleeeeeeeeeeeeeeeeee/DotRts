using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using static GridSystem;
using static UnityEngine.EventSystems.EventTrigger;
using Unity.Collections;
using Unity.Jobs;
partial struct UnityMoveSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;

    public ComponentLookup<TargetPositionPathQueued> targetPositionPathQueuedLookup;
    public ComponentLookup<FlowFieldPathRequest> flowFieldPathRequestLookup;
    public ComponentLookup<FlowFieldFollow> flowFieldFollowLookup;
    public ComponentLookup<MoveOverride> moveOverrideLookup;
    public ComponentLookup<GridSystem.GridNode> gridNodeLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridSystem.GridSystemData>();

        targetPositionPathQueuedLookup = SystemAPI.GetComponentLookup<TargetPositionPathQueued>(false);
        flowFieldPathRequestLookup = SystemAPI.GetComponentLookup<FlowFieldPathRequest>(false);
        flowFieldFollowLookup = SystemAPI.GetComponentLookup<FlowFieldFollow>(false);
        moveOverrideLookup = SystemAPI.GetComponentLookup<MoveOverride>(false);
        gridNodeLookup = SystemAPI.GetComponentLookup<GridSystem.GridNode>(false);

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        GridSystem.GridSystemData gridSystemData = SystemAPI.GetSingleton<GridSystem.GridSystemData>();

        targetPositionPathQueuedLookup.Update(ref state);
        flowFieldPathRequestLookup.Update(ref state);
        flowFieldFollowLookup.Update(ref state);
        moveOverrideLookup.Update(ref state);
        gridNodeLookup.Update(ref state);

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        TargetPositionPathQueuedjob targetPositionPathQueuedjob = new TargetPositionPathQueuedjob
        {
            collisionWorld = collisionWorld,
            gridNodeSize = gridSystemData.gridNodeSize,
            width = gridSystemData.width,
            height = gridSystemData.height,  
            costMap = gridSystemData.costMap,
            flowFieldFollowLookup = flowFieldFollowLookup,
            flowFieldPathRequestLookup = flowFieldPathRequestLookup,
            moveOverrideLookup = moveOverrideLookup,
            targetPositionPathQueuedLookup = targetPositionPathQueuedLookup,
        };
        targetPositionPathQueuedjob.ScheduleParallel();

    
        TestCanMoveStraightJob testCanMoveStraightJob = new TestCanMoveStraightJob
        {
            collisionWorld = collisionWorld,
            flowFieldFollowLookup = flowFieldFollowLookup,
        };
        
        testCanMoveStraightJob.ScheduleParallel();



        FlowFieldFollowerJob flowFieldFollowerJob = new FlowFieldFollowerJob
        {
            width = gridSystemData.width, 
            height = gridSystemData.height, 
            gridNodeSize = gridSystemData.gridNodeSize,
            gridNodeSizeDouble = gridSystemData.gridNodeSize * 2f,
            flowFieldFollowLookup = flowFieldFollowLookup,
            totalGridMapEntityArray=  gridSystemData.totalGridMapEntityArray,
            gridNodeLookup = gridNodeLookup, 
        };
        flowFieldFollowerJob.ScheduleParallel();

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
        float distanceSq = math.lengthsq(moveDirection);


        if (distanceSq <= UnityMoveSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
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


[BurstCompile]
[WithAll(typeof(TargetPositionPathQueued))]
public partial struct TargetPositionPathQueuedjob : IJobEntity
{

    [NativeDisableParallelForRestriction] public ComponentLookup<TargetPositionPathQueued> targetPositionPathQueuedLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldPathRequest> flowFieldPathRequestLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollow> flowFieldFollowLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<MoveOverride> moveOverrideLookup;

    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public NativeArray<byte> costMap;
    [ReadOnly] public float gridNodeSize;
    public void Execute(
        in LocalTransform localTransform,
           ref UnityMover unitMover,
           Entity entity
        )
    {

        RaycastInput raycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = targetPositionPathQueuedLookup[entity].targetPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.PATHFINDING_WALL,
                GroupIndex = 0
            }
        };

        if (!collisionWorld.CastRay(raycastInput))
        {

            unitMover.targetPosition = targetPositionPathQueuedLookup[entity].targetPosition;
            flowFieldPathRequestLookup.SetComponentEnabled(entity, false);
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
        }
        else
        {

            if (moveOverrideLookup.HasComponent(entity))
            {
                moveOverrideLookup.SetComponentEnabled(entity, false);
            }

            if (GridSystem.IsValidWalkableGridPosition(targetPositionPathQueuedLookup[entity].targetPosition, width, height, costMap, gridNodeSize))
            {

                FlowFieldPathRequest flowFieldPathRequest = flowFieldPathRequestLookup[entity];
                flowFieldPathRequest.targetPosition = targetPositionPathQueuedLookup[entity].targetPosition;
                flowFieldPathRequestLookup[entity] = flowFieldPathRequest;
                flowFieldPathRequestLookup.SetComponentEnabled(entity, true);
            }
            else
            {

                unitMover.targetPosition = localTransform.Position;
                flowFieldPathRequestLookup.SetComponentEnabled(entity, false);
                flowFieldFollowLookup.SetComponentEnabled(entity, false);
            }
        }
    }
}


[BurstCompile]
[WithAll(typeof(FlowFieldFollow))]
public partial struct TestCanMoveStraightJob : IJobEntity
{
    [NativeDisableParallelForRestriction]public ComponentLookup<FlowFieldFollow> flowFieldFollowLookup;


    [ReadOnly]public CollisionWorld collisionWorld;    
    public void Execute(
        in LocalTransform localTransform,
        ref UnityMover unityMover,
        Entity entity)
    {

        FlowFieldFollow  flowFieldFollow = flowFieldFollowLookup[entity];
        RaycastInput raycastInput = new RaycastInput
        {

            Start = localTransform.Position,
            End = flowFieldFollow.targetPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.PATHFINDING_WALL,
                GroupIndex = 0
            }
        };

        if (!collisionWorld.CastRay(raycastInput))
        {

            unityMover.targetPosition = flowFieldFollow.targetPosition;
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
        }
        else
        {
        }
    }
}


[BurstCompile]
[WithAll(typeof(FlowFieldFollow))]
public partial struct FlowFieldFollowerJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollow> flowFieldFollowLookup;
    
    [ReadOnly] public ComponentLookup<GridSystem.GridNode> gridNodeLookup;
    [ReadOnly] public float gridNodeSize;
    [ReadOnly] public float gridNodeSizeDouble;
    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public NativeArray<Entity> totalGridMapEntityArray;
    public void Execute(
        in LocalTransform localTransform,
        ref UnityMover unitMover,
        Entity entity)
    {
        
        FlowFieldFollow flowFieldFollow = flowFieldFollowLookup[entity];

        int2 gridPosition = GridSystem.GetGridPosition(localTransform.Position, gridNodeSize);

        int index = GridSystem.CalculateIndex(gridPosition, width);

        int totalCount = width * height;
        Entity gridNodeEntity = totalGridMapEntityArray[totalCount * flowFieldFollow.gridIndex + index];

        GridSystem.GridNode gridNode = gridNodeLookup[(gridNodeEntity)];

        float3 gridNodeMoveVecotr = GridSystem.GetWorldMovementVector(gridNode.vector);

        if (GridSystem.IsWall(gridNode))
        {
            gridNodeMoveVecotr = flowFieldFollow.lastMoveVector;
        }
        else
        {
            flowFieldFollow.lastMoveVector = gridNodeMoveVecotr;
        }

        unitMover.targetPosition =
            GridSystem.GetWorldCenterPosition(gridPosition.x, gridPosition.y, gridNodeSize)
            + gridNodeMoveVecotr
            * gridNodeSizeDouble;

        if (math.distance(localTransform.Position, flowFieldFollow.targetPosition) < gridNodeSize)
        {
            unitMover.targetPosition = localTransform.Position;
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
        }
        flowFieldFollowLookup[entity] = flowFieldFollow;
    }
}
/*
[BurstCompile]
[WithAll(typeof(FlowFieldFollow))]
public partial struct FlowFieldFollowerJob : IJobEntity
{


    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollow> flowFieldFollowLookup;


    [ReadOnly] public ComponentLookup<GridSystem.GridNode> gridNodeLookup;
    [ReadOnly] public float gridNodeSize;
    [ReadOnly] public float gridNodeSizeDouble;
    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public NativeArray<Entity> totalGridMapEntityArray;


    public void Execute(
        in LocalTransform localTransform,
        ref UnityMover unitMover,
        Entity entity)
    {

        FlowFieldFollow flowFieldFollower = flowFieldFollowLookup[entity];
        int2 gridPosition = GridSystem.GetGridPosition(localTransform.Position, gridNodeSize);
        int index = GridSystem.CalculateIndex(gridPosition, width);
        int totalCount = width * height;
        Entity gridNodeEntity = totalGridMapEntityArray[totalCount * flowFieldFollower.gridIndex + index];
        GridSystem.GridNode gridNode = gridNodeLookup[gridNodeEntity];
        float3 gridNodeMoveVector = GridSystem.GetWorldMovementVector(gridNode.vector);

        if (GridSystem.IsWall(gridNode))
        {
            gridNodeMoveVector = flowFieldFollower.lastMoveVector;
        }
        else
        {
            flowFieldFollower.lastMoveVector = gridNodeMoveVector;
        }

        unitMover.targetPosition =
            GridSystem.GetWorldCenterPosition(gridPosition.x, gridPosition.y, gridNodeSize) +
            gridNodeMoveVector *
            gridNodeSizeDouble;

        if (math.distance(localTransform.Position, flowFieldFollower.targetPosition) < gridNodeSize)
        {
            // Target destination
            unitMover.targetPosition = localTransform.Position;
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
        }

        flowFieldFollowLookup[entity] = flowFieldFollower;
    }
}*/