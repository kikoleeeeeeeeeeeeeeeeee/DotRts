using Unity.Collections;
using Unity.Entities;

/// <summary>一条持久化的 Buff 记录（跨波保留）。</summary>
public struct BuffModifier
{
    public BuffType buffType;
    public BuffTargetType targetType;
    public float value;
}

/// <summary>
/// 全局 Buff 乘区。
///
/// 与 <see cref="BuffManager"/> 的「即时修改现有实体」互补：
/// BuffManager 负责让已存在的单位/建筑立刻吃到加成，这里记录累计加成，
/// 由 <see cref="GlobalBuffApplySystem"/> 在新单位/建筑出生时应用，
/// 避免「选了 Buff，但之后生产的兵和新建的建筑没有加成」。
///
/// 该单例实体不带 <see cref="RuntimeEntity"/>，因此不会被每波/清场逻辑销毁。
/// </summary>
public struct GlobalBuff : IComponentData
{
    public FixedList512Bytes<BuffModifier> modifiers;

    /// <summary>累加指定属性对指定目标类型的百分比加成（AllUnits 对任何目标都生效）。</summary>
    public float SumPercent(BuffType buffType, BuffTargetType actualTarget)
    {
        float total = 0f;
        for (int i = 0; i < modifiers.Length; i++)
        {
            BuffModifier modifier = modifiers[i];
            if (modifier.buffType != buffType) continue;
            if (modifier.targetType != actualTarget && modifier.targetType != BuffTargetType.AllUnits) continue;
            total += modifier.value;
        }
        return total;
    }
}

/// <summary>标记「刚生成、还没应用过全局 Buff」的玩家实体。</summary>
public struct PendingGlobalBuff : IComponentData
{
}
