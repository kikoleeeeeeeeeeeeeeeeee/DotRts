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
        GoldHarvester,
        IronHarvester,
        OilHarvester,

    }


    public float buildingConstructionTimerMax;
    public float constructionYOffset;
    public BuildingType buildingType;
    public Transform prefab;
    public float buildingDistanceMin;
    public bool showInBuildingPlacementManagerUi;
    public Sprite sprite;
    public Transform visualPrefab;
    public ResourceAmount[] buildCostResourceAmountArray;

    public bool IsNone()
    {
        return buildingType == BuildingType.None;
    }

    public Entity GetPrefabEntity(EntitiesReferences entitiesReferences)
    {
        switch (buildingType)
        {
            default:
            case BuildingType.None:
            case BuildingType.Tower: return entitiesReferences.buildingTowerPrefabEntity;
            case BuildingType.Barrack: return entitiesReferences.buildingBarrackPrefabEntity;

            case BuildingType.IronHarvester: return entitiesReferences.buildingIronHarvestPrefabEntity;
            case BuildingType.GoldHarvester: return entitiesReferences.buildingGoldHarvestPrefabEntity;
            case BuildingType.OilHarvester: return entitiesReferences.buildingOilHarvestPrefabEntity;
        }
    }

    public Entity GetViusalPrefabEntity(EntitiesReferences entitiesReferences)
    {
        switch (buildingType)
        {
            default:
            case BuildingType.None:
            case BuildingType.Tower: return entitiesReferences.buildingTowerVisualPrefabEntity;
            case BuildingType.Barrack: return entitiesReferences.buildingBarrackVisualPrefabEntity;

            case BuildingType.IronHarvester: return entitiesReferences.buildingIronHarvestVisualPrefabEntity;
            case BuildingType.GoldHarvester: return entitiesReferences.buildingGoldHarvestVisualPrefabEntity;
            case BuildingType.OilHarvester: return entitiesReferences.buildingOilHarvestVisualPrefabEntity;
        }
    }
}
