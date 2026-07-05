using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct DestroyPreplacedZombiesSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameSceneTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (_, entity) in
            SystemAPI.Query<RefRO<Zombie>>().WithNone<RuntimeEntity>().WithEntityAccess())
        {
            if (state.EntityManager.HasComponent<AnimatedMesh>(entity))
            {
                var animatedMesh = state.EntityManager.GetComponentData<AnimatedMesh>(entity);
                if (state.EntityManager.Exists(animatedMesh.meshEntity))
                    ecb.DestroyEntity(animatedMesh.meshEntity);
            }
            ecb.DestroyEntity(entity);
        }

        state.Enabled = false;
    }
}

public struct PreplacedZombie : IComponentData {}