# AGENTS.md — DotRts (Unity 6 DOTS RTS)

## 交流约定
- **所有回复和思考必须使用中文**

## 项目概览
- **引擎**: Unity 6000.0.21f1c1 (Unity 6)
- **渲染**: URP 17.0.3
- **架构**: ECS/DOTS (Entities 1.2.4) + 传统 MonoBehaviour 混合模式
- **游戏**: 丧尸生存/塔防 RTS，含建造、资源采集、波次进攻

## 开发方向（重要）
- **DOTS 优先原则**：新功能应优先用 ECS 实现，尽量避免新增 MonoBehaviour
- MonoBehaviour 仅用于 UI、相机、场景管理等与 ECS 解耦的层
- 已有 MonoBehaviour 代码在重构时逐步向 ECS 迁移
- 所有游戏逻辑（战斗、移动、AI、生成等）必须放在 ECS System 中

## 关键目录
| 目录 | 内容 |
|------|------|
| `Assets/Scripts/Systems/` | ECS System（核心玩法逻辑） |
| `Assets/Scripts/Athuoring/` | ECS Authoring/Baker（MonoBehaviour→Entity 转换） |
| `Assets/Scripts/Components/` | ECS Component 定义 |
| `Assets/Scripts/MonoBehavior/` | 传统 MonoBehaviour（相机、UI、地图生成等） |
| `Assets/Scripts/Ui/` | UI 控制脚本 |
| `Assets/Scripts/Editor/` | 仅编辑器工具 |
| `Assets/Scripts/Data/` | 纯数据定义（BuffData 等） |
| `Assets/Scenes/` | 两个场景：GameScene、MainMenuScene |

## 程序集结构
- **无 `.asmdef` 文件** — 全部代码编译为单个 `Assembly-CSharp`（Editor 脚本在 `Assembly-CSharp-Editor`）
- `.csproj` / `.sln` 被 `.gitignore` 忽略但本地存在，由 Unity 自动生成

## ECS 工作流
- Authoring 组件继承 `MonoBehaviour`，通过 Baker 转换为 ECS Component
- System 使用 `SystemBase` 或 `ISystem`，按 Unity DOTS 1.x 标准编写
- 不要在 System 中直接引用场景 GameObject 或 MonoBehaviour — 通过 Baker 提前转换

## 开发命令
- **统计代码行数**: 运行 `.\sum.ps1`（统计 .cs / .shader / .hlsl / .cginc / .uxml / .uss）
- **调试**: 通过 `F5` 附加 Unity 编辑器（使用 `.vscode/launch.json` 中的配置）
- **测试**: 虽然安装了 `com.unity.test-framework`，但**项目中尚无实际测试**，需要新建

## 注意事项
- `.vscode/settings.json` 在文件浏览器中隐藏了 `.asset`、`.prefab`、`.unity`、`.meta` 等文件 — **这些文件实际存在且很重要**，不要误删
- `.trae/specs/` 有最近的设计文档，涉及丧尸生成系统的 ECS 重构
- 无 CI/CD、无 Linter、无 `.editorconfig`
- 项目包含移动端支持（Android 计费、独立 Renderer 配置）
- ScriptableObject 配置位于 `Assets/ScriptsableObjects/`（建筑、资源、单位、波次等）
