using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Rendering;

public class VisualUnderFogOrWarAuthoring : MonoBehaviour
{


    public float sphereCastSize;
    public GameObject parentGameObject; 
    public class Baker : Baker<VisualUnderFogOrWarAuthoring>
    {
        public override void Bake(VisualUnderFogOrWarAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VisualUnderFogOfWar
            {
                isVisible = false,
                parentEntity = GetEntity(authoring.parentGameObject, TransformUsageFlags.Dynamic),
                sphereCastSize = authoring.sphereCastSize,
                timer = 0f,
                timerMax = .2f,
            });
            AddComponent(entity, new DisableRendering());
        }
    }
}

public struct VisualUnderFogOfWar : IComponentData
{
    public bool isVisible;
    public Entity parentEntity;
    public float sphereCastSize;
    public float timer;
    public float timerMax;
}