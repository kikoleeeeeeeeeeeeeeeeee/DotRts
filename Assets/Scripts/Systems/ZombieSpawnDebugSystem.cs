using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
partial struct ZombieNaNTrackerBeforeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<LocalTransform> lt
            in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<Zombie>())
        {
            if (!math.isfinite(lt.ValueRO.Position.x) || !math.isfinite(lt.ValueRO.Position.y) || !math.isfinite(lt.ValueRO.Position.z))
            {
                lt.ValueRW.Position = float3.zero;
                Debug.LogWarning("[ZombieFix] Invalid Position reset (BEFORE Move)");
            }
            if (!math.isfinite(lt.ValueRO.Rotation.value.x) || !math.isfinite(lt.ValueRO.Rotation.value.y) || !math.isfinite(lt.ValueRO.Rotation.value.z) || !math.isfinite(lt.ValueRO.Rotation.value.w))
            {
                lt.ValueRW.Rotation = quaternion.identity;
                Debug.LogWarning("[ZombieFix] Invalid Rotation reset (BEFORE Move)");
            }
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
partial struct ZombiePositionFixAfterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<LocalTransform> lt
            in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<Zombie>())
        {
            if (!math.isfinite(lt.ValueRO.Position.x) || !math.isfinite(lt.ValueRO.Position.y) || !math.isfinite(lt.ValueRO.Position.z))
            {
                lt.ValueRW.Position = float3.zero;
                Debug.LogWarning("[ZombieFix] Invalid Position reset (AFTER all systems)");
            }
            if (lt.ValueRO.Scale <= 0f)
            {
                lt.ValueRW.Scale = 1f;
                Debug.LogWarning("[ZombieFix] Zero/negative Scale reset (AFTER all systems)");
            }
        }
    }
}