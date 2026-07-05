# ECS 僵尸生成系统重构 Spec

## Why
当前 WaveManager 使用 GameObject.Instantiate 方式生成僵尸，与 ECS 架构不兼容。需要修改为使用 ECS Entity 实例化方式，确保生成的僵尸具有完整的 ECS 组件和渲染功能。

## What Changes
- 修改 WaveManager 使用 ECS Entity 方式生成僵尸
- 修改 ZombieSpawnerSystem 使用 ZombiePrefabMarker 动态查找预制体
- 移除对 EntitiesReferences.zombiePrefabEntity 的依赖（避免子场景 Entity 失效问题）
- 确保生成的僵尸带有完整的 ECS 组件和渲染功能

## Impact
- **Affected specs**: ECS 生成系统、僵尸预制体系统
- **Affected code**: 
  - `WaveManager.cs` - 僵尸生成逻辑
  - `ZombieSpawnerSystem.cs` - ECS 僵尸生成系统
  - `ZombiePrefabMarker.cs` - 僵尸预制体标记组件

## ADDED Requirements

### Requirement: ECS 僵尸生成
The system SHALL provide a way to spawn zombies using ECS Entity instantiation with proper rendering.

#### Scenario: WaveManager 生成僵尸
- **WHEN** WaveManager 需要生成僵尸
- **THEN** 系统应该使用 EntityManager.Instantiate 创建 ECS Entity
- **THEN** 生成的僵尸应该带有 Mesh 渲染组件
- **THEN** 生成的僵尸应该带有所有必要的 ECS 组件（Zombie, Unit, UnityMover 等）

### Requirement: 动态预制体查找
The system SHALL dynamically find zombie prefab Entity at runtime to avoid stale Entity references from subscenes.

#### Scenario: ZombieSpawnerSystem 查找预制体
- **WHEN** ZombieSpawnerSystem 需要生成僵尸
- **THEN** 系统应该通过查询 ZombiePrefabMarker + Prefab 组件找到预制体 Entity
- **THEN** 查找应该在每次生成时执行，避免缓存失效的 Entity

## MODIFIED Requirements

### Requirement: WaveManager 僵尸生成逻辑
WaveManager 应该使用 ECS Event 系统触发生成，而不是直接实例化 GameObject。

**Before**: WaveManager 使用 GameObject.Instantiate 或创建 SpawnZombieEvent
**After**: WaveManager 使用 EntityManager.Instantiate 直接生成带有完整组件的僵尸 Entity

### Requirement: ZombieSpawnerSystem 预制体引用
ZombieSpawnerSystem 不应该依赖 EntitiesReferences 中的预制体 Entity 引用。

**Before**: 使用 EntitiesReferences.zombiePrefabEntity（会在子场景关闭后失效）
**After**: 使用 ZombiePrefabMarker 动态查找预制体 Entity

## REMOVED Requirements

### Requirement: EntitiesReferences.zombiePrefabEntity 用于僵尸生成
**Reason**: 子场景中的 Entity 引用会在子场景关闭后失效
**Migration**: 使用 ZombiePrefabMarker 动态查找
