using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    public event EventHandler OnBuffSelectionStarted;
    public event EventHandler<BuffData> OnBuffSelected;

    [Header("Buff Settings")]
    [SerializeField] private BuffData[] availableBuffs;
    [SerializeField] private int buffOptionsCount = 3;
    
    [Header("UI References")]
    [SerializeField] private BuffSelectionUI buffSelectionUI;

    private List<BuffData> currentBuffOptions = new List<BuffData>();
    private List<BuffData> selectedBuffs = new List<BuffData>();
    private bool isSelectingBuff = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartBuffSelection()
    {
        currentBuffOptions.Clear();
        
        if (availableBuffs == null || availableBuffs.Length == 0)
        {
            // 添加默认 Buff
            currentBuffOptions.Add(new BuffData { buffName = "攻击力提升", description = "提高所有单位的攻击力", buffType = BuffType.AttackDamage, targetType = BuffTargetType.AllUnits, value = 20 });
            currentBuffOptions.Add(new BuffData { buffName = "生命值提升", description = "提高所有单位的生命值", buffType = BuffType.Health, targetType = BuffTargetType.AllUnits, value = 20 });
            currentBuffOptions.Add(new BuffData { buffName = "移动速度提升", description = "提高所有单位的移动速度", buffType = BuffType.MoveSpeed, targetType = BuffTargetType.AllUnits, value = 20 });
            currentBuffOptions.Add(new BuffData { buffName = "HQ生命值提升", description = "提高HQ的生命值", buffType = BuffType.Health, targetType = BuffTargetType.HQ, value = 30 });
            currentBuffOptions.Add(new BuffData { buffName = "HQ攻击力提升", description = "提高HQ的攻击力", buffType = BuffType.AttackDamage, targetType = BuffTargetType.HQ, value = 30 });
            currentBuffOptions.Add(new BuffData { buffName = "HQ攻击范围提升", description = "提高HQ的攻击范围", buffType = BuffType.AttackRange, targetType = BuffTargetType.HQ, value = 25 });
        }
        else
        {
            HashSet<int> usedIndices = new HashSet<int>();
            int optionsToSelect = Mathf.Min(buffOptionsCount, availableBuffs.Length);
            while (currentBuffOptions.Count < optionsToSelect)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableBuffs.Length);
                if (!usedIndices.Contains(randomIndex))
                {
                    usedIndices.Add(randomIndex);
                    currentBuffOptions.Add(availableBuffs[randomIndex]);
                }
            }
        }

        isSelectingBuff = true;
        OnBuffSelectionStarted?.Invoke(this, EventArgs.Empty);
        
        // 直接调用 BuffSelectionUI 的 ShowBuffSelectionUI 方法
        if (buffSelectionUI != null)
        {
            buffSelectionUI.ShowBuffSelectionUI();
        }
        else
        {
            // 尝试在场景中查找 BuffSelectionUI
            BuffSelectionUI foundUI = FindObjectOfType<BuffSelectionUI>();
            if (foundUI != null)
            {
                buffSelectionUI = foundUI;
                buffSelectionUI.ShowBuffSelectionUI();
            }
        }
    }

    public void SelectBuff(int optionIndex)
    {
        if (!isSelectingBuff || optionIndex < 0 || optionIndex >= currentBuffOptions.Count)
        {
            return;
        }

        BuffData selectedBuff = currentBuffOptions[optionIndex];
        selectedBuffs.Add(selectedBuff);
        ApplyBuff(selectedBuff);
        
        isSelectingBuff = false;
        OnBuffSelected?.Invoke(this, selectedBuff);
    }

    private void ApplyBuff(BuffData buff)
    {
        // 先记录到全局乘区：之后生产的单位/新建的建筑出生时会自动应用。
        RecordGlobalBuff(buff);

        switch (buff.targetType)
        {
            case BuffTargetType.AllUnits:
                ApplyBuffToAllUnits(buff);
                break;
            case BuffTargetType.Soldier:
                ApplyBuffToSoldiers(buff);
                break;

            case BuffTargetType.HQ:
                ApplyBuffToHQ(buff);
                break;
            case BuffTargetType.Barracks:
                ApplyBuffToBarracks(buff);
                break;
            case BuffTargetType.Turret:
                ApplyBuffToTurrets(buff);
                break;
            case BuffTargetType.ResourceBuilding:
                ApplyBuffToResourceBuildings(buff);
                break;
            case BuffTargetType.Resource:
                ApplyBuffToResource(buff);
                break;
        }
    }

    private void ApplyBuffToAllUnits(BuffData buff)
    {
        // 约定：AllUnits 指「玩家方全部实体」——单位、HQ、建筑都算，但绝不能加强僵尸。
        // 各 Apply*BuffToUnits 内部都会跳过带 Zombie 的实体。
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        switch (buff.buffType)
        {
            case BuffType.AttackDamage:
                ApplyAttackDamageBuffToUnits(entityManager, buff.value, true);
                break;
            case BuffType.AttackSpeed:
                ApplyAttackSpeedBuffToUnits(entityManager, buff.value, true);
                break;
            case BuffType.Health:
                ApplyHealthBuffToUnits(entityManager, buff.value, true);
                break;
            case BuffType.MoveSpeed:
                ApplyMoveSpeedBuffToUnits(entityManager, buff.value, true);
                break;
            case BuffType.AttackRange:
                ApplyAttackRangeBuffToUnits(entityManager, buff.value, true);
                break;
        }

    }

    private void ApplyBuffToSoldiers(BuffData buff)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        switch (buff.buffType)
        {
            case BuffType.AttackDamage:
                ApplyAttackDamageBuffToSoldiers(entityManager, buff.value);
                break;
            case BuffType.AttackSpeed:
                ApplyAttackSpeedBuffToSoldiers(entityManager, buff.value);
                break;
            case BuffType.Health:
                ApplyHealthBuffToSoldiers(entityManager, buff.value);
                break;
            case BuffType.MoveSpeed:
                ApplyMoveSpeedBuffToSoldiers(entityManager, buff.value);
                break;
            case BuffType.AttackRange:
                ApplyAttackRangeBuffToSoldiers(entityManager, buff.value);
                break;
        }

    }



    private void ApplyBuffToHQ(BuffData buff)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        switch (buff.buffType)
        {
            case BuffType.Health:
                ApplyHealthBuffToHQ(entityManager, buff.value);
                break;
            case BuffType.AttackDamage:
                ApplyAttackDamageBuffToHQ(entityManager, buff.value);
                break;
            case BuffType.AttackSpeed:
                ApplyAttackSpeedBuffToHQ(entityManager, buff.value);
                break;
            case BuffType.AttackRange:
                ApplyAttackRangeBuffToHQ(entityManager, buff.value);
                break;
        }

    }

    private void ApplyBuffToBarracks(BuffData buff)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (buff.buffType == BuffType.ProductionCount)
        {
            ApplyProductionCountBuffToBarracks(entityManager, buff.value);
        }

    }

    private void ApplyBuffToTurrets(BuffData buff)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        switch (buff.buffType)
        {
            case BuffType.AttackDamage:
                ApplyAttackDamageBuffToTurrets(entityManager, buff.value);
                break;
            case BuffType.AttackSpeed:
                ApplyAttackSpeedBuffToTurrets(entityManager, buff.value);
                break;
            case BuffType.Health:
                ApplyHealthBuffToTurrets(entityManager, buff.value);
                break;
            case BuffType.AttackRange:
                ApplyAttackRangeBuffToTurrets(entityManager, buff.value);
                break;
        }

    }

    private void ApplyBuffToResourceBuildings(BuffData buff)
    {
        if (buff.buffType == BuffType.ResourceGain)
        {
            ApplyResourceGainMultiplier(buff.value);
        }
    }

    private void ApplyBuffToResource(BuffData buff)
    {
        if (buff.buffType == BuffType.DirectResource)
        {
            GiveDirectResource(buff.value);
        }
        else if (buff.buffType == BuffType.ResourceGain)
        {
            ApplyResourceGainMultiplier(buff.value);
        }
    }

    private void ApplyResourceGainMultiplier(float percent)
    {
        if (ResrouceManager.Instance == null) return;

        ResrouceManager.Instance.GainMultiplier += percent / 100f;
        Debug.Log($"[BuffManager] 资源获取倍率提升到 x{ResrouceManager.Instance.GainMultiplier:F2}");
    }

    // 辅助方法：应用攻击伤害 buff 到所有单位
    private void ApplyAttackDamageBuffToUnits(EntityManager entityManager, float value, bool includeSoldiers)
    {
        // 应用到近战单位
        var meleeAttackQuery = entityManager.CreateEntityQuery(typeof(MeleeAttack));
        var meleeAttackEntities = meleeAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in meleeAttackEntities)
        {
            // AllUnits 只作用于玩家方，不能把僵尸一起加强。
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<MeleeAttack>(entity))
            {
                var meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                meleeAttack.damageAmount = (int)(meleeAttack.damageAmount * (1 + value / 100));
                entityManager.SetComponentData(entity, meleeAttack);
            }
        }
        meleeAttackEntities.Dispose();

        // 应用到远程单位
        var shootAttackQuery = entityManager.CreateEntityQuery(typeof(ShootAttack));
        var shootAttackEntities = shootAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in shootAttackEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.damageAmount = (int)(shootAttack.damageAmount * (1 + value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        shootAttackEntities.Dispose();
    }

    // 应用攻击速度 buff 到所有单位
    private void ApplyAttackSpeedBuffToUnits(EntityManager entityManager, float value, bool includeSoldiers)
    {
        // 应用到近战单位
        var meleeAttackQuery = entityManager.CreateEntityQuery(typeof(MeleeAttack));
        var meleeAttackEntities = meleeAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in meleeAttackEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<MeleeAttack>(entity))
            {
                var meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                meleeAttack.timerMax = Mathf.Max(0.1f, meleeAttack.timerMax * (1 - value / 100));
                entityManager.SetComponentData(entity, meleeAttack);
            }
        }
        meleeAttackEntities.Dispose();

        // 应用到远程单位
        var shootAttackQuery = entityManager.CreateEntityQuery(typeof(ShootAttack));
        var shootAttackEntities = shootAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in shootAttackEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.timeMax = Mathf.Max(0.1f, shootAttack.timeMax * (1 - value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        shootAttackEntities.Dispose();
    }

    // 应用生命值 buff 到所有单位
    private void ApplyHealthBuffToUnits(EntityManager entityManager, float value, bool includeSoldiers)
    {
        var healthQuery = entityManager.CreateEntityQuery(typeof(Health));
        var healthEntities = healthQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in healthEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<Health>(entity))
            {
                var health = entityManager.GetComponentData<Health>(entity);
                int healthIncrease = (int)(health.healthAmountMax * (value / 100));
                health.healthAmountMax += healthIncrease;
                health.healthAmount += healthIncrease;
                health.onHealthChanged = true;
                entityManager.SetComponentData(entity, health);
            }
        }
        healthEntities.Dispose();
    }

    // 应用移动速度 buff 到所有单位
    private void ApplyMoveSpeedBuffToUnits(EntityManager entityManager, float value, bool includeSoldiers)
    {
        var moverQuery = entityManager.CreateEntityQuery(typeof(UnityMover));
        var moverEntities = moverQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in moverEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<UnityMover>(entity))
            {
                var mover = entityManager.GetComponentData<UnityMover>(entity);
                mover.moveSpeed *= (1 + value / 100);
                entityManager.SetComponentData(entity, mover);
            }
        }
        moverEntities.Dispose();
    }

    // 应用攻击范围 buff 到所有单位
    private void ApplyAttackRangeBuffToUnits(EntityManager entityManager, float value, bool includeSoldiers)
    {
        var shootAttackQuery = entityManager.CreateEntityQuery(typeof(ShootAttack));
        var shootAttackEntities = shootAttackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in shootAttackEntities)
        {
            if (entityManager.HasComponent<Zombie>(entity)) continue;
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.attackDistance *= (1 + value / 100);
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        shootAttackEntities.Dispose();
    }

    // 应用攻击伤害 buff 到士兵
    private void ApplyAttackDamageBuffToSoldiers(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(UnitTypeHolder));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);
            if (unitTypeHolder.unitType == UnitTypeSo.UnitType.Soldier || unitTypeHolder.unitType == UnitTypeSo.UnitType.Scout)
            {
                if (entityManager.HasComponent<MeleeAttack>(entity))
                {
                    var meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                    meleeAttack.damageAmount = (int)(meleeAttack.damageAmount * (1 + value / 100));
                    entityManager.SetComponentData(entity, meleeAttack);
                }
                if (entityManager.HasComponent<ShootAttack>(entity))
                {
                    var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                    shootAttack.damageAmount = (int)(shootAttack.damageAmount * (1 + value / 100));
                    entityManager.SetComponentData(entity, shootAttack);
                }
            }
        }
        entities.Dispose();
    }

    // 应用攻击速度 buff 到士兵
    private void ApplyAttackSpeedBuffToSoldiers(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(UnitTypeHolder));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);
            if (unitTypeHolder.unitType == UnitTypeSo.UnitType.Soldier || unitTypeHolder.unitType == UnitTypeSo.UnitType.Scout)
            {
                if (entityManager.HasComponent<MeleeAttack>(entity))
                {
                    var meleeAttack = entityManager.GetComponentData<MeleeAttack>(entity);
                    meleeAttack.timerMax = Mathf.Max(0.1f, meleeAttack.timerMax * (1 - value / 100));
                    entityManager.SetComponentData(entity, meleeAttack);
                }
                if (entityManager.HasComponent<ShootAttack>(entity))
                {
                    var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                    shootAttack.timeMax = Mathf.Max(0.1f, shootAttack.timeMax * (1 - value / 100));
                    entityManager.SetComponentData(entity, shootAttack);
                }
            }
        }
        entities.Dispose();
    }

    // 应用生命值 buff 到士兵
    private void ApplyHealthBuffToSoldiers(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(UnitTypeHolder), typeof(Health));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);
            if (unitTypeHolder.unitType == UnitTypeSo.UnitType.Soldier || unitTypeHolder.unitType == UnitTypeSo.UnitType.Scout)
            {
                if (entityManager.HasComponent<Health>(entity))
                {
                    var health = entityManager.GetComponentData<Health>(entity);
                    int healthIncrease = (int)(health.healthAmountMax * (value / 100));
                    health.healthAmountMax += healthIncrease;
                    health.healthAmount += healthIncrease;
                    health.onHealthChanged = true;
                    entityManager.SetComponentData(entity, health);
                }
            }
        }
        entities.Dispose();
    }

    // 应用移动速度 buff 到士兵
    private void ApplyMoveSpeedBuffToSoldiers(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(UnitTypeHolder), typeof(UnityMover));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);
            if (unitTypeHolder.unitType == UnitTypeSo.UnitType.Soldier || unitTypeHolder.unitType == UnitTypeSo.UnitType.Scout)
            {
                if (entityManager.HasComponent<UnityMover>(entity))
                {
                    var mover = entityManager.GetComponentData<UnityMover>(entity);
                    mover.moveSpeed *= (1 + value / 100);
                    entityManager.SetComponentData(entity, mover);
                }
            }
        }
        entities.Dispose();
    }

    // 应用攻击范围 buff 到士兵
    private void ApplyAttackRangeBuffToSoldiers(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(UnitTypeHolder), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);
            if (unitTypeHolder.unitType == UnitTypeSo.UnitType.Soldier || unitTypeHolder.unitType == UnitTypeSo.UnitType.Scout)
            {
                if (entityManager.HasComponent<ShootAttack>(entity))
                {
                    var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                    shootAttack.attackDistance *= (1 + value / 100);
                    entityManager.SetComponentData(entity, shootAttack);
                }
            }
        }
        entities.Dispose();
    }

    // 应用生命值 buff 到 HQ
    private void ApplyHealthBuffToHQ(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(Health));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<Health>(entity))
            {
                var health = entityManager.GetComponentData<Health>(entity);
                int healthIncrease = (int)(health.healthAmountMax * (value / 100));
                health.healthAmountMax += healthIncrease;
                health.healthAmount += healthIncrease;
                health.onHealthChanged = true;
                entityManager.SetComponentData(entity, health);
            }
        }
        entities.Dispose();
    }

    // 应用攻击伤害 buff 到 HQ
    private void ApplyAttackDamageBuffToHQ(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.damageAmount = (int)(shootAttack.damageAmount * (1 + value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 应用攻击速度 buff 到 HQ
    private void ApplyAttackSpeedBuffToHQ(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.timeMax = Mathf.Max(0.1f, shootAttack.timeMax * (1 - value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 应用攻击范围 buff 到 HQ
    private void ApplyAttackRangeBuffToHQ(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.attackDistance *= (1 + value / 100);
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 应用生产数量 buff 到兵营
    private void ApplyProductionCountBuffToBarracks(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingBarracks));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var barracks = entityManager.GetComponentData<BuildingBarracks>(entity);
            barracks.productionCountBonusPercent += value;
            entityManager.SetComponentData(entity, barracks);
        }
        entities.Dispose();

        Debug.Log($"[BuffManager] 兵营生产数量 +{value}%，之后每次生产按百分比多产（每满 100% 多出 1 个）。");
    }

    // 应用攻击伤害 buff 到炮台
    private void ApplyAttackDamageBuffToTurrets(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingTypeSOHolder), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.damageAmount = (int)(shootAttack.damageAmount * (1 + value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 应用攻击速度 buff 到炮台
    private void ApplyAttackSpeedBuffToTurrets(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingTypeSOHolder), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.timeMax = Mathf.Max(0.1f, shootAttack.timeMax * (1 - value / 100));
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 应用生命值 buff 到炮台
    private void ApplyHealthBuffToTurrets(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingTypeSOHolder), typeof(Health));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<Health>(entity))
            {
                var health = entityManager.GetComponentData<Health>(entity);
                int healthIncrease = (int)(health.healthAmountMax * (value / 100));
                health.healthAmountMax += healthIncrease;
                health.healthAmount += healthIncrease;
                health.onHealthChanged = true;
                entityManager.SetComponentData(entity, health);
            }
        }
        entities.Dispose();
    }

    // 应用攻击范围 buff 到炮台
    private void ApplyAttackRangeBuffToTurrets(EntityManager entityManager, float value)
    {
        var query = entityManager.CreateEntityQuery(typeof(BuildingTypeSOHolder), typeof(ShootAttack));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<ShootAttack>(entity))
            {
                var shootAttack = entityManager.GetComponentData<ShootAttack>(entity);
                shootAttack.attackDistance *= (1 + value / 100);
                entityManager.SetComponentData(entity, shootAttack);
            }
        }
        entities.Dispose();
    }

    // 直接给予资源
    private void GiveDirectResource(float value)
    {
        // 增加三种资源，数量对应 buff 的数值
        ResrouceManager.Instance.AddResourceAmount(ResourceTypeSo.ResourceType.Iron, (int)value);
        ResrouceManager.Instance.AddResourceAmount(ResourceTypeSo.ResourceType.Gold, (int)value);
        ResrouceManager.Instance.AddResourceAmount(ResourceTypeSo.ResourceType.Oil, (int)value);
    }

    public List<BuffData> GetCurrentBuffOptions()
    {
        return new List<BuffData>(currentBuffOptions);
    }

    public bool IsSelectingBuff()
    {
        return isSelectingBuff;
    }

    public void ResetBuffs()
    {
        selectedBuffs.Clear();
        currentBuffOptions.Clear();
        isSelectingBuff = false;

        if (ResrouceManager.Instance != null)
        {
            ResrouceManager.Instance.GainMultiplier = 1f;
        }

        ClearGlobalBuffs();
    }

    /// <summary>把一次 Buff 选择记录到全局乘区（跨波、对新单位生效）。</summary>
    private void RecordGlobalBuff(BuffData buff)
    {
        if (!TryGetGlobalBuffEntity(out EntityManager entityManager, out Entity globalBuffEntity)) return;

        GlobalBuff globalBuff = entityManager.GetComponentData<GlobalBuff>(globalBuffEntity);
        if (globalBuff.modifiers.Length >= globalBuff.modifiers.Capacity) return;

        globalBuff.modifiers.Add(new BuffModifier
        {
            buffType = buff.buffType,
            targetType = buff.targetType,
            value = buff.value
        });
        entityManager.SetComponentData(globalBuffEntity, globalBuff);
    }

    /// <summary>清空全局乘区（新开一局时调用）。</summary>
    public void ClearGlobalBuffs()
    {
        if (!TryGetGlobalBuffEntity(out EntityManager entityManager, out Entity globalBuffEntity)) return;

        GlobalBuff globalBuff = entityManager.GetComponentData<GlobalBuff>(globalBuffEntity);
        globalBuff.modifiers.Clear();
        entityManager.SetComponentData(globalBuffEntity, globalBuff);
    }

    private bool TryGetGlobalBuffEntity(out EntityManager entityManager, out Entity globalBuffEntity)
    {
        entityManager = default;
        globalBuffEntity = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;

        entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(GlobalBuff));
        bool found = query.CalculateEntityCount() > 0;
        if (found)
        {
            globalBuffEntity = query.GetSingletonEntity();
        }
        query.Dispose();
        return found;
    }

    public List<BuffData> GetSelectedBuffs()
    {
        return new List<BuffData>(selectedBuffs);
    }
}
