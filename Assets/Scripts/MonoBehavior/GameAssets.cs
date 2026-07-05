using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public const int DEFAULT_LAYER = 0;

    public const int UNITS_LAYER = 6;
    public const int BUILDINGS_LAYER = 8;
    public const int PATHFINDING_WALL = 9;
    public const int PATHFINDING_HEAVY = 10;
    public const int FOR_OF_WAR = 12;


    public static GameAssets Instance {  get; private set; }

    private void Awake()
    { 
        Instance = this;
    } 


    public UnitTypeSoList unitTypeListSO;
    public BuildingTypeListSo buildingTypeListSo;


}
