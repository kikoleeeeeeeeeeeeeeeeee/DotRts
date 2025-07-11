using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisualUnderFogOrWarAuthoring : MonoBehaviour
{


    public float sphereCastSize;
    public GameObject parentGameObject; 
    public class Baker : Baker<VisualUnderFogOrWarAuthoring>
    {
        public override void Bake(VisualUnderFogOrWarAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VisualUnderFogOfWar {
                isVisible = true,
            parentEntity = GetEntity(authoring.parentGameObject, TransformUsageFlags.Dynamic),
            sphereCastSize =authoring.sphereCastSize,
            });

        }
    }
}

public struct VisualUnderFogOfWar : IComponentData
{
    public bool isVisible;
    public Entity parentEntity;
    public float sphereCastSize;

}