using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ZombieSpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        Entity zombiePrefabEntity = entitiesReferences.zombiePrefabEntity;

        if (zombiePrefabEntity == Entity.Null || !state.EntityManager.Exists(zombiePrefabEntity))
            return;

        float3 hqPosition = float3.zero;
        if (SystemAPI.HasSingleton<BuildingHQ>())
        {
            Entity hqEntity = SystemAPI.GetSingletonEntity<BuildingHQ>();
            if (state.EntityManager.HasComponent<LocalTransform>(hqEntity))
            {
                LocalTransform hqTransform = state.EntityManager.GetComponentData<LocalTransform>(hqEntity);
                if (!math.isnan(hqTransform.Position.x) && !math.isnan(hqTransform.Position.z))
                    hqPosition = hqTransform.Position;
            }
        }

        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        int waveSpawnCount = 0;
        foreach (DynamicBuffer<SpawnZombieRequestBuffer> spawnRequestBuffer
            in SystemAPI.Query<DynamicBuffer<SpawnZombieRequestBuffer>>())
        {
            if (spawnRequestBuffer.IsEmpty)
                continue;

            for (int i = 0; i < spawnRequestBuffer.Length; i++)
            {
                float3 spawnPosition = spawnRequestBuffer[i].spawnPosition;
                if (math.isnan(spawnPosition.x) || math.isnan(spawnPosition.y) || math.isnan(spawnPosition.z))
                    continue;

                Entity zombieEntity = ecb.Instantiate(zombiePrefabEntity);
                ecb.AddComponent<RuntimeEntity>(zombieEntity);
                ecb.SetComponent(zombieEntity, LocalTransform.FromPosition(spawnPosition));
                ecb.AddComponent<EnemyAttackHQ>(zombieEntity);
                ecb.SetComponent(zombieEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));

                ecb.SetComponentEnabled<TargetPositionPathQueued>(zombieEntity, true);
                ecb.SetComponent(zombieEntity, new TargetPositionPathQueued
                {
                    targetPosition = hqPosition
                });

                waveSpawnCount++;
            }

            spawnRequestBuffer.Clear();
        }

        if (waveSpawnCount > 0)
        {
            Debug.Log($"[ZombieSpawner] Wave spawned {waveSpawnCount} zombies. HQ: {hqPosition}");
        }

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<DistanceHit> distanceHitsList = new NativeList<DistanceHit>(Allocator.Temp);

        foreach ((RefRO<LocalTransform> localTransform,
                 RefRW<ZombieSpawner> zombieSpawner)
            in SystemAPI.Query<RefRO<LocalTransform>, RefRW<ZombieSpawner>>())
        {
            zombieSpawner.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            if (zombieSpawner.ValueRO.timer > 0f)
                continue;
            zombieSpawner.ValueRW.timer = zombieSpawner.ValueRO.timerMax;

            float3 spawnerPos = localTransform.ValueRO.Position;
            if (math.isnan(spawnerPos.x) || math.isnan(spawnerPos.y) || math.isnan(spawnerPos.z))
                continue;

            distanceHitsList.Clear();
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0,
            };

            int nearbyZombieAmount = 0;
            if (collisionWorld.OverlapSphere(
                spawnerPos,
                zombieSpawner.ValueRO.nearbyZombieAmountDistance,
                ref distanceHitsList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitsList)
                {
                    if (!SystemAPI.Exists(distanceHit.Entity))
                        continue;
                    if (SystemAPI.HasComponent<Unit>(distanceHit.Entity) && SystemAPI.HasComponent<Zombie>(distanceHit.Entity))
                        nearbyZombieAmount++;
                }
            }

            if (nearbyZombieAmount >= zombieSpawner.ValueRO.nearbyZombieAmountMax)
                continue;

            Entity zombieEntity2 = ecb.Instantiate(zombiePrefabEntity);
            ecb.AddComponent<RuntimeEntity>(zombieEntity2);
            ecb.SetComponent(zombieEntity2, LocalTransform.FromPosition(spawnerPos));
            ecb.AddComponent<EnemyAttackHQ>(zombieEntity2);
            ecb.SetComponent(zombieEntity2, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));

            ecb.SetComponentEnabled<TargetPositionPathQueued>(zombieEntity2, true);
            ecb.SetComponent(zombieEntity2, new TargetPositionPathQueued
            {
                targetPosition = hqPosition
            });
        }

        distanceHitsList.Dispose();
    }
}

public struct SpawnZombieRequestBuffer : IBufferElementData
{
    public float3 spawnPosition;
}