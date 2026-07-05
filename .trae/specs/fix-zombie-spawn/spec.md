# 修复僵尸生成不可见问题 Spec

## Why
用户报告生成的僵尸看不到，但兵营生成的士兵可以看到。问题在于僵尸预制体的 ECS 烘焙配置不完整，导致渲染组件没有正确设置。

## What Changes
1. 修改 ZombieAuthoring 的 Baker 代码，确保正确烘焙渲染相关的组件
2. 确保僵尸预制体实例化时带有 LinkedEntityGroup 和渲染网格信息
3. 验证生成位置是否正确设置

## Impact
- 受影响的规格：僵尸生成系统、僵尸预制体烘焙
- 受影响的代码：ZombieAuthoring.cs、ZombieUnit.prefab

## ADDED Requirements
### Requirement: 僵尸预制体正确渲染
系统应该确保僵尸预制体在实例化后能够正确渲染

#### Scenario: 成功生成可见的僵尸
- **WHEN** WaveSystem 生成僵尸
- **THEN** 僵尸应该出现在正确位置并且可见

### Requirement: 僵尸生成位置正确
系统应该确保僵尸生成在远离基地但不超出地图的位置

#### Scenario: 成功计算生成位置
- **WHEN** 计算生成位置
- **THEN** 位置应该远离基地但在地图边界内

## MODIFIED Requirements
### Requirement: ZombieAuthoring 烘焙
ZombieAuthoring 的 Baker 应该添加所有必要的渲染组件，包括：
- Prefab 标签
- ZombiePrefabMarker
- Zombie 标签
- **LinkedEntityGroup** (用于渲染)
- **LocalTransform** (用于位置)

**BREAKING**: 现有的 ZombieAuthoring 只添加了 Prefab、ZombiePrefabMarker 和 Zombie 组件，缺少渲染所需的 LinkedEntityGroup
