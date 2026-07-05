using Unity.Entities;
using Unity.Mathematics;

public struct SpawnZombieEvent : IComponentData
{
    public float3 spawnPosition;
}
