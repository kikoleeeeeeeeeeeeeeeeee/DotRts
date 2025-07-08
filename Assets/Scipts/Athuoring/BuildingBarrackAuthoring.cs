using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingBarrackAuthoring : MonoBehaviour
{
    public float progressMax;
    public class Baker : Baker<BuildingBarrackAuthoring>
    {
        public override void Bake(BuildingBarrackAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingBarracks { 
                progressMax = authoring.progressMax,
                rallyPositionOffset = new float3 (10, 0, 0),
            });
            

            AddBuffer<SpawnUnitTypeBuffer>(entity);

            AddComponent(entity, new BuildingBarracksUnitEnqueue() );
            SetComponentEnabled<BuildingBarracksUnitEnqueue>(entity, false);    
        }
    }
}
public struct BuildingBarracksUnitEnqueue : IComponentData, IEnableableComponent
{
    public UnitTypeSo.UnitType unitType;
}



public struct BuildingBarracks : IComponentData
{
    public float progress;
    public float progressMax;
    public UnitTypeSo.UnitType activeUnitType;
    public float3 rallyPositionOffset;
    public bool onUnitQueueChanged;
}



[InternalBufferCapacity(10)] 
public struct SpawnUnitTypeBuffer : IBufferElementData
{
    public UnitTypeSo.UnitType unitType;
}