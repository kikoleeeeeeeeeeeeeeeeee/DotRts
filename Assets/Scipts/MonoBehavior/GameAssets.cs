using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public const int UNITS_LAYER = 6;
    public const int BUILDINGS_LAYER = 8;
    public const int PATHFINDGIN_WALLS = 9;
    public static GameAssets Instance {  get; private set; }

    private void Awake()
    {
        Instance = this;
    }


    public UnitTypeSoList unitTypeListSO;
    public BuildingTypeListSo buildingTypeListSo;


}
