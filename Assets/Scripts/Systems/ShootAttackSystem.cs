using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ShootAttackSystem : ISystem
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

        // ��ȡ��LocalTransform�����ֻ�����ʣ����ڼ��Ͷ�ȡĿ��ʵ���LocalTransform
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRO<Target> target,
            RefRW<LocalTransform> localTransform,
            RefRW<TargetPositionPathQueued> targetPositionPathQueued,
            EnabledRefRW < TargetPositionPathQueued > targetPositionPathQueuedEnable,
            RefRW <UnityMover> unitMover,
            Entity entity)
            in SystemAPI.Query<
            RefRW<ShootAttack>,
            RefRO<Target>,
            RefRW<LocalTransform>,
            RefRW<TargetPositionPathQueued>,
            EnabledRefRW<TargetPositionPathQueued>,
            RefRW<UnityMover>>().WithDisabled<MoveOverride>().WithPresent<TargetPositionPathQueued>().WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            // ���ж�Ŀ��ʵ���Ƿ���LocalTransform���
            if (!localTransformLookup.HasComponent(target.ValueRO.targetEntity))
            {
                // ���û�У���������ǰѭ������������쳣
                continue;
            }

            // ��ȡĿ��ʵ���LocalTransform
            LocalTransform targetLocalTransform =SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
            {
                // Too far, move closer
                targetPositionPathQueued.ValueRW.targetPosition = targetLocalTransform.Position;
                targetPositionPathQueuedEnable.ValueRW = true;
                continue;
            }
            else
            {
                // Close enough, stop moving and attack
                targetPositionPathQueued.ValueRW.targetPosition = localTransform.ValueRO.Position;
                targetPositionPathQueuedEnable.ValueRW = true;
            }

            float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
            aimDirection = math.normalize(aimDirection);

            quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());

            localTransform.ValueRW.Rotation =
                math.slerp(localTransform.ValueRO.Rotation, targetRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

          
        }


        foreach ((
           RefRW<ShootAttack> shootAttack,
           RefRO<Target> target,
           RefRW<LocalTransform> localTransform,
           Entity entity)
           in SystemAPI.Query<
           RefRW<ShootAttack>,
           RefRO<Target>,
           RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }
            // ���ж�Ŀ��ʵ���Ƿ���LocalTransform���
            if (!localTransformLookup.HasComponent(target.ValueRO.targetEntity))
            {
                // ���û�У���������ǰѭ������������쳣
                continue;
            }

            // ��ȡĿ��ʵ���LocalTransform
            LocalTransform targetLocalTransform = localTransformLookup[target.ValueRO.targetEntity];

            shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            if (shootAttack.ValueRO.timer > 0f)
            {
                continue;
            }

            shootAttack.ValueRW.timer = shootAttack.ValueRO.timeMax;

            if (SystemAPI.HasComponent<TargetOverride>(target.ValueRO.targetEntity))
            {
                RefRW<TargetOverride> enemyTargetOverride = SystemAPI.GetComponentRW<TargetOverride>(target.ValueRO.targetEntity);
                if (enemyTargetOverride.ValueRO.targetEntity == Entity.Null)
                {
                    enemyTargetOverride.ValueRW.targetEntity = entity;
                }
            }

            if(math.distance(localTransform.ValueRO.Position,targetLocalTransform.Position)>shootAttack.ValueRO.attackDistance)
            {
                continue;
            }

            if (SystemAPI.HasComponent<MoveOverride>(entity) && SystemAPI.IsComponentEnabled<MoveOverride>(entity))
            {
                continue;
            }

            Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
            state.EntityManager.AddComponent<RuntimeEntity>(bulletEntity);
            float3 bulletSpawnWorldPosition = localTransform.ValueRO.TransformPoint(shootAttack.ValueRO.bulletSpawnLocalPosition);
            SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));

            RefRW<Bullet> bulletBullet = SystemAPI.GetComponentRW<Bullet>(bulletEntity);
            bulletBullet.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;

            RefRW<Target> bulletTarget = SystemAPI.GetComponentRW<Target>(bulletEntity);
            bulletTarget.ValueRW.targetEntity = target.ValueRO.targetEntity;

            shootAttack.ValueRW.onShoot.isTriggered = true;
            shootAttack.ValueRW.onShoot.shootFromPosition = bulletSpawnWorldPosition;
        }
    }
}
