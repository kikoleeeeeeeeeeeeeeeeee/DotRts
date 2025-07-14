using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines.Interpolators;

public class BuildingConstructAuthoring : MonoBehaviour
{
   public class Baker : Baker<BuildingConstructAuthoring>
    {
        public override void Bake(BuildingConstructAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,new BuildignConstruction());
        }
    }
}

public struct BuildignConstruction : IComponentData {
    public float constructionTimer;
    public float constructionTimerMax;
    public float3 starPosition;
    public float3 endPosition;
    public BuildingTypeSo.BuildingType buildingType;
    public Entity finalPrefabEntity;
    public Entity visualEntity;
}
