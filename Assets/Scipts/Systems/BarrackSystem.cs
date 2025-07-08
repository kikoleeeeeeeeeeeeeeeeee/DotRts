using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct BuildingBarrackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();



        foreach((RefRW<BuildingBarracks> buildingBarrack,
            DynamicBuffer<SpawnUnitTypeBuffer> spawnUnitTypeBuffer,
            RefRO<BuildingBarracksUnitEnqueue> buildingBarracksUnitEnqueue,
            EnabledRefRW<BuildingBarracksUnitEnqueue> buildingBarracksUnitEnqueueEnabled)
            in SystemAPI.Query<RefRW<BuildingBarracks>,
            DynamicBuffer<SpawnUnitTypeBuffer>,
            RefRO<BuildingBarracksUnitEnqueue>,
            EnabledRefRW<BuildingBarracksUnitEnqueue>>())
        {
            spawnUnitTypeBuffer.Add(new SpawnUnitTypeBuffer
            {
                unitType = buildingBarracksUnitEnqueue.ValueRO.unitType
            });
            buildingBarracksUnitEnqueueEnabled.ValueRW = false;
            buildingBarrack.ValueRW.onUnitQueueChanged = true;
        }




        foreach((RefRO<LocalTransform> localTransform,
            RefRW<BuildingBarracks> buildingBarracks,
            DynamicBuffer<SpawnUnitTypeBuffer> spawnUnitTypeBuffers)
        in SystemAPI.Query<
            RefRO<LocalTransform>,
            RefRW<BuildingBarracks>,
            DynamicBuffer<SpawnUnitTypeBuffer>>())
        {

            if (spawnUnitTypeBuffers.IsEmpty)
            {
                continue;
            }
            if (buildingBarracks.ValueRO.activeUnitType != spawnUnitTypeBuffers[0].unitType)
            {
                buildingBarracks.ValueRW.activeUnitType = spawnUnitTypeBuffers[0].unitType;

                UnitTypeSo activeUnitTypeSO =
                    GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(buildingBarracks.ValueRO.activeUnitType);
                    
                buildingBarracks.ValueRW.progressMax = activeUnitTypeSO.progressMax; 
            }

            buildingBarracks.ValueRW.progress += SystemAPI.Time.DeltaTime;

            if (buildingBarracks.ValueRO.progress < buildingBarracks.ValueRO.progressMax)
            {
                continue;
            }

            buildingBarracks.ValueRW.progress = 0f;

            UnitTypeSo.UnitType unitType = spawnUnitTypeBuffers[0].unitType;

            UnitTypeSo unitTypeSo =  GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(unitType);

            spawnUnitTypeBuffers.RemoveAt(0);
            buildingBarracks.ValueRW.onUnitQueueChanged =true;

            Entity spawnedUnitEntity =  state.EntityManager.Instantiate(unitTypeSo.GetPrefabEntity(entitiesReferences));
            
            SystemAPI.SetComponent(spawnedUnitEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));

            SystemAPI.SetComponent(spawnedUnitEntity, new MoveOverride {
                targetPosition = localTransform.ValueRO.Position + buildingBarracks.ValueRO.rallyPositionOffset
            });

            SystemAPI.SetComponentEnabled<MoveOverride>(spawnedUnitEntity, true) ;
        }
    }

}
