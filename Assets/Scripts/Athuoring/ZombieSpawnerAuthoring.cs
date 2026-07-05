using Unity.Entities;
using UnityEngine;

public class ZombieSpawnerAuthoring : MonoBehaviour
{
    public float timerMax;
    public float randomWalkingDisantanceMin;
    public float randomWalkingDisantanceMax;
    public int nearbyZombieAmountMax;
    public float nearbyZombieAmountDistance;
    public class Baker : Baker<ZombieSpawnerAuthoring>
    { 
        public override void Bake(ZombieSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ZombieSpawner
            {
                timerMax = authoring.timerMax,
                randomWalkingDisantanceMin = authoring.randomWalkingDisantanceMin,
                randomWalkingDisantanceMax = authoring.randomWalkingDisantanceMax,
                nearbyZombieAmountDistance = authoring.nearbyZombieAmountDistance,
                nearbyZombieAmountMax = authoring.nearbyZombieAmountMax,
            });
        }
    }
}
public struct ZombieSpawner : IComponentData {
    public float timer;
    public float timerMax;
    public float randomWalkingDisantanceMin;
    public float randomWalkingDisantanceMax;
    public int nearbyZombieAmountMax;
    public float nearbyZombieAmountDistance;
}

