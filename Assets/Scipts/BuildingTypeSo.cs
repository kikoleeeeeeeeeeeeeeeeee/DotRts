using Unity.Entities;
using UnityEngine;
using static UnitTypeSo;

[CreateAssetMenu()]
public class BuildingTypeSo : ScriptableObject
{
   public enum BuildingType
    {
       None,
       ZombieSpawner,
       Tower,
       Barrack,
       HQ, 
    }
     
    public BuildingType buildingType;
    public Transform prefab;
    public float buildingDistanceMin;
    public bool showInBuildingPlacementManagerUi;
    public Sprite sprite;
    public Transform visualPrefab;

    public bool IsNone()
    {
        return  buildingType == BuildingType.None;
    }

    public Entity GetPrefabEntity(EntitiesReferences entitiesReferences)
    {
        switch (buildingType)
        {
            default:
            case BuildingType.None:
            case BuildingType.Tower: return entitiesReferences.buildingTowerPrefabEntity;
            case BuildingType.Barrack: return entitiesReferences.buildingBarrackPrefabEntity;
        }
    }
}
