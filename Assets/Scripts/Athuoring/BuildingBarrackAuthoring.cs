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

    /// <summary>「生产数量」Buff 累计的百分比加成（由 BuffManager / GlobalBuffApplySystem 写入）。</summary>
    public float productionCountBonusPercent;

    /// <summary>不足 100% 的余量，攒满 100% 就多产 1 个，避免 20% 这类加成被四舍五入吞掉。</summary>
    public float productionCountCarry;
}



[InternalBufferCapacity(10)] 
public struct SpawnUnitTypeBuffer : IBufferElementData
{
    public UnitTypeSo.UnitType unitType;
}