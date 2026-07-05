using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial struct MapConfigSystemBootstrap : ISystem
{
    private bool hasInitialized;

    public void OnCreate(ref SystemState state)
    {
        hasInitialized = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        if (hasInitialized)
        {
            state.Enabled = false;
            return;
        }

        var query = state.EntityManager.CreateEntityQuery(typeof(MapConfigData));
        if (query.CalculateEntityCount() > 0)
        {
            hasInitialized = true;
            state.Enabled = false;
            query.Dispose();
            return;
        }
        query.Dispose();

        var gameSceneTagQuery = state.EntityManager.CreateEntityQuery(typeof(GameSceneTag));
        if (gameSceneTagQuery.CalculateEntityCount() == 0)
        {
            gameSceneTagQuery.Dispose();
            return;
        }
        gameSceneTagQuery.Dispose();

        // 优先从场景中的 MapManager 读取配置，其次从 Resources 加载，最后用默认值
        MapConfig config;

        if (MapManager.Instance != null && MapManager.Instance.MapConfigSoInstance != null)
        {
            config = MapManager.Instance.MapConfigSoInstance.mapConfig;
            Debug.Log($"[MapConfigBootstrap] Loaded config from scene MapManager: {config.mapWidth}x{config.mapHeight}");
        }
        else
        {
            MapConfigSo mapConfigSo = Resources.Load<MapConfigSo>("MapConfig");
            if (mapConfigSo != null)
            {
                config = mapConfigSo.mapConfig;
                Debug.Log($"[MapConfigBootstrap] Loaded MapConfigSo from Resources: {config.mapWidth}x{config.mapHeight}");
            }
            else
            {
                config = new MapConfig
                {
                    mapWidth = 200,
                    mapHeight = 200,
                    terrainType = TerrainType.Grassland,
                    obstacleDensity = 0.003f,
                    resourceCount = 15,
                    spawnAreaRadius = 30f,
                    basePlacementRadius = 20f,
                };
                Debug.LogWarning("[MapConfigBootstrap] No MapConfigSo found (scene or Resources). Using default 200x200. " +
                    "Assign MapConfigSo to MapManager in the scene, or put it in a Resources folder named 'MapConfig'.");
            }
        }

        Entity mapConfigEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(mapConfigEntity, new MapConfigData
        {
            mapWidth = config.mapWidth,
            mapHeight = config.mapHeight,
            gridNodeSize = 5f,
            seed = 0,
            obstacleDensity = config.obstacleDensity,
            resourceCount = config.resourceCount,
            basePlacementRadius = config.basePlacementRadius,
            isGenerated = false,
            needRecalculateCostMap = false,
            isGenerating = false,
        });

        state.EntityManager.AddComponent<MapSpawnRequest>(mapConfigEntity);
        state.EntityManager.SetComponentEnabled<MapSpawnRequest>(mapConfigEntity, false);

        state.EntityManager.AddComponent<MapClearRequest>(mapConfigEntity);
        state.EntityManager.SetComponentEnabled<MapClearRequest>(mapConfigEntity, false);

        Debug.Log($"[MapConfigBootstrap] MapConfigData entity created: {config.mapWidth}x{config.mapHeight}");

        hasInitialized = true;
        state.Enabled = false;
    }
}