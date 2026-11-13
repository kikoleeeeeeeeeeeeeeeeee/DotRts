using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public const int DEFAULT_LAYER = 0;

    public const int UNITS_LAYER = 6;
    public const int BUILDINGS_LAYER = 8;
    public const int PATHFINDING_WALL = 9;
    public const int PATHFINDING_HEAVY = 10;
    public const int FOR_OF_WAR = 12;

    /// <summary>
    /// 地图生成 / 基地放置时用于重叠检测的层：障碍物（墙）、资源点、建筑。
    /// 场景里没有显式配置 LayerMask 时用它兜底，否则检测会退化成「什么都不挡」。
    /// </summary>
    public static int BlockingMask => (1 << PATHFINDING_WALL) | (1 << PATHFINDING_HEAVY) | (1 << BUILDINGS_LAYER);


    public static GameAssets Instance {  get; private set; }

    private void Awake()
    { 
        Instance = this;
    } 


    public UnitTypeSoList unitTypeListSO;
    public BuildingTypeListSo buildingTypeListSo;


}
