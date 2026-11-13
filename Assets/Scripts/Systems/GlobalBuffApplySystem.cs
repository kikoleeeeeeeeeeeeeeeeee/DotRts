using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 给「刚生成的玩家实体」应用持久化全局 Buff。
///
/// 生成点（兵营产兵、建筑完工、放置 HQ）只负责挂 <see cref="PendingGlobalBuff"/> 标记，
/// 具体加成在这里统一落地：这样 Burst 生成系统不需要访问 managed 数据，
/// 也不需要在每个生成点重复写一遍加成公式。
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct GlobalBuffApplySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GlobalBuff>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<PendingGlobalBuff>().Build();
        int count = query.CalculateEntityCount();
        if (count == 0) return;

        EntityManager entityManager = state.EntityManager;
        GlobalBuff globalBuff = SystemAPI.GetSingleton<GlobalBuff>();

        NativeArray<Entity> pending = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < pending.Length; i++)
        {
            Entity entity = pending[i];
            if (!entityManager.Exists(entity)) continue;

            Apply(entityManager, entity, globalBuff, ResolveTarget(entityManager, entity));
            entityManager.RemoveComponent<PendingGlobalBuff>(entity);
        }
        pending.Dispose();
    }

    /// <summary>根据实体自身携带的组件判断它属于哪类 Buff 目标。</summary>
    private static BuffTargetType ResolveTarget(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<BuildingHQ>(entity)) return BuffTargetType.HQ;
        if (entityManager.HasComponent<BuildingBarracks>(entity)) return BuffTargetType.Barracks;
        if (entityManager.HasComponent<BuildingHarvester>(entity)) return BuffTargetType.ResourceBuilding;
        if (entityManager.HasComponent<Unit>(entity)) return BuffTargetType.Soldier;

        // 没有专属标记的进攻型建筑（炮台等）走 Turret 分支。
        if (entityManager.HasComponent<ShootAttack>(entity)) return BuffTargetType.Turret;

        return BuffTargetType.AllUnits;
    }

    private static void Apply(EntityManager entityManager, Entity entity, GlobalBuff globalBuff, BuffTargetType target)
    {
        // 约定：AllUnits 指「玩家方全部实体」。SumPercent 会把 AllUnits 的加成算进任何目标类型，
        // 所以这里不需要按目标类型提前返回——不带专属标记的实体按 AllUnits 处理即可。
        float healthPercent = globalBuff.SumPercent(BuffType.Health, target);
        if (healthPercent != 0f && entityManager.HasComponent<Health>(entity))
        {
            Health health = entityManager.GetComponentData<Health>(entity);
            int increase = (int)math.round(health.healthAmountMax * (healthPercent / 100f));
            if (increase != 0)
            {
                health.healthAmountMax += increase;
                health.healthAmount += increase;
                health.onHealthChanged = true;
                entityManager.SetComponentData(entity, health);
            }
        }

        float damagePercent = globalBuff.SumPercent(BuffType.AttackDamage, target);
        if (damagePercent != 0f)
        {
            if (entityManager.HasComponent<MeleeAttack>(entity))
            {
                MeleeAttack meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                meleeAttack.damageAmount = math.max(1, (int)math.round(meleeAttack.damageAmount * (1f + damagePercent / 100f)));
                entityManager.SetComponentData(entity, meleeAttack);
            }

            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                ShootAttack shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.damageAmount = math.max(1, (int)math.round(shootAttack.damageAmount * (1f + damagePercent / 100f)));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }

        float attackSpeedPercent = globalBuff.SumPercent(BuffType.AttackSpeed, target);
        if (attackSpeedPercent != 0f)
        {
            float factor = math.max(0.1f, 1f - attackSpeedPercent / 100f);

            if (entityManager.HasComponent<MeleeAttack>(entity))
            {
                MeleeAttack meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                meleeAttack.timerMax = math.max(0.1f, meleeAttack.timerMax * factor);
                entityManager.SetComponentData(entity, meleeAttack);
            }

            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                ShootAttack shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.timeMax = math.max(0.1f, shootAttack.timeMax * factor);
                entityManager.SetComponentData(entity, shootAttack);
            }
        }

        float attackRangePercent = globalBuff.SumPercent(BuffType.AttackRange, target);
        if (attackRangePercent != 0f && entityManager.HasComponent<ShootAttack>(entity))
        {
            ShootAttack shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
            shootAttack.attackDistance *= 1f + attackRangePercent / 100f;
            entityManager.SetComponentData(entity, shootAttack);
        }

        float moveSpeedPercent = globalBuff.SumPercent(BuffType.MoveSpeed, target);
        if (moveSpeedPercent != 0f && entityManager.HasComponent<UnityMover>(entity))
        {
            UnityMover mover = entityManager.GetComponentData<UnityMover>(entity);
            mover.moveSpeed *= 1f + moveSpeedPercent / 100f;
            entityManager.SetComponentData(entity, mover);
        }

        float productionPercent = globalBuff.SumPercent(BuffType.ProductionCount, target);
        if (productionPercent != 0f && entityManager.HasComponent<BuildingBarracks>(entity))
        {
            BuildingBarracks barracks = entityManager.GetComponentData<BuildingBarracks>(entity);
            barracks.productionCountBonusPercent += productionPercent;
            entityManager.SetComponentData(entity, barracks);
        }
    }
}
