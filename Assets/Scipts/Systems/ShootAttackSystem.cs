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

        // 获取对LocalTransform组件的只读访问，用于检查和读取目标实体的LocalTransform
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRO<Target> target,
            RefRW<LocalTransform> localTransform,
            RefRW<UnityMover> unitMover,
            Entity entity)
            in SystemAPI.Query<
            RefRW<ShootAttack>,
            RefRO<Target>,
            RefRW<LocalTransform>,
            RefRW<UnityMover>>().WithDisabled<MoveOverride>().WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            // 先判断目标实体是否有LocalTransform组件
            if (!localTransformLookup.HasComponent(target.ValueRO.targetEntity))
            {
                // 如果没有，则跳过当前循环，避免访问异常
                continue;
            }

            // 读取目标实体的LocalTransform
            LocalTransform targetLocalTransform =SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
            {
                // 距离太远，子弹自动消失，移动到目标位置
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
                continue;
            }
            else
            {
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
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
            // 先判断目标实体是否有LocalTransform组件
            if (!localTransformLookup.HasComponent(target.ValueRO.targetEntity))
            {
                // 如果没有，则跳过当前循环，避免访问异常
                continue;
            }

            // 读取目标实体的LocalTransform
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
