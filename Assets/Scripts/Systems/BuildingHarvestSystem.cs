using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;

partial struct BuildingHarvestSystem : ISystem
{

    public void OnUpdate(ref SystemState state)
    {
        foreach(RefRW<BuildingHarvester> buildingHarvester 
        in SystemAPI.Query<RefRW<BuildingHarvester>>())
        {
            buildingHarvester.ValueRW.harvestTimer -= SystemAPI.Time.DeltaTime;
            if (buildingHarvester.ValueRO.harvestTimer <= 0f)
            {
                buildingHarvester.ValueRW.harvestTimer = buildingHarvester.ValueRW.harvestTimeMax;

                ResrouceManager.Instance.AddResourceAmount(buildingHarvester.ValueRO.resourceType, 1);
            }
        }
    }
}
