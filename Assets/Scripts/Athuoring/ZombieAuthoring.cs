using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class ZombieAuthoring : MonoBehaviour
{
    public class Baker: Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Zombie());
            AddComponent(entity, new Prefab());
            AddComponent(entity, new ZombiePrefabMarker());
            AddComponent(entity, new ZombieSpawnSource());

            if (authoring.GetComponent<EnemyAttackHqAuthoring>() == null)
            {
                AddComponent(entity, new EnemyAttackHQ { });
            }

            if (authoring.GetComponent<UnityMoverAuthoring>() == null)
            {
                AddComponent(entity, new UnityMover
                {
                    moveSpeed = 3f,
                    rotationSpeed = 5f,
                    targetPosition = float3.zero,
                    isMoving = false
                });
            }

            if (authoring.GetComponent<TargetPositionPathQueueAuthoring>() == null)
            {
                AddComponent(entity, new TargetPositionPathQueued());
                SetComponentEnabled<TargetPositionPathQueued>(entity, false);
            }

            if (authoring.GetComponent<FlowFieldFollowAuthoring>() == null)
            {
                AddComponent(entity, new FlowFieldFollow
                {
                    targetPosition = float3.zero,
                    lastMoveVector = float3.zero,
                    gridIndex = -1
                });
                SetComponentEnabled<FlowFieldFollow>(entity, false);
            }

            if (authoring.GetComponent<FlowFieldPathRequestAuthoring>() == null)
            {
                AddComponent(entity, new FlowFieldPathRequest());
                SetComponentEnabled<FlowFieldPathRequest>(entity, false);
            }

            if (authoring.GetComponent<TargetAuthoring>() == null)
            {
                AddComponent(entity, new Target
                {
                    targetEntity = Entity.Null
                });
            }
        }
    }
}

public struct Zombie : IComponentData
{
}

public struct ZombieSpawnSource : IComponentData
{
}