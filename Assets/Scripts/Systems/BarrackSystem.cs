using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BuildingBarrackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

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

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

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

            UnitTypeSo unitTypeSo = GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(unitType);

            spawnUnitTypeBuffers.RemoveAt(0);
            buildingBarracks.ValueRW.onUnitQueueChanged = true;

            // 「生产数量」Buff：累计百分比每满 100% 就多产 1 个，余量留到下次，避免 20% 这类加成被舍入吞掉。
            int extraCount = 0;
            float bonusPercent = buildingBarracks.ValueRO.productionCountBonusPercent;
            if (bonusPercent > 0f)
            {
                buildingBarracks.ValueRW.productionCountCarry += bonusPercent / 100f;
                const int maxExtraPerCycle = 8;
                while (buildingBarracks.ValueRO.productionCountCarry >= 1f && extraCount < maxExtraPerCycle)
                {
                    buildingBarracks.ValueRW.productionCountCarry -= 1f;
                    extraCount++;
                }
                if (extraCount >= maxExtraPerCycle)
                {
                    buildingBarracks.ValueRW.productionCountCarry = 0f;
                }
            }

            int spawnCount = 1 + extraCount;
            for (int i = 0; i < spawnCount; i++)
            {
                // 多产的第一个单位照旧堆在兵营中心，其余的围一圈散开，避免完全重叠。
                float3 spawnPosition = localTransform.ValueRO.Position;
                if (i > 0)
                {
                    float angle = (i - 1) * math.PI * 2f / math.max(1, spawnCount - 1);
                    spawnPosition += new float3(math.cos(angle), 0f, math.sin(angle)) * 1.5f;
                }

                Entity spawnedUnitEntity = ecb.Instantiate(unitTypeSo.GetPrefabEntity(entitiesReferences));
                ecb.AddComponent<RuntimeEntity>(spawnedUnitEntity);
                ecb.AddComponent<PendingGlobalBuff>(spawnedUnitEntity);

                ecb.SetComponent(spawnedUnitEntity, LocalTransform.FromPosition(spawnPosition));

                ecb.SetComponent(spawnedUnitEntity, new MoveOverride
                {
                    targetPosition = spawnPosition + buildingBarracks.ValueRO.rallyPositionOffset
                });

                ecb.SetComponentEnabled<MoveOverride>(spawnedUnitEntity, true);
            }
        }
    }
}
