using System;
using UnityEngine;

[Serializable]
public struct MapConfig
{
    // 地图大小
    public int mapWidth;
    public int mapHeight;
    
    // 地形类型
    public TerrainType terrainType;
    
    // 障碍物密度
    [Range(0f, 1f)]
    public float obstacleDensity;
    
    // 资源点数量
    public int resourceCount;
    
    // 生成区域边界
    public float spawnAreaRadius;
    
    // 基地放置区域边界
    public float basePlacementRadius;
}

[Serializable]
public enum TerrainType
{
    Grassland,
    Desert,
    Snow,
    Forest
}
