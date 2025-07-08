#define GRID_DEBUG

using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

public partial struct GridSystem : ISystem
{

    public const int WALL_COST = byte.MaxValue;
    public struct GridSystemData : IComponentData
    {
        public int width;
        public int height;
        public float gridNodeSize;
        public GridMap gridMap;
    }


    public struct GridMap
    {
        public NativeArray<Entity> gridEntityArray;
    }
    public struct GridNode : IComponentData
    {
        public int index;
        public int x;
        public int y;
        public byte cost;
        public byte bestCost;
        public float2 vector;
    }

#if GRID_DEBUG
    [BurstCompile]
#endif
    public void OnCreate(ref SystemState state)
    {
        int width = 20;
        int height = 20;
        float griNodeSize = 5f;
        int totalCount = width * height;
        Entity gridNodeEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridNode>(gridNodeEntityPrefab);

        GridMap gridMap = new GridMap();
        gridMap.gridEntityArray = new NativeArray<Entity>(totalCount, Allocator.Persistent);

        state.EntityManager.Instantiate(gridNodeEntityPrefab, gridMap.gridEntityArray);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = CalculateIndex(x, y, width);
                GridNode gridNode = new GridNode
                {
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

        state.EntityManager.AddComponent<GridSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle,
        new GridSystemData
        {
            width = width,
            height = height,
            gridNodeSize = griNodeSize,
            gridMap = gridMap
        });
    }

#if !GRID_DEBUG
    [BurstCompile]
#endif
    public void OnUpdate(ref SystemState state)
    {

        GridSystemData gridSystemData = SystemAPI.GetComponent<GridSystemData>(state.SystemHandle);

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

            flowFieldFollow.ValueRW.targetPosition = flowFieldPathRequest.ValueRO.targetPosition;
            flowFieldFollowEnableRw.ValueRW = true;

        NativeArray<RefRW<GridNode>> gridNodeNativeArray =
            new NativeArray<RefRW<GridNode>>(gridSystemData.width * gridSystemData.height, Allocator.Temp);


            for (int x = 0; x < gridSystemData.width; x++)
            {
                for (int y = 0; y < gridSystemData.height; y++)
                {
                    int index = CalculateIndex(x, y, gridSystemData.width);
                    Entity gridNodeEntity = gridSystemData.gridMap.gridEntityArray[index];

                    RefRW<GridNode> gridNode = SystemAPI.GetComponentRW<GridNode>(gridNodeEntity);

                    gridNodeNativeArray[index] = gridNode;

                    gridNode.ValueRW.vector = new float2(0, 1);

                    if (x == targetGridPosition.x && y == targetGridPosition.y)
                    {
                        gridNode.ValueRW.cost = 0;
                        gridNode.ValueRW.bestCost = 0;
                    }
                    else
                    {
                        gridNode.ValueRW.cost = 1;
                        gridNode.ValueRW.bestCost = byte.MaxValue;
                    }
                }
            }

            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            NativeList<DistanceHit> distanceHitsList = new NativeList<DistanceHit>(Allocator.Temp);
            for (int x = 0; x < gridSystemData.width; x++)
            {
                for (int y = 0; y < gridSystemData.height; y++)
                {
                    if (collisionWorld.OverlapSphere(
                        GetWorldCenterPosition(x, y, gridSystemData.gridNodeSize),
                         gridSystemData.gridNodeSize * .5f,
                         ref distanceHitsList,
                         new CollisionFilter
                         {
                             BelongsTo = ~0u,
                             CollidesWith = 1u << GameAssets.PATHFINDGIN_WALLS,
                             GroupIndex = 0,
                         })) {
                        int index = CalculateIndex(x, y, gridSystemData.width);
                        gridNodeNativeArray[index].ValueRW.cost = WALL_COST;
                    }
                }
            }
            distanceHitsList.Dispose();


            NativeQueue<RefRW<GridNode>> gridNodeOpenQueue = new NativeQueue<RefRW<GridNode>>(Allocator.Temp);

            RefRW<GridNode> targetgridNode = gridNodeNativeArray[CalculateIndex(targetGridPosition, gridSystemData.width)];

            gridNodeOpenQueue.Enqueue(targetgridNode);


            int safety = 1000;
            while (gridNodeOpenQueue.Count > 0)
            {
                safety--;
                if (safety < 0)
                {
                    Debug.LogError("break");
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
                    byte newBestCost = (byte)(currenGridNode.ValueRO.bestCost + neighbourGridNode.ValueRO.cost);

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
        }

        if (Input.GetMouseButtonDown(0))
        {
            float3 mouseWorldPosition = MouseWorldPostion.Instance.GetPostion();

            int2 mouseGridPosition = GetGridPosition(mouseWorldPosition, gridSystemData.gridNodeSize);
            if (IsValidGridPosition(mouseGridPosition, gridSystemData.width, gridSystemData.height))
            {
                int index = CalculateIndex(mouseGridPosition.x, mouseGridPosition.y, gridSystemData.width);
                Entity gridNodeEntity = gridSystemData.gridMap.gridEntityArray[index];
                RefRW<GridNode> gridNode = SystemAPI.GetComponentRW<GridNode>(gridNodeEntity);
            }

        }
#if GRID_DEBUG
        GridSystemDebug.Instance?.InitializeGrid(gridSystemData);
        GridSystemDebug.Instance?.UpdateGrid(gridSystemData);
#endif

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<GridSystemData> gridSystemData = SystemAPI.GetComponentRW<GridSystemData>(state.SystemHandle);
        gridSystemData.ValueRW.gridMap.gridEntityArray.Dispose();
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
}
