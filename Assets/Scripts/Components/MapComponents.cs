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

    /// <summary>刷怪点距 HQ 的期望距离（由 <see cref="MapConfig.spawnAreaRadius"/> 注入）。</summary>
    public float spawnAreaRadius;

    /// <summary>HQ 允许放置的距地图中心半径（由 <see cref="MapConfig.basePlacementRadius"/> 注入）。</summary>
    public float basePlacementRadius;
    public bool isGenerated;
    public bool needRecalculateCostMap;
    public bool isGenerating;
}

public struct ObstacleTag : IComponentData { }

public struct ResourceNodeTag : IComponentData { }
