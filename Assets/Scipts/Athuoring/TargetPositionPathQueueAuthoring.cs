using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TargetPositionPathQueueAuthoring : MonoBehaviour
{
    public class Baker: Baker<TargetPositionPathQueueAuthoring>
    {
        public override void Bake(TargetPositionPathQueueAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TargetPositionPathQueued());
            SetComponentEnabled<TargetPositionPathQueued>(entity, false);
        }
    }
}

public struct TargetPositionPathQueued : IComponentData,IEnableableComponent
{
    public float3 targetPosition;
}