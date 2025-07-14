using Unity.Entities;
using UnityEngine;

public class BuildingHarvestAuthoring : MonoBehaviour
{
    public float harvestTimeMax;
    public ResourceTypeSo.ResourceType resourceType;
    public class Baker : Baker<BuildingHarvestAuthoring>{
        public override void Bake(BuildingHarvestAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingHarvester
            {
                harvestTimeMax = authoring.harvestTimeMax,
                resourceType = authoring.resourceType,
            });
        }
    }
}

public struct BuildingHarvester : IComponentData
{
    public float harvestTimer;
    public float harvestTimeMax;
    public ResourceTypeSo.ResourceType resourceType;
}