using Unity.Entities;
using Unity.Mathematics;

public struct MapConfigData : IComponentData
{
    public int mapWidth;
    public int mapHeight;
    public float gridNodeSize;
    public int seed;
    public float obstacleDensity;
    public int resourceCount;
    public float basePlacementRadius;
    public bool isGenerated;
    public bool needRecalculateCostMap;
    public bool isGenerating;
}

public struct MapSpawnRequest : IComponentData, IEnableableComponent
{
    public int seed;
    public int mapWidth;
    public int mapHeight;
    public float obstacleDensity;
    public int resourceCount;
    public float basePlacementRadius;
}

public struct MapClearRequest : IComponentData, IEnableableComponent
{
}

public struct MapGeneratedTag : IComponentData { }

public struct ObstacleTag : IComponentData { }

public struct ResourceNodeTag : IComponentData { }

public struct MapPrefabsReferences : IComponentData
{
    public Entity obstaclePrefabEntity;
    public Entity resourcePrefabEntity;
}