using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BuildingConstructionSystems : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        bool anyBuildingCompleted = false;

        foreach ((RefRO<LocalTransform> localTransform,
            RefRW<BuildignConstruction> buildingConstruction,
            Entity entity)
       in SystemAPI.Query
       <RefRO<LocalTransform>,
        RefRW<BuildignConstruction>>().WithEntityAccess())
        {

            RefRW<LocalTransform> visualLocalTransform =    
            SystemAPI.GetComponentRW<LocalTransform>(buildingConstruction.ValueRO.visualEntity);

            visualLocalTransform.ValueRW.Position =
                math.lerp(buildingConstruction.ValueRO.starPosition,
                buildingConstruction.ValueRO.endPosition,
                buildingConstruction.ValueRO.constructionTimer / buildingConstruction.ValueRO.constructionTimerMax);


            buildingConstruction.ValueRW.constructionTimer += SystemAPI.Time.DeltaTime;
            if (buildingConstruction.ValueRO.constructionTimer >= buildingConstruction.ValueRO.constructionTimerMax)
            {
                Entity spawnedBuildingEntity = entityCommandBuffer.Instantiate(buildingConstruction.ValueRO.finalPrefabEntity);
                entityCommandBuffer.AddComponent<RuntimeEntity>(spawnedBuildingEntity);
                entityCommandBuffer.AddComponent<PendingGlobalBuff>(spawnedBuildingEntity);
                entityCommandBuffer.SetComponent(spawnedBuildingEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));


                entityCommandBuffer.DestroyEntity(buildingConstruction.ValueRO.visualEntity);
                entityCommandBuffer.DestroyEntity(entity);

                anyBuildingCompleted = true;
            }
        }

        if (anyBuildingCompleted && SystemAPI.HasSingleton<MapConfigData>())
        {
            var mapConfig = SystemAPI.GetSingleton<MapConfigData>();
            mapConfig.needRecalculateCostMap = true;
            SystemAPI.SetSingleton(mapConfig);
        }
    }


}
