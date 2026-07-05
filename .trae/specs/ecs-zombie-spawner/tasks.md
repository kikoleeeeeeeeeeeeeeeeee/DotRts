# Tasks

- [ ] Task 1: 修改 ZombieSpawnerSystem 使用 ZombiePrefabMarker 动态查找预制体
  - [ ] SubTask 1.1: 实现 FindZombiePrefab 方法，查询带有 ZombiePrefabMarker 和 Prefab 组件的 Entity
  - [ ] SubTask 1.2: 在 OnUpdate 中调用 FindZombiePrefab 获取预制体 Entity
  - [ ] SubTask 1.3: 移除对 EntitiesReferences 的依赖

- [ ] Task 2: 修改 WaveManager 使用 ECS Entity 方式生成僵尸
  - [ ] SubTask 2.1: 移除 GameObject.Instantiate 方式
  - [ ] SubTask 2.2: 使用 EntityManager.Instantiate 直接生成僵尸 Entity
  - [ ] SubTask 2.3: 确保生成的僵尸带有所有必要的 ECS 组件

- [ ] Task 3: 验证僵尸生成和渲染
  - [ ] SubTask 3.1: 测试僵尸能正确生成并显示
  - [ ] SubTask 3.2: 验证僵尸带有所有必要的组件（Zombie, Unit, UnityMover 等）
  - [ ] SubTask 3.3: 验证流场组件正确启用

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 2]
