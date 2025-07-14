using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct HordeSpawnerSystem : ISystem
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

        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRO<LocalTransform> localTransform, RefRW<Horde> horde)
            in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Horde>>())
        {
            // ���¼�ʱ��
            horde.ValueRW.spawnTimer -= SystemAPI.Time.DeltaTime;

            // ����Ƿ�Ӧ�����ɽ�ʬ
            if (horde.ValueRO.spawnTimer <= 0 && horde.ValueRO.zombieAmountToSpawn > 0)
            {
                // ���ü�ʱ��
                horde.ValueRW.spawnTimer = horde.ValueRO.spawnTimerMax;


                Random random = horde.ValueRO.random;

                float3 spawnPosition = localTransform.ValueRO.Position;

                spawnPosition.x += random.NextFloat(-horde.ValueRO.spawnAreaWidth, +horde.ValueRO.spawnAreaWidth);
                spawnPosition.z += random.NextFloat(-horde.ValueRO.spawnAreaHeight, +horde.ValueRO.spawnAreaHeight);
                horde.ValueRW.random = random;


                // ���ɽ�ʬ
                Entity zombieEntity = entityCommandBuffer.Instantiate(entitiesReferences.zombiePrefabEntity);
                entityCommandBuffer.SetComponent(zombieEntity, LocalTransform.FromPosition(spawnPosition));
                entityCommandBuffer.AddComponent<EnemyAttackHQ>(zombieEntity);

                // ���ٴ���������
                horde.ValueRW.zombieAmountToSpawn--;
            }
        }
    }

}
