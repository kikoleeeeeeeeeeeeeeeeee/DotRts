using Unity.Entities;
using UnityEngine;

public class EntitiesReferenceAuthoring : MonoBehaviour
{
    public GameObject bulletPrefabGameObject;
    public GameObject zombiePrefabGameObject;
    public GameObject shootLightPrefabGameObject;
    public GameObject scoutPrefabGameObject;
    public GameObject soldierPrefabGameObject;


    public GameObject buildingTowerGameObject;
    public GameObject buildingBarrackGameObject;
    public GameObject buildingIronHarvestGameObject;
    public GameObject buildingGoldHarvestGameObject;
    public GameObject buildingOilHarvestGameObject;

    public GameObject bbuildingConstructionPrefabGameObject;


    public GameObject buildingTowerVisualGameObject;
    public GameObject buildingBarrackVisualGameObject;
    public GameObject buildingIronHarvestVisualGameObject;
    public GameObject buildingGoldHarvestVisualGameObject;
    public GameObject buildingOilHarvestVisualGameObject;
    public class Baker : Baker<EntitiesReferenceAuthoring>
    {
        public override void Bake(EntitiesReferenceAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences
            {
                bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
                zombiePrefabEntity = GetEntity(authoring.zombiePrefabGameObject, TransformUsageFlags.Dynamic),
                shootLightPrefabEntity = GetEntity(authoring.shootLightPrefabGameObject, TransformUsageFlags.Dynamic),
                scoutPrefabEntity = GetEntity(authoring.scoutPrefabGameObject, TransformUsageFlags.Dynamic),
                soldierPrefabEntity = GetEntity(authoring.soldierPrefabGameObject, TransformUsageFlags.Dynamic),
                buildingTowerPrefabEntity= GetEntity(authoring.buildingTowerGameObject, TransformUsageFlags.Dynamic),
                buildingBarrackPrefabEntity = GetEntity(authoring.buildingBarrackGameObject, TransformUsageFlags.Dynamic),
                buildingIronHarvestPrefabEntity = GetEntity(authoring.buildingIronHarvestGameObject, TransformUsageFlags.Dynamic),
                buildingGoldHarvestPrefabEntity = GetEntity(authoring.buildingGoldHarvestGameObject, TransformUsageFlags.Dynamic),
                buildingOilHarvestPrefabEntity = GetEntity(authoring.buildingOilHarvestGameObject, TransformUsageFlags.Dynamic),
                buildingConstructionPrefabEntity =GetEntity(authoring.bbuildingConstructionPrefabGameObject, TransformUsageFlags.Dynamic),

                buildingTowerVisualPrefabEntity = GetEntity(authoring.buildingTowerVisualGameObject, TransformUsageFlags.Dynamic),
                buildingBarrackVisualPrefabEntity = GetEntity(authoring.buildingBarrackVisualGameObject, TransformUsageFlags.Dynamic),
                buildingIronHarvestVisualPrefabEntity = GetEntity(authoring.buildingIronHarvestVisualGameObject, TransformUsageFlags.Dynamic),
                buildingGoldHarvestVisualPrefabEntity = GetEntity(authoring.buildingGoldHarvestVisualGameObject, TransformUsageFlags.Dynamic),
                buildingOilHarvestVisualPrefabEntity = GetEntity(authoring.buildingOilHarvestVisualGameObject, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct EntitiesReferences : IComponentData
{
    public Entity bulletPrefabEntity;
    public Entity zombiePrefabEntity;
    public Entity shootLightPrefabEntity;
    public Entity scoutPrefabEntity;
    public Entity soldierPrefabEntity;

    public Entity buildingTowerPrefabEntity;
    public Entity buildingBarrackPrefabEntity;
    public Entity buildingOilHarvestPrefabEntity;
    public Entity buildingIronHarvestPrefabEntity;
    public Entity buildingGoldHarvestPrefabEntity;


    public Entity buildingTowerVisualPrefabEntity;
    public Entity buildingBarrackVisualPrefabEntity;
    public Entity buildingOilHarvestVisualPrefabEntity;
    public Entity buildingIronHarvestVisualPrefabEntity;
    public Entity buildingGoldHarvestVisualPrefabEntity;

    public Entity buildingConstructionPrefabEntity;


}