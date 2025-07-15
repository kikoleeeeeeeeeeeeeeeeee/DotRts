#define GRID_DEBUG

using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using static GridSystem;

public partial struct GridSystem : ISystem
{

    public const int WALL_COST = byte.MaxValue;
    public const int HEAVY_COST = 50;

    public const int FLOW_FIELD_MAP_COUNT = 50;
    public struct GridSystemData : IComponentData
    {
        public int width;
        public int height;
        public float gridNodeSize;
        public NativeArray<GridMap> gridMapArray;
        public int nextGridIndex;
        public NativeArray<byte> costMap;
        public NativeArray<Entity> totalGridMapEntityArray;
    }


    public struct GridMap
    {
        public NativeArray<Entity> gridEntityArray;
        public int2 targetGridPosition;
        public bool isVaild;
    }
    public struct GridNode : IComponentData
    {
        public int gridIndex;
        public int index;
        public int x;
        public int y;
        public int cost;
        public int bestCost;
        public float2 vector;
    }
    public ComponentLookup<GridNode> gridNodeComponentLookup;


#if GRID_DEBUG
    [BurstCompile]
#endif
    public void OnCreate(ref SystemState state)
    {
        int width = 40;
        int height = 40;
        float griNodeSize = 5f;
        int totalCount = width * height;
        Entity gridNodeEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridNode>(gridNodeEntityPrefab);

        NativeArray<GridMap> gridMapArray = new NativeArray<GridMap>(FLOW_FIELD_MAP_COUNT, Allocator.Persistent);
        NativeList<Entity> totalGridMapEntityList = new NativeList<Entity>(totalCount*FLOW_FIELD_MAP_COUNT,Allocator.Temp);


        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            GridMap gridMap = new GridMap();
            gridMap.isVaild = false;
            gridMap.gridEntityArray = new NativeArray<Entity>(totalCount, Allocator.Persistent);

            state.EntityManager.Instantiate(gridNodeEntityPrefab, gridMap.gridEntityArray);

            totalGridMapEntityList.AddRange(gridMap.gridEntityArray);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = CalculateIndex(x, y, width);
                    GridNode gridNode = new GridNode
                    {
                        gridIndex = i,
                        index = index,
                        x = x,
                        y = y,
                    };
#if GRID_DEBUG
                    state.EntityManager.SetName(gridMap.gridEntityArray[index], "GridNode_" + x + "_" + y);
#endif
                    SystemAPI.SetComponent(gridMap.gridEntityArray[index], gridNode);
                }
            }
            gridMapArray[i] = gridMap;
        }



        state.EntityManager.AddComponent<GridSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle,
        new GridSystemData
        {
            width = width,
            height = height,
            gridNodeSize = griNodeSize,
            gridMapArray = gridMapArray,
            costMap = new NativeArray<byte>(totalCount, Allocator.Persistent),
            totalGridMapEntityArray = totalGridMapEntityList.ToArray(Allocator.Persistent)
        });
        totalGridMapEntityList.Dispose();
        gridNodeComponentLookup = SystemAPI.GetComponentLookup<GridNode>(false);
    }

#if !GRID_DEBUG
    [BurstCompile]
#endif
    public void OnUpdate(ref SystemState state)
    {

        GridSystemData gridSystemData = SystemAPI.GetComponent<GridSystemData>(state.SystemHandle);

        gridNodeComponentLookup.Update(ref state);

        foreach ((RefRW<FlowFieldPathRequest> flowFieldPathRequest,
            EnabledRefRW<FlowFieldPathRequest> flowFieldPathRequestEnableRw,
            RefRW<FlowFieldFollow> flowFieldFollow,
            EnabledRefRW<FlowFieldFollow> flowFieldFollowEnableRw)
            in SystemAPI.Query<RefRW<FlowFieldPathRequest>,
            EnabledRefRW<FlowFieldPathRequest>,
            RefRW<FlowFieldFollow>,
            EnabledRefRW<FlowFieldFollow>>().WithPresent<FlowFieldFollow>())
        {
            int2 targetGridPosition = GetGridPosition(flowFieldPathRequest.ValueRO.targetPosition, gridSystemData.gridNodeSize);

            flowFieldPathRequestEnableRw.ValueRW = false;

            bool alreadyCalculatePath = false;

            for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
            {
                if (gridSystemData.gridMapArray[i].isVaild && gridSystemData.gridMapArray[i].targetGridPosition.Equals(targetGridPosition))
                {
                    flowFieldFollow.ValueRW.gridIndex = i;
                    flowFieldFollow.ValueRW.targetPosition = flowFieldPathRequest.ValueRO.targetPosition;
                    flowFieldFollowEnableRw.ValueRW = true;

                    alreadyCalculatePath = true;
                    break;
                }
            }

            if (alreadyCalculatePath)
            {
                continue;
            }

            int gridIndex = gridSystemData.nextGridIndex;
            gridSystemData.nextGridIndex = (gridSystemData.nextGridIndex + 1) % FLOW_FIELD_MAP_COUNT;


           // Debug.Log("¼ÆËãÂ·¾¶" + targetGridPosition + "  " + gridIndex);
            flowFieldFollow.ValueRW.gridIndex = gridIndex;
            flowFieldFollow.ValueRW.targetPosition = flowFieldPathRequest.ValueRO.targetPosition;
            flowFieldFollowEnableRw.ValueRW = true;

            NativeArray<RefRW<GridNode>> gridNodeNativeArray =
            new NativeArray<RefRW<GridNode>>(gridSystemData.width * gridSystemData.height, Allocator.Temp);

            InitializeGridJob initializeGridJob = new InitializeGridJob
            {
                gridIndex = gridIndex,
                targetGridPosition = targetGridPosition,
            };
            JobHandle initializeGridJobHandle = initializeGridJob.ScheduleParallel(state.Dependency);
            initializeGridJobHandle.Complete();

            for (int x = 0; x < gridSystemData.width; x++)
            {
                for (int y = 0; y < gridSystemData.height; y++)
                {
                    int index = CalculateIndex(x, y, gridSystemData.width);
                    Entity gridNodeEntity = gridSystemData.gridMapArray[gridIndex].gridEntityArray[index];

                    RefRW<GridNode> gridNode = SystemAPI.GetComponentRW<GridNode>(gridNodeEntity);

                    gridNodeNativeArray[index] = gridNode;

                }
            }

            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            UpdateCostMapJob updateCostMapJob = new UpdateCostMapJob
            {
                width = gridSystemData.width,
                collisionWorld = collisionWorld,
                costMap = gridSystemData.costMap,
                gridMap = gridSystemData.gridMapArray[gridIndex],
                gridNodeSize = gridSystemData.gridNodeSize,
                gridNodeSizeHalf = gridSystemData.gridNodeSize*.5f,
                gridNodeComponentLookup = gridNodeComponentLookup,
                collisionFilterWall = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALL,
                    GroupIndex = 0,
                },
                collisionFilterHeavy =new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_HEAVY,
                    GroupIndex = 0,
                }
            };

            JobHandle updateCostMapJobHandle = updateCostMapJob.ScheduleParallel(gridSystemData.width*gridSystemData.height,50,state.Dependency);
            updateCostMapJobHandle.Complete();
           

            NativeQueue<RefRW<GridNode>> gridNodeOpenQueue = new NativeQueue<RefRW<GridNode>>(Allocator.Temp);

            RefRW<GridNode> targetgridNode = gridNodeNativeArray[CalculateIndex(targetGridPosition, gridSystemData.width)];

            gridNodeOpenQueue.Enqueue(targetgridNode);


            int safety = 1000;
            while (gridNodeOpenQueue.Count > 0)
            {
                safety--;
                if (safety < 0)
                {
                   // Debug.LogError("break");
                    break;
                }

                RefRW<GridNode> currenGridNode = gridNodeOpenQueue.Dequeue();

                NativeList<RefRW<GridNode>> neighbourGridNodeList =
                    GetNeighbourGridNodeList(currenGridNode, gridNodeNativeArray, gridSystemData.width, gridSystemData.height);

                foreach (RefRW<GridNode> neighbourGridNode in neighbourGridNodeList)
                {
                    if (neighbourGridNode.ValueRO.cost == WALL_COST)
                    {
                        continue;
                    }
                    int newBestCost = (currenGridNode.ValueRO.bestCost + neighbourGridNode.ValueRO.cost);

                    if (newBestCost < neighbourGridNode.ValueRO.bestCost)
                    {
                        neighbourGridNode.ValueRW.bestCost = newBestCost;

                        neighbourGridNode.ValueRW.vector = CalcultateVector(
                            neighbourGridNode.ValueRO.x, neighbourGridNode.ValueRO.y,
                            currenGridNode.ValueRO.x, currenGridNode.ValueRO.y
                        );
                        gridNodeOpenQueue.Enqueue(neighbourGridNode);
                    }
                }
                neighbourGridNodeList.Dispose();
            }
            gridNodeOpenQueue.Dispose();
            gridNodeNativeArray.Dispose();

            GridMap gridMap = gridSystemData.gridMapArray[gridIndex];
            gridMap.targetGridPosition = targetGridPosition;
            gridMap.isVaild = true;
            gridSystemData.gridMapArray[gridIndex] = gridMap;
            SystemAPI.SetComponent(state.SystemHandle, gridSystemData);
        }

        /*if (Input.GetMouseButtonDown(0))
        {
            float3 mouseWorldPosition = MouseWorldPostion.Instance.GetPostion();

            int2 mouseGridPosition = GetGridPosition(mouseWorldPosition, gridSystemData.gridNodeSize);
            if (IsValidGridPosition(mouseGridPosition, gridSystemData.width, gridSystemData.height))
            {
                int index = CalculateIndex(mouseGridPosition.x, mouseGridPosition.y, gridSystemData.width);
                Entity gridNodeEntity = gridSystemData.gridMap.gridEntityArray[index];
                RefRW<GridNode> gridNode = SystemAPI.GetComponentRW<GridNode>(gridNodeEntity);
            }

        }*/
#if GRID_DEBUG
        GridSystemDebug.Instance?.InitializeGrid(gridSystemData);
        GridSystemDebug.Instance?.UpdateGrid(gridSystemData);
#endif

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<GridSystemData> gridSystemData = SystemAPI.GetComponentRW<GridSystemData>(state.SystemHandle);
        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            gridSystemData.ValueRW.gridMapArray[i].gridEntityArray.Dispose();

        }
        gridSystemData.ValueRW.gridMapArray.Dispose();
        gridSystemData.ValueRW.costMap.Dispose();
        gridSystemData.ValueRW.totalGridMapEntityArray.Dispose();

    }

    public static NativeList<RefRW<GridNode>> GetNeighbourGridNodeList(RefRW<GridNode> currentGridNode,
        NativeArray<RefRW<GridNode>> gridNodeNativeArray,
        int width, int height)
    {
        NativeList<RefRW<GridNode>> neighbourGridNodeList = new NativeList<RefRW<GridNode>>(Allocator.Temp);

        int gridNodeX = currentGridNode.ValueRO.x;
        int gridNodeY = currentGridNode.ValueRO.y;

        int2 posititonLeft = new int2(gridNodeX - 1, gridNodeY + 0);
        int2 posititonRight = new int2(gridNodeX + 1, gridNodeY + 0);
        int2 posititonUp = new int2(gridNodeX + 0, gridNodeY + 1);
        int2 posititonDown = new int2(gridNodeX + 0, gridNodeY - 1);

        int2 posititonLowerLeft = new int2(gridNodeX - 1, gridNodeY - 1);
        int2 posititonLowerRight = new int2(gridNodeX + 1, gridNodeY - 1);
        int2 posititonUpperLeft = new int2(gridNodeX - 1, gridNodeY + 1);
        int2 posititonUpperRight = new int2(gridNodeX + 1, gridNodeY + 1);

        if (IsValidGridPosition(posititonLeft, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonLeft, width)]);
        }

        if (IsValidGridPosition(posititonRight, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonRight, width)]);
        }

        if (IsValidGridPosition(posititonUp, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonUp, width)]);
        }

        if (IsValidGridPosition(posititonDown, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonDown, width)]);

        }

        if (IsValidGridPosition(posititonLowerLeft, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonLowerLeft, width)]);
        }

        if (IsValidGridPosition(posititonLowerRight, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonLowerRight, width)]);
        }

        if (IsValidGridPosition(posititonUpperLeft, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonUpperLeft, width)]);
        }

        if (IsValidGridPosition(posititonUpperRight, width, height))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[CalculateIndex(posititonUpperRight, width)]);
        }



        return neighbourGridNodeList;
    }

    public static float2 CalcultateVector(int fromX, int fromY, int toX, int toY)
    {
        return new float2(toX, toY) - new float2(fromX, fromY);
    }

    public static int CalculateIndex(int2 gridPosition, int width)
    {
        return CalculateIndex(gridPosition.x, gridPosition.y, width);
    }

    public static int CalculateIndex(int x, int y, int width)
    {
        return x + y * width;
    }

    public static int2 GetGridPositionFromIndex(int index,int width)
    {
        int y = (int)math.floor(index/width);
        int x = index % width;
        return new int2(x, y);
    }


    public static float3 GetWorldPosition(int x, int y, float gridNodeSize)
    {
        return new float3(x * gridNodeSize, 0f, y * gridNodeSize);
    }
    public static float3 GetWorldCenterPosition(int x, int y, float gridNodeSize)
    {
        return new float3(x * gridNodeSize + gridNodeSize * .5f,
            0f,
            y * gridNodeSize + gridNodeSize * .5f);
    }


    public static int2 GetGridPosition(float3 worldPosition, float gridNodeSize)
    {
        return new int2(
            (int)math.floor(worldPosition.x / gridNodeSize),
           (int)math.floor(worldPosition.z / gridNodeSize)
            );
    }

    public static bool IsValidGridPosition(int2 gridPosition, int width, int height)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < width &&
            gridPosition.y < height;
    }

    public static float3 GetWorldMovementVector(float2 vector)
    {
        return new float3(vector.x, 0, vector.y);
    }

    public static bool IsWall(GridNode gridnode)
    {
        return gridnode.cost == WALL_COST;
    }
    public static bool IsWall(int2 gridPosition, GridSystemData gridSystemData)
    {
        return gridSystemData.costMap[CalculateIndex(gridPosition, gridSystemData.width)] == WALL_COST;
    }

    public static bool IsWall(int2 gridPosition, int width,NativeArray<byte> costMap)
    {
        return costMap[CalculateIndex(gridPosition, width)] == WALL_COST;
    }
    public static bool IsValidWalkableGridPosition(float3 worldPostion, GridSystemData gridSystemData)
    {
        int2 gridPosition = GetGridPosition(worldPostion, gridSystemData.gridNodeSize);
        return IsValidGridPosition(gridPosition, gridSystemData.width, gridSystemData.height) &&
            !IsWall(gridPosition, gridSystemData);
    }
    public static bool IsValidWalkableGridPosition(float3 worldPostion, int width,int height ,NativeArray<byte> costMap,float gridNodeSize)
    {
        int2 gridPosition = GetGridPosition(worldPostion, gridNodeSize);
        return IsValidGridPosition(gridPosition, width, height) &&!IsWall(gridPosition,width,costMap);
    }
}


[BurstCompile]
public partial struct InitializeGridJob : IJobEntity
{
    [ReadOnly] public int gridIndex;
    [ReadOnly] public int2 targetGridPosition;

    public void Execute(ref GridNode gridNode)
    {
        if (gridNode.gridIndex != gridIndex)
        {
            return;
        }

        gridNode.vector = new float2(0, 1);

        if (gridNode.x == targetGridPosition.x && gridNode.y == targetGridPosition.y)
        {
            gridNode.cost = 0;
            gridNode.bestCost = 0;
        }
        else
        {
            gridNode.cost = 1;
            gridNode.bestCost = int.MaxValue;
        }
    }
}


[BurstCompile]
public partial struct UpdateCostMapJob : IJobFor
{
    [NativeDisableParallelForRestriction]public ComponentLookup<GridNode> gridNodeComponentLookup;
    [NativeDisableParallelForRestriction] public NativeArray<byte> costMap;


    [ReadOnly]public GridMap gridMap;
    [ReadOnly]public CollisionWorld collisionWorld;
    [ReadOnly]public int width;
    [ReadOnly] public float gridNodeSize;
    [ReadOnly] public float gridNodeSizeHalf;
    [ReadOnly] public CollisionFilter collisionFilterWall;
    [ReadOnly] public CollisionFilter collisionFilterHeavy;



    public void Execute(int index)
    {
        NativeList<DistanceHit> distanceHitsList = new NativeList<DistanceHit>(Allocator.TempJob);
        int2 gridPosition = GetGridPositionFromIndex(index, width);
        if (collisionWorld.OverlapSphere(
            GetWorldCenterPosition(gridPosition.x,gridPosition.y, gridNodeSize),
             gridNodeSizeHalf,
             ref distanceHitsList,
             collisionFilterWall))
             
        {
            GridNode gridNode = gridNodeComponentLookup[gridMap.gridEntityArray[index]];
            gridNode.cost = WALL_COST;
            gridNodeComponentLookup[gridMap.gridEntityArray[index]] = gridNode;
            costMap[index] = WALL_COST;
        }
        if (collisionWorld.OverlapSphere(
            GetWorldCenterPosition(gridPosition.x,gridPosition.y, gridNodeSize),
             gridNodeSizeHalf,
             ref distanceHitsList,
             collisionFilterHeavy))
        {
            GridNode gridNode = gridNodeComponentLookup[gridMap.gridEntityArray[index]];
            gridNode.cost = HEAVY_COST;
            gridNodeComponentLookup[gridMap.gridEntityArray[index]] = gridNode;
            costMap[index] = HEAVY_COST;

        }
        distanceHitsList.Dispose();
    }
}
