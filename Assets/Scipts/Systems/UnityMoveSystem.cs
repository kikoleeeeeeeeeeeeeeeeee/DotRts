using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
partial struct UnityMoveSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UnitMoverJob unitMoverJob = new UnitMoverJob
        {
            detalTime = SystemAPI.Time.DeltaTime
        };
        unitMoverJob.ScheduleParallel();
        /*
        foreach ((RefRW<LocalTransform> localTransform, 
            RefRO < UnityMover > unityMover,
            RefRW<PhysicsVelocity> physicsVelocity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<UnityMover>,
                RefRW<PhysicsVelocity>>()){
       
            float3 moveDirection = unityMover.ValueRO.targetPosition - localTransform.ValueRO.Position;
            moveDirection = math.normalize(moveDirection);


            localTransform.ValueRW.Rotation =
                math.slerp(localTransform.ValueRO.Rotation, 
                quaternion.LookRotation(moveDirection,math.up()),
                SystemAPI.Time.DeltaTime*unityMover.ValueRO.rotationSpeed);

            physicsVelocity.ValueRW.Linear = moveDirection * unityMover.ValueRO.moveSpeed;

            physicsVelocity.ValueRW.Angular = float3.zero;

        }
        */
    }
}

[BurstCompile]
public partial struct UnitMoverJob : IJobEntity
{
    public float detalTime;
    public void Execute(ref LocalTransform localTransform ,ref UnityMover unityMover ,ref  PhysicsVelocity physicsVelocity)
    {
        float3 moveDirection = unityMover.targetPosition - localTransform.Position;

        float reachedTargetDistanceSq = UnityMoveSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;

        if(math.lengthsq(moveDirection) <= reachedTargetDistanceSq){
            physicsVelocity.Linear = float3.zero; 
            physicsVelocity.Angular = float3.zero;
            unityMover.isMoving = false;
            return;
        }
        unityMover.isMoving = true;

        moveDirection = math.normalize(moveDirection);


        localTransform.Rotation =
            math.slerp(localTransform.Rotation,
            quaternion.LookRotation(moveDirection, math.up()),
            detalTime * unityMover.rotationSpeed);

        physicsVelocity.Linear = moveDirection * unityMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;

    }
}

