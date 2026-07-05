using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct ShootLightSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // ȷ��ϵͳֻ�ڴ��� EntitiesReferences ʵ��ʱ����
        state.RequireForUpdate<EntitiesReferences>();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        foreach (RefRO<ShootAttack> shootAttack in SystemAPI.Query<RefRO<ShootAttack>>())
        {
            if (shootAttack.ValueRO.onShoot.isTriggered)
            {
                Entity shootlightEntity = state.EntityManager.Instantiate(entitiesReferences.shootLightPrefabEntity);
                state.EntityManager.AddComponent<RuntimeEntity>(shootlightEntity);
                SystemAPI.SetComponent(shootlightEntity, LocalTransform.FromPosition(shootAttack.ValueRO.onShoot.shootFromPosition));
            }
        }
    }

}
