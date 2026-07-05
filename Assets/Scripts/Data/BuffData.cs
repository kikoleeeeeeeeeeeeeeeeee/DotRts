using System;
using UnityEngine;

[Serializable]
public struct BuffData
{
    public string buffName;
    public string description;
    public BuffType buffType;
    public BuffTargetType targetType;
    public float value;
    public Sprite icon;
}

public enum BuffType
{
    AttackDamage,       // 攻击力
    AttackSpeed,        // 攻击速度
    Health,             // 生命值
    MoveSpeed,          // 移动速度
    AttackRange,        // 攻击范围
    ProductionCount,    // 生产数量（兵营）
    ResourceGain,       // 资源获取
    DirectResource      // 直接给予资源
}

public enum BuffTargetType
{
    AllUnits,           // 所有单位
    Soldier,            // 士兵
    HQ,                 // 基地
    Barracks,           // 兵营
    Turret,             // 炮台
    ResourceBuilding,   // 资源建筑
    Resource            // 资源
}
