# Tasks
- [x] Task 1: 修改 ZombieAuthoring.cs 的 Baker 代码，添加正确的渲染组件
  - [x] 检查士兵预制体的 Baker 实现作为参考
  - [x] 修改 ZombieAuthoring 添加 LinkedEntityGroup 和渲染相关组件
  - [x] 确保烘焙时使用正确的 TransformUsageFlags

- [x] Task 2: 验证僵尸预制体结构
  - [x] 检查僵尸预制体是否有 AnimatedMeshAuthoring 或其他渲染相关的 Authoring 组件
  - [x] 确保预制体上有必要的渲染网格和材质

- [x] Task 3: 测试僵尸生成
  - [x] 运行游戏并测试僵尸生成
  - [x] 验证僵尸是否可见
  - [x] 验证生成位置是否正确

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 2]
