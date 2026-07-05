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

[UpdateInGroup(typeof(InitializationSystemGroup))]
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
        public float mapWidth;
        public float mapHeight;
        public NativeArray<GridMap> gridMapArray;
        public int nextGridIndex;
        public NativeArray<byte> costMap;
        public NativeArray<Entity> totalGridMapEntityArray;
        public bool needRecalculateCostMap;
        public bool isInitialized;
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
        // 从 ECS MapConfigData 获取网格配置，如果尚未烘焙则使用默认值
        int width = 40;
        int height = 40;
        float gridNodeSize = 5f;
        float mapWidthWorld = 200f;
        float mapHeightWorld = 200f;

        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            mapWidthWorld = mapConfig.mapWidth;
            mapHeightWorld = mapConfig.mapHeight;
            gridNodeSize = mapConfig.gridNodeSize;
            width = Mathf.CeilToInt(mapWidthWorld / gridNodeSize);
            height = Mathf.CeilToInt(mapHeightWorld / gridNodeSize);
        }
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



        float mapWidth = mapWidthWorld;
        float mapHeight = mapHeightWorld;

        state.EntityManager.DestroyEntity(gridNodeEntityPrefab);

        state.EntityManager.AddComponent<GridSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle,
        new GridSystemData
        {
            width = width,
            height = height,
            gridNodeSize = gridNodeSize,
            mapWidth = mapWidth,
            mapHeight = mapHeight,
            gridMapArray = gridMapArray,
            costMap = new NativeArray<byte>(totalCount, Allocator.Persistent),
            totalGridMapEntityArray = totalGridMapEntityList.ToArray(Allocator.Persistent),
            needRecalculateCostMap = true,
            isInitialized = true
        });
        totalGridMapEntityList.Dispose();
        gridNodeComponentLookup = SystemAPI.GetComponentLookup<GridNode>(false);
    }

    private int GetMapWidth(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            return Mathf.CeilToInt(mapConfig.mapWidth / mapConfig.gridNodeSize);
        }
        return 40;
    }

    private int GetMapHeight(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            return Mathf.CeilToInt(mapConfig.mapHeight / mapConfig.gridNodeSize);
        }
        return 40;
    }

    private float GetGridNodeSize(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            return mapConfig.gridNodeSize;
        }
        return 5f;
    }

    private float GetMapWidthWorld(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            return mapConfig.mapWidth;
        }
        return 200f;
    }

    private float GetMapHeightWorld(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            return mapConfig.mapHeight;
        }
        return 200f;
    }

#if !GRID_DEBUG
    [BurstCompile]
#endif
    public void OnUpdate(ref SystemState state)
    {

        GridSystemData gridSystemData = SystemAPI.GetComponent<GridSystemData>(state.SystemHandle);

        gridNodeComponentLookup.Update(ref state);
        
        // 创建命令缓冲区来处理实体销毁
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        // 检查是否有地图生成事件（通过 MapConfigData 标志）
        if (SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            if (!mapConfig.isGenerated && !mapConfig.needRecalculateCostMap)
            {
                // 地图尚未生成，跳过流场计算
            }
            else if (mapConfig.needRecalculateCostMap)
            {
                int actualWidth = GetMapWidth(ref state);
                int actualHeight = GetMapHeight(ref state);
                float actualMapWidth = GetMapWidthWorld(ref state);
                float actualMapHeight = GetMapHeightWorld(ref state);

                if (!gridSystemData.isInitialized
                    || gridSystemData.width != actualWidth
                    || gridSystemData.height != actualHeight
                    || math.abs(gridSystemData.mapWidth - actualMapWidth) > 0.1f
                    || math.abs(gridSystemData.mapHeight - actualMapHeight) > 0.1f)
                {
                    RebuildGridSystem(ref state, ref gridSystemData, ecb);
                }
                else
                {
                    RecalculateCostMap(ref state, ref gridSystemData);
                    gridSystemData.needRecalculateCostMap = false;
                    SystemAPI.SetComponent(state.SystemHandle, gridSystemData);
                }

                // 清除标志
                mapConfig.needRecalculateCostMap = false;
                SystemAPI.SetSingleton(mapConfig);
            }
        }

        foreach ((RefRW<FlowFieldPathRequest> flowFieldPathRequest,
            EnabledRefRW<FlowFieldPathRequest> flowFieldPathRequestEnableRw,
            RefRW<FlowFieldFollow> flowFieldFollow,
            EnabledRefRW<FlowFieldFollow> flowFieldFollowEnableRw)
            in SystemAPI.Query<RefRW<FlowFieldPathRequest>,
            EnabledRefRW<FlowFieldPathRequest>,
            RefRW<FlowFieldFollow>,
            EnabledRefRW<FlowFieldFollow>>().WithPresent<FlowFieldFollow>())
        {
            int2 targetGridPosition = GetGridPosition(flowFieldPathRequest.ValueRO.targetPosition, gridSystemData.gridNodeSize, gridSystemData.mapWidth, gridSystemData.mapHeight);

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

            for (int i = 0; i < gridSystemData.width * gridSystemData.height; i++)
            {
                gridNodeNativeArray[i].ValueRW.cost = gridSystemData.costMap[i];
            }
           

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
        if (gridSystemData.ValueRW.gridMapArray.IsCreated)
        {
            for (int i = 0; i < gridSystemData.ValueRW.gridMapArray.Length; i++)
            {
                if (gridSystemData.ValueRW.gridMapArray[i].gridEntityArray.IsCreated)
                    gridSystemData.ValueRW.gridMapArray[i].gridEntityArray.Dispose();
            }
            gridSystemData.ValueRW.gridMapArray.Dispose();
        }
        if (gridSystemData.ValueRW.costMap.IsCreated)
            gridSystemData.ValueRW.costMap.Dispose();
        if (gridSystemData.ValueRW.totalGridMapEntityArray.IsCreated)
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


    // 带地图大小参数的版本，用于Job中
    public static float3 GetWorldPosition(int x, int y, float gridNodeSize, float mapWidth, float mapHeight)
    {
        // 计算网格的世界坐标，从地图左下角开始
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        
        return new float3(
            -halfWidth + x * gridNodeSize,
            0f,
            -halfHeight + y * gridNodeSize
        );
    }
    
    // 带地图大小参数的版本，用于Job中
    public static float3 GetWorldCenterPosition(int x, int y, float gridNodeSize, float mapWidth, float mapHeight)
    {
        float3 worldPosition = GetWorldPosition(x, y, gridNodeSize, mapWidth, mapHeight);
        return new float3(
            worldPosition.x + gridNodeSize * .5f,
            0f,
            worldPosition.z + gridNodeSize * .5f
        );
    }
    
    // 不带地图大小参数的版本，用于其他地方
    public static float3 GetWorldPosition(int x, int y, float gridNodeSize)
    {
        float mapWidth = 200;
        float mapHeight = 200;
        if (MapManager.Instance != null)
        {
            Bounds bounds = MapManager.Instance.GetMapBounds();
            mapWidth = bounds.size.x;
            mapHeight = bounds.size.z;
        }

        return GetWorldPosition(x, y, gridNodeSize, mapWidth, mapHeight);
    }

    // 不带地图大小参数的版本，用于其他地方
    public static float3 GetWorldCenterPosition(int x, int y, float gridNodeSize)
    {
        float mapWidth = 200;
        float mapHeight = 200;
        if (MapManager.Instance != null)
        {
            Bounds bounds = MapManager.Instance.GetMapBounds();
            mapWidth = bounds.size.x;
            mapHeight = bounds.size.z;
        }

        return GetWorldCenterPosition(x, y, gridNodeSize, mapWidth, mapHeight);
    }

    // 带地图大小参数的版本，用于Job中
    public static int2 GetGridPosition(float3 worldPosition, float gridNodeSize, float mapWidth, float mapHeight)
    {
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        
        int x = (int)math.floor((worldPosition.x + halfWidth) / gridNodeSize);
        int y = (int)math.floor((worldPosition.z + halfHeight) / gridNodeSize);
        
        return new int2(
            math.clamp(x, 0, (int)math.ceil(mapWidth / gridNodeSize) - 1),
            math.clamp(y, 0, (int)math.ceil(mapHeight / gridNodeSize) - 1)
        );
    }
    
    // 不带地图大小参数的版本，用于其他地方
    public static int2 GetGridPosition(float3 worldPosition, float gridNodeSize)
    {
        float mapWidth = 200;
        float mapHeight = 200;
        if (MapManager.Instance != null)
        {
            Bounds bounds = MapManager.Instance.GetMapBounds();
            mapWidth = bounds.size.x;
            mapHeight = bounds.size.z;
        }

        return GetGridPosition(worldPosition, gridNodeSize, mapWidth, mapHeight);
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
        int2 gridPosition = GetGridPosition(worldPostion, gridSystemData.gridNodeSize, gridSystemData.mapWidth, gridSystemData.mapHeight);
        return IsValidGridPosition(gridPosition, gridSystemData.width, gridSystemData.height) &&
            !IsWall(gridPosition, gridSystemData);
    }
    public static bool IsValidWalkableGridPosition(float3 worldPostion, int width,int height ,NativeArray<byte> costMap,float gridNodeSize)
    {
        int2 gridPosition = GetGridPosition(worldPostion, gridNodeSize);
        return IsValidGridPosition(gridPosition, width, height) &&!IsWall(gridPosition,width,costMap);
    }
    public static bool IsValidWalkableGridPosition(float3 worldPostion, int width, int height, NativeArray<byte> costMap, float gridNodeSize, float mapWidth, float mapHeight)
    {
        int2 gridPosition = GetGridPosition(worldPostion, gridNodeSize, mapWidth, mapHeight);
        return IsValidGridPosition(gridPosition, width, height) && !IsWall(gridPosition, width, costMap);
    }
    
    // 重新计算成本地图
    private void RecalculateCostMap(ref SystemState state, ref GridSystemData gridSystemData)
    {
        Debug.Log("Recalculating cost map...");
        
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        
        CalculateCostMapWithPhysics(ref gridSystemData, collisionWorld);
        
        // 将 costMap 值应用到所有有效流场地图的 GridNode 实体
        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            if (gridSystemData.gridMapArray[i].isVaild)
            {
                for (int j = 0; j < gridSystemData.width * gridSystemData.height; j++)
                {
                    Entity gridNodeEntity = gridSystemData.gridMapArray[i].gridEntityArray[j];
                    RefRW<GridNode> gridNode = SystemAPI.GetComponentRW<GridNode>(gridNodeEntity);
                    gridNode.ValueRW.cost = gridSystemData.costMap[j];
                }
            }
        }
        
        // 标记所有流场地图为无效，因为地图已经改变，目标位置可能不同
        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            GridMap gridMap = gridSystemData.gridMapArray[i];
            gridMap.isVaild = false;
            gridSystemData.gridMapArray[i] = gridMap;
        }
        
        Debug.Log("All flowfield maps invalidated. New paths will be calculated on demand.");
    }

    private void CalculateCostMapWithPhysics(ref GridSystemData gridSystemData, CollisionWorld collisionWorld)
    {
        int width = gridSystemData.width;
        int height = gridSystemData.height;
        float gridNodeSize = gridSystemData.gridNodeSize;
        float gridNodeSizeHalf = gridNodeSize * 0.5f;
        float mapWidth = gridSystemData.mapWidth;
        float mapHeight = gridSystemData.mapHeight;

        int layerMaskWall = 1 << GameAssets.PATHFINDING_WALL;
        int layerMaskHeavy = 1 << GameAssets.PATHFINDING_HEAVY;
        UnityEngine.Collider[] hitColliders = new UnityEngine.Collider[2];

        NativeList<DistanceHit> distanceHitsList = new NativeList<DistanceHit>(Allocator.Temp);

        CollisionFilter collisionFilterBuilding = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.BUILDINGS_LAYER,
            GroupIndex = 0,
        };

        for (int index = 0; index < width * height; index++)
        {
            int x = index % width;
            int y = index / width;
            float3 worldCenter = GetWorldCenterPosition(x, y, gridNodeSize, mapWidth, mapHeight);
            Vector3 position = new Vector3(worldCenter.x, worldCenter.y, worldCenter.z);

            if (Physics.OverlapSphereNonAlloc(position, gridNodeSizeHalf, hitColliders, layerMaskWall) > 0)
            {
                gridSystemData.costMap[index] = WALL_COST;
                continue;
            }

            distanceHitsList.Clear();
            if (collisionWorld.OverlapSphere(worldCenter, gridNodeSizeHalf, ref distanceHitsList, collisionFilterBuilding))
            {
                gridSystemData.costMap[index] = WALL_COST;
                continue;
            }

            if (Physics.OverlapSphereNonAlloc(position, gridNodeSizeHalf, hitColliders, layerMaskHeavy) > 0)
            {
                gridSystemData.costMap[index] = HEAVY_COST;
                continue;
            }

            gridSystemData.costMap[index] = 1;
        }

        distanceHitsList.Dispose();
    }

    public static bool HasLineOfSight(float3 start, float3 end,
        int width, int height, NativeArray<byte> costMap,
        float gridNodeSize, float mapWidth, float mapHeight)
    {
        float distance = math.distance(start, end);
        if (distance < gridNodeSize * 0.5f)
            return true;

        int2 startGrid = GetGridPosition(start, gridNodeSize, mapWidth, mapHeight);
        int2 endGrid = GetGridPosition(end, gridNodeSize, mapWidth, mapHeight);

        float3 direction = (end - start) / distance;
        float stepSize = gridNodeSize * 0.5f;
        int steps = (int)(distance / stepSize);

        int2 lastCheckedGrid = new int2(int.MinValue, int.MinValue);

        for (int i = 1; i < steps; i++)
        {
            float3 point = start + direction * (i * stepSize);
            int2 gridPos = GetGridPosition(point, gridNodeSize, mapWidth, mapHeight);

            if (gridPos.x == lastCheckedGrid.x && gridPos.y == lastCheckedGrid.y)
                continue;
            lastCheckedGrid = gridPos;

            if (gridPos.x == startGrid.x && gridPos.y == startGrid.y)
                continue;
            if (gridPos.x == endGrid.x && gridPos.y == endGrid.y)
                continue;

            if (!IsValidGridPosition(gridPos, width, height))
                return false;

            if (costMap[CalculateIndex(gridPos, width)] == WALL_COST)
                return false;
        }

        return true;
    }

    private void RebuildGridSystem(ref SystemState state, ref GridSystemData gridSystemData, EntityCommandBuffer ecb)
    {
        int newWidth = GetMapWidth(ref state);
        int newHeight = GetMapHeight(ref state);
        float newMapWidth = GetMapWidthWorld(ref state);
        float newMapHeight = GetMapHeightWorld(ref state);
        float gridNodeSize = GetGridNodeSize(ref state);
        int totalCount = newWidth * newHeight;

        Debug.Log($"[GridSystem] Rebuilding grid: ({gridSystemData.width}x{gridSystemData.height}) -> ({newWidth}x{newHeight}), map: ({gridSystemData.mapWidth}x{gridSystemData.mapHeight}) -> ({newMapWidth}x{newMapHeight})");

        var flowFieldQuery = state.EntityManager.CreateEntityQuery(typeof(FlowFieldFollow));
        var flowFieldEntities = flowFieldQuery.ToEntityArray(Allocator.Temp);
        foreach (var e in flowFieldEntities)
        {
            ecb.SetComponentEnabled<FlowFieldFollow>(e, false);
        }
        flowFieldEntities.Dispose();

        for (int i = 0; i < gridSystemData.gridMapArray.Length; i++)
        {
            if (gridSystemData.gridMapArray[i].gridEntityArray.IsCreated)
            {
                ecb.DestroyEntity(gridSystemData.gridMapArray[i].gridEntityArray);
                gridSystemData.gridMapArray[i].gridEntityArray.Dispose();
            }
        }

        gridSystemData.gridMapArray.Dispose();
        gridSystemData.costMap.Dispose();
        gridSystemData.totalGridMapEntityArray.Dispose();

        NativeArray<GridMap> gridMapArray = new NativeArray<GridMap>(FLOW_FIELD_MAP_COUNT, Allocator.Persistent);
        NativeList<Entity> totalGridMapEntityList = new NativeList<Entity>(totalCount * FLOW_FIELD_MAP_COUNT, Allocator.Temp);

        Entity gridNodeEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridNode>(gridNodeEntityPrefab);

        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            GridMap gridMap = new GridMap();
            gridMap.isVaild = false;
            gridMap.gridEntityArray = new NativeArray<Entity>(totalCount, Allocator.Persistent);

            state.EntityManager.Instantiate(gridNodeEntityPrefab, gridMap.gridEntityArray);
            totalGridMapEntityList.AddRange(gridMap.gridEntityArray);

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    int index = CalculateIndex(x, y, newWidth);
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

        state.EntityManager.DestroyEntity(gridNodeEntityPrefab);

        gridSystemData.width = newWidth;
        gridSystemData.height = newHeight;
        gridSystemData.gridNodeSize = gridNodeSize;
        gridSystemData.mapWidth = newMapWidth;
        gridSystemData.mapHeight = newMapHeight;
        gridSystemData.gridMapArray = gridMapArray;
        gridSystemData.costMap = new NativeArray<byte>(totalCount, Allocator.Persistent);
        gridSystemData.totalGridMapEntityArray = totalGridMapEntityList.ToArray(Allocator.Persistent);
        gridSystemData.needRecalculateCostMap = true;
        gridSystemData.nextGridIndex = 0;
        gridSystemData.isInitialized = true;

        totalGridMapEntityList.Dispose();
        SystemAPI.SetComponent(state.SystemHandle, gridSystemData);
        gridNodeComponentLookup.Update(ref state);

        Debug.Log($"[GridSystem] Grid rebuilt successfully: {newWidth}x{newHeight}, map: {newMapWidth}x{newMapHeight}");
    }

    // 公共方法：标记需要重新计算成本地图
    public void MarkCostMapForRecalculation(ref SystemState state)
    {
        if (SystemAPI.HasComponent<GridSystemData>(state.SystemHandle))
        {
            GridSystemData gridSystemData = SystemAPI.GetComponent<GridSystemData>(state.SystemHandle);
            gridSystemData.needRecalculateCostMap = true;
            SystemAPI.SetComponent(state.SystemHandle, gridSystemData);
        }
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
