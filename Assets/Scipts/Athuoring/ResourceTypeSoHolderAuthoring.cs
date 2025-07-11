using Unity.Entities;
using UnityEngine;

public class ResourceTypeSoHolderAuthoring : MonoBehaviour
{
    public ResrouceTypeSo.ResourceType resourceType;

    public class Baker : Baker<ResourceTypeSoHolderAuthoring>
    {
        public override void Bake(ResourceTypeSoHolderAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ResourceTypeSoHolder
            {
                resourceType = authoring.resourceType,
            });
        }
    }
}

public struct ResourceTypeSoHolder : IComponentData
{
    public ResrouceTypeSo.ResourceType resourceType;
}
