using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ZombieTargetBaseSystem : ISystem
{
    private bool hasLoggedNoBase;
    private bool hasLoggedBaseFound;

    public void OnCreate(ref SystemState state)
    {
        hasLoggedNoBase = false;
        hasLoggedBaseFound = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        float3 basePosition = float3.zero;
        bool hasValidBase = false;
        
        if (BasePlacementSystem.Instance != null)
        {
            Vector3 bp = BasePlacementSystem.Instance.GetBasePosition();
            basePosition = new float3(bp.x, bp.y, bp.z);
            
            if (!bp.Equals(Vector3.zero) && 
                !float.IsNaN(bp.x) && 
                !float.IsNaN(bp.y) && 
                !float.IsNaN(bp.z))
            {
                hasValidBase = true;
                if (!hasLoggedBaseFound)
                {
                    Debug.Log($"[ZombieTarget] HQ found at {basePosition}. Zombies will target this position.");
                    hasLoggedBaseFound = true;
                }
            }
        }
        else
        {
            if (!hasLoggedNoBase)
            {
                Debug.LogError("[ZombieTarget] BasePlacementSystem.Instance is null! Zombies will target (0,0,0). Has the HQ been placed?");
                hasLoggedNoBase = true;
            }
        }
        
        if (!hasValidBase)
        {
            basePosition = float3.zero;
        }
        
        int updateCount = 0;
        foreach ((
            RefRO<Zombie> zombie,
            RefRW<TargetPositionPathQueued> targetPositionPathQueued,
            EnabledRefRW<TargetPositionPathQueued> targetPositionPathQueuedEnable,
            RefRO<LocalTransform> localTransform,
            Entity entity)
              in SystemAPI.Query
            <RefRO<Zombie>,
            RefRW<TargetPositionPathQueued>,
            EnabledRefRW<TargetPositionPathQueued>,
            RefRO<LocalTransform>>().WithPresent<TargetPositionPathQueued>().WithEntityAccess())
        {
            float3 currentTarget = targetPositionPathQueued.ValueRO.targetPosition;
            float distanceToNewTarget = math.length(basePosition - currentTarget);
            
            if (currentTarget.Equals(float3.zero) || distanceToNewTarget > 5f)
            {
                targetPositionPathQueued.ValueRW.targetPosition = basePosition;
                targetPositionPathQueuedEnable.ValueRW = true;
                updateCount++;
            }
        }

        if (updateCount > 0 && !hasValidBase)
        {
            Debug.LogError($"[ZombieTarget] Updated {updateCount} zombies to target (0,0,0) because no HQ exists!");
        }
    }
}