using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlowFieldFollowAuthoring : MonoBehaviour
{
      public class Baker : Baker<FlowFieldFollowAuthoring>
    {
        public override void Bake(FlowFieldFollowAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FlowFieldFollow ());
            SetComponentEnabled<FlowFieldFollow>(entity, false);
        }
    }
}


public struct FlowFieldFollow : IComponentData,IEnableableComponent{
    public float3 targetPosition;
    public float3 lastMoveVector;
    public int gridIndex;
}
 