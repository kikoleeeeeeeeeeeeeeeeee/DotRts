using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using static GridSystem;
using Unity.Collections;
using Unity.Jobs;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(WaveSystem))]
[UpdateAfter(typeof(ZombieSpawnerSystem))]
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

        TargetPositionPathQueuedjob targetPositionPathQueuedjob = new TargetPositionPathQueuedjob
        {
            gridNodeSize = gridSystemData.gridNodeSize,
            width = gridSystemData.width,
            height = gridSystemData.height,  
            costMap = gridSystemData.costMap,
            mapWidth = gridSystemData.mapWidth,
            mapHeight = gridSystemData.mapHeight,
            flowFieldFollowLookup = flowFieldFollowLookup,
            flowFieldPathRequestLookup = flowFieldPathRequestLookup,
            moveOverrideLookup = moveOverrideLookup,
            targetPositionPathQueuedLookup = targetPositionPathQueuedLookup,
        };
        var pathHandle1 = targetPositionPathQueuedjob.ScheduleParallel(state.Dependency);

    
        TestCanMoveStraightJob testCanMoveStraightJob = new TestCanMoveStraightJob
        {
            width = gridSystemData.width,
            height = gridSystemData.height,
            costMap = gridSystemData.costMap,
            gridNodeSize = gridSystemData.gridNodeSize,
            mapWidth = gridSystemData.mapWidth,
            mapHeight = gridSystemData.mapHeight,
            flowFieldFollowLookup = flowFieldFollowLookup,
        };
        
        var pathHandle2 = testCanMoveStraightJob.ScheduleParallel(pathHandle1);

        var combinedPathHandle = JobHandle.CombineDependencies(pathHandle1, pathHandle2);

        FlowFieldFollowerJob flowFieldFollowerJob = new FlowFieldFollowerJob
        {
            width = gridSystemData.width, 
            height = gridSystemData.height, 
            gridNodeSize = gridSystemData.gridNodeSize,
            gridNodeSizeDouble = gridSystemData.gridNodeSize * 2f,
            mapWidth = gridSystemData.mapWidth,
            mapHeight = gridSystemData.mapHeight,
            flowFieldFollowLookup = flowFieldFollowLookup,
            totalGridMapEntityArray=  gridSystemData.totalGridMapEntityArray,
            gridNodeLookup = gridNodeLookup, 
        };
        var flowHandle = flowFieldFollowerJob.ScheduleParallel(combinedPathHandle);

        UnitMoverJob unitMoverJob = new UnitMoverJob
        { 
            detalTime = SystemAPI.Time.DeltaTime
        };
        state.Dependency = unitMoverJob.ScheduleParallel(JobHandle.CombineDependencies(combinedPathHandle, flowHandle));

    }
}

[BurstCompile]
public partial struct UnitMoverJob : IJobEntity
{
    public float detalTime;
    public void Execute(ref LocalTransform localTransform, ref UnityMover unityMover, ref PhysicsVelocity physicsVelocity)
    {
        if (!math.isfinite(localTransform.Position.x) || !math.isfinite(localTransform.Position.y) || !math.isfinite(localTransform.Position.z))
        {
            localTransform.Position = float3.zero;
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }
        
        if (!math.isfinite(unityMover.targetPosition.x) || !math.isfinite(unityMover.targetPosition.y) || !math.isfinite(unityMover.targetPosition.z))
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }
        
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
            quaternion.LookRotationSafe(moveDirection, math.up()),
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

    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public NativeArray<byte> costMap;
    [ReadOnly] public float gridNodeSize;
    [ReadOnly] public float mapWidth;
    [ReadOnly] public float mapHeight;
    
    public void Execute(
        in LocalTransform localTransform,
           ref UnityMover unitMover,
           Entity entity
        )
    {
        TargetPositionPathQueued targetPositionPathQueued = targetPositionPathQueuedLookup[entity];
        float3 targetPosition = targetPositionPathQueued.targetPosition;
        
        if (math.distance(localTransform.Position, targetPosition) > 1000f || targetPosition.Equals(float3.zero))
        {
            return;
        }

        if (localTransform.Position.Equals(float3.zero) || math.isnan(localTransform.Position.x) || math.isnan(localTransform.Position.y) || math.isnan(localTransform.Position.z))
        {
            return;
        }

        if (GridSystem.HasLineOfSight(localTransform.Position, targetPosition, width, height, costMap, gridNodeSize, mapWidth, mapHeight))
        {
            unitMover.targetPosition = targetPosition;
            flowFieldPathRequestLookup.SetComponentEnabled(entity, false);
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
        }
        else
        {
            if (moveOverrideLookup.HasComponent(entity))
            {
                moveOverrideLookup.SetComponentEnabled(entity, false);
            }

            if (GridSystem.IsValidWalkableGridPosition(targetPosition, width, height, costMap, gridNodeSize, mapWidth, mapHeight))
            {
                FlowFieldPathRequest flowFieldPathRequest = flowFieldPathRequestLookup[entity];
                flowFieldPathRequest.targetPosition = targetPosition;
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

    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public NativeArray<byte> costMap;
    [ReadOnly] public float gridNodeSize;
    [ReadOnly] public float mapWidth;
    [ReadOnly] public float mapHeight;

    public void Execute(
        in LocalTransform localTransform,
        ref UnityMover unityMover,
        Entity entity)
    {

        FlowFieldFollow  flowFieldFollow = flowFieldFollowLookup[entity];
        
        if (flowFieldFollow.targetPosition.Equals(float3.zero))
        {
            return;
        }
        
        if (localTransform.Position.Equals(float3.zero) || math.isnan(localTransform.Position.x) || math.isnan(localTransform.Position.y) || math.isnan(localTransform.Position.z))
        {
            return;
        }

        if (GridSystem.HasLineOfSight(localTransform.Position, flowFieldFollow.targetPosition, width, height, costMap, gridNodeSize, mapWidth, mapHeight))
        {
            unityMover.targetPosition = flowFieldFollow.targetPosition;
            flowFieldFollowLookup.SetComponentEnabled(entity, false);
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
    [ReadOnly] public float mapWidth;
    [ReadOnly] public float mapHeight;
    [ReadOnly] public NativeArray<Entity> totalGridMapEntityArray;
    public void Execute(
        in LocalTransform localTransform,
        ref UnityMover unitMover,
        Entity entity)
    {
        // 验证当前位置是否有效
        if (math.isnan(localTransform.Position.x) || math.isnan(localTransform.Position.y) || math.isnan(localTransform.Position.z))
        {
            return;
        }
        
        FlowFieldFollow flowFieldFollow = flowFieldFollowLookup[entity];

        // 验证 gridIndex 是否有效
        if (flowFieldFollow.gridIndex < 0)
        {
            return;
        }

        int2 gridPosition = GridSystem.GetGridPosition(localTransform.Position, gridNodeSize, mapWidth, mapHeight);

        int index = GridSystem.CalculateIndex(gridPosition, width);

        // 验证索引是否在有效范围内
        if (index < 0 || index >= width * height)
        {
            return;
        }

        int totalCount = width * height;
        int arrayIndex = totalCount * flowFieldFollow.gridIndex + index;
        
        // 验证数组索引是否有效
        if (arrayIndex < 0 || arrayIndex >= totalGridMapEntityArray.Length)
        {
            return;
        }
        
        Entity gridNodeEntity = totalGridMapEntityArray[arrayIndex];
        
        // 验证 Entity 是否有效
        if (gridNodeEntity == Entity.Null || !gridNodeLookup.HasComponent(gridNodeEntity))
        {
            return;
        }

        GridSystem.GridNode gridNode = gridNodeLookup[(gridNodeEntity)];

        float3 gridNodeMoveVecotr = GridSystem.GetWorldMovementVector(gridNode.vector);
        
        // 验证移动向量是否有效
        if (math.isnan(gridNodeMoveVecotr.x) || math.isnan(gridNodeMoveVecotr.y) || math.isnan(gridNodeMoveVecotr.z))
        {
            return;
        }

        if (GridSystem.IsWall(gridNode))
        {
            gridNodeMoveVecotr = flowFieldFollow.lastMoveVector;
        }
        else
        {
            flowFieldFollow.lastMoveVector = gridNodeMoveVecotr;
        }

        float3 newTargetPosition =
            GridSystem.GetWorldCenterPosition(gridPosition.x, gridPosition.y, gridNodeSize, mapWidth, mapHeight)
            + gridNodeMoveVecotr
            * gridNodeSizeDouble;
        
        // 验证计算出的目标位置是否有效
        if (!math.isnan(newTargetPosition.x) && !math.isnan(newTargetPosition.y) && !math.isnan(newTargetPosition.z))
        {
            unitMover.targetPosition = newTargetPosition;
        }

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