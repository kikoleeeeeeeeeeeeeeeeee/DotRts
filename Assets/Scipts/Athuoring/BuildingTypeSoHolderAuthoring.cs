using Unity.Entities;
using UnityEngine;

public class BuildingTypeSoHolderAuthoring : MonoBehaviour
{
    public BuildingTypeSo.BuildingType buildingType;
    public class Baker : Baker<BuildingTypeSoHolderAuthoring>
    {
        public override void Bake(BuildingTypeSoHolderAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingTypeSOHolder {
                buildingType = authoring.buildingType
            });
        }
    }
}

public struct BuildingTypeSOHolder : IComponentData
{
    public BuildingTypeSo.BuildingType buildingType;
}